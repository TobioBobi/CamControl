# CamControl 1.2.0

Vorbereitet am 15. Oktober 2026. App-Version: **1.2.0**;
MSIX-Version: **1.2.0.0**. Die Projektversion ist die Quelle für Paket-Builds.

## Text für „Neu in dieser Version“

Neuer Look, Dark Mode und mehr Platz auf deinem Desktop. Große Buttons,
kurze Wege – volle Kontrolle beim nächsten Livestream.

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

Der lokale Windows-MSIX-Update-Test von 1.1.0 auf 1.2.0 wurde mit separater
Testidentität und identischen Programmdateien erfolgreich durchgeführt,
einschließlich Datenerhaltung und installierter WPF-Demo. Siehe
[Update-Prüfbericht](msix-upgrade-1.2.0-test.md).

Vor Store-Einreichung weiterhin manuell prüfen:

- Bedienung und Stopps mit echter Kamera.
- Windows-Skalierung und Wechsel zwischen Monitoren mit verschiedenen DPI.
- Windows App Certification Kit und aktuelle Store-Screenshots.

Der frühere Installationstest gilt für die WinForms-Version; der neue
Update-Prüfbericht deckt den Wechsel zu WPF ab. Dieses Update wird durch das
Erstellen und lokale Testen der Pakete noch nicht veröffentlicht.
