# Settings and camera service checks

```powershell
dotnet run --project Tests/SettingsCheck/SettingsCheck.csproj -c Release
```

Uses .NET 10 without a UI framework. Source links exercise the production
settings, HTTP service, error runner and demo simulation. Fake handlers and
temporary files in the build directory prevent real camera commands or changes
to user configuration. Use Release because Debug intentionally selects CamDummy.

Covers IPv4 validation and normalization; settings persistence, fallback and
migration; configurable preset names/numbers; automatic-position defaults and
persistence; exact command URLs; HTTP failures, timeouts and response disposal;
independent direction/zoom serialization; cancellation of pending and queued
starts; all-channel stops despite individual failures; and custom demo preset
positions including zoom. WPF dialog and controller checks live in
`Tests/CameraUiCheck`.

Localization checks cover language persistence and invalid settings, Windows
language variants and English fallback, explicit overrides, startup/background
thread UI culture, preserved regional formats, exact German/English resource
key parity and valid format placeholders, and localized help/privacy documents.
