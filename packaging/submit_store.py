"""Submit a tested x64 MSIX update using the Microsoft Store submission API.

No third-party dependencies. See docs/store-deployment.md for one-time setup.
"""

import argparse
import copy
import json
import os
from pathlib import Path
import re
import sys
import tempfile
import time
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode, urlsplit
from urllib.request import Request, urlopen
from uuid import UUID
import xml.etree.ElementTree as ET
from zipfile import BadZipFile, ZIP_STORED, ZipFile


API = "https://manage.devcenter.microsoft.com"
ACCEPTED = {"PreProcessing", "Certification", "PendingPublication", "Publishing", "Published", "Release"}
FAILED = {"Canceled", "CommitFailed", "PreProcessingFailed", "CertificationFailed", "PublishFailed", "ReleaseFailed"}


class DeploymentError(Exception):
    pass


def require_master_push(env):
    if env.get("GITHUB_EVENT_NAME") != "push" or env.get("GITHUB_REF") != "refs/heads/master":
        raise DeploymentError("Store submission is only allowed for a push to master.")


def configuration(env):
    required = ("STORE_TENANT_ID", "STORE_CLIENT_ID", "STORE_CLIENT_SECRET", "STORE_APP_ID", "STORE_IDENTITY_JSON")
    missing = [key for key in required if not env.get(key, "").strip()]
    if missing:
        raise DeploymentError("Configure GitHub variables/secrets: " + ", ".join(missing))
    for key in ("STORE_TENANT_ID", "STORE_CLIENT_ID"):
        try:
            UUID(env[key])
        except ValueError:
            raise DeploymentError(f"{key} must be a tenant/client GUID.") from None
    if not re.fullmatch(r"[A-Za-z0-9]+", env["STORE_APP_ID"]):
        raise DeploymentError("STORE_APP_ID must be the Store product ID.")
    try:
        identity = json.loads(env["STORE_IDENTITY_JSON"])
    except ValueError:
        raise DeploymentError("STORE_IDENTITY_JSON must contain valid JSON.") from None
    fields = ("Name", "Publisher", "PublisherDisplayName", "DisplayName")
    if not isinstance(identity, dict) or any(
        not isinstance(identity.get(key), str) or not identity[key].strip() or "REPLACE_WITH" in identity[key]
        for key in fields
    ):
        raise DeploymentError("STORE_IDENTITY_JSON must contain all four actual Partner Center identity fields.")
    if identity["Name"] == "CamControl.LocalTest":
        raise DeploymentError("The local test identity cannot be submitted to the Store.")
    return {**{key: env[key] for key in required}, "identity": identity}


def version(value):
    if not isinstance(value, str) or not re.fullmatch(r"\d+\.\d+\.\d+\.\d+", value):
        raise DeploymentError("A package version must have four numeric components.")
    parts = tuple(map(int, value.split(".")))
    if parts[0] == 0 or any(part > 65535 for part in parts) or parts[3] != 0:
        raise DeploymentError("Invalid Store package version; the fourth component must be 0.")
    return parts


def package_identity(package, expected):
    with ZipFile(package) as archive:
        root = ET.fromstring(archive.read("AppxManifest.xml"))
    identity = root.find("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity")
    if identity is None:
        raise DeploymentError("MSIX manifest has no Identity.")
    data = identity.attrib
    if data.get("Name") != expected["Name"] or data.get("Publisher") != expected["Publisher"]:
        raise DeploymentError("Package identity does not match the configured Store app.")
    if data.get("ProcessorArchitecture") != "x64":
        raise DeploymentError("This deployment workflow submits x64 packages only.")
    version(data.get("Version"))
    return data


def request(method, url, data=None, headers=None, timeout=120):
    try:
        with urlopen(Request(url, data=data, headers=headers or {}, method=method), timeout=timeout) as response:
            return response.read()
    except HTTPError as error:
        # Error bodies/URLs can contain an OAuth secret or the upload SAS token.
        raise DeploymentError(f"{method} request failed (HTTP {error.code}). Check Partner Center configuration and permissions.") from None
    except (URLError, TimeoutError, OSError):
        raise DeploymentError(f"{method} request failed due to a network error or timeout. Check submission status before retrying.") from None


def mask(value):
    if os.environ.get("GITHUB_ACTIONS") == "true":
        print("::add-mask::" + value, flush=True)


class StoreClient:
    def __init__(self, config):
        form = urlencode({
            "grant_type": "client_credentials", "client_id": config["STORE_CLIENT_ID"],
            "client_secret": config["STORE_CLIENT_SECRET"], "resource": API,
        }).encode()
        token = json.loads(request("POST", f"https://login.microsoftonline.com/{config['STORE_TENANT_ID']}/oauth2/token",
                                   form, {"Content-Type": "application/x-www-form-urlencoded"}))
        self.token = token["access_token"]
        mask(self.token)

    def api(self, method, path, body=None):
        headers = {"Authorization": "Bearer " + self.token, "Content-Type": "application/json"}
        data = None if body is None else json.dumps(body).encode()
        result = request(method, API + "/v1.0/my/" + path, data, headers)
        return json.loads(result) if result else {}

    def upload(self, url, archive):
        parsed = urlsplit(url)
        if parsed.scheme != "https" or not (parsed.hostname or "").endswith(".blob.core.windows.net"):
            raise DeploymentError("Partner Center returned an unexpected upload URL.")
        mask(url)
        # A separate request: never send the Store bearer token to Azure Storage.
        request("PUT", url, archive.read_bytes(), {
            "x-ms-blob-type": "BlockBlob", "x-ms-version": "2021-12-02", "Content-Type": "application/zip",
        }, timeout=600)


def replacement(submission, package, new_version):
    body = copy.deepcopy(submission)
    for old in body["applicationPackages"]:
        architecture = old.get("architecture", "").lower()
        if architecture not in {"x64", "x86", "arm", "arm64", "neutral"}:
            raise DeploymentError("Existing package architecture is unknown; review it in Partner Center.")
        if architecture == "x64":
            if version(old.get("version")) >= version(new_version):
                raise DeploymentError("Increase CamControl.csproj Version before merging; the Store already has this or a newer x64 version.")
            old["fileStatus"] = "PendingDelete"
    body["applicationPackages"].append({
        "fileName": package.name, "fileStatus": "PendingUpload",
        "minimumDirectXVersion": "None", "minimumSystemRam": "None",
    })
    body["targetPublishMode"] = "Immediate"
    for key in ("id", "status", "statusDetails", "fileUploadUrl", "friendlyName"):
        body.pop(key, None)
    return body


def deploy(config, package, client, sleep=time.sleep, polls=60):
    metadata = package_identity(package, config["identity"])
    app_path = "applications/" + config["STORE_APP_ID"]
    app = client.api("GET", app_path)
    if app.get("packageIdentityName") != metadata["Name"] or app.get("publisherName") != metadata["Publisher"]:
        raise DeploymentError("STORE_APP_ID points to a different package identity/publisher.")
    if app.get("pendingApplicationSubmission"):
        raise DeploymentError("A Store submission is already pending. Finish or remove it before retrying; it has not been modified.")
    published = app.get("lastPublishedApplicationSubmission")
    if not published:
        raise DeploymentError("Complete the first submission (including age ratings) in Partner Center before enabling API deployment.")
    # Validate versions and package metadata before creating a draft.
    previous = client.api("GET", app_path + "/submissions/" + published["id"])
    replacement(previous, package, metadata["Version"])
    submission = client.api("POST", app_path + "/submissions")
    submission_id = submission["id"]
    path = app_path + "/submissions/" + submission_id
    print(f"Created Store submission {submission_id} for version {metadata['Version']}.", flush=True)
    try:
        body = replacement(submission, package, metadata["Version"])
        client.api("PUT", path, body)
        with tempfile.TemporaryDirectory(prefix="camcontrol-store-") as directory:
            archive = Path(directory) / "submission.zip"
            with ZipFile(archive, "w", compression=ZIP_STORED) as output:
                output.write(package, arcname=package.name)
            client.upload(submission["fileUploadUrl"], archive)
        client.api("POST", path + "/commit")
        for attempt in range(polls):
            state = client.api("GET", path + "/status").get("status")
            if state in ACCEPTED:
                print(f"Submission {submission_id} accepted by Store ingestion ({state}). Certification/publication may still be pending.")
                return {"id": submission_id, "version": metadata["Version"], "status": state}
            if state in FAILED:
                raise DeploymentError(f"Store submission failed ({state}); inspect details in Partner Center.")
            if state not in {"PendingCommit", "CommitStarted"}:
                raise DeploymentError("Store returned an unexpected submission status; inspect Partner Center.")
            if attempt + 1 < polls:
                sleep(10)
        raise DeploymentError("Timed out waiting for Store ingestion; inspect submission status before retrying.")
    except Exception:
        print(f"Submission {submission_id} was left in Partner Center for inspection; no draft was automatically deleted.", file=sys.stderr)
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--prepare-identity", type=Path)
    mode.add_argument("--package-dir", type=Path)
    args = parser.parse_args()
    try:
        require_master_push(os.environ)
        config = configuration(os.environ)
        if args.prepare_identity:
            args.prepare_identity.parent.mkdir(parents=True, exist_ok=True)
            args.prepare_identity.write_text(json.dumps(config["identity"], ensure_ascii=False), encoding="utf-8")
            print("Store configuration validated and identity file written.")
            return 0
        packages = sorted(args.package_dir.glob("*.msix"))
        if len(packages) != 1:
            raise DeploymentError("Expected exactly one freshly built Store MSIX.")
        package_identity(packages[0], config["identity"])
        result = deploy(config, packages[0], StoreClient(config))
        if os.environ.get("GITHUB_STEP_SUMMARY"):
            with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as summary:
                summary.write(f"## Microsoft Store\n\nVersion {result['version']}, submission {result['id']}: {result['status']}.\n\n"
                              "Uploaded and committed for automatic publication after Microsoft certification; this is not a confirmation that it is already live.\n")
        return 0
    except DeploymentError as error:
        print(f"Store deployment failed: {error}", file=sys.stderr)
    except (KeyError, TypeError, ValueError, OSError, BadZipFile, ET.ParseError):
        print("Store deployment failed: invalid configuration, package or API response. Inspect the configuration and Partner Center status.", file=sys.stderr)
    return 1


if __name__ == "__main__":
    sys.exit(main())
