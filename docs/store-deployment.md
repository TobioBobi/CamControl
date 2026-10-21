# Automatisches Store-Deployment von master

Nach einem Merge beziehungsweise jedem Push auf `master` führt der Workflow
`.github/workflows/dotnet-desktop.yml` diese Schritte aus:

1. Debug und Release bauen; Einstellungen, Kameraservice, WPF-Oberfläche und
   die Store-Automatisierung testen.
2. Das lokale MSIX-Testpaket bauen und als Artifact ablegen.
3. Erst nach erfolgreichem Build-Job das x64-Store-Paket mit der echten
   Partner-Center-Identität bauen und prüfen.
4. Das Store-Paket als Artifact ablegen, hochladen und zur Zertifizierung
   einreichen. `targetPublishMode = Immediate` sorgt nach erfolgreicher
   Microsoft-Prüfung für die automatische Veröffentlichung.

Änderungen werden zuerst auf `develop` gesammelt und als Release-PR nach
`master` gebracht. Der [Branch-Ablauf](branch-workflow.md) beschreibt den Prozess.
Pushes auf `develop`, Pull Requests nach `develop` oder `master` und
`workflow_dispatch` führen ausschließlich den Build-Job aus.
Der Store-Job und das Upload-Script verlangen ausdrücklich `push` auf
`refs/heads/master`. Das Script benötigt nur Python 3.13 und dessen
Standardbibliothek. Der Store-Job hat eine eigene Concurrency-Gruppe und bricht
eine laufende Übertragung bei weiteren Merges nicht ab. GitHub kann ältere
wartende Läufe durch neuere ersetzen; laufende Einreichungen bleiben erhalten.

## Einmalige Einrichtung

### Microsoft Partner Center

1. CamControl muss als **MSIX-App** im Partner Center existieren. Die erste
   Einreichung einschließlich Altersfreigaben muss dort abgeschlossen und eine
   Version veröffentlicht sein. Das Script aktualisiert die letzte
   veröffentlichte Einreichung mit den vorhandenen Store-Einträgen.
2. Das Partner-Center-Konto mit einem Microsoft-Entra-Verzeichnis verknüpfen.
   Eine Entra-Anwendung unter Kontoeinstellungen > Benutzer hinzufügen und
   ihr die von Microsoft für die Submission API verlangte Rolle **Manager**
   zuweisen. Tenant-ID, Client-ID und einen Client-Secret-Wert erzeugen/kopieren.
   Zum Einrichten des Verzeichnisses sind entsprechende Administratorrechte nötig.
3. Die **Store-ID** der App und unter App-Identität die exakten Werte für
   Paketname, Publisher, Publisher-Anzeigename und reservierten App-Namen kopieren.
   Store-ID und Paketname sind unterschiedliche Angaben.

Die jeweiligen Kontofunktionen müssen die Submission API unterstützen.
Bei API-Einschränkungen, beispielsweise verpflichtenden Updates, kann eine
manuelle Einreichung erforderlich bleiben. Maßgeblich sind Microsofts aktuelle
[Voraussetzungen und Einschränkungen](https://learn.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services).

### GitHub-Konfiguration

Im Repository **Settings > Secrets and variables > Actions** einrichten:

| Art | Name | Inhalt |
| --- | --- | --- |
| Variable | `STORE_TENANT_ID` | Entra-Tenant-ID als GUID |
| Variable | `STORE_CLIENT_ID` | Client-ID der Entra-Anwendung als GUID |
| Secret | `STORE_CLIENT_SECRET` | Secret-**Wert**, nicht die Secret-ID |
| Variable | `STORE_APP_ID` | Store-Produkt-ID von CamControl, etwa `9N…` |
| Variable | `STORE_IDENTITY_JSON` | JSON mit den vier Identitätsfeldern, siehe unten |

Für `STORE_IDENTITY_JSON` die Struktur aus
[`packaging/StoreIdentity.example.json`](../packaging/StoreIdentity.example.json)
verwenden und alle Platzhalter durch die tatsächlichen Partner-Center-Werte
ersetzen. Die Identität enthält keine Zugangsdaten. Das Workflow-Script erzeugt
daraus die ignorierte `packaging/StoreIdentity.json`.

Ohne vollständige Konfiguration schlägt der Store-Job mit den Namen der fehlenden
Variablen fehl. Er überspringt den Upload nicht stillschweigend. Zugangsdaten
werden nur an Konfigurationsprüfung und Submission-Schritt übergeben. Das
Store-Paket bleibt unsigniert; Microsoft signiert es für den Store.

## Versionen und Ergebnis

Vor jedem Merge, der ein neues Store-Paket veröffentlichen soll,
`CamControl/CamControl.csproj` auf eine höhere Version setzen und Changelog sowie
Manifestvorlage passend aktualisieren. Aus `1.2.1` entsteht beispielsweise
`1.2.1.0`. Die vierte Stelle bleibt für den Store `0`.

Gleiche oder niedrigere bereits veröffentlichte x64-Versionen werden vor dem
Erzeugen einer neuen Einreichung abgewiesen. Es gibt keine automatische
Versionsänderung und keinen automatischen Versions-Commit auf `master`.

Der Upload ersetzt die bisherigen x64-Pakete durch das neue x64-Paket. Pakete
anderer Architekturen, Store-Beschreibungen, Bilder, Preise, Sichtbarkeit und
bestehende Rollout-Einstellungen werden aus der veröffentlichten Einreichung
übernommen. Eine zusätzliche Bestätigung zur Veröffentlichung ist nicht
vorgesehen: Nach Microsofts Zertifizierung gilt der Veröffentlichungsmodus
`Immediate`.

Der Workflow wartet bis zur bestätigten Übernahme in die Store-Verarbeitung,
maximal etwa zehn Minuten. Ein grüner Lauf bestätigt **Upload und Einreichung**,
nicht bereits die endgültige Zertifizierung oder Verfügbarkeit im Store.
Einreichungs-ID, Paketversion und Verarbeitungsstatus stehen im Log und in der
GitHub-Job-Zusammenfassung. Spätere Zertifizierungsfehler im Partner Center prüfen.

## Fehlgeschlagene oder parallele Einreichungen

Existiert bereits eine offene Einreichung, bricht das Script ab. Vorhandene
Entwürfe werden weder gelöscht noch überschrieben. Eine laufende Prüfung zuerst
abschließen lassen, danach bei Bedarf den fehlgeschlagenen `push`-Lauf erneut
ausführen. Ein manueller neuer `workflow_dispatch`-Lauf veröffentlicht nichts.

Bei Fehlern nach dem Erzeugen einer Einreichung bleibt sie mit ihrer im Log
genannten ID zur Prüfung bestehen. Vor erneutem Ausführen den Zustand prüfen;
einen nicht mehr benötigten API-Entwurf gezielt entfernen. API-Einreichungen
während ihrer Bearbeitung nicht über die Partner-Center-Oberfläche ändern:
Microsoft beschreibt, dass dadurch weitere API-Änderungen scheitern können.
Es gibt bewusst keine automatischen Wiederholungen schreibender API-Aufrufe,
weil eine unterbrochene Antwort einen bereits ausgeführten Upload/Commit
bedeuten kann.

## Offline-Prüfungen

```powershell
python -m unittest discover -s Tests/StoreSubmissionCheck -v
```

Diese Tests verwenden erzeugte Test-MSIX-Dateien und nachgebildete API-Antworten.
Sie prüfen Branch-/Event-Begrenzung, Identität, Versionsreihenfolge, Erhalt von
Store-Daten, Upload vor Commit sowie Fehler und Timeouts ohne echte Zugangsdaten.
Den ersten echten Store-Upload kann erst ein vollständig eingerichteter
`master`-Push prüfen.

Microsoft-Dokumentation:

- [Submission API einrichten](https://learn.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services)
- [Einreichungen erstellen, hochladen und committen](https://learn.microsoft.com/en-us/windows/uwp/monetize/manage-app-submissions)
- [Daten einer Einreichung aktualisieren](https://learn.microsoft.com/en-us/windows/uwp/monetize/update-an-app-submission)
