# Camera UI checks

Run on Windows with the .NET 10 SDK:

```powershell
dotnet run --project Tests/CameraUiCheck/CameraUiCheck.csproj -c Release
```

These checks instantiate the real form without showing a window, inject a fake
camera HTTP handler and exercise button and keyboard handlers via reflection.
They verify error display, usable controls after failures, standby cleanup,
recovery, closing while the camera fails, and that an older successful request
does not clear a newer error. No camera commands are sent over the network.
