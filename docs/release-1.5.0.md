# CamControl 1.5.0

Vorbereitet am 22. November 2026. App-Version **1.5.0**, MSIX-Version **1.5.0.0**.

## Text für „Neu in dieser Version“

CamControl kann Kameras mit aktiviertem ONVIF im lokalen Netzwerk finden.
Eine gespeicherte Kamera wird nach einem Wechsel ihrer IP-Adresse automatisch
wiedererkannt. Im Tab „Kamera“ kannst du die aktuelle Einrichtung prüfen und
die Kamera per Netzwerksuche oder mit einer festen IPv4-Adresse einrichten.

## Abnahme

- Bei der AIDA PTZ-X20-IP ONVIF aktivieren. Die Kamera in den Einstellungen
  suchen, auswählen und speichern. Anschließend die CGI-Steuerung prüfen.
- Einen tatsächlichen DHCP-Adresswechsel herbeiführen. Prüfen, ob dieselbe
  ONVIF-Geräte-ID gemeldet wird und sich die Kamera unter der neuen IP steuern lässt.
- Dieselbe Prüfung bei geöffneter App und nach einem Neustart mit konfiguriertem
  Start-Preset ausführen. Das Start-Preset wartet auf die erste Wiedererkennung
  der ausgewählten Kamera.
- Paketbau und lokale Installation nach [MSIX-Anleitung](msix.md) prüfen.
