Datenschutz für CamControl
Stand: 11. Oktober 2026
Herausgeber: TobioBobi

Lokale Daten
Kamera-IP, Buttonnamen und Start-/Endpositionen werden unter %LOCALAPPDATA%\CamControl gespeichert (settings.json, buttons.json, automatic-positions.json). Bei MSIX kann Windows Dateien in den privaten App-Datenbereich umleiten. Es gibt kein Benutzerkonto, keine Telemetrie oder Werbung und keine Datenübertragung an den Herausgeber.

Kamerasteuerung
HTTP-CGI-Befehle für Bewegung, Zoom und Presets gehen über unverschlüsseltes HTTP an die konfigurierte Kamera. CamControl empfängt und speichert weder Kamerabilder noch Video oder Ton. Preset-Positionen speichert die Kamera; ihre eigene Datenverarbeitung hängt vom Gerät ab.

Demo
Keine Netzwerkbefehle. Demo-Namen, Positionen und Automatik liegen nur im Arbeitsspeicher und werden beim Beenden verworfen. Deine Kameraeinstellungen bleiben erhalten.

GitHub und Support
Der Support-Link öffnet GitHub erst nach einem Klick. Dort gelten GitHubs Datenschutzbestimmungen. Öffentliche Issues sind für alle sichtbar; keine vertraulichen Daten einstellen.
Projekt & Support: https://github.com/TobioBobi/CamControl
GitHub-Datenschutz: https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement

Daten löschen
Bei geschlossener App %LOCALAPPDATA%\CamControl löschen. Einen privaten MSIX-Datenbereich kannst du über die Windows-App-Einstellungen zurücksetzen; bereits vorhandene Dateien im genannten Ordner separat löschen. Auf der Kamera gespeicherte Presets bleiben bestehen.
