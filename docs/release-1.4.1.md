# CamControl 1.4.1

Vorbereitet am 8. November 2026. App-Version: **1.4.1**;
MSIX-Version: **1.4.1.0**. Gemeinsame Zielversion für die nächste Veröffentlichung.

## Text für „Neu in dieser Version“

CamControl merkt sich jetzt im Kamerabetrieb die Fenstergröße, Position und
Maximierung. Auch kleine Ansichten bleiben nach einem Neustart erhalten;
mit einem Klick kehren Sie zur letzten Vollansichtsgröße zurück. Nach Änderungen
an Monitoren oder Skalierung bleibt das Fenster erreichbar. Der Demo-Modus
verändert Ihre gespeicherten Fensterdaten nicht.

## Entwicklung

- Repository-Konfiguration für Engineering-Skills: GitHub Issues,
  Standard-Triage-Labels und Regeln für Glossar und Architekturentscheidungen.
- Glossar und ADRs werden bei Bedarf angelegt.
- Fensterdaten getrennt von Kameraeinstellungen in `window-placement.json`;
  fehlende, beschädigte oder unbrauchbare Werte verhindern den Start nicht.
- Maximierter Start erhält die normale Größe und Position; minimiertes Beenden
  führt beim nächsten Start zu einem sichtbaren Fenster.
- Kamera-/Demo-Wechsel bewahrt die Fensterdaten des Kamerabetriebs.

## Pakete und Veröffentlichung

- Lokaler Test: `artifacts/msix/test/x64/CamControl-1.4.1.0-x64-test.msix`
- Store: `artifacts/msix/store/x64/CamControl-1.4.1.0-x64-store.msix`

Feature-PR nach `develop`; Veröffentlichung durch Merge von `develop` nach `master`.
Historische Release-Dateien bleiben unverändert.

## Abnahme

- Release-Vorbereitung mit `python packaging/check_release.py --base-ref origin/master`
  prüfen und `python -m unittest discover -s Tests/StoreSubmissionCheck -v` ausführen.
- Vor der Veröffentlichung die Build-, App- und Paketprüfungen aus README.md ausführen.
- Automatische WPF-Tests prüfen Neustarts in Normal-, Kompakt-, Minimal- und
  maximierter Ansicht, minimiertes Beenden, Vollansichts-Wiederherstellung,
  beschädigte Dateien, entfernte Monitorpositionen und Demo-Trennung.
- Auf echten Monitoren noch prüfen: Wechsel zwischen unterschiedlichen
  DPI-Skalierungen, Monitor entfernen, Arbeitsfläche/Auflösung ändern und
  Sichtbarkeit von Titelleiste, Schließen und allen sieben Presets bestätigen.

Paketbau: [MSIX-Anleitung](msix.md).
Workflow: [Branches und Veröffentlichungen](branch-workflow.md).
