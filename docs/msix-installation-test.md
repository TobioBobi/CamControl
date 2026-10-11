# Lokaler MSIX-Installationstest

Durchgeführt am 11. Oktober 2026 auf Tobias' Windows-PC (x64), App-Code aus
Commit `39ea0e5`. Der Test ersetzt weder das Windows App Certification Kit noch
die Microsoft-Store-Zertifizierung oder einen Test mit echter Kamera.

## Ergebnisse

- Das signierte Originalpaket `CamControl-1.1.0.0-x64-test.msix` wurde mit
  `Add-AppxPackage` erfolgreich installiert. Windows meldete Paketstatus `Ok`.
- Der Startmenü-Eintrag **CamControl (Test)** wurde mit der korrekten App-ID
  registriert. Die Aktivierung startete die installierte EXE aus `WindowsApps`.
- Ein gezielter Start mit `--demo` erzeugte ein reagierendes Demo-Fenster.
- Ein lokales Diagnoseprogramm prüfte die tatsächlich installierte
  `CamControl.dll` unter bestätigter MSIX-Paketidentität. Demo-Start,
  Preset-Aufruf und reguläres Schließen über die WinForms-Nachrichtenschleife
  waren erfolgreich. Ein gerendertes Bild der Demo wurde visuell geprüft.
- Kamera-IP, Buttonname und Automatik-Auswahl wurden gespeichert und in einem
  neuen Prozess wieder geladen. Nach dem Update von Testversion `1.1.2.0` auf
  `1.1.3.0` blieben alle drei Einstellungen erhalten; die Demo funktionierte
  weiterhin. Beide Versionen enthielten denselben App-Code und zusätzlich das
  temporäre Diagnoseprogramm mit eigener .NET-Laufzeit. Das reguläre
  Build-Script und die App-Version wurden dafür nicht geändert.
- Nach `Remove-AppxPackage` waren Paketregistrierung, Startmenü-Eintrag und
  Installationsverzeichnis entfernt. Alle eigenen Testprozesse waren beendet.
- Vorhandene Kameraeinstellungen wurden gesichert und nach dem Test bytegenau
  wiederhergestellt (SHA-256-Vergleich). Die im Test neu angelegte Datei für
  Buttonnamen wurde ebenfalls entfernt. Das zuvor autorisierte Testzertifikat
  bleibt für weitere lokale Tests vertrauenswürdig installiert.

## Grenzen und Hinweise

Die Computer-Use-Automation konnte die Paketfenster nicht erfassen. Deshalb
wurde die vollständige sichtbare Bedienung des installierten Programms nicht
automatisch durchgeklickt. Die Funktionsprüfung verwendete dessen echte Forms
und Einstellungen im Paketkontext; sie bestätigt keine Kamera-Kompatibilität.

Im Diagnosekontext lagen die Einstellungen im bestehenden
`%LOCALAPPDATA%\CamControl`-Ordner. Ein getrenntes Datenverzeichnis und das
Löschen bestehender Konfiguration durch Windows-Reset oder Deinstallation
sind deshalb keine bestätigten Eigenschaften dieses Tests.

Die Diagnosepakete und Ergebnisse liegen ausschließlich im ignorierten
`artifacts/msix/test`-Verzeichnis und sind keine Store-Pakete. Vor Einreichung
das finale Paket mit der Partner-Center-Identität erneut installieren und mit
dem Windows App Certification Kit prüfen.
