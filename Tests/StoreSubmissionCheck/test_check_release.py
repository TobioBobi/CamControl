"""Offline checks for release-ready feature branches and shared target versions."""

from io import StringIO
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "packaging"))
import check_release as release


class ReleasePreparationChecks(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        for folder in ("CamControl", "packaging", "docs"):
            (self.root / folder).mkdir()
        self.write_project()
        self.write_manifest()
        self.notes = self.root / "docs/release-1.3.0.md"
        self.notes.write_text(
            '# CamControl 1.3.0\n\n## Text für „Neu in dieser Version“\n\n'
            'Deutsche und englische Oberfläche.\n', encoding="utf-8",
        )

    def write_project(self, version="1.3.0"):
        (self.root / "CamControl/CamControl.csproj").write_text(
            f"<Project><PropertyGroup><Version>{version}</Version></PropertyGroup></Project>", encoding="utf-8",
        )

    def write_manifest(self, version="1.3.0.0"):
        (self.root / "packaging/AppxManifest.xml").write_text(
            '<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">'
            f'<Identity Version="{version}" /></Package>', encoding="utf-8",
        )

    def previous(self, version="1.2.2"):
        return subprocess.CompletedProcess(
            [], 0, f"<Project><PropertyGroup><Version>{version}</Version></PropertyGroup></Project>", "",
        )

    def test_matching_project_manifest_and_notes_pass_without_git_or_network(self):
        with patch.object(release.subprocess, "run") as git:
            self.assertEqual(release.check_release(self.root), "1.3.0.0")
        git.assert_not_called()

    def test_stale_manifest_or_nonzero_store_revision_is_rejected(self):
        for version in ("1.2.2.0", "1.3.0.1"):
            self.write_manifest(version)
            with self.subTest(version=version), self.assertRaisesRegex(release.DeploymentError, "must match"):
                release.check_release(self.root)

    def test_missing_identity_is_rejected(self):
        (self.root / "packaging/AppxManifest.xml").write_text("<Package />", encoding="utf-8")
        with self.assertRaisesRegex(release.DeploymentError, "must match"):
            release.check_release(self.root)

    def test_missing_notes_for_target_are_rejected(self):
        self.notes.unlink()
        with self.assertRaisesRegex(release.DeploymentError, "release-1.3.0.md"):
            release.check_release(self.root)

    def test_empty_store_notes_are_rejected(self):
        self.notes.write_text('# CamControl 1.3.0\n\n## Text für „Neu in dieser Version“\n', encoding="utf-8")
        with self.assertRaisesRegex(release.DeploymentError, "must not be empty"):
            release.check_release(self.root)

    def test_shared_target_is_compared_to_master_not_incremented_for_each_feature(self):
        with patch.object(release.subprocess, "run", return_value=self.previous()) as git:
            self.assertEqual(release.check_release(self.root, "origin/master"), "1.3.0.0")
            self.assertEqual(release.check_release(self.root, "origin/master"), "1.3.0.0")
        self.assertEqual(git.call_args.args[0], ["git", "show", "origin/master:CamControl/CamControl.csproj"])
        self.assertEqual(git.call_args.kwargs["cwd"], self.root)

    def test_same_or_older_target_is_rejected(self):
        for previous in ("1.3.0", "1.4.0"):
            with self.subTest(previous=previous), patch.object(release.subprocess, "run", return_value=self.previous(previous)):
                with self.assertRaisesRegex(release.DeploymentError, "must be newer"):
                    release.check_release(self.root, "origin/master")

    def test_version_comparison_is_numeric(self):
        self.write_project("1.2.10")
        self.write_manifest("1.2.10.0")
        (self.root / "docs/release-1.2.10.md").write_text(
            '# CamControl 1.2.10\n\n## Text für „Neu in dieser Version“\n\nVerbesserungen.\n', encoding="utf-8",
        )
        with patch.object(release.subprocess, "run", return_value=self.previous("1.2.9")):
            self.assertEqual(release.check_release(self.root, "previous-master-sha"), "1.2.10.0")

    def test_missing_base_fails_with_fetch_guidance(self):
        with patch.object(release.subprocess, "run", return_value=subprocess.CompletedProcess([], 128, "", "missing")):
            with self.assertRaisesRegex(release.DeploymentError, "Fetch"):
                release.check_release(self.root, "missing")

    def test_git_options_cannot_be_passed_as_base(self):
        with patch.object(release.subprocess, "run") as git:
            with self.assertRaisesRegex(release.DeploymentError, "not an option"):
                release.check_release(self.root, "--help")
        git.assert_not_called()

    def test_invalid_previous_version_is_rejected(self):
        for version in ("1.2", "1.2.3.4", "0.2.0", "1.2.65536"):
            with self.subTest(version=version), patch.object(release.subprocess, "run", return_value=self.previous(version)):
                with self.assertRaises(release.DeploymentError):
                    release.check_release(self.root, "origin/master")

    def test_cli_reports_failure_without_credentials(self):
        with patch("sys.argv", ["check_release.py"]), patch.object(release, "check_release", side_effect=release.DeploymentError("Manifest mismatch")):
            with patch("sys.stderr", new_callable=StringIO) as errors:
                self.assertEqual(release.main(), 1)
        self.assertIn("Manifest mismatch", errors.getvalue())

    def test_cli_reports_success_and_passes_the_previous_master_revision(self):
        with patch("sys.argv", ["check_release.py", "--base-ref", "previous-master-sha"]):
            with patch.object(release, "check_release", return_value="1.3.0.0") as check, patch("sys.stdout", new_callable=StringIO):
                self.assertEqual(release.main(), 0)
        check.assert_called_once_with(base_ref="previous-master-sha")


if __name__ == "__main__":
    unittest.main()
