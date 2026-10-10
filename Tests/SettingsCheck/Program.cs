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
await service.MoveInDirection(Direction.Up);
await service.StopDirectionalMovement();
await service.Zoom(Zoom.ZoomIn);
await service.StopZooming();
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

var commands = new (string Name, Func<Task> Execute)[]
{
    ("Position", () => service.MoveToPosition(Position.Redner)),
    ("Standby", () => service.MoveToPosition(Position.StandBy)),
    ("Movement", () => service.MoveInDirection(Direction.Up)),
    ("Movement stop", () => service.StopDirectionalMovement()),
    ("Zoom", () => service.Zoom(Zoom.ZoomIn)),
    ("Zoom stop", () => service.StopZooming())
};
foreach (var command in commands)
{
    foreach (var status in new[] { HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError })
    {
        using var content = new TrackingContent();
        handler.Respond = _ => Task.FromResult(new HttpResponseMessage(status) { Content = content });
        var messages = new List<string>();
        int requestCount = handler.Requests.Count;
        Check(!await CameraCommandRunner.RunAsync(command.Execute, messages.Add), command.Name + " accepted HTTP error");
        Check(messages.Count == 1 && messages[0].Contains($"HTTP {(int)status}"), command.Name + " did not report HTTP status");
        Check(content.Disposed, command.Name + " leaked failed response");
        Check(handler.Requests.Count == requestCount + 1, command.Name + " continued after rejected command");
    }

    foreach (var failure in new Exception[] { new HttpRequestException("Simulated network failure"), new TaskCanceledException("Simulated timeout") })
    {
        handler.Respond = _ => Task.FromException<HttpResponseMessage>(failure);
        var messages = new List<string>();
        Check(!await CameraCommandRunner.RunAsync(command.Execute, messages.Add), command.Name + " did not handle network failure");
        Check(messages.Count == 1 && messages[0].Contains(failure is TaskCanceledException ? "nicht rechtzeitig" : "Verbindung"),
            command.Name + " reported wrong network error");
    }
}

// A failed focus command must not continue the standby sequence.
int standbyRequestCount = handler.Requests.Count;
handler.Respond = _ => Task.FromResult(new HttpResponseMessage(
    handler.Requests.Count == standbyRequestCount + 1 ? HttpStatusCode.OK : HttpStatusCode.BadRequest));
var standbyErrors = new List<string>();
Check(!await CameraCommandRunner.RunAsync(() => service.MoveToPosition(Position.StandBy), standbyErrors.Add),
    "Standby focus error was ignored");
Check(handler.Requests.Count == standbyRequestCount + 2 && standbyErrors.Count == 1,
    "Standby continued after focus failure");

// Exercise an actual HttpClient timeout, including asynchronously delivered cancellation.
using var timeoutHandler = new RecordingHandler
{
    Respond = async token =>
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, token);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
};
using var timeoutClient = new HttpClient(timeoutHandler) { Timeout = TimeSpan.FromMilliseconds(50) };
var timeoutService = new CamControlService(timeoutClient, "192.168.188.24");
var timeoutErrors = new List<string>();
Check(!await CameraCommandRunner.RunAsync(() => timeoutService.StopZooming(), timeoutErrors.Add), "HttpClient timeout escaped");
Check(timeoutErrors.Count == 1 && timeoutErrors[0].Contains("nicht rechtzeitig"), "HttpClient timeout message missing");

// After failures the same service and runner must accept new commands.
using var successContent = new TrackingContent();
handler.Respond = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = successContent });
var recoveryErrors = new List<string>();
Check(await CameraCommandRunner.RunAsync(() => service.MoveInDirection(Direction.Down), recoveryErrors.Add),
    "Service did not recover after errors");
Check(recoveryErrors.Count == 0 && successContent.Disposed, "Successful response leaked or raised an error");

// Construct WinForms controls after the asynchronous checks: this console test
// does not run a message loop for the WinForms synchronization context.
using var dialog = new CameraSettingsDialog("192.168.188.24");
Check(dialog.AcceptButton is Button save && save.Text == "Speichern", "Save button missing");
Check(dialog.CancelButton is Button cancel && cancel.DialogResult == DialogResult.Cancel, "Cancel button missing");
Check(dialog.Controls.OfType<TextBox>().Single().Text == "192.168.188.24", "Dialog did not show active IP");

Console.WriteLine($"Passed {checks} checks: settings, camera commands, HTTP errors, timeouts, response disposal and recovery.");

sealed class RecordingHandler : HttpMessageHandler
{
    public List<Uri> Requests { get; } = new();
    public Func<CancellationToken, Task<HttpResponseMessage>> Respond { get; set; } =
        _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);
        return Respond(cancellationToken);
    }
}

sealed class TrackingContent : ByteArrayContent
{
    public TrackingContent() : base(Array.Empty<byte>()) { }
    public bool Disposed { get; private set; }
    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}
