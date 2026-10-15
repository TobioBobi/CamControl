# WPF camera UI and lifecycle checks

```powershell
dotnet run --project Tests/CameraUiCheck/CameraUiCheck.csproj -c Release
```

Runs on Windows with .NET 10. Every camera command uses a recording HTTP handler
or the integrated demo handler; no real network transport is used. User settings
are read but never modified. Dialog persistence tests use an isolated file under
the ignored `artifacts/ui-preview` directory.

The shared controller checks cover independent direction/zoom channels, held
inputs, early release during a delayed response, all-channel lifecycle stops,
shared concurrent stops, cancellation during preset preparation and recall,
blocked overlapping presets, focus reactivation during a pending stop, settings
suspension, recovery, stale successes after newer errors, and exit presets only
after successful stops.

WPF integration checks construct the actual windows, exercise keyboard routing,
mouse capture loss and editor deactivation, verify the seven presets and six
manual controls, and check compact layout with a 32-character name and enlarged
text, including a two-line camera error without unnecessary scrollbars.
Settings checks cover all seven sections, persistent appearance selection, address validation/save/cancel and
write failure, read-only help/privacy, demo entry, duplicate/reserved preset
numbers, explicit mapping/automation saves, custom demo save/recall, and mapped
startup/exit presets. Editors must stop and close on one close request, including
synchronous demo responses.

Most windows render offscreen. Local modal save/cancel, demo and welcome dialogs
open briefly. The checks save actual WPF renders under `artifacts/ui-preview` for
visual review, including light/dark main windows, settings, the preset editor,
long names and enlarged text. These checks do not replace physical camera tests,
real DPI/monitor changes or a fresh MSIX installation test.
