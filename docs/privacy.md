Datenschutz für CamControl
Stand: 11. Oktober 2026
Herausgeber: Tobias Heer

Lokale Einstellungen
CamControl speichert die Kamera-Adresse in settings.json, die Namen der Buttons in buttons.json und die Auswahl der automatischen Start- und Endposition in automatic-positions.json. Diese Dateien liegen im Benutzerprofil unter %LOCALAPPDATA%\CamControl. Sie werden nicht an den Herausgeber übertragen.

Kommunikation mit der Kamera
Im Kameramodus sendet CamControl Steuerbefehle an die konfigurierte Kamera-Adresse. Dazu gehören Bewegung, Zoom, das Anfahren und das Speichern von Presets. Die Kommunikation erfolgt derzeit über unverschlüsseltes HTTP. CamControl empfängt oder speichert keine Kamerabilder, Videos oder Tonaufnahmen. Die Kamera selbst speichert die Preset-Positionen; ihre eigene Datenverarbeitung hängt vom jeweiligen Gerät ab.

Demo-Modus
Im Demo-Modus werden keine Netzwerkbefehle gesendet. Bewegung und Presets werden im Arbeitsspeicher simuliert. Demo-Buttonnamen, Demo-Presets und die Demo-Automatik gelten nur für die laufende App-Sitzung. Vorhandene Einstellungen für die echte Kamera bleiben erhalten.

Analyse und Übertragung
CamControl enthält keine Analyse-, Telemetrie- oder Werbefunktionen und sendet keine Nutzungsdaten an den Herausgeber. Es ist kein Benutzerkonto erforderlich.

Externe Links und Support
Der Link „Projekt & Support auf GitHub“ öffnet erst nach einem Klick den Standardbrowser. Beim Besuch von GitHub gelten dessen Datenschutzbestimmungen. Angaben, die du dort freiwillig in einem öffentlichen Issue veröffentlichst, sind öffentlich sichtbar. Bitte keine Passwörter oder vertraulichen Netzwerkdaten einstellen.
Projekt und Support: https://github.com/TobioBobi/CamControl
GitHub-Datenschutz: https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement

Lokale Daten entfernen
Zum Zurücksetzen der lokalen Konfiguration kannst du bei geschlossener App den Ordner %LOCALAPPDATA%\CamControl löschen. Beim nächsten Start wird die Kamera-Einrichtung erneut angeboten. Auf der Kamera gespeicherte Presets werden dadurch nicht gelöscht. Demo-Daten werden beim Beenden der App verworfen.
