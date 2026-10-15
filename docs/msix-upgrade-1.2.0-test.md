# MSIX-Update-Test: 1.1.0 auf 1.2.0

Am 15. Oktober 2026 auf Tobias' Windows-PC (x64) erfolgreich durchgeführt.
Geprüfter App-Code: Release 1.2.0, Stand `291f28f`.

## Testaufbau

Die installierte Store-App `TobioBobi.CamControl_1.1.0.0_x64__c9ntkvfng3n1a`
blieb installiert und wurde nicht verändert. Stattdessen wurde die separate
App `CamControl.LocalTest` erst als 1.1.0.0 installiert und dann mit
`Add-AppxPackage` direkt auf 1.2.0.0 aktualisiert, ohne vorherige Deinstallation.

Die Basis verwendete den Inhalt des alten Store-Pakets. Dessen `CamControl.dll`
stimmt per SHA-256 mit der tatsächlich installierten Store-App überein.
Für die lokale Testidentität wurden Paketname, Herausgeber und Anzeigename
angepasst. Die neue Version verwendete das aktuelle Testpaket.
Alle ursprünglichen Nutzdateien beider Versionen wurden per SHA-256 mit den
jeweiligen Store-Paketen verglichen; sie sind identisch. Unterschiede bestehen
in den Paketmetadaten und zusätzlichen Signatur-/Codeintegritätsdateien.

Beide Testpakete wurden mit dem bereits vorhandenen, vertrauenswürdigen
Testzertifikat signiert und mit SignTool validiert. Es wurde kein Zertifikat
angelegt und keine Vertrauensstellung verändert.

## Ergebnisse

- Windows registrierte 1.1.0.0 und nach dem Update 1.2.0.0 jeweils mit Status
  `Ok` und derselben Paketfamilie `CamControl.LocalTest_zggqfe30yp53y`.
- Die unveränderten installierten EXEs beider Versionen wurden mit `--demo`
  im Paketkontext gestartet. Beide erzeugten ein Demo-Fenster und schlossen
  über die reguläre Fensteraktion mit Exitcode 0.
- Vor dem Update wurden über die Persistenzfunktionen der alten installierten
  Assembly Testwerte gespeichert: Kamera-IP, Preset-Namen, eigene
  Kamerapreset-Nummern 20/21 sowie automatische Start-/Endpositionen.
- Nach dem Update waren die Konfigurationsdateien bytegenau unverändert.
  Die neue installierte Assembly las alle Testwerte korrekt wieder ein.
- In einem Diagnoseprozess mit bestätigter neuer Paketidentität wurden das
  tatsächliche WPF-Hauptfenster und seine Demo erstellt. Preset-Aufruf,
  Bewegung, Loslassen/Stopp und reguläres Schließen funktionierten. Ein
  Rendering der installierten WPF-Oberfläche wurde visuell geprüft.
- Die ursprüngliche Benutzerkonfiguration wurde im Paketkontext und zusätzlich
  außerhalb des Pakets wiederhergestellt und bytegenau verglichen.
- Die Test-App wurde anschließend entfernt. Die Store-App blieb auf 1.1.0.0
  mit derselben Paketidentität und Status `Ok`; keine Testprozesse blieben aktiv.

## Grenzen und Belege

Das ist ein tatsächlicher Windows-MSIX-Upgrade-Test mit identischen
Programmdateien unter einer separaten Testidentität. Die Microsoft-Store-
Signierung und -Auslieferung wurden damit nicht getestet: 1.2.0 ist noch nicht
veröffentlicht. Der Diagnoseprozess verwendet
`Invoke-CommandInDesktopPackage`; sein Token ist kein vollständiger Ersatz für
die normale Store-Aktivierung. Deshalb wurden zusätzlich beide Original-EXEs
aus ihren installierten Verzeichnissen gestartet und regulär geschlossen.

Alle Kameraaktionen liefen ausschließlich im Demo-Modus ohne Netzwerktransport.
Der Test bestätigt keine Hardware-Kompatibilität und ersetzt weder echte
DPI-/Monitorwechsel noch das Windows App Certification Kit.

Lokale Belege im ignorierten Verzeichnis `artifacts/update-check`:
`summary.json`, `seed.json`, `verify.json`, `restore.json`,
`payload-comparison.json`, die EXE-Prozessberichte, Signaturprotokolle und
`installed-wpf.png`. Dort liegen auch die Testskripte und die lokale Sicherung
der Benutzerkonfiguration; diese Dateien gehören nicht in ein Release-Paket.
