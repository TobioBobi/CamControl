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

            Jeder Button ruft seine zugeordnete Kamerapreset-Nummer auf. Tastenkürzel bleiben an den Button gebunden. Die App legt keine Kamerapositionen automatisch an. Vor dem ersten Aufruf die gewünschte Position einrichten.

            Unter „Buttons & Presets“ Namen und Kamerapreset-Nummer festlegen und „Zuordnung speichern“. Nummern: 0–89 oder 100–254, sofern die Kamera sie unterstützt. Mehrere Benutzer sprechen ihre Nummernbereiche ab. Doppelte Nummern innerhalb eines Windows-Kontos sind gesperrt.

            Kamera ausrichten und „Position speichern“ wählen. Das überschreibt nach Bestätigung das zugeordnete Kamerapreset. Mit „Position anfahren“ prüfen. Namen und Zuordnungen bleiben pro Windows-Konto lokal; Positionen speichert die Kamera.

            Automatische Positionen
            Unter „Einstellungen > Automatische Positionen“ eine Start- oder Endposition wählen und „Automatik speichern“. Ohne eigene Auswahl gibt es keine automatische Fahrt. Beim Beenden wird die Endposition nur nach bestätigtem Stopp angefahren. Eine HTTP-Antwort bestätigt nicht, dass die Kamera ihr Ziel erreicht hat.

            Demo-Modus
            Unter „Demo-Modus“ ohne Kamera ausprobieren. Links: Bühne mit Bildausschnitt; rechts: simuliertes Kamerabild, kein Live-Video. Es werden keine Netzwerkbefehle gesendet. Demo-Namen, Zuordnungen, Positionen und Automatik gelten nur für die Sitzung; deine Kameraeinstellungen bleiben erhalten.

            Verbindungsprobleme
            IP-Adresse, Netzwerk und Stromversorgung prüfen. HTTP-Fehler können auf eine inkompatible Kamerasteuerung hinweisen.

            Beim Fensterwechsel und Öffnen der Einstellungen versucht die App, die Kamera zu stoppen. Wird ein Stopp nicht bestätigt, kann sie weiterlaufen: Kamera prüfen und bei Bedarf deren eigene Steuerung verwenden.
            """;
    }
}
