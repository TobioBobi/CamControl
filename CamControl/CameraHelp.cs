namespace CamControl
{
    internal static class CameraHelp
    {
        public const string Text = """
            Voraussetzungen
            Windows-PC und PTZ-Kamera mit passender HTTP-CGI-Steuerung im erreichbaren Netzwerk. Das MSIX-Paket enthält .NET; andere Builds benötigen die .NET 10 Desktop Runtime.

            Entscheidend sind die unterstützten Befehle, unabhängig von Hersteller oder Modell: Bewegung, Zoom, Stopps und Presets über /cgi-bin/ptzctrl.cgi?ptzcmd&… . Benötigt werden IPv4 und HTTP auf Port 80 ohne Anmeldung. Nicht jede HTTP-CGI-Schnittstelle verwendet diesen Befehlssatz.

            Kamera einrichten
            Unter „Kamera“ die IP-Adresse ändern und speichern. Abbrechen verwirft die Eingabe.

            Bewegung und Presets
            Pfeile und Zoom-Buttons gedrückt halten; Loslassen stoppt. Tastatur: Pfeiltasten, Plus/Minus auf dem Nummernblock und 1–6 oder 9 für die Presets.

            Jeder Preset-Button ruft die entsprechende Nummer auf der Kamera auf. Die App legt keine Kamerapositionen automatisch an. Vor dem ersten Aufruf die gewünschte Position einrichten.

            Unter „Buttons & Presets“ einen Button bearbeiten: Namen vergeben (1–32 Zeichen), Kamera ausrichten und „Position speichern“ wählen. Das überschreibt nach Bestätigung das Kamera-Preset. Mit „Position anfahren“ prüfen, ob es gespeichert wurde.

            Buttonnamen bleiben lokal gespeichert; Positionen speichert die Kamera. Schließen verwirft ungespeicherte Namen, macht aber keine Bewegungen oder gespeicherten Positionen rückgängig.

            Automatische Positionen
            Unter „Buttons & Presets“ eine Start- oder Endposition wählen und „Automatik speichern“. Ohne eigene Auswahl gibt es keine automatische Fahrt. Beim Beenden wird die Endposition nur nach bestätigtem Stopp angefahren. Eine HTTP-Antwort bestätigt nicht, dass die Kamera ihr Ziel erreicht hat.

            Demo-Modus
            Unter „Demo-Modus“ ohne Kamera ausprobieren. Links: Bühne mit Bildausschnitt; rechts: simuliertes Kamerabild, kein Live-Video. Es werden keine Netzwerkbefehle gesendet. Demo-Namen, Positionen und Automatik gelten nur für die Sitzung; deine Kameraeinstellungen bleiben erhalten.

            Verbindungsprobleme
            IP-Adresse, Netzwerk und Stromversorgung prüfen. HTTP-Fehler können auf eine inkompatible Kamerasteuerung hinweisen.

            Beim Fensterwechsel und Öffnen der Einstellungen versucht die App, die Kamera zu stoppen. Wird ein Stopp nicht bestätigt, kann sie weiterlaufen: Kamera prüfen und bei Bedarf deren eigene Steuerung verwenden.
            """;
    }
}
