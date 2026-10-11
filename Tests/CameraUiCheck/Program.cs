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
        var settingsButton = (Button)type.GetField("settingsButton", flags)!.GetValue(form)!;
        int checks = 0;
        void Check(bool ok, string name)
        {
            if (!ok) throw new Exception(name);
            checks++;
        }
        Check(settingsButton.Text.Contains("Einstellungen") && settingsButton.Enabled,
            "Visible settings access missing");
        Check(settingsButton.Parent is Panel { Dock: DockStyle.Top } &&
            ((Button)type.GetField("buttonRedner", flags)!.GetValue(form)!).Top >= settingsButton.Parent.Bottom,
            "Settings access overlaps camera controls");
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

        // Exercise font autoscaling and restricted screen space without camera transport.
        IEnumerable<Control> Children(Control parent) => parent.Controls.Cast<Control>()
            .SelectMany(child => new[] { child }.Concat(Children(child)));
        void ShowOffscreen(Form window)
        {
            window.ShowInTaskbar = false;
            window.StartPosition = FormStartPosition.Manual;
            window.Location = new Point(-32000, -32000);
            window.Show();
            Application.DoEvents();
        }
        using (var layoutMain = (Form1)Activator.CreateInstance(type, flags, null, new object[] { true }, null)!)
        {
            ShowOffscreen(layoutMain);
            layoutMain.Font = new Font(layoutMain.Font.FontFamily, layoutMain.Font.Size * 2);
            layoutMain.PerformAutoScale();
            Application.DoEvents();
            var area = Screen.FromControl(layoutMain).WorkingArea;
            Check(layoutMain.Height <= area.Height && layoutMain.Width <= area.Width,
                "Scaled demo window exceeded the screen working area");
            var preview = (Control)type.GetField("demoView", flags)!.GetValue(layoutMain)!;
            var error = (Control)type.GetField("cameraErrorLabel", flags)!.GetValue(layoutMain)!;
            Check(preview.Height > 0 && preview.Bottom <= error.Top && error.Bottom <= layoutMain.ClientSize.Height,
                $"Resized demo preview covered the error area or escaped the window: preview={preview.Bounds}, error={error.Bounds}, client={layoutMain.ClientSize}");
            int scaledDemoHeight = layoutMain.Height;
            for (int switchCount = 0; switchCount < 3; switchCount++)
            {
                type.GetMethod("SetDemoMode", flags)!.Invoke(layoutMain, new object[] { false });
                type.GetMethod("SetDemoMode", flags)!.Invoke(layoutMain, new object[] { true });
                Application.DoEvents();
            }
            Check(layoutMain.Height == scaledDemoHeight, "Switching modes changed the capped demo window height cumulatively");
            var layoutService = type.GetField("camControlService", flags)!.GetValue(layoutMain)!;
            var layoutDemo = type.GetField("demoCamera", flags)!.GetValue(layoutMain)!;
            using var editor = (Form)Activator.CreateInstance(type.Assembly.GetType("CamControl.PresetEditorDialog")!,
                new object[] { layoutService, Enum.Parse(type.Assembly.GetType("CamControl.Position")!, "Redner"),
                    new string('W', 32), new Action<string>(_ => { }), layoutDemo })!;
            ShowOffscreen(editor);
            editor.Font = new Font(editor.Font.FontFamily, editor.Font.Size * 2);
            editor.PerformAutoScale();
            editor.Height = Math.Min(600, area.Height);
            Application.DoEvents();
            var close = (Button)editor.CancelButton!;
            var closeBounds = editor.RectangleToClient(close.RectangleToScreen(close.ClientRectangle));
            Check(editor.Height <= area.Height && editor.ClientRectangle.Contains(closeBounds),
                "Preset close button was not reachable on a restricted screen");
            var scroll = Children(editor).OfType<Panel>().Single(control => control.Name == "presetEditorContent");
            Check(scroll.AutoScroll && scroll.VerticalScroll.Visible, "Preset contents could not scroll at large font sizes");
            var storeButton = Children(editor).Single(control => control.Name == "storePreset");
            scroll.ScrollControlIntoView(storeButton);
            Application.DoEvents();
            Check(scroll.ClientRectangle.Contains(scroll.RectangleToClient(storeButton.RectangleToScreen(storeButton.ClientRectangle))),
                "Scrolling did not make the preset save button reachable");
            using var settings = (Form)Activator.CreateInstance(type.Assembly.GetType("CamControl.CameraSettingsDialog")!,
                new object?[] { "192.168.188.24", null, true, null, null, null, null })!;
            ShowOffscreen(settings);
            settings.Font = new Font(settings.Font.FontFamily, settings.Font.Size * 2);
            settings.PerformAutoScale();
            Application.DoEvents();
            var settingsClose = (Button)settings.CancelButton!;
            Check(settings.Height <= area.Height && settings.ClientRectangle.Contains(
                settings.RectangleToClient(settingsClose.RectangleToScreen(settingsClose.ClientRectangle))),
                "Scaled settings window hid its close button");
        }

        using (var demoForm = (Form1)Activator.CreateInstance(type, flags, null, new object[] { true }, null)!)
        {
            _ = demoForm.Handle;
            Check(demoForm.Text.Contains("Demo") &&
                (bool)type.GetProperty("IsDemoMode", flags)!.GetValue(demoForm)!, "Demo entry was not marked");
            var demo = type.GetField("demoCamera", flags)!.GetValue(demoForm)!;
            object DemoState() => demo.GetType().GetProperty("State")!.GetValue(demo)!;
            bool DemoFlag(string name) => (bool)DemoState().GetType().GetProperty(name)!.GetValue(DemoState())!;
            var directionType = type.Assembly.GetType("CamControl.Direction")!;
            var zoomType = type.Assembly.GetType("CamControl.Zoom")!;
            PumpUntil((Task)type.GetMethod("StartDirectionAsync", flags)!.Invoke(demoForm,
                new object[] { Enum.Parse(directionType, "Up"), true })!);
            PumpUntil((Task)type.GetMethod("StartZoomAsync", flags)!.Invoke(demoForm,
                new object[] { Enum.Parse(zoomType, "ZoomIn"), true })!);
            Check(DemoFlag("Moving") && DemoFlag("Zooming"), "Actual UI did not drive demo channels");
            type.GetMethod("OnDeactivate", flags)!.Invoke(demoForm, new object[] { EventArgs.Empty });
            PumpUntilCondition(() => type.GetField("stopControlTask", flags)!.GetValue(demoForm) == null);
            Check(!DemoFlag("Moving") && !DemoFlag("Zooming") && !DemoFlag("Focusing"), "Demo ignored focus-loss stops");
            type.GetMethod("OnActivated", flags)!.Invoke(demoForm, new object[] { EventArgs.Empty });
            type.GetMethod("buttonRedner_Click", flags)!.Invoke(demoForm, new object[] { demoForm, EventArgs.Empty });
            Check((string)DemoState().GetType().GetProperty("Action")!.GetValue(DemoState())! == "Redner",
                "Preset button did not reach the demo handler");
            int demoHeight = demoForm.ClientSize.Height;
            PumpUntil((Task)type.GetMethod("PrepareCameraSettingsAsync", flags)!.Invoke(demoForm, null)!);
            type.GetMethod("SetDemoMode", flags)!.Invoke(demoForm, new object[] { false });
            Check(!demoForm.Text.Contains("Demo") && demoForm.ClientSize.Height < demoHeight, "Leaving demo retained its view or label");
            type.GetMethod("SetDemoMode", flags)!.Invoke(demoForm, new object[] { true });
            Check(demoForm.ClientSize.Height == demoHeight, "Repeated demo entry accumulated window height");
            var presetType = type.Assembly.GetType("CamControl.PresetEditorDialog")!;
            var demoPositionType = type.Assembly.GetType("CamControl.Position")!;
            var demoService = type.GetField("camControlService", flags)!.GetValue(demoForm)!;
            // Exercise the actual modal message loop: Close() on an unshown form
            // does not reproduce the synchronous demo-stop reentrancy.
            using (var modalEditor = (Form)Activator.CreateInstance(presetType, BindingFlags.Public | BindingFlags.Instance,
                null, new object[] { demoService, Enum.Parse(demoPositionType, "Redner"), "Redner",
                    new Action<string>(_ => { }), demo }, null)!)
            {
                modalEditor.StartPosition = FormStartPosition.Manual;
                modalEditor.Location = new Point(-32000, -32000);
                bool timedOut = false;
                using var watchdog = new System.Windows.Forms.Timer { Interval = 2000 };
                watchdog.Tick += (_, _) =>
                {
                    timedOut = true;
                    watchdog.Stop();
                    presetType.GetField("finalClose", flags)!.SetValue(modalEditor, true);
                    modalEditor.Close();
                };
                modalEditor.Shown += async (_, _) =>
                {
                    await (Task)presetType.GetMethod("RunPositionAsync", flags)!.Invoke(modalEditor, new object[] { true })!;
                    watchdog.Start();
                    ((Button)modalEditor.CancelButton!).PerformClick();
                };
                modalEditor.ShowDialog();
                watchdog.Stop();
                Check(!timedOut, "Modal demo editor needed a second close after saving a preset");
                Check(!DemoFlag("Moving") && !DemoFlag("Zooming"), "Modal editor closed without stopping demo channels");
            }
            string? savedName = null;
            using var editor = (Form)Activator.CreateInstance(presetType, BindingFlags.Public | BindingFlags.Instance,
                null, new object[] { demoService, Enum.Parse(demoPositionType, "Redner"), "Redner",
                    new Action<string>(value => savedName = value), demo }, null)!;
            _ = editor.Handle;
            Task EditorTask(string method, params object[] args) => (Task)presetType.GetMethod(method, flags)!.Invoke(editor, args)!;
            PumpUntil(EditorTask("RunPositionAsync", false));
            var original = DemoState();
            var editorName = (TextBox)presetType.GetField("name", flags)!.GetValue(editor)!;
            editorName.Text = "Mein Redner";
            presetType.GetMethod("SaveName", flags)!.Invoke(editor, null);
            Check(savedName == "Mein Redner" && DemoState().Equals(original), "Renaming changed camera state");
            Func<CancellationToken, Task> editorZoom = token => (Task)demoService.GetType().GetMethod("Zoom")!.Invoke(demoService,
                new object[] { Enum.Parse(zoomType, "ZoomIn"), token })!;
            PumpUntil(EditorTask("StartMotionAsync", editorZoom));
            demo.GetType().GetMethod("Advance")!.Invoke(demo, new object[] { TimeSpan.FromMilliseconds(500) });
            PumpUntil(EditorTask("RunPositionAsync", true));
            var stored = DemoState();
            Check(!DemoFlag("Zooming") && !DemoFlag("Moving"), "Saving did not stop manual channels first");
            PumpUntil((Task)demoService.GetType().GetMethod("MoveToPosition")!.Invoke(demoService,
                new object[] { Enum.Parse(demoPositionType, "Tisch"), CancellationToken.None })!);
            PumpUntil(EditorTask("RunPositionAsync", false));
            Check(DemoState().GetType().GetProperty("Zoom")!.GetValue(DemoState())!.Equals(
                stored.GetType().GetProperty("Zoom")!.GetValue(stored)), "Editor failed to recall custom zoom");
            PumpUntil(EditorTask("StartMotionAsync", editorZoom));
            presetType.GetMethod("OnDeactivate", flags)!.Invoke(editor, new object[] { EventArgs.Empty });
            PumpUntilCondition(() => !DemoFlag("Zooming"));
            Check(!DemoFlag("Moving"), "Editor focus loss left motion active");
            presetType.GetMethod("OnActivated", flags)!.Invoke(editor, new object[] { EventArgs.Empty });
            PumpUntil(EditorTask("StartMotionAsync", editorZoom));
            editor.Close();
            PumpUntilCondition(() => !DemoFlag("Zooming"));
            Check(!DemoFlag("Moving"), "Editor close left motion active");
            using var saveHandler = new FailingHandler();
            using var saveClient = new HttpClient(saveHandler);
            var fakeService = Activator.CreateInstance(demoService.GetType(), flags, null,
                new object[] { saveClient, "192.168.188.24" }, null)!;
            foreach (bool saving in new[] { false, true })
            {
                using var waitingEditor = (Form)Activator.CreateInstance(presetType, BindingFlags.Public | BindingFlags.Instance,
                    null, new object[] { fakeService, Enum.Parse(demoPositionType, "Redner"), "Redner", new Action<string>(_ => { }), null! }, null)!;
                _ = waitingEditor.Handle;
                saveHandler.Requests.Clear();
                var waitingStop = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                saveHandler.Respond = (uri, _) => uri.Query.Contains("ptzstop") ? waitingStop.Task
                    : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                Task waitingOperation = (Task)presetType.GetMethod("RunPositionAsync", flags)!.Invoke(waitingEditor, new object[] { saving })!;
                Check(!waitingOperation.IsCompleted, "Preset preparation did not wait for stop");
                presetType.GetMethod("OnDeactivate", flags)!.Invoke(waitingEditor, new object[] { EventArgs.Empty });
                presetType.GetMethod("OnActivated", flags)!.Invoke(waitingEditor, new object[] { EventArgs.Empty });
                waitingStop.SetResult(new HttpResponseMessage(HttpStatusCode.OK));
                PumpUntil(waitingOperation);
                PumpUntilCondition(() => presetType.GetField("stopping", flags)!.GetValue(waitingEditor) == null);
                Check(saveHandler.Requests.All(uri => !uri.Query.Contains("poscall") && !uri.Query.Contains("posset")),
                    "Focus loss/return resumed a cancelled preset action");
                saveHandler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                PumpUntil((Task)presetType.GetMethod("RunPositionAsync", flags)!.Invoke(waitingEditor, new object[] { saving })!);
                Check(saveHandler.Requests.Last().Query.Contains(saving ? "posset" : "poscall"), "New preset action did not recover after cancellation");

                CancellationToken pendingPresetToken = default;
                saveHandler.Respond = async (uri, token) =>
                {
                    if (uri.Query.Contains(saving ? "posset" : "poscall"))
                    {
                        pendingPresetToken = token;
                        await Task.Delay(Timeout.Infinite, token);
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK);
                };
                Task pendingPreset = (Task)presetType.GetMethod("RunPositionAsync", flags)!.Invoke(waitingEditor, new object[] { saving })!;
                presetType.GetMethod("OnDeactivate", flags)!.Invoke(waitingEditor, new object[] { EventArgs.Empty });
                PumpUntil(pendingPreset);
                PumpUntilCondition(() => presetType.GetField("stopping", flags)!.GetValue(waitingEditor) == null);
                Check(pendingPresetToken.IsCancellationRequested, "Focus loss did not cancel an in-flight preset request");
            }
            saveHandler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            using (var closeErrorEditor = (Form)Activator.CreateInstance(presetType, BindingFlags.Public | BindingFlags.Instance,
                null, new object[] { fakeService, Enum.Parse(demoPositionType, "Redner"), "Redner", new Action<string>(_ => { }), null! }, null)!)
            {
                string? forwardedStopError = null;
                presetType.GetEvent("CloseStopFailed", flags)!.GetAddMethod(true)!.Invoke(closeErrorEditor,
                    new object[] { new Action<string>(error => forwardedStopError = error) });
                closeErrorEditor.StartPosition = FormStartPosition.Manual;
                closeErrorEditor.Location = new Point(-32000, -32000);
                bool closeTimedOut = false;
                using var closeWatchdog = new System.Windows.Forms.Timer { Interval = 2000 };
                closeWatchdog.Tick += (_, _) =>
                {
                    closeTimedOut = true;
                    closeWatchdog.Stop();
                    presetType.GetField("finalClose", flags)!.SetValue(closeErrorEditor, true);
                    closeErrorEditor.Close();
                };
                closeErrorEditor.Shown += (_, _) => { closeWatchdog.Start(); ((Button)closeErrorEditor.CancelButton!).PerformClick(); };
                closeErrorEditor.ShowDialog();
                closeWatchdog.Stop();
                Check(!closeTimedOut && !closeErrorEditor.Visible, "Failed stop prevented closing the modal editor");
                Check(forwardedStopError?.Contains("HTTP 500") == true, "Closing lost the failed stop error instead of forwarding it");
            }
            saveHandler.Requests.Clear();
            using var failedEditor = (Form)Activator.CreateInstance(presetType, BindingFlags.Public | BindingFlags.Instance,
                null, new object[] { fakeService, Enum.Parse(demoPositionType, "Redner"), "Redner",
                    new Action<string>(_ => throw new IOException("Test write failure")), null! }, null)!;
            _ = failedEditor.Handle;
            PumpUntil((Task)presetType.GetMethod("RunPositionAsync", flags)!.Invoke(failedEditor, new object[] { true })!);
            var saveStatus = (Label)presetType.GetField("status", flags)!.GetValue(failedEditor)!;
            Check(saveStatus.Text.Contains("HTTP 500") && !saveStatus.Text.Contains("erfolgreich"), "Failed stop falsely reported preset saved");
            Check(saveHandler.Requests.All(uri => !uri.Query.Contains("posset")), "Save proceeded despite failed stop");
            saveHandler.Respond = (uri, _) => Task.FromResult(new HttpResponseMessage(
                uri.Query.Contains("posset") ? HttpStatusCode.Forbidden : HttpStatusCode.OK));
            PumpUntil((Task)presetType.GetMethod("RunPositionAsync", flags)!.Invoke(failedEditor, new object[] { true })!);
            Check(saveStatus.Text.Contains("HTTP 403") && !saveStatus.Text.Contains("erfolgreich"), "Rejected preset save falsely reported success");
            var failedName = (TextBox)presetType.GetField("name", flags)!.GetValue(failedEditor)!;
            failedName.Text = "Neuer Name";
            presetType.GetMethod("SaveName", flags)!.Invoke(failedEditor, null);
            Check(saveStatus.Text.Contains("nicht gespeichert"), "Name write failure was not shown");
        }

        PumpUntil(Run(() => Task.FromException(new HttpRequestException("test", null, HttpStatusCode.Forbidden)), "Position"));
        var automaticType = type.Assembly.GetType("CamControl.AutomaticPositions")!;
        foreach (var exitCase in new[] { (Number: (int?)null, Stop: true), (Number: (int?)6, Stop: true),
            (Number: (int?)9, Stop: true), (Number: (int?)6, Stop: false) })
        {
            int? exitNumber = exitCase.Number;
            using var automaticForm = (Form1)Activator.CreateInstance(type, flags, null, new object[] { true }, null)!;
            _ = automaticForm.Handle;
            using var automaticHandler = new FailingHandler { Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)) };
            using var automaticClient = new HttpClient(automaticHandler);
            var automaticServiceType = type.Assembly.GetType("CamControl.CamControlService")!;
            var automaticService = Activator.CreateInstance(automaticServiceType, flags, null, new object[] { automaticClient, "192.168.188.24" }, null)!;
            type.GetField("camControlService", flags)!.SetValue(automaticForm, automaticService);
            object automaticChoices = Activator.CreateInstance(automaticType, true)!;
            type.GetField("demoAutomaticPositions", flags)!.SetValue(automaticForm, automaticChoices);
            type.GetMethod("Form1_Load", flags)!.Invoke(automaticForm, new object[] { automaticForm, EventArgs.Empty });
            Check(automaticHandler.Requests.Count == 0, "None startup selection still moved camera");
            automaticType.GetProperty("StartPreset")!.SetValue(automaticChoices, 2);
            type.GetMethod("Form1_Load", flags)!.Invoke(automaticForm, new object[] { automaticForm, EventArgs.Empty });
            Check(automaticHandler.Requests.Last().Query == "?ptzcmd&poscall&2", "Startup ignored selected preset");
            automaticHandler.Requests.Clear();
            if (!exitCase.Stop) automaticHandler.Respond = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            automaticType.GetProperty("ExitPreset")!.SetValue(automaticChoices, exitNumber);
            type.GetMethod("Form1_FormClosingAsync", flags)!.Invoke(automaticForm,
                new object[] { automaticForm, new FormClosingEventArgs(CloseReason.UserClosing, false) });
            PumpUntilCondition(() => (bool)type.GetField("finalClose", flags)!.GetValue(automaticForm)!);
            Check(automaticHandler.Requests.Count == (exitNumber.HasValue && exitCase.Stop ? 4 : 3), "Exit selection sent unexpected commands");
            Check(automaticHandler.Requests.Take(3).All(uri => uri.Query.Contains("stop")), "Exit preset ran before all stops");
            if (exitNumber.HasValue && exitCase.Stop) Check(automaticHandler.Requests.Last().Query == $"?ptzcmd&poscall&{exitNumber}", "Exit ignored selected preset");
            Check(automaticHandler.Requests.All(uri => !uri.Query.Contains("focusin")), "Normal presets still sent special focus motion");
        }
        Check(label.Text.Contains("HTTP 403"), "HTTP error not displayed");
        Check(label.Text.Contains("Einstellungen") && !label.Text.Contains("fünfmal"), "Error hints reference hidden access");
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
        handler.Respond = async (uri, token) =>
        {
            if (uri.Query.Contains("poscall")) await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };
        Task standby = (Task)movePosition.Invoke(form, new[] { Enum.Parse(positionType, "StandBy") })!;
        Check(settingsButton.Enabled, "Settings unavailable during a preset");
        Check(!standby.IsCompleted && !(bool)((Button)type.GetField("buttonStandBy", flags)!.GetValue(form)!).Enabled,
            "Standby did not block overlapping commands");
        before = handler.Requests.Count;
        PumpUntil((Task)movePosition.Invoke(form, new[] { Enum.Parse(positionType, "Redner") })!);
        keyDown.Invoke(form, new object[] { form, new KeyEventArgs(Keys.Up) });
        Check(handler.Requests.Count == before, "A second preset or manual movement overlapped standby");
        PumpUntil((Task)prepareSettings.Invoke(form, null)!);
        Check(!settingsButton.Enabled, "Settings button allowed duplicate dialogs");
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
