# Settings checks

Run on Windows with the .NET 10 SDK:

```powershell
dotnet run --project Tests/SettingsCheck/SettingsCheck.csproj -c Release
```

Use Release: Debug intentionally routes requests to the existing camera dummy.
The checks use a fake HTTP handler and a temporary settings file in the build
directory. They do not send camera commands or change the user's saved settings.
They cover IPv4 validation, persistence and fallback, the hidden click gesture,
dialog controls, address changes after the first request, and the destination of
an already running standby sequence. The standby checks take about nine seconds.
