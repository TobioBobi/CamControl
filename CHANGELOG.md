# Änderungen

## 1.4.0 – vorbereitet am 31. Oktober 2026

- Automatische Voll-, Kompakt- und Minimalansicht mit Kachelblock, horizontaler
  Leiste oder vertikaler Spalte. Alle sieben Presets bleiben direkt bedienbar.
- Kleine Richtungstasten und Zoomtasten als Zwischenstufe; Layoutwechsel stoppen
  laufende manuelle Eingaben und geben den Mausfang frei.
- Rückkehr zur letzten Vollansichtsgröße und Position mit einem Klick; kurze
  Presetnamen mit vollständigen Tooltips und zugänglichen Beschriftungen.
- Hinweise und Kamerafehler werden nur in der Vollansicht angezeigt.
  Die Demovorschau vergrößert das Fenster nicht mehr automatisch.
- Pfeil- und Zoomtasten der Leiste sind direkt nebeneinander ausgerichtet;
  schmale Spalten blenden die Steuerung bei ausreichender Höhe zuverlässig ein.

Hinweise zur Vorbereitung und Abnahme: [Release 1.4.0](docs/release-1.4.0.md).

## 1.3.0 – vorbereitet am 22. Oktober 2026

- Deutsche, englische und spanische Oberfläche einschließlich Einrichtung, Hilfe,
  Datenschutz, Demo, Fehlermeldungen und Screenreader-Beschriftungen.
- Automatische Spracherkennung und gespeicherte Auswahl unter
  „Einstellungen > Sprache / Language“; Änderungen gelten beim nächsten Start.
- Gemeinsame Sprachressourcen als Grundlage für weitere Übersetzungen.
- Version und Release Notes werden bereits in Feature-/Fix-Branches für die
  gemeinsame Zielversion vorbereitet. Die CI prüft Projektversion,
  MSIX-Manifestvorlage und Store-Versionshinweise vor dem Merge.

Hinweise zur Vorbereitung und Abnahme: [Release 1.3.0](docs/release-1.3.0.md).

## 1.2.2 – 22. Oktober 2026

- Deutsche Store-Versionshinweise werden aus der passenden Release-Datei
  geprüft und automatisch mit dem Paket eingereicht.

Hinweise zur Vorbereitung und Abnahme: [Release 1.2.2](docs/release-1.2.2.md).

## 1.2.1 – 22. Oktober 2026

- Unnötiges Neuzeichnen bei der Kamerabedienung reduziert: Preset-Beschriftungen
  werden weiterverwendet; unveränderte Farben und Titelleisten werden nicht
  erneut aktualisiert.
- Automatische Microsoft-Store-Einreichung nach erfolgreichen Builds und Tests
  für Releases auf `master`; Änderungen werden zuvor auf `develop` gesammelt.

Hinweise zur Vorbereitung und Abnahme: [Release 1.2.1](docs/release-1.2.1.md).

## 1.2.0 – 15. Oktober 2026

- Neue, kompakte WPF-Oberfläche mit großen Preset-Tasten und gruppierter
  Richtungs- und Zoomsteuerung.
- Einheitliche Gestaltung von Einstellungen, Preset-Editor, Einrichtung und
  Bestätigungsdialogen.
- Dunkles Farbschema als Standard. Dunkel, Hell und Windows-Einstellung sind
  unter „Einstellungen > Darstellung“ auswählbar und werden gespeichert.
- Passende Titelleiste und Berücksichtigung von Windows-Kontrastdesigns.
- Verbesserte Abstände und Ausrichtung; zweizeilige Kamerafehler erzeugen bei
  normaler Fenstergröße keinen unnötigen Scrollbalken.
- Kamera-IP, Preset-Namen und -Zuordnungen sowie automatische Positionen werden
  im bisherigen Format weiterverwendet. Kamera-Protokoll und Tastenkürzel
  bleiben erhalten.

Hinweise zur Vorbereitung und Abnahme: [Release 1.2.0](docs/release-1.2.0.md).
