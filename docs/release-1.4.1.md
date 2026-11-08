# CamControl 1.4.1

Vorbereitet am 8. November 2026. App-Version: **1.4.1**;
MSIX-Version: **1.4.1.0**. Gemeinsame Zielversion für die nächste Veröffentlichung.

## Text für „Neu in dieser Version“

Diese Version bereitet die weitere Pflege von CamControl vor. Die Kamerasteuerung
und die Oberfläche bleiben unverändert.

## Entwicklung

- Repository-Konfiguration für Engineering-Skills: GitHub Issues,
  Standard-Triage-Labels und Regeln für Glossar und Architekturentscheidungen.
- Glossar und ADRs werden bei Bedarf angelegt.

## Pakete und Veröffentlichung

- Lokaler Test: `artifacts/msix/test/x64/CamControl-1.4.1.0-x64-test.msix`
- Store: `artifacts/msix/store/x64/CamControl-1.4.1.0-x64-store.msix`

Feature-PR nach `develop`; Veröffentlichung durch Merge von `develop` nach `master`.
Historische Release-Dateien bleiben unverändert.

## Abnahme

- Release-Vorbereitung mit `python packaging/check_release.py --base-ref origin/master`
  prüfen und `python -m unittest discover -s Tests/StoreSubmissionCheck -v` ausführen.
- Vor der Veröffentlichung die Build-, App- und Paketprüfungen aus README.md ausführen.

Paketbau: [MSIX-Anleitung](msix.md).
Workflow: [Branches und Veröffentlichungen](branch-workflow.md).
