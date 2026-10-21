"""Offline checks: no Microsoft credentials or network access are used."""

import copy
from io import StringIO
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import MagicMock, patch
from urllib.error import HTTPError
from urllib.parse import parse_qs
from zipfile import ZipFile


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("submit_store", ROOT / "packaging/submit_store.py")
store = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(store)
IDENTITY = {"Name": "12345.TobioBobi.CamControl", "Publisher": "CN=publisher-id",
            "PublisherDisplayName": "TobioBobi", "DisplayName": "CamControl"}
ENV = {"STORE_TENANT_ID": "12345678-1234-1234-1234-123456789012",
       "STORE_CLIENT_ID": "12345678-1234-1234-1234-123456789013",
       "STORE_CLIENT_SECRET": "secret+with&symbols", "STORE_APP_ID": "9NTESTAPP",
       "STORE_IDENTITY_JSON": json.dumps(IDENTITY)}
APP = {"packageIdentityName": IDENTITY["Name"], "publisherName": IDENTITY["Publisher"],
       "lastPublishedApplicationSubmission": {"id": "published"}, "pendingApplicationSubmission": None}
PREVIOUS = {
    "applicationPackages": [
        {"fileName": "old.msix", "version": "1.2.0.0", "architecture": "X64", "fileStatus": "Uploaded"},
        {"fileName": "arm.msix", "version": "1.0.0.0", "architecture": "ARM64", "fileStatus": "Uploaded"},
    ],
    "listings": {"de-de": {"baseListing": {"title": "CamControl", "images": [{"fileStatus": "Uploaded"}]}}},
    "pricing": {"priceId": "Free"}, "visibility": "Public", "targetPublishMode": "Manual",
}


class FakeClient:
    def __init__(self, states=("CommitStarted", "PreProcessing")):
        self.app = copy.deepcopy(APP)
        self.previous = copy.deepcopy(PREVIOUS)
        self.states = iter(states)
        self.calls = []
        self.body = None
        self.archive_files = None

    def api(self, method, path, body=None):
        self.calls.append((method, path))
        if path.endswith("/status"):
            return {"status": next(self.states)}
        if method == "GET" and path.endswith("/published"):
            return copy.deepcopy(self.previous)
        if method == "GET":
            return copy.deepcopy(self.app)
        if method == "POST" and path.endswith("/submissions"):
            return {**copy.deepcopy(self.previous), "id": "new-submission", "status": "PendingCommit",
                    "fileUploadUrl": "https://example.blob.core.windows.net/upload?sig=secret"}
        if method == "PUT":
            self.body = body
        return {}

    def upload(self, url, archive):
        self.calls.append(("UPLOAD", "blob"))
        with ZipFile(archive) as zipped:
            self.archive_files = {name: zipped.read(name) for name in zipped.namelist()}


class SubmissionChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.package = Path(self.temp.name) / "CamControl-1.2.1.0-x64-store.msix"
        self.write_package()
        self.config = store.configuration(ENV)

    def write_package(self, name=IDENTITY["Name"], publisher=IDENTITY["Publisher"], architecture="x64", version="1.2.1.0"):
        with ZipFile(self.package, "w") as package:
            package.writestr("AppxManifest.xml", f'<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">'
                             f'<Identity Name="{name}" Publisher="{publisher}" ProcessorArchitecture="{architecture}" Version="{version}" /></Package>')
            package.writestr("CamControl.exe", b"mock executable")

    def deploy(self, client, **kwargs):
        with patch("sys.stdout", new_callable=StringIO), patch("sys.stderr", new_callable=StringIO):
            return store.deploy(self.config, self.package, client, sleep=lambda _: None, **kwargs)

    def assert_no_writes(self, client):
        self.assertTrue(all(method == "GET" for method, _ in client.calls))

    def test_only_master_push_can_deploy(self):
        store.require_master_push({"GITHUB_EVENT_NAME": "push", "GITHUB_REF": "refs/heads/master"})
        for event, ref in [("pull_request", "refs/pull/1/merge"), ("pull_request_target", "refs/heads/master"),
                           ("push", "refs/heads/develop"), ("pull_request", "refs/heads/develop"),
                           ("workflow_dispatch", "refs/heads/master"), ("push", "refs/heads/feature"),
                           ("push", "refs/tags/master"), (None, None)]:
            with self.subTest(event=event, ref=ref), self.assertRaises(store.DeploymentError):
                store.require_master_push({"GITHUB_EVENT_NAME": event, "GITHUB_REF": ref})

    def test_missing_configuration_fails_without_exposing_secret(self):
        for key in ENV:
            env = {name: value for name, value in ENV.items() if name != key}
            with self.subTest(key=key), self.assertRaises(store.DeploymentError) as error:
                store.configuration(env)
            self.assertIn(key, str(error.exception))
            self.assertNotIn(ENV["STORE_CLIENT_SECRET"], str(error.exception))

    def test_invalid_identity_and_ids_are_rejected(self):
        invalid = [None, {}, {**IDENTITY, "Name": "CamControl.LocalTest"}, {**IDENTITY, "Publisher": "REPLACE_WITH_VALUE"}]
        for identity in invalid:
            with self.subTest(identity=identity), self.assertRaises(store.DeploymentError):
                store.configuration({**ENV, "STORE_IDENTITY_JSON": json.dumps(identity)})
        for key, value in [("STORE_CLIENT_ID", "not-a-guid"), ("STORE_TENANT_ID", "tenant/path"), ("STORE_APP_ID", "app/path")]:
            with self.subTest(key=key), self.assertRaises(store.DeploymentError):
                store.configuration({**ENV, key: value})

    def test_upload_preserves_listing_and_other_architectures(self):
        client = FakeClient()
        result = self.deploy(client)
        self.assertEqual(result, {"id": "new-submission", "version": "1.2.1.0", "status": "PreProcessing"})
        self.assertEqual(client.body["listings"], PREVIOUS["listings"])
        self.assertEqual(client.body["pricing"], PREVIOUS["pricing"])
        self.assertEqual(client.body["applicationPackages"][1], PREVIOUS["applicationPackages"][1])
        self.assertEqual(client.body["applicationPackages"][0]["fileStatus"], "PendingDelete")
        self.assertEqual(client.body["applicationPackages"][-1], {
            "fileName": self.package.name, "fileStatus": "PendingUpload",
            "minimumDirectXVersion": "None", "minimumSystemRam": "None"})
        self.assertEqual(client.body["targetPublishMode"], "Immediate")
        self.assertNotIn("fileUploadUrl", client.body)
        self.assertEqual(client.archive_files, {self.package.name: self.package.read_bytes()})
        methods = [method for method, _ in client.calls]
        self.assertEqual(methods, ["GET", "GET", "POST", "PUT", "UPLOAD", "POST", "GET", "GET"])
        self.assertTrue(client.calls[5][1].endswith("/commit"))

    def test_pending_submission_is_never_modified_or_deleted(self):
        client = FakeClient()
        client.app["pendingApplicationSubmission"] = {"id": "manual-draft"}
        with self.assertRaisesRegex(store.DeploymentError, "already pending"):
            self.deploy(client)
        self.assert_no_writes(client)

    def test_first_publication_must_already_exist(self):
        client = FakeClient()
        client.app["lastPublishedApplicationSubmission"] = None
        with self.assertRaisesRegex(store.DeploymentError, "first submission"):
            self.deploy(client)
        self.assert_no_writes(client)

    def test_wrong_product_is_rejected_before_mutation(self):
        for key in ("packageIdentityName", "publisherName"):
            client = FakeClient()
            client.app[key] = "other-app"
            with self.subTest(key=key), self.assertRaisesRegex(store.DeploymentError, "different package identity"):
                self.deploy(client)
            self.assert_no_writes(client)

    def test_bad_msix_identity_architecture_or_revision_cannot_upload(self):
        for changes in [{"name": "CamControl.LocalTest"}, {"publisher": "CN=other"}, {"architecture": "arm64"},
                        {"version": "1.2.1.1"}]:
            self.write_package(**changes)
            client = FakeClient()
            with self.subTest(changes=changes), self.assertRaises(store.DeploymentError):
                self.deploy(client)
            self.assertEqual(client.calls, [])

    def test_same_or_older_version_fails_before_creating_draft(self):
        for existing in ("1.2.1.0", "1.2.10.0", "2.0.0.0"):
            client = FakeClient()
            client.previous["applicationPackages"][0]["version"] = existing
            with self.subTest(existing=existing), self.assertRaisesRegex(store.DeploymentError, "Increase"):
                self.deploy(client)
            self.assert_no_writes(client)

    def test_numeric_versions_are_not_compared_as_text(self):
        client = FakeClient()
        self.write_package(version="1.2.10.0")
        client.previous["applicationPackages"][0]["version"] = "1.2.9.0"
        self.assertEqual(self.deploy(client)["version"], "1.2.10.0")

    def test_ambiguous_architecture_requires_review(self):
        client = FakeClient()
        client.previous["applicationPackages"][0]["architecture"] = ""
        with self.assertRaisesRegex(store.DeploymentError, "architecture is unknown"):
            self.deploy(client)
        self.assert_no_writes(client)

    def test_store_failure_does_not_report_success_or_delete_draft(self):
        for state in store.FAILED:
            client = FakeClient(states=(state,))
            with self.subTest(state=state), self.assertRaisesRegex(store.DeploymentError, "Store submission failed"):
                self.deploy(client)
            self.assertNotIn("DELETE", [method for method, _ in client.calls])

    def test_commit_timeout_does_not_retry_a_mutation(self):
        client = FakeClient(states=("CommitStarted", "CommitStarted"))
        with self.assertRaisesRegex(store.DeploymentError, "Timed out"):
            self.deploy(client, polls=2)
        self.assertEqual(sum(method == "POST" for method, _ in client.calls), 2)

    def test_upload_failure_never_commits(self):
        client = FakeClient()
        client.upload = MagicMock(side_effect=store.DeploymentError("Upload failed"))
        with self.assertRaisesRegex(store.DeploymentError, "Upload failed"):
            self.deploy(client)
        self.assertFalse(any(path.endswith("/commit") for _, path in client.calls))

    def test_token_request_encodes_secret_and_uses_store_resource(self):
        response = MagicMock()
        response.__enter__.return_value.read.return_value = b'{"access_token":"mock-token"}'
        with patch.object(store, "urlopen", return_value=response) as network:
            store.StoreClient(self.config)
        sent = network.call_args.args[0]
        form = parse_qs(sent.data.decode())
        self.assertEqual(form["client_secret"], [ENV["STORE_CLIENT_SECRET"]])
        self.assertEqual(form["resource"], [store.API])

    def test_upload_never_receives_bearer_token(self):
        client = object.__new__(store.StoreClient)
        client.token = "mock-bearer-token"
        with patch.object(store, "request") as network:
            client.upload("https://example.blob.core.windows.net/upload?sig=secret", self.package)
        self.assertEqual(network.call_args.args[0], "PUT")
        self.assertNotIn("Authorization", network.call_args.args[3])

    def test_upload_rejects_unexpected_destination(self):
        client = object.__new__(store.StoreClient)
        with patch.object(store, "request") as network:
            for url in ("http://example.blob.core.windows.net/file", "https://example.com/file"):
                with self.subTest(url=url), self.assertRaises(store.DeploymentError):
                    client.upload(url, self.package)
        network.assert_not_called()

    def test_http_error_does_not_expose_url_or_response_credentials(self):
        error = HTTPError("https://example.com?sig=hidden-secret", 403, "hidden-secret", {}, None)
        with patch.object(store, "urlopen", side_effect=error):
            with self.assertRaises(store.DeploymentError) as caught:
                store.request("PUT", error.url)
        self.assertIn("403", str(caught.exception))
        self.assertNotIn("hidden-secret", str(caught.exception))


if __name__ == "__main__":
    unittest.main()
