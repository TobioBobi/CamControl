using CamControl;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

internal static partial class Program
{
    private static async Task CameraDiscoveryChecks()
    {
        string file = Path.Combine(Path.GetTempPath(), $"camcontrol-discovery-{Guid.NewGuid()}.json");
        var manual = new CameraTarget("192.168.188.24");
        var camera = new DiscoveredCamera("urn:uuid:86ab39a6-5a1c-41ee-9663-ae9712dc0943", manual.Ip, "AIDA PTZ");
        var automatic = new CameraTarget(camera.Ip, camera.DeviceId);
        try
        {
            string previews = Path.GetFullPath("artifacts/ui-preview/camera-setup");
            Directory.CreateDirectory(previews);
            foreach (var (configured, save) in new[] { (false, false), (false, true), (true, false), (true, true) })
            {
                File.Delete(file);
                if (configured) CameraSettings.SaveTarget(manual, file);
                int searches = 0;
                var other = new DiscoveredCamera("urn:uuid:5b3d5bee-fdac-4d8c-b83e-692b3f787227", "192.168.188.27", null);
                var dialog = new CameraSettingsDialog(configured ? manual : new CameraTarget(""), settingsPath: file,
                    discover: _ => Task.FromResult<IReadOnlyList<DiscoveredCamera>>(++searches switch
                    {
                        1 => [],
                        2 => [camera],
                        _ => [other, camera]
                    }));
                dialog.ContentRendered += (_, _) =>
                {
                    Check(searches == 0 && !dialog.Address.IsVisible && dialog.SaveAddress.IsVisible != configured,
                        "Initial camera page opened discovery or the IP field automatically");
                    if (configured)
                    {
                        Check(Descendants(dialog).OfType<TextBlock>().Any(text => text.IsVisible && text.Text == manual.Ip) &&
                            Descendants(dialog).OfType<TextBlock>().Any(text => text.IsVisible && text.Text == Texts.Camera_Setup_FixedIp),
                            "The configured camera overview omitted the IP or setup method");
                        ClickCameraButton(dialog, "changeCamera");
                    }
                    var find = CameraButton(dialog, "findCamera");
                    var useManual = CameraButton(dialog, "useManualCameraIp");
                    Check(find.IsVisible && useManual.IsVisible && !dialog.Address.IsVisible && !dialog.SaveAddress.IsEnabled,
                        "Setup did not offer both paths before search");
                    if (!configured && !save) Screenshot(dialog, Path.Combine(previews, "setup.png"));
                    var results = Descendants(dialog).OfType<ListBox>().Single(list => list.Name == "discoveredCameras");
                    ClickCameraButton(dialog, "findCamera");
                    Check(find.IsEnabled && ((TextBlock)find.Content).Text == Texts.Camera_Discovery_FindAgain &&
                        results.Items.Count == 0 && useManual.IsVisible && !dialog.Address.IsVisible &&
                        Descendants(dialog).OfType<TextBlock>().Any(text => text.IsVisible && text.Text == Texts.Camera_Discovery_NoResults),
                        "Empty discovery did not show a retry");
                    if (!configured && !save) Screenshot(dialog, Path.Combine(previews, "no-results.png"));
                    ClickCameraButton(dialog, "findCamera");
                    Check(results.IsVisible && results.Items[0].ToString() == $"AIDA PTZ · {camera.Ip}", "Discovery omitted name or address");
                    Check(results.SelectedIndex == 0 && dialog.SaveAddress.IsEnabled, "A single camera was not preselected");
                    ClickCameraButton(dialog, "findCamera");
                    Check(results.Items.Count == 2 && results.Items[0].ToString() == other.Ip &&
                        results.SelectedIndex == -1 && !dialog.SaveAddress.IsEnabled,
                        "Multiple cameras did not require a new explicit selection");
                    results.SelectedIndex = 1;
                    Check(dialog.SaveAddress.IsEnabled && (configured ? CameraSettings.LoadTarget(file) == manual : !File.Exists(file)),
                        "Selecting a result applied it early or could not change mode at the same IP");
                    if (save) dialog.SaveCamera(); else dialog.Close();
                };
                Check((dialog.ShowDialog() == true) == save &&
                    (save ? CameraSettings.LoadTarget(file) == automatic : File.Exists(file) == configured &&
                        (!configured || CameraSettings.LoadTarget(file) == manual)),
                    "Closing or saving discovery did not preserve the expected selection");
            }

            var switchBack = new CameraSettingsDialog(automatic, settingsPath: file);
            switchBack.ContentRendered += (_, _) =>
            {
                Check(Descendants(switchBack).OfType<TextBlock>().Any(text => text.IsVisible && text.Text == Texts.Camera_Discovery_Automatic),
                    "The configured camera overview omitted automatic setup");
                Screenshot(switchBack, Path.Combine(previews, "configured.png"));
                OpenManualCameraForm(switchBack);
                Check(switchBack.SaveAddress.IsEnabled && switchBack.Address.Text == manual.Ip && Keyboard.FocusedElement == switchBack.Address,
                    "The current IPv4 could not be kept in manual mode");
                Screenshot(switchBack, Path.Combine(previews, "manual.png"));
                switchBack.SaveCamera();
            };
            Check(switchBack.ShowDialog() == true && CameraSettings.LoadTarget(file) == manual, "Same-IP manual mode did not persist");

            File.Delete(file);
            var directManual = new CameraSettingsDialog(new CameraTarget(""), settingsPath: file,
                discover: _ => throw new InvalidOperationException("Manual setup unexpectedly started discovery"));
            directManual.ContentRendered += (_, _) =>
            {
                ClickCameraButton(directManual, "useManualCameraIp");
                Check(directManual.Address.IsVisible && Keyboard.FocusedElement == directManual.Address &&
                    directManual.Address.Text == "" && !directManual.SaveAddress.IsEnabled,
                    "First-run manual entry was not empty, enabled or focused as expected");
                directManual.Address.Text = manual.Ip;
                ClickCameraButton(directManual, "saveConfiguration");
            };
            Check(directManual.ShowDialog() == true && CameraSettings.LoadTarget(file) == manual,
                "First-run manual setup failed without discovery");

            var reply = new TaskCompletionSource<IReadOnlyList<DiscoveredCamera>>();
            CancellationToken searchToken = default;
            var manualDuringSearch = new CameraSettingsDialog(new CameraTarget(""), settingsPath: file, discover: token =>
            {
                searchToken = token;
                return reply.Task;
            });
            ShowOffscreen(manualDuringSearch);
            await Layout();
            ClickCameraButton(manualDuringSearch, "findCamera");
            Check(!CameraButton(manualDuringSearch, "findCamera").IsEnabled &&
                Descendants(manualDuringSearch).OfType<TextBlock>().Any(text => text.IsVisible && text.Text == Texts.Camera_Discovery_Searching),
                "Discovery did not show the active search state");
            ClickCameraButton(manualDuringSearch, "useManualCameraIp");
            manualDuringSearch.Address.Text = "192.168.188.28";
            reply.SetResult([camera]);
            await Layout();
            Check(searchToken.IsCancellationRequested && manualDuringSearch.Address.IsVisible && manualDuringSearch.SaveAddress.IsEnabled &&
                manualDuringSearch.Address.Text == "192.168.188.28" && CameraButton(manualDuringSearch, "findCamera").IsEnabled,
                "Late discovery replaced or blocked manual entry");
            ClickCameraButton(manualDuringSearch, "findCamera");
            ClickCameraButton(manualDuringSearch, "useManualCameraIp");
            Check(manualDuringSearch.Address.Text == "192.168.188.28", "Switching setup paths discarded the manual draft");
            manualDuringSearch.Close();

            using var handler = new RecordingHandler();
            using var client = new HttpClient(handler);
            var replies = System.Threading.Channels.Channel.CreateUnbounded<IReadOnlyList<DiscoveredCamera>>();
            using var service = new CamControlService(client, automatic, async token => await replies.Reader.ReadAsync(token),
                null, TimeSpan.FromMilliseconds(10));
            using var session = new CameraSession(service, new AutomaticPositions { StartPreset = 2 });
            var window = new MainWindow(session, file + ".placement");
            ShowOffscreen(window);
            await Layout();
            Check(handler.Commands.Count == 0 && !session.Controller.ControlsEnabled && window.SettingsButton.IsEnabled,
                "Startup allowed commands before recognition or blocked settings");
            replies.Writer.TryWrite([camera with { DeviceId = "another-camera" }]);
            await Layout();
            Check(handler.Commands.Count == 0 && !session.Controller.ControlsEnabled &&
                window.Status.Text == Texts.Camera_Unreachable && window.SettingsButton.IsEnabled,
                "Only a different camera answered and the app did not report the selected camera unreachable");
            replies.Writer.TryWrite([camera with { Ip = "192.168.188.25" }]);
            await service.ResolveCameraAsync().WaitAsync(TimeSpan.FromSeconds(3));
            await Layout();
            Check(handler.Commands.LastOrDefault() == $"poscall&{session.Numbers[2]}" && service.CameraIp == "192.168.188.25",
                "Startup did not run the configured preset after recognition");

            handler.Commands.Clear();
            await session.Controller.StartDirectionAsync(Direction.Left);
            handler.Respond = (command, _) => command == "zoomin&5"
                ? Task.FromException<HttpResponseMessage>(new HttpRequestException("Camera disconnected")) : Ok();
            await session.Controller.StartZoomAsync(Zoom.ZoomIn);
            await session.Controller.StopDirectionAsync();
            await session.Controller.StopZoomAsync();
            Check(handler.Commands.SequenceEqual(["left&10&10", "zoomin&5"]) && !session.Controller.ControlsEnabled,
                "Releasing controls during rediscovery sent commands to an unconfirmed IP or enabled movement");

            handler.Respond = (command, _) => command == "ptzstop&10&10"
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)) : Ok();
            var safetyRecovery = service.ResolveCameraAsync();
            replies.Writer.TryWrite([camera with { Ip = "192.168.188.26" }]);
            await Layout();
            Check(!safetyRecovery.IsCompleted && !session.Controller.ControlsEnabled && handler.Commands.Count == 5,
                "A rejected safety stop enabled movement or prevented the other stops");

            var pendingStop = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            handler.Respond = (command, _) =>
            {
                if (command != "ptzstop&10&10") return Ok();
                stopStarted.SetResult();
                return pendingStop.Task;
            };
            replies.Writer.TryWrite([camera with { Ip = "192.168.188.26" }]);
            await stopStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Check(!safetyRecovery.IsCompleted && !session.Controller.ControlsEnabled && handler.Commands.Count == 8,
                "Recovery enabled movement before all stops were confirmed");
            pendingStop.SetResult(new(HttpStatusCode.OK));
            Check(await safetyRecovery.WaitAsync(TimeSpan.FromSeconds(3)) && session.Controller.ControlsEnabled &&
                handler.Commands.Skip(5).ToHashSet().SetEquals(["ptzstop&10&10", "zoomstop&5", "focusstop&5"]),
                "Rediscovery did not stop the camera before restoring control or replayed a start");
            handler.Respond = (_, _) => Ok();
            window.Close();
            await Layout();
        }
        finally
        {
            File.Delete(file);
            File.Delete(file + ".placement");
        }
    }

    private static Button CameraButton(CameraSettingsDialog dialog, string name)
        => Descendants(dialog).OfType<Button>().Single(button => button.Name == name);

    private static void ClickCameraButton(CameraSettingsDialog dialog, string name)
    {
        CameraButton(dialog, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        dialog.UpdateLayout();
    }

    private static void OpenManualCameraForm(CameraSettingsDialog dialog)
    {
        ClickCameraButton(dialog, "changeCamera");
        ClickCameraButton(dialog, "useManualCameraIp");
    }
}
