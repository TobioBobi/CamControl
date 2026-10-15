# WPF-Layoutprüfung

Die WPF-Umstellung vom 15. Oktober 2026 ersetzt die frühere WinForms-Oberfläche.
Die aktuellen Prüfungen laufen mit:

```powershell
dotnet run --project Tests/CameraUiCheck/CameraUiCheck.csproj -c Release
```

Das tatsächliche WPF-Hauptfenster, Einstellungen und der Preset-Editor werden
mit dem Demo-Transport erstellt und nach `artifacts/ui-preview` gerendert.
Geprüft werden sieben Presets, vier Richtungs- und zwei Zoomtasten, ein
32 Zeichen langer Name, kleine Fenster, um 50 % vergrößerte Schrift sowie
helle und dunkle Ressourcen. Einstellungen und Fehlerstatus bleiben außerhalb
des scrollbaren Bedienbereichs. Bei normaler Größe ist kein Scrollen nötig;
bei Platzmangel werden Bedienelemente nicht verkleinert oder entfernt. Eine
zweizeilige Kamerafehlermeldung ist als Regression abgedeckt: Sie nutzt den
reservierten Statusbereich und erzeugt bei normaler Größe keinen Scrollbalken.

Die gemeinsame Fensterklasse begrenzt Fenster auf die Arbeitsfläche des
aktuellen Monitors. Das Anwendungsmanifest aktiviert PerMonitorV2. Vergrößerte
Schrift und RenderTargetBitmap prüfen das Layout, ersetzen jedoch keine echten
Windows-DPI-Wechsel. Die Windows-Anzeigeeinstellungen werden nicht verändert.

Vor Freigabe manuell prüfen: 100 %, 150 % und 200 % Windows-Skalierung, kleine
Arbeitsflächen, Wechsel zwischen Monitoren mit unterschiedlichen DPI,
High-Contrast-Darstellung, Tastaturfokus und Mausbedienung. Der historische
MSIX-Installationstest der WinForms-Version bestätigt die neue WPF-Version
nicht; der separate [Update-Test 1.1.0 auf 1.2.0](msix-upgrade-1.2.0-test.md)
bestätigt inzwischen Installation, Datenerhaltung und Demo-Betrieb.
