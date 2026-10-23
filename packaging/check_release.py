"""Validate release preparation locally or in CI, without Store credentials."""

import argparse
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

from submit_store import DeploymentError, ROOT, load_release_notes, project_package_version, version


def check_release(root=ROOT, base_ref=None):
    package_version = project_package_version(root)
    manifest = ET.parse(root / "packaging/AppxManifest.xml")
    identity = manifest.find("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity")
    if identity is None or identity.get("Version") != package_version:
        raise DeploymentError(f"AppxManifest.xml Identity Version must match the project: {package_version}.")
    load_release_notes(package_version, root)

    if base_ref:
        if base_ref.startswith("-"):
            raise DeploymentError("The release base must be a Git revision, not an option.")
        previous = subprocess.run(
            ["git", "show", f"{base_ref}:CamControl/CamControl.csproj"],
            cwd=root, capture_output=True, text=True, encoding="utf-8", check=False,
        )
        if previous.returncode:
            raise DeploymentError(f"Cannot read project version at {base_ref}. Fetch the release base before checking.")
        old_version = ET.fromstring(previous.stdout).findtext(".//Version")
        if not old_version or not re.fullmatch(r"\d+\.\d+\.\d+", old_version):
            raise DeploymentError(f"Invalid project version at {base_ref}.")
        if version(package_version) <= version(old_version + ".0"):
            raise DeploymentError(
                f"Target version {package_version} must be newer than {old_version}.0 at {base_ref}. "
                "Prepare the next release version and notes in the feature/fix branch."
            )
    return package_version


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-ref", help="Require a version newer than this Git revision (for example origin/master).")
    args = parser.parse_args()
    try:
        package_version = check_release(base_ref=args.base_ref)
        print(f"Release preparation validated: {package_version}, matching manifest and German Store release notes.")
        return 0
    except (DeploymentError, OSError, UnicodeError, ET.ParseError) as error:
        print(f"Release preparation failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
