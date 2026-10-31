# Branches und Veröffentlichungen

Änderungen werden vor einer gemeinsamen Veröffentlichung auf `develop`
gesammelt:

```text
feature/* oder fix/* → develop → master → Microsoft Store
```

Neue Pull Requests einschließlich Drafts werden erst nach ausdrücklichem Okay
des Nutzers angelegt. Branches, Implementierung, Prüfungen, Commits und Pushes
werden vorher fertig vorbereitet. Das bei PR-Erstellung gestartete Code-Review
prüft spätere Commits nicht erneut.

## Änderungen entwickeln und prüfen

1. Einen neuen `feature/...`- oder `fix/...`-Branch vom aktuellen `develop`
   erstellen.
2. Bereits im Feature-/Fix-Branch die Release-Vorbereitung erledigen: gemeinsame
   Zielversion festlegen beziehungsweise von `develop` übernehmen, Projektversion,
   MSIX-Manifestvorlage, `CHANGELOG.md`, `docs/release-<Version>.md` und aktuelle
   Paketbeispiele in `docs/msix.md` pflegen. Die Zielversion muss höher als auf
   `master` sein; die MSIX-Version besteht aus der App-Version plus `.0`.
   Für mehrere Features eines Releases dieselbe Zielversion verwenden und die
   vorhandenen Hinweise ergänzen. Nicht für jedes Feature erneut hochzählen.
   Parallele Branches vor dem Merge auf die gemeinsame Zielversion abstimmen.
3. In der Release-Datei den Abschnitt `## Text für „Neu in dieser Version“`
   mit dem deutschen Store-Text vervollständigen. Historische Release-Dateien
   unverändert lassen. Die Änderung als Pull Request mit **Zielbranch `develop`**
   einreichen; Versionsänderungen und Release Notes gehören in diesen PR.
4. Die Windows-CI prüft vor dem Merge die höhere Zielversion gegenüber `master`,
   die Übereinstimmung mit der Manifestvorlage und die passenden Store-Hinweise.
   Sie baut Debug und Release, führt die automatischen Prüfungen aus
   und erzeugt ein unsigniertes x64-MSIX-Testpaket als GitHub-Artifact.
5. Nach Review nach `develop` mergen. Auch der gesammelte Stand wird dort
   erneut gebaut und getestet. Mehrere kleine Fixes können so gemeinsam unter
   Windows geprüft werden, ohne eine Store-Version einzureichen.

Lokale Prüfung vor dem PR (gegebenenfalls zuerst `git fetch origin master`):

```powershell
python packaging/check_release.py --base-ref origin/master
python -m unittest discover -s Tests/StoreSubmissionCheck -v
```

Die CI lädt die Git-Historie für den Versionsvergleich. Auf einem Push nach
`master` vergleicht sie mit dem vorherigen `master`-Commit; beim manuellen
Prüflauf auf `master` prüft sie die Konsistenz des bestehenden Releases.

## Eine gesammelte Version veröffentlichen

Version, Dokumentation und Versionshinweise sind bereits auf `develop`
vollständig vorbereitet und geprüft. Ein Release erfordert keine zusätzlichen
Versions- oder Dokumentations-Commits.

1. Für den abgenommenen Stand einen Release-PR **von `develop` nach `master`**
   öffnen und die CI abwarten.
2. Den Release-PR nach `master` mergen. Das ist der einzige Veröffentlichungsschritt.
   Bei eingerichteter Store-Automatisierung
   baut und testet dieser Push erneut, erstellt das Store-Paket und reicht es
   zur Zertifizierung ein. Microsoft veröffentlicht nach erfolgreicher Prüfung.

Store-Einreichungen sind ausschließlich bei einem **Push auf `master`**
vorgesehen. Pull Requests, Pushes auf `develop` und manuelle Workflow-Läufe
veröffentlichen nichts. Die Microsoft-Anbindung und GitHub-Variablen/Secrets
müssen vor dem ersten automatischen Release eingerichtet sein. Die Anleitung
zur Store-Automatisierung steht nach deren Integration in
`docs/store-deployment.md`.

`develop` ist ein dauerhafter Sammelbranch. Beim Release den normalen
**Merge-Commit** verwenden, damit die Historie zwischen `develop` und `master`
erhalten bleibt. Wenn `master` zusätzliche Änderungen enthält, diese zurück
nach `develop` mergen, bevor die nächste Änderung davon abgezweigt wird.

## Dringende Korrekturen

Bei Bedarf einen `hotfix/...`-Branch vom aktuellen `master` erstellen und den
Fix einschließlich höherer Version, Manifest, Changelog und Release Notes im
Hotfix-Branch vorbereiten und per PR nach `master` bringen. Auch dieser Merge
startet nach Einrichtung die Store-Pipeline. Anschließend `master` zurück nach
`develop` mergen, damit der Fix und seine Release-Datei im Sammelbranch enthalten
sind. Eine bereits höhere kommende Zielversion auf `develop` dabei beibehalten.
