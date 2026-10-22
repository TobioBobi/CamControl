# CamControl 1.3.0

Vorbereitet am 22. Oktober 2026. App-Version: **1.3.0**;
MSIX-Version: **1.3.0.0**. Gemeinsame Zielversion für die nächste Veröffentlichung;
weitere Features dieses Releases ergänzen diese Datei und den Changelog.

## Text für „Neu in dieser Version“

CamControl ist jetzt auf Deutsch und Englisch verfügbar – einschließlich Hilfe,
Datenschutz, Demo und Fehlermeldungen. Die App folgt deiner Windows-Anzeigesprache.
Unter „Einstellungen > Sprache / Language“ kannst du eine eigene Sprache wählen.
Die gespeicherte Auswahl wird beim nächsten App-Start übernommen.

## Pakete und Veröffentlichung

- Lokaler Test: `artifacts/msix/test/x64/CamControl-1.3.0.0-x64-test.msix`
- Store: `artifacts/msix/store/x64/CamControl-1.3.0.0-x64-store.msix`

Version, Manifest und Versionshinweise sind bereits im Feature-Branch vorbereitet.
Nach erfolgreicher Abnahme auf `develop` ist der Merge nach `master` der einzige
Veröffentlichungsschritt. Die Pipeline baut und testet den Stand, erstellt das
Store-Paket und reicht es bei eingerichteter Store-Anbindung zur Zertifizierung
ein. Die Veröffentlichung folgt nach erfolgreicher Microsoft-Zertifizierung.

Die deutschen Versionshinweise werden automatisch eingereicht. Die englische
Store-Beschreibung unter [store-listing.en.md](store-listing.en.md) ist ein Entwurf
für einen neuen Spracheintrag in Partner Center; die Pipeline legt diesen nicht an.

## Abnahme

- Deutsch und Englisch: Einrichtung, alle Einstellungsseiten, Preset-Editor,
  Bestätigung, Hilfe, Datenschutz, Demo und Screenreader-Texte prüfen.
- Automatische Erkennung, manuelle Auswahl, Speichern und Übernahme beim nächsten
  Start sowie englische Rückfallsprache prüfen.
- Eigene Preset-Namen, Kamerazuordnungen und regionale Zahlenformate bleiben erhalten.
- Debug-/Release-Builds, SettingsCheck, CameraUiCheck und die Offline-Prüfungen
  der Release-/Store-Automatisierung ausführen; MSIX-Inhalt prüfen.

Paketbau: [MSIX-Anleitung](msix.md).
Workflow: [Branches und Veröffentlichungen](branch-workflow.md).
Einreichung: [Store-Deployment](store-deployment.md).
