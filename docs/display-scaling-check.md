# Lokale Layoutprüfung

Geprüft am 11. Oktober 2026 unter Windows, bei 125 % Windows-Skalierung
(120 DPI) und einer verfügbaren Arbeitsfläche von 1920 × 1020 Pixeln.

Zusätzlich wurde die Schrift nach dem Öffnen der Fenster um 25 %, 50 % und
100 % vergrößert und die WinForms-Autoskalierung ausgeführt. Diese Simulation
prüft Platzbedarf und Layout, ersetzt aber keine echten Windows-DPI-Wechsel.
Die Windows-Einstellungen wurden nicht verändert.

Insgesamt wurden 44 Darstellungen gerendert: Hauptfenster im Kamera- und
Demo-Modus, alle fünf Einstellungsseiten, die gescrollte Automatik,
Preset-Editor mit und ohne Demo sowie der Einrichtungsdialog, jeweils in vier
Schriftgrößen. Der Preset-Editor wurde auch mit einem 32 Zeichen langen Namen
und einer mehrzeiligen Fehlermeldung geprüft. Alle Kamerabefehle liefen über
den Demo-HTTP-Handler, ohne Netzwerktransport.

Die zuvor zu hohen Fenster passen nach der Korrektur in die Arbeitsfläche.
Die Demo-Vorschau nutzt den verbleibenden Platz und wird bei geringer Höhe
kompakter. Der Preset-Editor scrollt bei Platzmangel; „Schließen“ bleibt
außerhalb des scrollbaren Bereichs erreichbar. Die Einstellungen behalten
ihren festen Schließen-Button und scrollbare Inhalte.

Die UI-Regressionstests prüfen zusätzlich die Größenbegrenzung, die Trennung
von Vorschau und Fehlerbereich, wiederholte Moduswechsel sowie das Scrollen
zum Speichern-Button in einem auf 600 Pixel Höhe begrenzten Preset-Editor.

Echte Windows-Skalierung bei 100 %, 150 % und 200 %, kleinere Bildschirmflächen
und Wechsel zwischen Monitoren mit unterschiedlichen DPI bleiben manuell zu
prüfen. Lokal ist nur ein Monitor verfügbar.
