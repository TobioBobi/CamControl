# Camera UI checks

Run on Windows with the .NET 10 SDK:

```powershell
dotnet run --project Tests/CameraUiCheck/CameraUiCheck.csproj -c Release
```

These checks instantiate the real form without showing a window, inject a fake
camera HTTP handler and exercise button and keyboard handlers via reflection.
They verify error display, usable controls after failures, standby cleanup,
recovery, closing while the camera fails, and that an older successful request
does not clear a newer error. They also cover focus loss, reactivation during a
pending stop, lost mouse capture, release before a start response arrives,
blocked overlapping presets, settings preparation, and closing during standby.
Regression checks cover stops while the other channel has an unanswered start,
and presets cancelled during preparation by focus loss or settings opening.
No camera commands are sent over the network.
The checks also verify visible settings access separated from camera controls,
updated error hints, and availability during presets without duplicate dialogs.
