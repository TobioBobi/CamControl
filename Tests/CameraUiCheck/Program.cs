using System.Reflection;
using System.Net;
using CamControl;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form1();
        _ = form.Handle;
        var type = typeof(Form1);
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var execute = type.GetMethod("ExecuteCameraCommandAsync", flags)!;
        var label = (Label)type.GetField("cameraErrorLabel", flags)!.GetValue(form)!;
        int checks = 0;
        void Check(bool ok, string name)
        {
            if (!ok) throw new Exception(name);
            checks++;
        }
        Task Run(Func<Task> command, string action) =>
            (Task)execute.Invoke(form, new object[] { new Func<CancellationToken, Task>(_ => command()), action, CancellationToken.None })!;
        void PumpUntil(Task task)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!task.IsCompleted && DateTime.UtcNow < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            Check(task.IsCompleted, "UI command did not finish");
            task.GetAwaiter().GetResult();
            Application.DoEvents();
        }

        void PumpUntilCondition(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!condition() && DateTime.UtcNow < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            Check(condition(), "UI lifecycle did not finish");
        }

        PumpUntil(Run(() => Task.FromException(new HttpRequestException("test", null, HttpStatusCode.Forbidden)), "Position"));
        Check(label.Text.Contains("HTTP 403"), "HTTP error not displayed");
        Check(form.Controls.OfType<Button>().All(button => button.Enabled), "Controls disabled after error");
        PumpUntil(Run(() => Task.FromException(new TaskCanceledException()), "Zoom"));
        Check(label.Text.Contains("nicht rechtzeitig"), "Timeout not displayed");

        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task olderRequest = Run(() => pending.Task, "Older request");
        Check(!olderRequest.IsCompleted, "Pending request blocked UI");
        PumpUntil(Run(() => Task.FromException(new HttpRequestException("test")), "Bewegungsstopp nicht bestätigt"));
        Check(label.Text.Contains("Bewegungsstopp nicht bestätigt"), "Stop failure not displayed");
        pending.SetResult();
        PumpUntil(olderRequest);
        Check(label.Text.Contains("Bewegungsstopp nicht bestätigt"), "Older success hid newer failure");
        PumpUntil(Run(() => Task.CompletedTask, "Retry"));
        Check(label.Text.Length == 0, "Successful retry did not clear error");

        // Inject a fake service, then exercise the actual button handlers.
        using var handler = new FailingHandler();
        using var client = new HttpClient(handler);
        var serviceType = type.Assembly.GetType("CamControl.CamControlService")!;
        var service = Activator.CreateInstance(serviceType, flags, null, new object[] { client, "192.168.188.24" }, null)!;
        type.GetField("camControlService", flags)!.SetValue(form, service);
        foreach (var name in new[] { "buttonRedner_Click", "buttonRednerLeser_Click", "buttonTisch_Click", "buttonKompletteBuehne_Click", "buttonRednerBildschirm_Click", "buttonDemoStehend_Click", "buttonStandBy_Click", "buttonUp_MouseDown", "buttonDown_MouseDown", "buttonLeft_MouseDown", "buttonRight_MouseDown", "buttonZoomIn_MouseDown", "buttonZoomOut_MouseDown", "button_MouseUpStopDirectionalMovement", "button_MouseUpStopZooming" })
        {
            label.Text = "";
            object args = name.Contains("Mouse") ? new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0) : EventArgs.Empty;
            type.GetMethod(name, flags)!.Invoke(form, new[] { (object)form, args });
            Check(label.Text.Contains("HTTP 500"), name + " did not display failure");
        }
        Check(((Button)type.GetField("buttonStandBy", flags)!.GetValue(form)!).Enabled, "Standby button remained disabled");
        Check(!((PictureBox)type.GetField("pictureBoxLoaderSmall", flags)!.GetValue(form)!).Visible, "Standby loader remained visible");
        foreach (var key in new[] { Keys.Add, Keys.Subtract, Keys.Up, Keys.Down, Keys.Left, Keys.Right })
        {
            foreach (var name in new[] { "Form1_KeyDown", "Form1_KeyUp" })
            {
                label.Text = "";
                type.GetMethod(name, flags)!.Invoke(form, new object[] { form, new KeyEventArgs(key) });
                Check(label.Text.Contains("HTTP 500"), name + " did not handle " + key);
            }
        }
        foreach (var key in new[] { Keys.Up, Keys.Down, Keys.Left, Keys.Right })
        {
            label.Text = "";
            var args = new object[] { Message.Create(form.Handle, 0x100, (IntPtr)(int)key, IntPtr.Zero), key };
            Check((bool)type.GetMethod("ProcessCmdKey", flags)!.Invoke(form, args)!, "Arrow key not handled");
            Check(label.Text.Contains("HTTP 500"), "Arrow key failure not displayed");
            type.GetMethod("Form1_KeyUp", flags)!.Invoke(form, new object[] { form, new KeyEventArgs(key) });
        }

        var keyDown = type.GetMethod("Form1_KeyDown", flags)!;
        var keyUp = type.GetMethod("Form1_KeyUp", flags)!;
        var deactivate = type.GetMethod("OnDeactivate", flags)!;
        var activate = type.GetMethod("OnActivated", flags)!;
        var stopTaskField = type.GetField("stopControlTask", flags)!;
        var positionType = type.Assembly.GetType("CamControl.Position")!;
        var movePosition = type.GetMethod("MoveToPositionAsync", flags)!;
        var prepareSettings = type.GetMethod("PrepareCameraSettingsAsync", flags)!;
        handler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        bool AllStopsSince(int index) => new[] { "ptzstop", "zoomstop", "focusstop" }
            .All(stop => handler.Requests.Skip(index).Any(uri => uri.Query.Contains(stop)));
        bool Pressed(string field) => (bool)type.GetField(field, flags)!.GetValue(form)!;

        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Add) });
        int before = handler.Requests.Count;
        deactivate.Invoke(form, new object[] { EventArgs.Empty });
        PumpUntilCondition(() => stopTaskField.GetValue(form) == null);
        Check(AllStopsSince(before), "Focus loss did not stop all camera channels");
        Check(!Pressed("keyDirectionIsPressed") && !Pressed("keyZoomIsPressed"), "Focus loss retained pressed keys");
        before = handler.Requests.Count;
        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        Check(handler.Requests.Count == before, "Inactive window started movement");
        activate.Invoke(form, new object[] { EventArgs.Empty });
        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        Check(handler.Requests.Count == before + 1, "Direction could not restart after focus loss");
        keyUp.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });

        // Releasing a key cancels an unanswered start and sends a stop promptly.
        handler.Respond = async (uri, token) =>
        {
            if (uri.Query.Contains("&up&"))
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };
        label.Text = "";
        before = handler.Requests.Count;
        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        keyUp.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        PumpUntilCondition(() => handler.Requests.Skip(before).Any(uri => uri.Query.Contains("ptzstop")));
        Check(label.Text.Length == 0, "Key release cancellation was reported as timeout");
        handler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // A pending response on the other channel must not delay a release stop.
        foreach (var scenario in new[]
        {
            (First: Keys.Up, Other: Keys.Add, Pending: "zoomin", Stop: "ptzstop", Release: "StopDirectionAsync"),
            (First: Keys.Add, Other: Keys.Up, Pending: "&up&", Stop: "zoomstop", Release: "StopZoomAsync")
        })
        {
            var otherReply = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            handler.Respond = (uri, _) => uri.Query.Contains(scenario.Pending)
                ? otherReply.Task : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            keyDown.Invoke(form, new object[] { form, new KeyEventArgs(scenario.First) });
            keyDown.Invoke(form, new object[] { form, new KeyEventArgs(scenario.Other) });
            before = handler.Requests.Count;
            Task release = (Task)type.GetMethod(scenario.Release, flags)!.Invoke(form, null)!;
            PumpUntil(release);
            Check(!otherReply.Task.IsCompleted && handler.Requests.Skip(before).Any(uri => uri.Query.Contains(scenario.Stop)),
                "Release stop waited for the other channel: " + scenario.Stop);
            // The released channel can restart without overtaking its stop.
            before = handler.Requests.Count;
            keyDown.Invoke(form, new object[] { form, new KeyEventArgs(scenario.First) });
            Check(handler.Requests.Count == before + 1, "Released channel could not restart independently");
            PumpUntil((Task)type.GetMethod(scenario.Release, flags)!.Invoke(form, null)!);
            otherReply.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
            Application.DoEvents();
            keyUp.Invoke(form, new object[] { form, new KeyEventArgs(scenario.Other) });
        }
        handler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Lost mouse capture must stop even if no MouseUp is delivered.
        type.GetMethod("buttonUp_MouseDown", flags)!.Invoke(form,
            new object[] { form, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0) });
        before = handler.Requests.Count;
        type.GetMethod("CameraButton_MouseCaptureChanged", flags)!.Invoke(form,
            new object[] { type.GetField("buttonUp", flags)!.GetValue(form)!, EventArgs.Empty });
        PumpUntilCondition(() => stopTaskField.GetValue(form) == null);
        Check(AllStopsSince(before) && !Pressed("mouseDirectionIsPressed"), "Lost mouse capture did not stop/reset movement");

        // Reactivation cannot restart motion while a stop response is pending.
        var slowStop = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        handler.Respond = (uri, _) => uri.Query.Contains("zoomstop")
            ? slowStop.Task : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        before = handler.Requests.Count;
        deactivate.Invoke(form, new object[] { EventArgs.Empty });
        activate.Invoke(form, new object[] { EventArgs.Empty });
        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        Check(handler.Requests.Count == before + 3 && !((Button)type.GetField("buttonUp", flags)!.GetValue(form)!).Enabled,
            "Reactivation allowed movement before pending stop completed");
        Task sharedStop = (Task)type.GetMethod("StopCameraControlAsync", flags)!.Invoke(form, null)!;
        Check(handler.Requests.Count == before + 3, "Concurrent lifecycle events duplicated stop requests");
        slowStop.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
        PumpUntil(sharedStop);
        Check(((Button)type.GetField("buttonUp", flags)!.GetValue(form)!).Enabled,
            "Controls stayed disabled after stop acknowledgement");
        handler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Cancel a preset while its preparatory stops are still pending.
        foreach (bool openSettings in new[] { false, true })
        {
            var preparationReply = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            handler.Respond = (uri, _) => uri.Query.Contains("zoomstop")
                ? preparationReply.Task : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            before = handler.Requests.Count;
            Task waitingPreset = (Task)movePosition.Invoke(form, new[] { Enum.Parse(positionType, "Redner") })!;
            Check(!waitingPreset.IsCompleted, "Preset did not wait for preparation");
            Task? settingsPreparation = null;
            if (openSettings)
                settingsPreparation = (Task)prepareSettings.Invoke(form, null)!;
            else
            {
                deactivate.Invoke(form, new object[] { EventArgs.Empty });
                activate.Invoke(form, new object[] { EventArgs.Empty });
            }
            preparationReply.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
            PumpUntil(waitingPreset);
            if (settingsPreparation != null)
            {
                PumpUntil(settingsPreparation);
                type.GetField("settingsDialogOpen", flags)!.SetValue(form, false);
                activate.Invoke(form, new object[] { EventArgs.Empty });
            }
            Check(handler.Requests.Skip(before).All(uri => !uri.Query.Contains("poscall")),
                "Cancelled preset started after preparation: settings=" + openSettings);
            Check(((Button)type.GetField("buttonRedner", flags)!.GetValue(form)!).Enabled,
                "Cancelled preparation left controls disabled");
            handler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            before = handler.Requests.Count;
            PumpUntil((Task)movePosition.Invoke(form, new[] { Enum.Parse(positionType, "Redner") })!);
            Check(handler.Requests.Skip(before).Any(uri => uri.Query.Contains("poscall")),
                "New preset could not start after cancelled preparation");
        }

        // Presets cannot overlap, and settings cancel standby before opening.
        Task standby = (Task)movePosition.Invoke(form, new[] { Enum.Parse(positionType, "StandBy") })!;
        Check(!standby.IsCompleted && !(bool)((Button)type.GetField("buttonStandBy", flags)!.GetValue(form)!).Enabled,
            "Standby did not block overlapping commands");
        before = handler.Requests.Count;
        PumpUntil((Task)movePosition.Invoke(form, new[] { Enum.Parse(positionType, "Redner") })!);
        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        Check(handler.Requests.Count == before, "A second preset or manual movement overlapped standby");
        PumpUntil((Task)prepareSettings.Invoke(form, null)!);
        PumpUntil(standby);
        Check(AllStopsSince(before) && handler.Requests.Skip(before).All(uri => !uri.Query.Contains("focusin")),
            "Settings did not cancel and stop standby before delayed focus");
        before = handler.Requests.Count;
        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        Check(handler.Requests.Count == before, "Movement started while settings were open");
        type.GetField("settingsDialogOpen", flags)!.SetValue(form, false);
        activate.Invoke(form, new object[] { EventArgs.Empty });
        Check(((Button)type.GetField("buttonStandBy", flags)!.GetValue(form)!).Enabled,
            "Controls did not recover after cancelled standby");

        // Close during another standby; all stops must be attempted even on failure.
        Task closingStandby = (Task)movePosition.Invoke(form, new[] { Enum.Parse(positionType, "StandBy") })!;
        handler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        before = handler.Requests.Count;
        var closing = new FormClosingEventArgs(CloseReason.UserClosing, false);
        type.GetMethod("Form1_FormClosingAsync", flags)!.Invoke(form, new object[] { form, closing });
        PumpUntilCondition(() => (bool)type.GetField("finalClose", flags)!.GetValue(form)!);
        PumpUntil(closingStandby);
        Check((bool)type.GetField("finalClose", flags)!.GetValue(form)!, "Camera error prevented closing");
        Check(AllStopsSince(before) && handler.Requests.Skip(before).All(uri => !uri.Query.Contains("poscall") && !uri.Query.Contains("focusin")),
            "Closing sent motion after failed stop or left delayed focus running");
        var finalClosing = new FormClosingEventArgs(CloseReason.UserClosing, false);
        type.GetMethod("Form1_FormClosingAsync", flags)!.Invoke(form, new object[] { form, finalClosing });
        Check(!finalClosing.Cancel, "Final close was cancelled");
        Console.WriteLine($"Passed {checks} UI checks without showing a window or contacting a camera.");
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = new();
        public Func<Uri, CancellationToken, Task<HttpResponseMessage>> Respond { get; set; } =
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Respond(request.RequestUri!, cancellationToken);
        }
    }
}
