# Projektworkflow

- Änderungen auf `feature/...` oder `fix/...` von `develop` entwickeln und per
  Pull Request nach `develop` bringen. Veröffentlichungen erfolgen durch Merge
  von `develop` nach `master`.
- Bereits im Feature-/Fix-Branch die gemeinsame Zielversion des geplanten Releases
  verwenden. Die Version muss höher als auf `master` sein. Wenn `develop` bereits
  eine kommende Zielversion hat, diese übernehmen; nicht für jedes Feature erneut
  erhöhen. Parallel laufende Branches auf dieselbe Zielversion abstimmen.
- Projektversion in `CamControl/CamControl.csproj`, MSIX-Version in
  `packaging/AppxManifest.xml` (App-Version plus `.0`), `CHANGELOG.md`,
  `docs/release-<Version>.md` und aktuelle Paketbeispiele in `docs/msix.md`
  gemeinsam pflegen. Bestehende Hinweise zur Zielversion ergänzen; historische
  Release-Dateien unverändert lassen.
- Der deutsche Store-Text steht unter `## Text für „Neu in dieser Version“` in
  der Release-Datei. Er muss vor dem Merge vollständig sein.
- Release-Vorbereitung mit `python packaging/check_release.py --base-ref origin/master`
  und `python -m unittest discover -s Tests/StoreSubmissionCheck -v` prüfen.
  Für Codeänderungen zusätzlich die passenden Build-/App-Prüfungen aus `README.md`
  ausführen. Details: `docs/branch-workflow.md`.
- Beim Release keine nachträglichen Versions- oder Dokumentationsänderungen auf
  `master` vornehmen. Der Merge veröffentlicht den vorbereiteten Stand automatisch.
