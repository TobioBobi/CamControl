using CamControl;
using System.Net;

int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    checks++;
}

foreach (var invalid in new[] { "", "192.168.1", "192.168.1.256", "-1.2.3.4", "1.2.3.4.5", "localhost", "http://192.168.1.1", "1.2.3.4:8080", "1.2. 3.4", "0x7f.0.0.1", "0000.1.2.3" })
    Check(!CameraSettings.TryNormalizeIp(invalid, out _), "Invalid IP accepted: " + invalid);
Check(CameraSettings.TryNormalizeIp(" 192.168.188.024 ", out var normalized) && normalized == "192.168.188.24", "Normalization failed");

var testPath = Path.Combine(AppContext.BaseDirectory, "test-settings.json");
File.Delete(testPath);
Check(CameraSettings.Load(testPath) == CameraSettings.DefaultCameraIp, "Missing settings fallback failed");
CameraSettings.Save("192.168.188.25", testPath);
Check(CameraSettings.Load(testPath) == "192.168.188.25", "Saved settings did not survive reload");
CameraSettings.Save("192.168.188.26", testPath);
Check(CameraSettings.Load(testPath) == "192.168.188.26", "Replacing settings failed");
File.WriteAllText(testPath, "broken json");
Check(CameraSettings.Load(testPath) == CameraSettings.DefaultCameraIp, "Damaged settings fallback failed");
File.WriteAllText(testPath, "{\"CameraIp\":\"http://invalid\"}");
Check(CameraSettings.Load(testPath) == CameraSettings.DefaultCameraIp, "Invalid stored address fallback failed");
bool rejected = false;
try { CameraSettings.Save("bad address", testPath); } catch (ArgumentException) { rejected = true; }
Check(rejected, "Invalid settings saved");
File.Delete(testPath);

var gesture = new HiddenSettingsGesture();
for (int index = 0; index < 4; index++) Check(!gesture.RegisterClick(index * 150), "Dialog opened before fifth click");
Check(gesture.RegisterClick(600), "Five fast clicks did not open dialog");
Check(!gesture.RegisterClick(750), "Gesture did not reset after opening");
gesture.Reset();
for (int index = 0; index < 10; index++) Check(!gesture.RegisterClick(index * 1000), "Slow clicks opened dialog");
gesture.Reset();
for (int index = 0; index < 5; index++) Check(!gesture.RegisterClick(index * 790), "Clicks outside total window opened dialog");

using var handler = new RecordingHandler();
using var client = new HttpClient(handler);
var service = new CamControlService(client, "192.168.188.24");
await service.MoveToPosition(Position.BuehneKomplett);
service.SetCameraIp("192.168.188.25");
await service.MoveToPosition(Position.BuehneKomplett);
service.MoveInDirection(Direction.Up);
service.StopDirectionalMovement();
service.Zoom(Zoom.ZoomIn);
service.StopZooming();
Check(handler.Requests[0].Host == "192.168.188.24", "Initial destination wrong");
Check(handler.Requests.Skip(1).All(uri => uri.Host == "192.168.188.25"), "Destination not updated after first request");
Check(handler.Requests.All(uri => uri.AbsolutePath == "/cgi-bin/ptzctrl.cgi"), "Camera command path changed");
rejected = false;
try { service.SetCameraIp("invalid"); } catch (ArgumentException) { rejected = true; }
Check(rejected && service.CameraIp == "192.168.188.25", "Invalid address changed active destination");

var firstStandbyRequest = handler.Requests.Count;
var standby = service.MoveToPosition(Position.StandBy);
service.SetCameraIp("192.168.188.26");
await standby;
Check(handler.Requests.Skip(firstStandbyRequest).Count() == 3, "Standby command sequence incomplete");
Check(handler.Requests.Skip(firstStandbyRequest).All(uri => uri.Host == "192.168.188.25"),
    "Pending standby sequence switched to a different camera");
await service.MoveToPosition(Position.BuehneKomplett);
Check(handler.Requests.Last().Host == "192.168.188.26", "New commands did not use updated destination");

using var dialog = new CameraSettingsDialog("192.168.188.24");
Check(dialog.AcceptButton is Button save && save.Text == "Speichern", "Save button missing");
Check(dialog.CancelButton is Button cancel && cancel.DialogResult == DialogResult.Cancel, "Cancel button missing");
Check(dialog.Controls.OfType<TextBox>().Single().Text == "192.168.188.24", "Dialog did not show active IP");
Console.WriteLine($"Passed {checks} checks: validation, persistence, hidden gesture, live destination switch, dialog controls.");

sealed class RecordingHandler : HttpMessageHandler
{
    public List<Uri> Requests { get; } = new();
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
