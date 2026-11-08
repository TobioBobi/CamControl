using CamControl;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;

internal static partial class Program
{
    private static void WindowPlacementFileChecks()
    {
        string directory = Path.GetFullPath(Path.Combine("artifacts", "ui-preview", "placement-" + Guid.NewGuid()));
        string path = Path.Combine(directory, "window.json");
        try
        {
            Check(WindowPlacement.Load(path) == null, "Missing window settings did not use defaults");
            var value = new WindowPlacement(new(40, 60, 360, 240), new(20, 30, 700, 520), true);
            WindowPlacement.Save(value, path);
            Check(WindowPlacement.Load(path) == value, "Normal/full bounds and maximized state did not survive reload: " +
                (File.Exists(path) ? File.ReadAllText(path) : "file missing"));
            foreach (string invalid in new[] { "broken", "null", "{}", "{\"NormalBounds\":null}",
                "{\"NormalBounds\":{\"Left\":0,\"Top\":0,\"Width\":-1,\"Height\":240}}",
                "{\"NormalBounds\":{\"Left\":1e300,\"Top\":0,\"Width\":360,\"Height\":240}}" })
            {
                File.WriteAllText(path, invalid);
                Check(WindowPlacement.Load(path) == null, "Damaged window settings did not fall back safely");
            }
            WindowPlacement.Save(value, directory); // An unwritable target must not interrupt closing.
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static async Task WindowPlacementWindowChecks()
    {
        string directory = Path.GetFullPath(Path.Combine("artifacts", "ui-preview", "placement-" + Guid.NewGuid()));
        string path = Path.Combine(directory, "window.json");
        using var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        MainWindow Open()
        {
            var session = new CameraSession(new CamControlService(client, "192.168.188.24"));
            var window = new MainWindow(session, path) { ShowActivated = false, ShowInTaskbar = false };
            window.Show();
            return window;
        }
        try
        {
            var window = Open(); await Layout();
            var area = window.CurrentWorkArea();
            window.Left = area.Left + 20; window.Top = area.Top + 20;
            window.Width = 700; window.Height = 520; await Layout();
            var full = new Rect(window.Left, window.Top, window.Width, window.Height);
            window.Width = 360; window.Height = 240; await Layout();
            window.Left = area.Left + 100; window.Top = area.Top + 100; await Layout();
            var small = new Rect(window.Left, window.Top, window.Width, window.Height);
            window.Close(); await Layout();
            window = Open(); await Layout();
            Check(new Rect(window.Left, window.Top, window.Width, window.Height) == small &&
                window.CurrentLayout.View == CameraView.Compact, "Small window did not survive restart with a matching layout");
            window.RestoreFull(); await Layout();
            Check(new Rect(window.Left, window.Top, window.Width, window.Height) == full,
                "Full-view bounds did not survive a compact restart");
            // Enter demo through the actual settings action. It must snapshot the
            // camera rectangle before resetting demo to its independent defaults.
            var switched = new TaskCompletionSource();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            timer.Tick += (_, _) =>
            {
                var dialog = Application.Current.Windows.OfType<CameraSettingsDialog>().FirstOrDefault(w => w.Owner == window);
                if (dialog == null) return;
                timer.Stop();
                dialog.Navigation.SelectedIndex = 3;
                dialog.DemoAction.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                switched.SetResult();
            };
            timer.Start(); window.SettingsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await switched.Task; await Layout();
            Check(window.Session.IsDemo && window.Width == 636 && window.Height == 468,
                "Camera-to-demo switch inherited camera bounds");
            string beforeDemoClose = File.ReadAllText(path);
            window.Width = 360; window.Height = 240; await Layout();
            window.Close(); await Layout();
            Check(File.ReadAllText(path) == beforeDemoClose, "Demo switch/close overwrote the saved camera rectangle");
            window = Open(); await Layout();
            Check(new Rect(window.Left, window.Top, window.Width, window.Height) == full,
                "Camera bounds were lost during an in-session demo switch");
            window.WindowState = WindowState.Maximized; await Layout();
            window.Close(); await Layout();
            window = Open(); await Layout();
            Check(window.WindowState == WindowState.Maximized, "Maximized window did not survive restart");
            window.WindowState = WindowState.Normal; await Layout();
            Check(new Rect(window.Left, window.Top, window.Width, window.Height) == full,
                $"Maximized restart lost normal size/position: expected {full}, got {window.Left},{window.Top},{window.Width},{window.Height}; file {File.ReadAllText(path)}");
            window.WindowState = WindowState.Minimized; await Layout();
            window.Close(); await Layout();
            window = Open(); await Layout();
            Check(window.WindowState == WindowState.Normal && new Rect(window.Left, window.Top, window.Width, window.Height) == full,
                "Minimized close did not restart visibly with the normal bounds");
            window.Width = 620; window.Height = 85; await Layout();
            window.Close(); await Layout();
            window = Open(); await Layout();
            Check(Math.Abs(window.Width - 620) < 1 && Math.Abs(window.Height - 85) < 1 && window.CurrentLayout.View == CameraView.Minimal,
                $"Minimal strip size/layout did not survive restart: {window.Width}x{window.Height}, {window.CurrentLayout}; {File.ReadAllText(path)}");
            window.Close(); await Layout();

            WindowPlacement.Save(new(new(-50000, -50000, 10000, 9000), new(-50000, -50000, 700, 520), true), path);
            window = Open(); await Layout();
            window.WindowState = WindowState.Normal; await Layout();
            Check(area.Contains(new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)),
                "Restored rectangle is outside the remaining monitor work area");
            window.Width = 360; window.Height = 240; await Layout();
            window.RestoreFull(); await Layout();
            Check(area.Contains(new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)) && window.CurrentLayout.View == CameraView.Full,
                "Stored full-view rectangle did not adapt to the remaining work area");
            window.Close(); await Layout();

            string cameraData = File.ReadAllText(path);
            using var demo = new CameraSession(true);
            var demoWindow = new MainWindow(demo, path) { ShowActivated = false, ShowInTaskbar = false };
            demoWindow.Show(); await Layout();
            Check(demoWindow.Width == 636 && demoWindow.Height == 468 && demoWindow.WindowState == WindowState.Normal,
                "Demo restored camera window data");
            demoWindow.Width = 360; demoWindow.Height = 240; await Layout();
            demoWindow.Close(); await Layout();
            Check(File.ReadAllText(path) == cameraData, "Demo overwrote camera window data");

            var dpi = VisualTreeHelper.GetDpi(window);
            // Simulate coordinates recorded at a different DPI, without changing
            // Windows settings. The physical anchor should become +100 DIPs now.
            WindowPlacement.Save(new(new(area.Left + 200, area.Top + 200, 360, 240, dpi.DpiScaleX, dpi.DpiScaleY),
                new((area.Left + 100) / 1.5, (area.Top + 100) / 1.5, 700, 520, dpi.DpiScaleX * 1.5, dpi.DpiScaleY * 1.5), false), path);
            window = Open(); await Layout();
            window.RestoreFull(); await Layout();
            Check(Math.Abs(window.Left - (area.Left + 100)) < 1 && Math.Abs(window.Top - (area.Top + 100)) < 1,
                "Full-view physical position was lost after a saved DPI change");
            window.Close(); await Layout();

            WindowPlacement.Save(new(new(20, 20, 152, 80), new(20, 20, 700, 520), false), path);
            window = Open(); await Layout();
            Check(window.Width == 636 && window.Height == 468,
                "An unusable saved width/height combination did not fall back to the default");
            // Exercise the same already-loaded restoration used when returning
            // from demo, keeping the recording camera transport throughout.
            window.RestoreWindowPlacement(); await Layout();
            Check(window.Width == 636 && window.Height == 468,
                "In-session restoration validated stale demo layout dimensions");
            window.Close(); await Layout();

            File.WriteAllText(path, "broken");
            window = Open(); await Layout();
            Check(window.Width == 636 && window.Height == 468 && window.WindowState == WindowState.Normal,
                "Invalid data prevented the default startup");
            window.Close(); await Layout();
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
