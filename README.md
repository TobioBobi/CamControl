# CamControl

Windows app for PTZ cameras that support the HTTP-CGI command interface
`/cgi-bin/ptzctrl.cgi?ptzcmd&…`, regardless of manufacturer or model.

The WPF interface uses a compact two-column preset panel and a tightly grouped
direction/zoom panel. The default camera window is 636 × 468 WPF logical units
including the Windows frame. All settings, the preset editor, first-run setup
and overwrite confirmation share the same styles. Dark is the default;
**Einstellungen > Darstellung** saves a choice of dark, light or the Windows
appearance in `appearance.json`. Windows high-contrast settings take priority.
Windows scaling can increase the physical
window size. Restricted work areas and enlarged text allow scrolling rather
than shrinking camera controls.

`CameraController` owns cancellation, independent movement/zoom inputs and
stop-before-preset/close sequencing. `CameraInput` connects WPF mouse capture
and keyboard events to it. `CamControlService` retains the HTTP protocol and
serialization. `CameraSession` preserves the existing settings formats and
keeps demo configuration separate. A highlighted preset means **last requested**,
not confirmed arrival at a physical position.

## Camera compatibility

Compatibility depends on the camera's supported commands, rather than a brand
or model list. The camera must accept HTTP GET requests at the endpoint above,
using IPv4, port 80, and no authentication. CamControl uses:

| Function | Commands and parameters |
| --- | --- |
| Pan/tilt | `up`, `down`, `left`, `right`, each with `&10&10` |
| Zoom | `zoomin`, `zoomout`, each with `&5` |
| Stop | `ptzstop&10&10`, `zoomstop&5`, `focusstop&5` |
| Presets | `poscall&N`, `posset&N`, with N = 1–6 or 9 |

For example, preset 1 is recalled with
`http://CAMERA-IP/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&1`.
HTTP-CGI describes the interface style; another HTTP camera API does not
necessarily support this command set. Verify the commands against the camera's
documentation and actual hardware. Protocol references:
[HTTP-CGI requests](https://docs.ptzoptics.com/dev/guides/http-api-getting-started/),
[preset commands](https://docs.ptzoptics.com/dev/http-api/Movement/preset-commands-get-preset-command/).

The draft German Store description is in [store-listing.md](docs/store-listing.md).

Settings > Info & Datenschutz shows the build version, publisher TobioBobi,
project/support link and privacy information. The app embeds the same
[privacy text](docs/privacy.md) maintained in this repository. The GitHub link
opens the browser only when clicked. A public privacy URL for the Store listing
will be configured when preparing the Store submission.

## Development

Requires Windows and the .NET 10 SDK. For Visual Studio development, use
Visual Studio 2026 with the .NET desktop development workload.
`global.json` selects a stable .NET 10 SDK and allows newer .NET 10 feature bands.

```powershell
dotnet build CamControl.sln -c Release
dotnet run --project Tests/SettingsCheck/SettingsCheck.csproj -c Release
dotnet run --project Tests/CameraUiCheck/CameraUiCheck.csproj -c Release
```

The checks use fake HTTP handlers and do not contact a real camera. WPF checks
create offscreen windows and briefly open local modal dialogs. They render
previews into `artifacts/ui-preview`. See [the settings and service tests](Tests/SettingsCheck/README.md)
and [the UI tests](Tests/CameraUiCheck/README.md) for coverage.

Changes follow `feature/*` or `fix/*` → `develop` → `master`. Start work from
`develop` and target feature/fix pull requests at `develop`. Merge a tested batch
into `master` for a release. See [the branch and release workflow](docs/branch-workflow.md).

GitHub Actions builds Debug and Release on Windows with .NET 10 and runs both
check programs on each push or pull request to `develop` or `master`. The workflow can also
be started manually. It also builds an unsigned self-contained x64 MSIX test
package and uploads it as an artifact. See [MSIX build and installation](docs/msix.md)
for local test signing and preparation of a Store package.

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

The visible Settings button opens the configuration dialog. The camera IP is
always editable; saving becomes available when a valid address changes
and persists the change. Cancel or closing the dialog discards the edits.
Opening settings cancels pending camera commands and attempts all stops first.
The "Hilfe & Voraussetzungen" page explains network and runtime requirements,
current camera compatibility limits, setup, preset numbers, keyboard/mouse
controls, optional startup/exit presets and troubleshooting. Compatibility of
the command set, including preset saving, must be tested on actual hardware.

Settings > **Buttons & Presets** lists all seven position buttons. **Bearbeiten …**
opens a separate editor with held direction/zoom controls, preset recall, and
confirmed position saving using `ptzcmd&posset&N`. Names are saved independently
in `%LOCALAPPDATA%/CamControl/buttons.json`; renaming sends no preset-save command.
Keyboard shortcuts 1–6 and 9 remain fixed; all seven camera preset numbers and positions are editable.
The camera stores physical positions. Demo names and saved positions stay in
memory for the session and do not change the real configuration. Closing an
editor discards an unsaved name, but does not undo camera motion or saved presets.
The save command must be supported by the camera's HTTP-CGI interface. A
successful HTTP response is not independent readback of the stored position.

Manual movement and zoom stop on key/mouse release, loss of mouse capture,
window deactivation, settings opening and application closing. Pending commands
are cancelled before the lifecycle stop commands are sent.
Camera controls are disabled during a preset sequence or while stopping.
Closing calls the selected exit preset only after all stop commands succeed.
Network failures can prevent the camera from receiving or confirming a stop;
an unconfirmed stop is shown as an error while the app is open.

Framework-dependent builds require the .NET 10 Desktop Runtime on the target
Windows PC. MSIX packages include the .NET runtime.

Automatic startup and exit presets are configured in **Einstellungen >
Automatische Positionen** and applied with **Automatik speichern**. Save becomes
enabled only when a selection differs from its saved value. Each selection
allows None or one of the seven buttons. Without saved choices, startup and exit
perform no preset motion. Choices persist in `automatic-positions.json`. Demo choices remain in
memory for the session. Preset 9 is ordinary: no timed focus sequence or loading
animation remains. HTTP completion does not confirm arrival at the physical target.

### Per-user camera preset numbers

Each button has a configurable camera preset number, independent of its keyboard
shortcut. In the preset editor, save the name and number with **Zuordnung speichern**;
then save or recall the camera position. Changing the assignment sends no camera
command and does not copy or delete the old camera preset. Saving a position also applies the edited name and number after confirmation.
Recalling a position requires the number assignment to be saved first.

Names and assignments are stored together in the current Windows account's
`buttons.json`. Existing name-only files retain their original preset numbers.
Duplicate assignments in one account are rejected. Users of the same camera can
agree on separate ranges, for example 10-19, 20-29 and 30-39. Allowed numbers are
0-89 and 100-254, subject to camera support; 90-99 are excluded because some cameras
use them for special commands. Other accounts and camera control tools are not
blocked from overwriting these presets. Demo assignments remain in memory.
