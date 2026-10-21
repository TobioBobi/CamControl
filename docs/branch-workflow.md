# Branches und Veröffentlichungen

Änderungen werden vor einer gemeinsamen Veröffentlichung auf `develop`
gesammelt:

```text
feature/* oder fix/* → develop → master → Microsoft Store
```

## Änderungen entwickeln und prüfen

1. Einen neuen `feature/...`- oder `fix/...`-Branch vom aktuellen `develop`
   erstellen.
2. Die Änderung als Pull Request mit **Zielbranch `develop`** einreichen.
3. Die Windows-CI baut Debug und Release, führt die automatischen Prüfungen aus
   und erzeugt ein unsigniertes x64-MSIX-Testpaket als GitHub-Artifact.
4. Nach Review nach `develop` mergen. Auch der gesammelte Stand wird dort
   erneut gebaut und getestet. Mehrere kleine Fixes können so gemeinsam unter
   Windows geprüft werden, ohne eine Store-Version einzureichen.

## Eine gesammelte Version veröffentlichen

1. Den gewünschten Stand von `develop` unter Windows prüfen.
2. Einmal für die gesammelte Veröffentlichung die App-Version in
   `CamControl/CamControl.csproj` erhöhen und Changelog, MSIX-Manifestvorlage
   sowie aktuelle Paket-Anleitung passend aktualisieren. Die MSIX-Version
   besteht aus der App-Version plus `.0`.
3. Einen Release-PR **von `develop` nach `master`** öffnen und die CI abwarten.
4. Den Release-PR nach `master` mergen. Bei eingerichteter Store-Automatisierung
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
Fix per PR nach `master` bringen. Auch dieser Merge startet nach Einrichtung
die Store-Pipeline und benötigt eine höhere Version. Anschließend `master`
zurück nach `develop` mergen, damit der Fix und die Versionsänderung im
Sammelbranch enthalten sind.
