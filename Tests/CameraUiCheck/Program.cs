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
            (Task)execute.Invoke(form, new object[] { command, action })!;
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

        var closing = new FormClosingEventArgs(CloseReason.UserClosing, false);
        type.GetMethod("Form1_FormClosingAsync", flags)!.Invoke(form, new object[] { form, closing });
        Check((bool)type.GetField("finalClose", flags)!.GetValue(form)!, "Camera error prevented closing");
        var finalClosing = new FormClosingEventArgs(CloseReason.UserClosing, false);
        type.GetMethod("Form1_FormClosingAsync", flags)!.Invoke(form, new object[] { form, finalClosing });
        Check(!finalClosing.Cancel, "Final close was cancelled");
        Console.WriteLine($"Passed {checks} UI checks without showing a window or contacting a camera.");
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }
}
