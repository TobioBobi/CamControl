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

            Positionen aufrufen
            Die großen Buttons rufen bereits in der Kamera gespeicherte Positionen auf. Ihre Namen beschreiben die bisherige Einrichtung; für eine andere Kamera müssen die passenden Positionen dort bereits gespeichert sein.

            Button / Taste → Presetnummer der Kamera
            Redner / 1 → 1
            Redner + Leser / 2 → 2
            Tisch / 3 → 3
            komplette Bühne / 4 → 4
            Redner + Bildschirm / 5 → 5
            Demo stehend / 6 → 6
            Standby / 9 → 9

            „Demo stehend“ ist eine normale Kameraposition und kein Simulationsmodus. Buttonnamen und Presets sind derzeit fest vorgegeben. Neue Positionen können mit CamControl noch nicht in der Kamera gespeichert werden.

            Bewegung und Zoom
            Einen Richtungspfeil oder „Zoom In“ / „Zoom Out“ mit der linken Maustaste gedrückt halten. Loslassen stoppt den jeweiligen Kanal. Alternativ die Pfeiltasten zum Bewegen und Plus/Minus auf dem Nummernblock zum Zoomen gedrückt halten. Die Zahlentasten 1–6 und 9 rufen die oben genannten Positionen auf.

            Beim Fensterwechsel und Öffnen der Einstellungen versucht die App, Bewegung, Zoom und Fokus zu stoppen. Während eines Preset-Ablaufs oder eines vollständigen Stopps sind die Kamerabuttons gesperrt.

            Start, Standby und Schließen
            Beim Start ruft die App die Position „komplette Bühne“ (Preset 4) auf. Standby ruft Preset 9 auf und führt anschließend eine Fokusfahrt aus: nach vier Sekunden wird der Fokus fünf Sekunden lang verändert und dann gestoppt. Beim Schließen versucht die App zuerst alle Kanäle zu stoppen und führt Standby nur nach bestätigten Stopps aus. Das Schließen kann deshalb einige Sekunden dauern.

            Wenn keine Verbindung zustande kommt
            Die Meldung im Hauptfenster unterscheidet Verbindungsfehler, Zeitüberschreitung und abgelehnte HTTP-Befehle. Prüfen, ob die Kamera eingeschaltet und erreichbar ist, die IPv4-Adresse stimmt und ihre HTTP-Steuerung zu CamControl passt. Anschließend kann der gewünschte Befehl erneut ausgelöst werden.

            Wird ein Stopp nicht bestätigt, kann die Kamera weiterlaufen. Netzwerk und Kamera prüfen und bei Bedarf die Bedienmöglichkeit der Kamera verwenden.
            """;
    }
}
