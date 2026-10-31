# CamControl 1.4.0

Vorbereitet am 31. Oktober 2026. App-Version: **1.4.0**;
MSIX-Version: **1.4.0.0**. Gemeinsame Zielversion für die nächste Veröffentlichung.

## Text für „Neu in dieser Version“

CamControl passt sich jetzt automatisch deiner Fenstergröße an: als kompakter
Kachelblock, horizontale Leiste oder schmale Spalte. Alle sieben Positionen bleiben
direkt erreichbar. Bei etwas mehr Platz stehen kleine Richtungs- und Zoomtasten
bereit. Ein Klick auf den Vergrößern-Button bringt die letzte Vollansicht zurück.
Vollständige Positionsnamen sind als Tooltip und für Screenreader verfügbar.
Hinweise und Kamerafehler werden in der Vollansicht angezeigt. Die Demovorschau
vergrößert das Fenster nicht mehr automatisch.

## Pakete und Veröffentlichung

- Lokaler Test: `artifacts/msix/test/x64/CamControl-1.4.0.0-x64-test.msix`
- Store: `artifacts/msix/store/x64/CamControl-1.4.0.0-x64-store.msix`

Feature-PR nach `develop`; Veröffentlichung durch Merge von `develop` nach `master`.
Historische Release-Dateien bleiben unverändert.

## Abnahme

- Vollansicht und alle sechs kleinen Varianten in Deutsch, Englisch und Spanisch
  prüfen, auch mit langen Namen, heller/dunkler Darstellung, Windows-Hochkontrast,
  vergrößerter Schrift und unterschiedlichen Monitor-DPI-Einstellungen.
- Langsam über die Layoutgrenzen ziehen: keine Größensprünge, stabile Übergänge,
  sieben erreichbare Presets und dauerhaft erreichbare Rückkehr zur Vollansicht.
- Maus gedrückt halten, Richtung und Zoom gleichzeitig über Tastatur bedienen,
  Layout wechseln, Mausfang/Fokus verlieren und schließen: Bewegung muss stoppen.
- Fehlermeldungen erst nach Vergrößern, Zuordnungen, Hervorhebung,
  Start-/Exit-Presets und Demo prüfen.
- Release-/Debug-Builds, SettingsCheck, CameraUiCheck und Release-/Store-Prüfungen
  ausführen. WPF-Prüfungen erzeugen Ansichten unter `artifacts/ui-preview`.
- Echte Kamerastopps und native Monitorwechsel zusätzlich auf Hardware abnehmen.

Paketbau: [MSIX-Anleitung](msix.md).
Workflow: [Branches und Veröffentlichungen](branch-workflow.md).
