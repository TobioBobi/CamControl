# Settings checks

Run on Windows with the .NET 10 SDK:

```powershell
dotnet run --project Tests/SettingsCheck/SettingsCheck.csproj -c Release
```

Use Release: Debug intentionally routes requests to the existing camera dummy.
The checks use a fake HTTP handler and a temporary settings file in the build
directory. They do not send camera commands or change the user's saved settings.
They cover IPv4 validation, persistence and fallback, explicit edit mode,
saving and discarding dialog edits, address changes after the first request, and the destination of
an already running standby sequence. They also cover HTTP 400/403/500 errors,
network failures and timeouts for position, standby, movement, zoom and stop
commands, failed standby focus commands, response disposal and recovery after
errors. Cancellation checks cover pending and queued starts, both standby waits,
all stop channels being attempted despite a failure, and new commands waiting
for all stop responses. Movement and zoom are checked as independent channels;
cancelled presets must release every acquired gate. The standby checks take
about seventeen seconds in total.
