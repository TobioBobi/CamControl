using CamControl;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

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
            CameraSettings.SaveTarget(manual, file);
            foreach (bool save in new[] { false, true })
            {
                int searches = 0;
                var dialog = new CameraSettingsDialog(manual, settingsPath: file, discover: _ =>
                    Task.FromResult<IReadOnlyList<DiscoveredCamera>>(++searches == 1 ? [] : [camera]));
                dialog.ContentRendered += (_, _) =>
                {
                    var find = Descendants(dialog).OfType<Button>().Single(button => button.Name == "findCamera");
                    var results = Descendants(dialog).OfType<ListBox>().Single(list => list.Name == "discoveredCameras");
                    find.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Check(find.IsEnabled && results.Items.Count == 0 &&
                        Descendants(dialog).OfType<TextBlock>().Any(text => text.Text == Texts.Camera_Discovery_NoResults),
                        "Empty discovery did not show a retry");
                    find.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Check(results.IsVisible && results.Items[0].ToString() == $"AIDA PTZ · {camera.Ip}", "Discovery omitted name or address");
                    results.SelectedIndex = 0;
                    Check(dialog.SaveAddress.IsEnabled && CameraSettings.LoadTarget(file) == manual,
                        "Selecting a result applied it early or could not change mode at the same IP");
                    if (save) dialog.SaveCamera(); else dialog.Close();
                };
                Check((dialog.ShowDialog() == true) == save && CameraSettings.LoadTarget(file) == (save ? automatic : manual),
                    "Closing or saving discovery did not preserve the expected selection");
            }

            var switchBack = new CameraSettingsDialog(automatic, settingsPath: file);
            switchBack.ContentRendered += (_, _) =>
            {
                var useManual = Descendants(switchBack).OfType<Button>().Single(button => button.Name == "useManualCameraIp");
                useManual.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(switchBack.SaveAddress.IsEnabled && switchBack.Address.Text == manual.Ip,
                    "The current IPv4 could not be kept in manual mode");
                switchBack.SaveCamera();
            };
            Check(switchBack.ShowDialog() == true && CameraSettings.LoadTarget(file) == manual, "Same-IP manual mode did not persist");

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
            window.Close();
            await Layout();
        }
        finally
        {
            File.Delete(file);
            File.Delete(file + ".placement");
        }
    }
}
