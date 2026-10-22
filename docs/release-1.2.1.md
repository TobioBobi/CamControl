# CamControl 1.2.1

Vorbereitet am 22. Oktober 2026. App-Version: **1.2.1**;
MSIX-Version: **1.2.1.0**. Die Projektversion ist die Quelle für Paket-Builds.
Die Version ist höher als die bereits veröffentlichte Store-Version 1.2.0.0.

## Text für „Neu in dieser Version“

Ruhigere Kamerabedienung: Unnötiges Neuzeichnen von Preset-Beschriftungen,
Farben und Titelleisten wurde reduziert.

## Pakete erstellen

```powershell
./packaging/Build-Msix.ps1
./packaging/Build-Msix.ps1 -Mode Store -StoreIdentityPath ./packaging/StoreIdentity.json
```

- Lokaler Test: `artifacts/msix/test/x64/CamControl-1.2.1.0-x64-test.msix`
- Store: `artifacts/msix/store/x64/CamControl-1.2.1.0-x64-store.msix`

Die Paketidentität bleibt erhalten. Store-Pakete sind unsigniert;
Microsoft übernimmt die Signierung bei der Veröffentlichung.

## Validierung und Veröffentlichung

Die Windows-CI prüft Debug-/Release-Builds, Einstellungen und Kameraservice,
WPF-Bedienung einschließlich unveränderter Preset-Beschriftungen sowie die
Store-Automatisierung. Anschließend wird das x64-MSIX-Testpaket gebaut und
validiert. Die automatisierten Kameraprüfungen verwenden Fake-HTTP-Handler
beziehungsweise die Demo.

Änderungen werden zunächst nach `develop` gemergt. Ein Release-PR von `develop`
nach `master` startet nach dem Merge und erfolgreichen Prüfungen die
Store-Einreichung. Die Veröffentlichung erfolgt automatisch nach erfolgreicher
Microsoft-Zertifizierung. Ein grüner Deployment-Job bestätigt die Einreichung,
noch nicht die Verfügbarkeit im Store. Siehe [Store-Deployment](store-deployment.md).

Die Einreichung übernimmt bestehende Store-Einträge; der obige Text unter
„Neu in dieser Version“ wird durch den Workflow nicht automatisch eingetragen.
