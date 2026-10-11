# CamControl

Windows Forms application for controlling a compatible PTZ camera over HTTP.

## Development

Requires Windows and the .NET 10 SDK. For Visual Studio development, use
Visual Studio 2026 with the .NET desktop development workload.
`global.json` selects a stable .NET 10 SDK and allows newer .NET 10 feature bands.

```powershell
dotnet build CamControl.sln -c Release
dotnet run --project Tests/SettingsCheck/SettingsCheck.csproj -c Release
dotnet run --project Tests/CameraUiCheck/CameraUiCheck.csproj -c Release
```

The checks use fake HTTP handlers; they do not contact a real camera or show
a window. See [the settings and service tests](Tests/SettingsCheck/README.md)
and [the UI tests](Tests/CameraUiCheck/README.md) for coverage.

GitHub Actions builds Debug and Release on Windows with .NET 10 and runs both
check programs on each push or pull request to `master`. The workflow can also
be started manually. Signing and MSIX packaging will be added separately.

For local camera simulation, run CamDummy and start CamControl in Debug mode
in another terminal:

```powershell
dotnet run --project CamDummy/CamDummy.csproj
dotnet run --project CamControl/CamControl.csproj -c Debug
```

Debug builds send commands to `http://localhost:8080/`. Release builds use the
configured camera IP and can move a real camera when started or closed.

## Running a build

Without a saved camera address, startup offers camera setup or an integrated
demo. To try the demo regardless of existing settings, start the Release build
with `--demo`, or choose Settings > Demo-Modus > Demo starten. The demo is only
active for that session and preserves the saved camera IP. Its internal HTTP
handler cannot send network requests. Both camera control modes share the
command and lifecycle logic.

The demo displays a schematic stage, the camera frame and a cropped preview.
Presets, held movement/zoom buttons and keyboard controls update the simulation.
This is not live video. The camera's real protocol still needs hardware testing.
See [the certification test instructions](docs/store-testing.md).

```powershell
dotnet run --project CamControl/CamControl.csproj -c Release -- --demo
```

The visible Settings button opens a read-only configuration dialog. Choose
"Konfiguration bearbeiten" to change the camera IP; only "Speichern" applies
and persists the change. Cancel or closing the dialog discards the edits.
Opening settings cancels pending camera commands and attempts all stops first.
The "Hilfe & Voraussetzungen" page explains network and runtime requirements,
current camera compatibility limits, setup, preset numbers, keyboard/mouse
controls, startup/standby behavior and troubleshooting. The confirmed camera
model list and the preset-save API still need to be verified on the camera.

Settings > **Buttons & Presets** lists the six position buttons. **Bearbeiten …**
opens a separate editor with held direction/zoom controls, preset recall, and
confirmed position saving using `ptzcmd&posset&N`. Names are saved independently
in `%LOCALAPPDATA%/CamControl/buttons.json`; renaming sends no preset-save command.
Preset numbers 1–6 remain fixed and standby (9) cannot be overwritten here.
The camera stores physical positions. Demo names and saved positions stay in
memory for the session and do not change the real configuration. Closing an
editor discards an unsaved name, but does not undo camera motion or saved presets.
The save command is documented by PTZOptics and SMTAV; it still requires testing
with the actual camera. A successful HTTP response is not independent readback.

Manual movement and zoom stop on key/mouse release, loss of mouse capture,
window deactivation, settings opening and application closing. Pending commands
and standby delays are cancelled before the lifecycle stop commands are sent.
Camera controls are disabled during a preset sequence or while stopping.
Closing attempts standby only after movement, zoom and focus stops succeed.
Network failures can prevent the camera from receiving or confirming a stop;
an unconfirmed stop is shown as an error while the app is open.

Framework-dependent builds require the .NET 10 Desktop Runtime on the target
Windows PC. MSIX packaging and self-contained distribution are not configured yet.
