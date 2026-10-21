# MSIX für CamControl

Der PowerShell-Build veröffentlicht ausschließlich CamControl in **Release**
mit mitgelieferter .NET-10-Laufzeit und erstellt ein MSIX mit Microsofts
MakeAppx. CamDummy, Tests, gespeicherte Einstellungen und private Schlüssel
gehören nicht zum Paket. Die Windows-Paketwerkzeuge werden in einer festgelegten
Version über NuGet bezogen; Visual Studio ist für den Paket-Build nicht nötig.
Voraussetzungen für den Build: Windows auf x64, PowerShell und .NET-10-SDK.
Das Standardpaket ist für x64; `-Architecture arm64` beziehungsweise `x86`
ermöglicht separate Pakete, die auf passenden Geräten getestet werden müssen.
Jeder Build prüft außerdem Paketinhalt, Laufzeit-Metadaten, Einstiegspunkt und
die Übereinstimmung von EXE-Architektur und Manifest.

## Lokales Testpaket

Im Repository-Verzeichnis:

```powershell
./packaging/Build-Msix.ps1
```

Ergebnis: `artifacts/msix/test/x64/CamControl-1.2.1.0-x64-test.msix`.
Die Version wird aus `CamControl.csproj` gelesen. Die vierte MSIX-Versionsstelle
ist 0. Der Anzeigename ist **CamControl (Test)**, die Paketidentität
**CamControl.LocalTest**. Dieses Paket ist kein Store-Paket.

Für die lokale Installation ist eine Testsignatur erforderlich:

```powershell
$testCertificate = ./packaging/New-TestCertificate.ps1
./packaging/Build-Msix.ps1 -CertificateThumbprint $testCertificate.Thumbprint
```

Das Script erzeugt beziehungsweise verwendet einen Testschlüssel in
`Cert:\CurrentUser\My`. Der private Schlüssel ist nicht exportierbar.
Nur das öffentliche Zertifikat wird nach `artifacts/msix/test/CamControl-Test.cer`
exportiert. Das Script installiert **keine Vertrauensstellung**.

Für einen Installationstest muss das Zertifikat auf dem Test-PC ausdrücklich
als vertrauenswürdig installiert werden. Die folgenden Schritte ändern den
Windows-Zertifikatsspeicher; dafür eine PowerShell **als Administrator** öffnen:

```powershell
Import-Certificate -FilePath ./artifacts/msix/test/CamControl-Test.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

Anschließend als normaler Benutzer:

```powershell
Add-AppxPackage -Path ./artifacts/msix/test/x64/CamControl-1.2.1.0-x64-test.msix
```

Im Startmenü **CamControl (Test)** öffnen und **Ohne Kamera ausprobieren** wählen.
Für einen garantierten Demo-Start lässt sich die installierte Paket-EXE auch
mit `--demo` starten. Das Testzertifikat ersetzt keine Store-Signatur.
Zum Entfernen: die Test-App in Windows deinstallieren und das importierte
Testzertifikat anhand seines Thumbprints aus `LocalMachine\TrustedPeople`
entfernen. Die Test-App kann eigene lokale Daten haben; bereits vor MSIX
vorhandene Konfiguration ist davon getrennt zu betrachten.

## Store-Paket

Im Partner Center ein Entwicklerkonto einrichten, den App-Namen reservieren
und unter der App-Identität die exakten Werte übernehmen. Eine Kopie von
`packaging/StoreIdentity.example.json` als `packaging/StoreIdentity.json` mit
`Name`, `Publisher`, `PublisherDisplayName` und `DisplayName` ausfüllen.
`Publisher` ist die technische Kennung von Microsoft und darf nicht einfach
durch `CN=Tobias Heer` ersetzt werden. Die lokale JSON-Datei ist Git-ignoriert.

```powershell
./packaging/Build-Msix.ps1 -Mode Store -StoreIdentityPath ./packaging/StoreIdentity.json
```

Das Script verweigert fehlende Angaben und Beispiel-Platzhalter. Das Ergebnis
unter `artifacts/msix/store/x64/` ist unsigniert für die Store-Einreichung.
Microsoft signiert das MSIX im Veröffentlichungsprozess nach erfolgreicher
Zertifizierung; dafür ist kein gekauftes Codesignatur-Zertifikat nötig.

Die Manifest-Capability `runFullTrust` ist für die klassische WPF-Desktop-App
erforderlich. Sie startet mit normalen Benutzerrechten, nicht erhöhten
Administratorrechten. Die App steuert ausschließlich auf Benutzeraktionen
beziehungsweise konfigurierte Start-/Endpositionen hin die Kamera über HTTP.
Für die Store-Zertifizierung die Demo und diese Verwendung der Capability
in den Prüferhinweisen erklären.

Installation, Update mit Datenerhaltung und Deinstallation wurden lokal
geprüft; Einzelheiten und Grenzen stehen im
[Installationstest](msix-installation-test.md) und im
[Update-Test 1.1.0 auf 1.2.0](msix-upgrade-1.2.0-test.md). Das endgültige Store-Paket mit
seiner eigenen Identität muss erneut getestet werden.

Vor Einreichung offen: Windows App Certification Kit, Hardwaretests, bestätigte
Kamerakompatibilität, Store-Name/Metadaten, Screenshots und öffentliche
Datenschutz-URL. Das Manifest setzt Windows 10 Build 19041 als technische
Untergrenze; eine unterstützte Windows-Version bleibt erforderlich.

## GitHub Actions

Der vorhandene Workflow erzeugt nach Build und Tests ein **unsigniertes x64-
Testpaket** und stellt es als Artifact bereit. Es werden weder Zertifikate
noch Schlüssel hochgeladen. Das Paket wird nicht automatisch veröffentlicht.
Store- und Test-Builds sind getrennt.

## Microsoft-Dokumentation

- [MSIX manuell erstellen](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion)
- [MakeAppx und seine Validierung](https://learn.microsoft.com/en-us/windows/msix/package/create-app-package-with-makeappx-tool)
- [Store-Paketdaten und Signierung](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements)
- [MSIX-Signierung](https://learn.microsoft.com/en-us/windows/msix/package/sign-app-package-using-signtool)
