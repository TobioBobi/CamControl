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
They also verify that repeated theme application keeps shared brushes, and that
motion, preset clicks and renaming reuse the existing preset content.

Responsive regressions cover full view plus compact/minimal tile, strip and
column layouts, unscrolled access to all seven numbered/ellipsized presets,
full-name tooltips/accessibility, light/dark palettes, enlarged fonts and
presentation scaling at 100/125/150/200%. A recording HTTP transport checks idle
resizing, held mouse movement/zoom, concurrent keyboard channels, pending safety
stops, capture release, minimal-view shortcuts, errors retained for full view, mapped presets and
one-click restoration of full size/position. Native WM_SIZING tests exercise all
eight edges/corners; column sweeps check controls at narrow widths and sufficient
height, strip checks verify aligned arrow/zoom controls. Boundary traversal checks hysteresis and unchanged outer
size. Long-name demo checks cover all six slim variants and startup/exit presets.
Restricted-area restoration retains full controls via scrolling. Screenshots use
the `responsive-*` prefix. Scaling simulations do not replace real monitor/DPI
and Windows high-contrast acceptance.
Settings checks cover all eight sections, persistent appearance and language selection, address validation/save/cancel and
write failure, read-only help/privacy, demo entry, duplicate/reserved preset
numbers, explicit mapping/automation saves, custom demo save/recall, and mapped
startup/exit presets. Editors must stop and close on one close request, including
synchronous demo responses.

Most windows render offscreen. Local modal save/cancel, demo and welcome dialogs
open briefly. The checks save actual WPF renders under `artifacts/ui-preview` for
visual review in `de-DE`, `en-US` and `es-ES` subdirectories, including light/dark main windows, settings, the preset editor,
long names and enlarged text. These checks do not replace physical camera tests,
real DPI/monitor changes or a fresh MSIX installation test.

All window checks run in German, English and Spanish. Language tests cover save/reopen,
returning to the Windows setting, write failure, restart guidance, XAML/C# UI
text, WPF language metadata and privacy language. Saving a preference must not
change the current UI language or custom preset names.
