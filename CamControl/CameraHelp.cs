namespace CamControl
{
    internal static class CameraHelp
    {
        public const string Text = """
            Voraussetzungen
            CamControl benötigt einen Windows-PC und eine kompatible PTZ-Kamera (Schwenken, Neigen und Zoom). Für die aktuelle Programmversion muss die .NET 10 Desktop Runtime installiert sein.

            PC und Kamera müssen sich über das Netzwerk erreichen können. Die Kamera muss ihre HTTP-Steuerung unter der eingestellten IPv4-Adresse auf Port 80 bereitstellen. Ein SDK ist zum Bedienen der App nicht erforderlich.

            Kamerakompatibilität
            CamControl verwendet HTTP-Befehle an /cgi-bin/ptzctrl.cgi. Eine bestätigte Liste unterstützter Kameramodelle liegt noch nicht vor. Eine beliebige PTZ-Kamera ist daher nicht automatisch kompatibel. Zugangsdaten und andere Ports können derzeit nicht eingestellt werden.

            Kamera-Adresse einstellen
            „Einstellungen“ oben rechts öffnen und auf der Seite „Kamera“ die aktuelle Adresse ansehen. „Konfiguration bearbeiten“ schaltet die Eingabe frei. Erst „Speichern“ übernimmt die Adresse und speichert sie für den nächsten Start. „Abbrechen“ oder das Schließen des Dialogs verwirft die Eingabe.

            Ohne Kamera ausprobieren
            Beim ersten Start ohne gespeicherte Kamera-Adresse „Ohne Kamera ausprobieren“ wählen. Später ist der Einstieg unter „Einstellungen“ → „Demo-Modus“ → „Demo starten“ möglich. Die Demo gilt für diese Sitzung; die gespeicherte Kamera-Adresse bleibt erhalten.

            Im Demo-Modus zeigt die linke Bühne den blauen Bildausschnitt, rechts steht das simulierte Kamerabild. Die gleichen Buttons und Tastenkürzel steuern Bewegung, Zoom und vorbereitete Presets. Es werden keine Kamerabefehle über das Netzwerk gesendet. Die schematische Darstellung ist kein Live-Video.

            Zum Beenden unter „Demo-Modus“ die Kamera einrichten und die Adresse speichern. Dies aktiviert wieder den Kamerabetrieb. Eigene Start- und Abschlusspositionen der Demo gelten nur für die Sitzung.

            Positionen aufrufen
            Die großen Buttons rufen die in der Kamera gespeicherten Positionen auf. Die folgenden Namen sind die Vorgaben; eigene Namen können in den Einstellungen gespeichert werden. Die Nummern und Tastenkürzel bleiben gleich.

            Button / Taste → Presetnummer der Kamera
            Redner / 1 → 1
            Redner + Leser / 2 → 2
            Tisch / 3 → 3
            komplette Bühne / 4 → 4
            Redner + Bildschirm / 5 → 5
            Demo stehend / 6 → 6
            Standby / 9 → 9

            „Demo stehend“ ist der vorgegebene Name einer normalen Kameraposition und kein Simulationsmodus.

            Buttons und Presets bearbeiten
            „Einstellungen“ → „Buttons & Presets“ → „Bearbeiten …“ beim gewünschten Button wählen. „Nur Namen speichern“ ändert ausschließlich den Buttontext. Namen dürfen 1 bis 32 Zeichen enthalten. Ungespeicherte Namen werden beim Schließen verworfen.

            Zum Einrichten einer Position die Kamera mit den Pfeil- und Zoombuttons ausrichten. „Gespeicherte Position anfahren“ lädt die bisherige Position. „Aktuelle Kameraposition … speichern“ überschreibt nach einer Bestätigung das betreffende Kamera-Preset. Die App stoppt zuvor die Bewegung. Ein erfolgreich gesendeter Speicherbefehl bestätigt noch nicht unabhängig die Speicherung; zum Prüfen die Position erneut anfahren.

            Ausgeführte Kamerabewegungen und gespeicherte Positionen werden beim Schließen nicht rückgängig gemacht. Alle sieben Buttons, einschließlich Preset 9, sind gleichwertig und können benannt und gespeichert werden. Echte Buttonnamen werden lokal für den nächsten Start gespeichert; die Positionen speichert die Kamera. Im Demo-Modus gelten eigene Namen und Positionen nur für die laufende Sitzung und verändern keine echte Kamera-Konfiguration.

            Ein Fensterwechsel bricht wartende Preset-Aktionen ab und versucht alle Kanäle zu stoppen. Bereits an die Kamera übertragene Befehle lassen sich dadurch nicht rückgängig machen. Wird beim Schließen des Preset-Editors ein Stopp nicht bestätigt, bleibt die Fehlermeldung im Hauptfenster sichtbar.

            Bewegung und Zoom
            Einen Richtungspfeil oder „Zoom In“ / „Zoom Out“ mit der linken Maustaste gedrückt halten. Loslassen stoppt den jeweiligen Kanal. Alternativ die Pfeiltasten zum Bewegen und Plus/Minus auf dem Nummernblock zum Zoomen gedrückt halten. Die Zahlentasten 1–6 und 9 rufen die oben genannten Positionen auf.

            Beim Fensterwechsel und Öffnen der Einstellungen versucht die App, Bewegung, Zoom und Fokus zu stoppen. Während eines Preset-Ablaufs oder eines vollständigen Stopps sind die Kamerabuttons gesperrt.

            Automatische Positionen beim Start und Beenden
            Unter „Buttons & Presets“ nach unten zu „Automatische Positionen“ scrollen. Für Start und Beenden jeweils „Keine Position“ oder einen der sieben Buttons auswählen. Erst „Automatik speichern“ übernimmt die Auswahl; Schließen verwirft ungespeicherte Änderungen. Neue Einrichtungen haben keine automatische Fahrt. Bestehende Einrichtungen behalten zunächst Preset 4 beim Start und Preset 9 beim Beenden.

            Beim Beenden versucht die App zuerst alle Kanäle zu stoppen und fährt nur nach bestätigten Stopps das ausgewählte Abschluss-Preset an. „Standby“ ist lediglich der vorgegebene Name von Preset 9; es gibt keine besondere Fokusfahrt oder Warteanimation mehr. Die App wartet auf die HTTP-Antwort, kann aber ohne Positionsrückmeldung nicht bestätigen, wann die Kamera ihr Ziel erreicht hat. Im Demo-Modus lässt sich die Abschlussfahrt an der Simulation ausprobieren; eine gespeicherte Startposition wird beim nächsten Einstieg in den Demo-Modus angefahren.

            Wenn keine Verbindung zustande kommt
            Die Meldung im Hauptfenster unterscheidet Verbindungsfehler, Zeitüberschreitung und abgelehnte HTTP-Befehle. Prüfen, ob die Kamera eingeschaltet und erreichbar ist, die IPv4-Adresse stimmt und ihre HTTP-Steuerung zu CamControl passt. Anschließend kann der gewünschte Befehl erneut ausgelöst werden.

            Wird ein Stopp nicht bestätigt, kann die Kamera weiterlaufen. Netzwerk und Kamera prüfen und bei Bedarf die Bedienmöglichkeit der Kamera verwenden.
            """;
    }
}
