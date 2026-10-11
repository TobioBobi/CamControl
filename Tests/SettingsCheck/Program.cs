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
Check(!CameraSettings.TryLoad(testPath, out _), "Missing configuration bypassed first-run choice");
Check(CameraSettings.Load(testPath) == CameraSettings.DefaultCameraIp, "Missing settings fallback failed");
CameraSettings.Save("192.168.188.25", testPath);
Check(CameraSettings.Load(testPath) == "192.168.188.25", "Saved settings did not survive reload");
Check(CameraSettings.TryLoad(testPath, out var configuredIp) && configuredIp == "192.168.188.25", "Saved configuration was not recognized");
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

using var handler = new RecordingHandler();
var namesPath = Path.Combine(AppContext.BaseDirectory, "test-buttons.json");
var automaticPath = Path.Combine(AppContext.BaseDirectory, "test-automatic-positions.json");
File.Delete(automaticPath);
var freshAutomatic = AutomaticPositions.Load(automaticPath, existingInstallation: false);
Check(freshAutomatic.StartPreset == null && freshAutomatic.ExitPreset == null, "Fresh setup unexpectedly enabled automatic motion");
var migratedAutomatic = AutomaticPositions.Load(automaticPath, existingInstallation: true);
Check(migratedAutomatic.StartPreset == 4 && migratedAutomatic.ExitPreset == 9, "Existing installation lost its automatic positions");
AutomaticPositions.Save(new() { StartPreset = 2, ExitPreset = 6 }, automaticPath);
var reloadedAutomatic = AutomaticPositions.Load(automaticPath, false);
Check(reloadedAutomatic.StartPreset == 2 && reloadedAutomatic.ExitPreset == 6, "Automatic choices did not survive restart");
AutomaticPositions.Save(new(), automaticPath);
Check(AutomaticPositions.Load(automaticPath, true).StartPreset == null && AutomaticPositions.Load(automaticPath, true).ExitPreset == null,
    "Saved none selection reverted to legacy behavior");
File.WriteAllText(automaticPath, "broken");
Check(AutomaticPositions.Load(automaticPath, true).StartPreset == null, "Damaged automation enabled legacy movement");
File.WriteAllText(automaticPath, "{\"StartPreset\":7,\"ExitPreset\":9}");
Check(AutomaticPositions.Load(automaticPath, false).StartPreset == null && AutomaticPositions.Load(automaticPath, false).ExitPreset == 9,
    "Invalid automatic preset accepted or valid preset discarded");
try { AutomaticPositions.Save(new() { ExitPreset = 8 }, automaticPath); throw new Exception("Invalid automatic preset saved"); }
catch (ArgumentException) { checks++; }
File.Delete(automaticPath);
File.Delete(namesPath);
var names = PresetSettings.Load(namesPath);
Check(names.Count == 7 && names[1] == "Redner", "Missing button settings lost defaults");
names[1] = "  Bühne links  ";
PresetSettings.Save(names, namesPath);
Check(PresetSettings.Load(namesPath)[1] == "Bühne links", "Button name did not survive restart");
File.WriteAllText(namesPath, "{\"1\":\"\",\"2\":\"Lesepult\",\"9\":\"Wrong\"}");
var loadedNames = PresetSettings.Load(namesPath);
Check(loadedNames[1] == "Redner" && loadedNames[2] == "Lesepult" && loadedNames[9] == "Wrong", "Invalid names or standby mapping accepted");
File.WriteAllText(namesPath, "broken json");
Check(PresetSettings.Load(namesPath)[1] == "Redner", "Damaged button file prevented fallback");
foreach (var invalidName in new[] { "", "   ", new string('x', 33), "a\nb" })
    Check(!PresetSettings.IsValidName(invalidName), "Invalid button name accepted");
names[1] = "";
try { PresetSettings.Save(names, namesPath); throw new Exception("Invalid name saved"); }
catch (ArgumentException) { checks++; }
Check(File.ReadAllText(namesPath) == "broken json", "Rejected save changed the previous file");
File.Delete(namesPath);
var demoCamera = new DemoCamera();
using (var demoClient = new HttpClient(new DemoCameraHandler(demoCamera)))
{
    var demoService = new CamControlService(demoClient, "192.168.188.24");
    foreach (var preset in Enum.GetValues<Position>())
    {
        await demoService.MoveToPosition(preset);
        Check(!demoCamera.State.Moving && !demoCamera.State.Zooming && demoCamera.State.Frame.Left >= 0 &&
            demoCamera.State.Frame.Right <= 1, "Demo preset was not contained/stopped: " + preset);
    }
    await demoService.MoveToPosition(Position.BuehneKomplett);
    await demoService.Zoom(Zoom.ZoomIn);
    demoCamera.Advance(TimeSpan.FromSeconds(1));
    await demoService.StopZooming();
    float zoom = demoCamera.State.Zoom;
    demoCamera.Advance(TimeSpan.FromSeconds(1));
    Check(zoom > 1 && demoCamera.State.Zoom == zoom, "Demo zoom did not start/stop");
    foreach (var direction in Enum.GetValues<Direction>())
    {
        var before = demoCamera.State;
        await demoService.MoveInDirection(direction);
        demoCamera.Advance(TimeSpan.FromMilliseconds(200));
        await demoService.StopDirectionalMovement();
        var after = demoCamera.State;
        Check((after.X != before.X || after.Y != before.Y) && !after.Moving, "Demo direction did not move/stop: " + direction);
        demoCamera.Advance(TimeSpan.FromSeconds(1));
        Check(demoCamera.State.X == after.X && demoCamera.State.Y == after.Y, "Stopped demo drifted");
    }
    demoCamera.Apply("?ptzcmd&focusin&5");
    await demoService.MoveInDirection(Direction.Right);
    await demoService.Zoom(Zoom.ZoomIn);
    await demoService.StopAll();
    Check(!demoCamera.State.Moving && !demoCamera.State.Zooming && !demoCamera.State.Focusing, "Demo lifecycle stop left a channel active");
    using var rejectedDemoResponse = await demoClient.GetAsync("http://example.invalid/unsupported");
    Check(rejectedDemoResponse.StatusCode == HttpStatusCode.BadRequest, "Demo handler accepted an unknown endpoint");
    Check(!demoCamera.Apply("?ptzcmd&poscall&999"), "Demo accepted an unsupported preset");
    await demoService.MoveToPosition(Position.Tisch);
    await demoService.Zoom(Zoom.ZoomIn);
    demoCamera.Advance(TimeSpan.FromMilliseconds(600));
    await demoService.StopAll();
    var savedPosition = demoCamera.State;
    await demoService.SavePosition(Position.Redner);
    await demoService.MoveToPosition(Position.BuehneKomplett);
    await demoService.MoveToPosition(Position.Redner);
    Check(demoCamera.State.X == savedPosition.X && demoCamera.State.Y == savedPosition.Y &&
        demoCamera.State.Zoom == savedPosition.Zoom, "Demo did not recall a custom position and zoom");
    await demoService.SavePosition(Position.Redner);
    var ninthPosition = demoCamera.State;
    await demoService.SavePosition(Position.StandBy);
    await demoService.MoveToPosition(Position.Tisch);
    await demoService.MoveToPosition(Position.StandBy);
    Check(demoCamera.State.X == ninthPosition.X && demoCamera.State.Y == ninthPosition.Y && demoCamera.State.Zoom == ninthPosition.Zoom,
        "Demo could not save/recall preset 9");
}
using var client = new HttpClient(handler);
var service = new CamControlService(client, "192.168.188.24");
await service.SavePosition(Position.Redner);
Check(handler.Requests.Last().Query == "?ptzcmd&posset&1", "Preset save URL differs from documented command");
handler.Requests.Clear();
try { await service.SavePosition((Position)7); throw new Exception("Invalid preset save accepted"); }
catch (ArgumentOutOfRangeException) { checks++; }
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
Check(handler.Requests.Skip(firstStandbyRequest).Count() == 1, "Preset 9 must send only one position command");
Check(handler.Requests.Skip(firstStandbyRequest).All(uri => uri.Host == "192.168.188.25"),
    "Pending standby sequence switched to a different camera");
await service.MoveToPosition(Position.BuehneKomplett);
Check(handler.Requests.Last().Host == "192.168.188.26", "New commands did not use updated destination");

var commands = new (string Name, Func<Task> Execute)[]
{
    ("Position", () => service.MoveToPosition(Position.Redner)),
    ("Save position", () => service.SavePosition(Position.Redner)),
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

// Cancellation must remove pending starts before the final stop commands.
using var sequencingHandler = new RecordingHandler();
using var sequencingClient = new HttpClient(sequencingHandler);
var sequencingService = new CamControlService(sequencingClient, "192.168.188.24");
using (var cancellation = new CancellationTokenSource())
{
    sequencingHandler.Respond = async token =>
    {
        if (sequencingHandler.Requests.Last().Query.Contains("&up&"))
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        return new HttpResponseMessage(HttpStatusCode.OK);
    };
    var cancellationErrors = new List<string>();
    Task<bool> moving = CameraCommandRunner.RunAsync(
        () => sequencingService.MoveInDirection(Direction.Up, cancellation.Token), cancellationErrors.Add, cancellation.Token);
    Task<bool> parallelZoom = CameraCommandRunner.RunAsync(
        () => sequencingService.Zoom(Zoom.ZoomIn, cancellation.Token), cancellationErrors.Add, cancellation.Token);
    Task<bool> queuedDirection = CameraCommandRunner.RunAsync(
        () => sequencingService.MoveInDirection(Direction.Down, cancellation.Token), cancellationErrors.Add, cancellation.Token);
    Check(sequencingHandler.Requests.Count == 2, "Channels were not independent or queued direction overlapped movement");
    cancellation.Cancel();
    await sequencingService.StopAll();
    Check(!await moving && await parallelZoom && !await queuedDirection && cancellationErrors.Count == 0,
        "Intentional stop was reported as a network timeout");
    Check(sequencingHandler.Requests.Count == 5 &&
        sequencingHandler.Requests.Skip(2).All(uri => uri.Query.Contains("stop")),
        "A cancelled start was sent after stopping");
}

// All stop channels are attempted even if the PTZ stop fails.
int stopsBefore = sequencingHandler.Requests.Count;
sequencingHandler.Respond = _ => Task.FromResult(new HttpResponseMessage(
    sequencingHandler.Requests.Last().Query.Contains("ptzstop") ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));
var stopErrors = new List<string>();
Check(!await CameraCommandRunner.RunAsync(() => sequencingService.StopAll(), stopErrors.Add), "Failed PTZ stop was ignored");
Check(sequencingHandler.Requests.Skip(stopsBefore).Count() == 3 &&
    sequencingHandler.Requests.Skip(stopsBefore).Any(uri => uri.Query.Contains("zoomstop")) &&
    sequencingHandler.Requests.Skip(stopsBefore).Any(uri => uri.Query.Contains("focusstop")),
    "One failed stop prevented the other stops");

// New starts must wait for every stop response, not only the first response.
var delayedStop = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
sequencingHandler.Respond = _ => sequencingHandler.Requests.Last().Query.Contains("zoomstop")
    ? delayedStop.Task : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
stopsBefore = sequencingHandler.Requests.Count;
Task allStops = sequencingService.StopAll();
Task restart = sequencingService.MoveInDirection(Direction.Down);
Task restartZoom = sequencingService.Zoom(Zoom.ZoomOut);
Check(sequencingHandler.Requests.Count == stopsBefore + 3 && !restart.IsCompleted && !restartZoom.IsCompleted,
    "A channel restarted before all stops completed");
delayedStop.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
await Task.WhenAll(allStops, restart, restartZoom);
Check(sequencingHandler.Requests.Skip(stopsBefore + 3).Any(uri => uri.Query.Contains("&down&")) &&
    sequencingHandler.Requests.Skip(stopsBefore + 3).Any(uri => uri.Query.Contains("zoomout")),
    "Both channels did not resume after stopping");

// Cancelling a preset waiting for zoom must release its acquired direction gate.
var pendingZoom = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
sequencingHandler.Respond = _ => sequencingHandler.Requests.Last().Query.Contains("zoomin")
    ? pendingZoom.Task : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
Task zoomIn = sequencingService.Zoom(Zoom.ZoomIn);
using (var cancellation = new CancellationTokenSource())
{
    int before = sequencingHandler.Requests.Count;
    Task preset = sequencingService.MoveToPosition(Position.Redner, cancellation.Token);
    Check(!preset.IsCompleted && sequencingHandler.Requests.Count == before, "Preset overlapped pending zoom");
    cancellation.Cancel();
    try { await preset; throw new Exception("Waiting preset ignored cancellation"); }
    catch (OperationCanceledException) { checks++; }
    await sequencingService.StopDirectionalMovement().WaitAsync(TimeSpan.FromSeconds(2));
    Check(sequencingHandler.Requests.Last().Query.Contains("ptzstop") && !zoomIn.IsCompleted,
        "Cancelled preset leaked the direction gate");
}
pendingZoom.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
await zoomIn;

// Construct WinForms controls after the asynchronous checks: this console test
// does not run a message loop for the WinForms synchronization context.
using var dialog = new CameraSettingsDialog("192.168.188.24", testPath);
IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>()
    .SelectMany(child => new[] { child }.Concat(Descendants(child)));
var dialogControls = Descendants(dialog).ToArray();
var address = dialogControls.OfType<TextBox>().Single();
var help = dialogControls.OfType<RichTextBox>().Single();
Check(help.ReadOnly && help.ScrollBars == RichTextBoxScrollBars.Vertical,
    "Help could be edited or was not scrollable");
var save = dialogControls.OfType<Button>().Single(button => button.Text == "Speichern");
Check(dialog.AcceptButton == null && !save.Enabled && address.ReadOnly,
    "Opening the dialog allowed accidental changes");
Check(dialog.CancelButton is Button cancel && cancel.DialogResult == DialogResult.Cancel, "Cancel button missing");
Check(address.Text == "192.168.188.24", "Dialog did not show active IP");
var dialogFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
typeof(CameraSettingsDialog).GetMethod("Save_Click", dialogFlags)!.Invoke(dialog, new object?[] { null, EventArgs.Empty });
Check(!File.Exists(testPath), "View mode persisted settings");
var pages = dialogControls.OfType<TabControl>().Single();
pages.SelectedIndex = 1;
typeof(CameraSettingsDialog).GetMethod("Edit_Click", dialogFlags)!.Invoke(dialog, new object?[] { null, EventArgs.Empty });
Check(!address.ReadOnly && save.Enabled && dialog.AcceptButton == save &&
    ((Button)dialog.CancelButton!).Text == "Abbrechen" && pages.SelectedIndex == 0,
    "Edit mode was not explicitly enabled on the camera page");
address.Text = "192.168.188.025";
pages.SelectedIndex = 1;
Check(pages.SelectedTab!.Text == "Hilfe & Voraussetzungen" &&
    address.Text == "192.168.188.025" && !File.Exists(testPath),
    "Reading help applied or discarded unsaved configuration changes");
Check(dialog.CameraIp == "192.168.188.24" && !File.Exists(testPath), "Editing applied unsaved changes");
typeof(CameraSettingsDialog).GetMethod("Save_Click", dialogFlags)!.Invoke(dialog, new object?[] { null, EventArgs.Empty });
Check(dialog.DialogResult == DialogResult.OK && dialog.CameraIp == "192.168.188.25" &&
    CameraSettings.Load(testPath) == "192.168.188.25", "Save did not persist and return the normalized IP");
using (var cancelledDialog = new CameraSettingsDialog("192.168.188.25", testPath))
{
    typeof(CameraSettingsDialog).GetMethod("Edit_Click", dialogFlags)!.Invoke(cancelledDialog, new object?[] { null, EventArgs.Empty });
    Descendants(cancelledDialog).OfType<TextBox>().Single().Text = "192.168.188.26";
    cancelledDialog.DialogResult = DialogResult.Cancel;
    cancelledDialog.Close();
    Check(cancelledDialog.CameraIp == "192.168.188.25" && CameraSettings.Load(testPath) == "192.168.188.25",
        "Cancel applied or persisted the changed IP");
}
File.Delete(testPath);

using (var demoDialog = new CameraSettingsDialog("192.168.188.25", testPath))
{
    var demoButton = Descendants(demoDialog).OfType<Button>().Single(button => button.Name == "demoAction");
    typeof(Button).GetMethod("OnClick", dialogFlags)!.Invoke(demoButton, new object[] { EventArgs.Empty });
    Check(demoDialog.RequestedDemoMode == true && demoDialog.DialogResult == DialogResult.OK &&
        !File.Exists(testPath) && demoDialog.CameraIp == "192.168.188.25", "Demo entry changed saved camera configuration");
}
using (var demoDialog = new CameraSettingsDialog("192.168.188.25", testPath, demoModeActive: true))
{
    var demoButton = Descendants(demoDialog).OfType<Button>().Single(button => button.Name == "demoAction");
    typeof(Button).GetMethod("OnClick", dialogFlags)!.Invoke(demoButton, new object[] { EventArgs.Empty });
    Check(demoDialog.RequestedDemoMode == false && demoDialog.DialogResult != DialogResult.OK &&
        !Descendants(demoDialog).OfType<TextBox>().Single().ReadOnly && !File.Exists(testPath),
        "Leaving demo did not require deliberate configuration saving");
    demoDialog.DialogResult = DialogResult.Cancel;
    Check(!File.Exists(testPath), "Cancelling demo exit saved the camera IP");
}

using (var automaticDialog = new CameraSettingsDialog("192.168.188.25", testPath, true,
    service, PresetSettings.Defaults(), (_, _) => { }, demoCamera))
{
    AutomaticPositions? savedChoices = null;
    automaticDialog.ConfigureAutomaticPositions(new(), value => savedChoices = value);
    var automaticControls = Descendants(automaticDialog).ToArray();
    var selectors = automaticControls.OfType<ComboBox>().ToArray();
    var saveAutomatic = automaticControls.OfType<Button>().Single(button => button.Name == "saveAutomaticPositions");
    Check(selectors.Length == 2 && selectors.All(combo => combo.Enabled && combo.Items.Count == 8 && combo.SelectedIndex == 0) && !saveAutomatic.Enabled,
        "Initial automatic selections were not editable or save was enabled without changes");
    selectors.Single(combo => combo.Name == "startPreset").SelectedIndex = 3;
    Check(saveAutomatic.Enabled, "Changing startup selection did not enable save");
    selectors.Single(combo => combo.Name == "startPreset").SelectedIndex = 0;
    Check(!saveAutomatic.Enabled, "Reverting to saved selections left save enabled");
    selectors.Single(combo => combo.Name == "startPreset").SelectedIndex = 3;
    selectors.Single(combo => combo.Name == "exitPreset").SelectedIndex = 7;
    Check(savedChoices == null, "Automatic selections applied before explicit save");
    typeof(Button).GetMethod("OnClick", dialogFlags)!.Invoke(saveAutomatic, new object[] { EventArgs.Empty });
    Check(savedChoices?.StartPreset == 3 && savedChoices.ExitPreset == 9 && selectors.All(combo => combo.Enabled) && !saveAutomatic.Enabled,
        "Automatic selections did not save correct fixed preset numbers");
    typeof(CameraSettingsDialog).GetMethod("RefreshAutomaticChoices", dialogFlags)!.Invoke(automaticDialog, null);
    Check(!saveAutomatic.Enabled, "Refreshing names falsely marked automatic choices changed");
    selectors.Single(combo => combo.Name == "exitPreset").SelectedIndex = 0;
    Check(saveAutomatic.Enabled, "Changing exit selection after saving did not enable save");
}
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
