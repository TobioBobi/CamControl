# Store-Beschreibung für CamControl

## Kurzbeschreibung

PTZ-Kameras über kompatible HTTP-CGI-Befehle steuern – mit großen Buttons,
eigenen Presets und Demo-Modus.

## Beschreibung

CamControl steuert Schwenken, Neigen, Zoom und Presets deiner PTZ-Kamera.
Große Buttons und Tastenkürzel machen die Bedienung einfach. Vergib eigene
Buttonnamen, speichere Kamerapositionen und wähle bei Bedarf eine automatische
Start- oder Endposition.

Die Kompatibilität richtet sich nach den unterstützten Steuerbefehlen,
unabhängig von Hersteller oder Modell. Die Kamera muss den HTTP-CGI-Befehlssatz
unter `/cgi-bin/ptzctrl.cgi?ptzcmd&…` für Bewegung, Zoom, Stopps und Presets
unterstützen. Erforderlich sind eine erreichbare IPv4-Adresse und HTTP auf
Port 80 ohne Anmeldung.

Ohne Kamera kannst du alle Funktionen im integrierten Demo-Modus ausprobieren.
Dabei werden keine Netzwerkbefehle gesendet. Die Vorschau ist eine Simulation;
CamControl zeigt kein Live-Video an.

Deine Einstellungen bleiben lokal. Kein Benutzerkonto, keine Werbung,
keine Telemetrie.

## Hinweise für die Einreichung

Beschreibung als Entwurf für das Partner Center; sie ist noch nicht dort
eingetragen. Hardwaretests müssen den verwendeten Befehlssatz bestätigen.
Die Demo-Prüfanleitung steht in [store-testing.md](store-testing.md).
