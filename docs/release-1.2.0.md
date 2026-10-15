# CamControl 1.2.0

Vorbereitet am 15. Oktober 2026. App-Version: **1.2.0**;
MSIX-Version: **1.2.0.0**. Die Projektversion ist die Quelle für Paket-Builds.

## Text für „Neu in dieser Version“

CamControl hat eine neue, kompakte Oberfläche: große Preset-Tasten und eine
übersichtliche Steuerung für Bewegung und Zoom. Alle Einstellungen passen zum
neuen Design. Der Dark Mode ist jetzt Standard; alternativ lassen sich ein
helles Farbschema oder die Windows-Einstellung dauerhaft auswählen.
Vorhandene Kameraeinstellungen, Preset-Zuordnungen und Tastenkürzel bleiben
erhalten.

## Pakete erstellen

```powershell
./packaging/Build-Msix.ps1
./packaging/Build-Msix.ps1 -Mode Store -StoreIdentityPath ./packaging/StoreIdentity.json
```

- Lokaler Test: `artifacts/msix/test/x64/CamControl-1.2.0.0-x64-test.msix`
- Store: `artifacts/msix/store/x64/CamControl-1.2.0.0-x64-store.msix`

Das Store-Paket verwendet dieselbe konfigurierte Identität. Es bleibt
unsigniert für die Microsoft-Store-Einreichung; Microsoft übernimmt die
Signierung. Ein lokales Testpaket benötigt zum Installieren eine gültige,
bereits vertrauenswürdige Testsignatur. Siehe [MSIX-Anleitung](msix.md).

## Validierung und Abnahme

Die automatisierten Prüfprogramme verwenden Fake-HTTP-Handler bzw. die Demo,
keine echte Kamera. Sie prüfen Konfiguration/Protokoll und WPF-Bedienung samt
Stopplogik, Farbschema, Ausrichtung und mehrzeiligen Fehlermeldungen.
Debug-/Release-Build und Paketvalidierung gehören zur Vorbereitung.

Vor Store-Einreichung manuell prüfen:

- Update einer installierten älteren Version mit Erhalt der Einstellungen.
- Bedienung und Stopps mit echter Kamera.
- Windows-Skalierung und Wechsel zwischen Monitoren mit verschiedenen DPI.
- Windows App Certification Kit und aktuelle Store-Screenshots.

Der dokumentierte frühere Installationstest gilt für die WinForms-Version.
Dieses Update wird durch das Erstellen der Pakete noch nicht veröffentlicht.
