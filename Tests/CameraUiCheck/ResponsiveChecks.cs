using CamControl;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Runtime.InteropServices;

internal static partial class Program
{
    private static async Task ResponsiveChecks()
    {
        using var handler = new RecordingHandler(); using var client = new HttpClient(handler);
        using var session = new CameraSession(new CamControlService(client, "192.168.188.24"));
        var window = new MainWindow(session); ShowOffscreen(window); await Layout();
        await session.Controller.SetActiveAsync(true);
        var presets = Descendants(window.Presets).OfType<Button>().ToArray();
        var contents = presets.Select(b => b.Content).ToArray();
        var originalController = session.Controller;
        string output = System.IO.Path.GetFullPath("artifacts/ui-preview/" + CultureInfo.CurrentUICulture.Name);

        async Task Resize(double width, double height, CameraView view, PresetArrangement arrangement)
        {
            int before = handler.Commands.Count;
            window.Width = width; window.Height = height; await Layout();
            Check(window.CurrentLayout == new CameraLayout(view, arrangement), $"Wrong layout at {width}×{height}: {window.CurrentLayout}");
            Check(Math.Abs(window.Width - width) < 1 && Math.Abs(window.Height - height) < 1, $"Reflow changed outer window size from {width}×{height} to {window.Width}×{window.Height}");
            Check(handler.Commands.Count == before, "Idle resize sent camera commands");
            Check(ReferenceEquals(originalController, session.Controller), "Layout replaced the camera controller");
            Check(window.SettingsButton.IsVisible == (view == CameraView.Full) && window.Status.IsVisible == (view == CameraView.Full), "Slim layout retained settings/status");
            Check(window.RestoreButton.IsVisible == (view != CameraView.Full), "Restore action missing in slim view");
            Check(window.ManualPanel.IsVisible == (view != CameraView.Minimal), "Incorrect manual control visibility");
            Check(presets.Select(b => b.Content).SequenceEqual(contents), "Reflow rebuilt preset contents");
            Check(window.ControlsScroll.ScrollableHeight == 0 && window.ControlsScroll.ScrollableWidth == 0,
                $"Essential controls require scrolling in {view}/{arrangement}: {window.ControlsScroll.ScrollableWidth}×{window.ControlsScroll.ScrollableHeight}");
            var viewport = new Rect(new Point(), window.ControlsScroll.RenderSize);
            viewport.Inflate(0.5, 0.5); // Fractional-DPI layout rounding.
            foreach (var button in presets)
            {
                var bounds = button.TransformToAncestor(window.ControlsScroll).TransformBounds(new Rect(button.RenderSize));
                Check(button.IsVisible && button.ActualHeight >= 28 && viewport.Contains(bounds), $"Preset clipped in {view}/{arrangement}: {button.Name} {bounds} viewport {viewport}");
                int n = int.Parse(button.Name[6..]);
                Check((string)button.ToolTip == session.Names[n] && AutomationProperties.GetName(button).Contains(session.Names[n]), "Full preset name inaccessible");
                var texts = Descendants(button).OfType<TextBlock>().ToArray();
                Check(texts[0].Text == n.ToString() && texts[0].ActualWidth >= 6 && texts[1].TextTrimming == TextTrimming.CharacterEllipsis, "Shortcut or ellipsis missing");
            }
            if (view == CameraView.Compact)
            {
                foreach (var button in Descendants(window.ManualPanel).OfType<Button>())
                {
                    var bounds = button.TransformToAncestor(window.ControlsScroll).TransformBounds(new Rect(button.RenderSize));
                    Check(button.IsVisible && button.ActualWidth >= 26 && button.ActualHeight >= 26 && viewport.Contains(bounds), $"Small manual target clipped: {button.Name} {bounds}");
                }
            }
        }
        foreach (bool dark in new[] { false, true })
        {
            Theme.Apply(dark);
            foreach (var (width, height, view, arrangement) in new[]
            {
                (360d, 240d, CameraView.Compact, PresetArrangement.Tiles),
                (240d, 215d, CameraView.Minimal, PresetArrangement.Tiles),
                (620d, 145d, CameraView.Compact, PresetArrangement.Strip),
                (620d, 85d, CameraView.Minimal, PresetArrangement.Strip),
                (160d, 460d, CameraView.Compact, PresetArrangement.Column),
                (160d, 310d, CameraView.Minimal, PresetArrangement.Column),
                (636d, 468d, CameraView.Full, PresetArrangement.Tiles)
            })
            {
                await Resize(width, height, view, arrangement);
                Screenshot(window, System.IO.Path.Combine(output, $"responsive-{view}-{arrangement}-{(dark ? "dark" : "light")}.png"));
            }
        }
        await Resize(360, 240, CameraView.Compact, PresetArrangement.Tiles);
        var left = Descendants(window.ManualPanel).OfType<Button>().Single(b => b.Name == "moveLeft");
        left.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });
        Check(session.Controller.IsMoving && CameraInput.GetIsHeld(left), "Small mouse control did not start");
        int movingCommands = handler.Commands.Count;
        window.Width += 2; await Layout();
        Check(session.Controller.IsMoving && handler.Commands.Count == movingCommands, "Same-layout resize rebuilt held inputs");
        var pendingStop = new TaskCompletionSource<HttpResponseMessage>();
        handler.Respond = (command, _) => command.StartsWith("ptzstop") ? pendingStop.Task : Ok();
        window.Width = 240; window.Height = 215; await Layout();
        Check(!session.Controller.IsMoving && !session.Controller.ControlsEnabled && !left.IsMouseCaptured && !CameraInput.GetIsHeld(left), "Reflow did not release capture/held state and gate pending stop");
        Check(handler.Commands.Skip(movingCommands).Order().SequenceEqual(new[] { "ptzstop&10&10", "zoomstop&5", "focusstop&5" }.Order()), "Reflow failed to send all safety stops");
        int stoppingCommands = handler.Commands.Count;
        await window.Input.PressKeyAsync(Key.Right);
        Check(handler.Commands.Count == stoppingCommands, "Input restarted during layout stop");
        Task reflowStop = session.Controller.StopAsync();
        pendingStop.SetResult(new(HttpStatusCode.OK)); await reflowStop; await Layout(); handler.Respond = (_, _) => Ok();
        await window.Input.ReleaseKeyAsync(Key.Right); await window.Input.PressKeyAsync(Key.Right);
        Check(session.Controller.IsMoving, "Keyboard movement unavailable in minimal view");
        await window.Input.ReleaseKeyAsync(Key.Right); Check(!session.Controller.IsMoving, "Minimal keyboard release failed");

        await Resize(360, 240, CameraView.Compact, PresetArrangement.Tiles);
        var zoomIn = Descendants(window.ManualPanel).OfType<Button>().Single(b => b.Name == "ZoomIn");
        zoomIn.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });
        Check(session.Controller.IsMoving && handler.Commands.Last() == "zoomin&5", "Small mouse zoom did not start");
        zoomIn.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseUpEvent });
        await Layout(); Check(!session.Controller.IsMoving && handler.Commands.Last() == "zoomstop&5", "Small mouse zoom release did not stop");
        await window.Input.PressKeyAsync(Key.Add); await window.Input.PressKeyAsync(Key.Down);
        window.Width = 620; window.Height = 145; await Layout();
        Check(!session.Controller.IsMoving && window.CurrentLayout.Arrangement == PresetArrangement.Strip, "Reordering visible controls did not stop both keyboard channels");
        int stoppedCommands = handler.Commands.Count;
        await window.Input.ReleaseKeyAsync(Key.Add); await window.Input.ReleaseKeyAsync(Key.Down);
        Check(handler.Commands.Count == stoppedCommands, "Stale key release sent extra commands after reflow");
        await Resize(160, 310, CameraView.Minimal, PresetArrangement.Column);
        var source = PresentationSource.FromVisual(window)!;
        window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.D3) { RoutedEvent = Keyboard.PreviewKeyDownEvent }); await Layout();
        Check(handler.Commands.Last() == $"poscall&{session.Numbers[3]}" && presets.Single(b => b.Name == "preset3").Tag as string == "Requested", $"Minimal shortcut lost mapping/highlight (enabled={session.Controller.ControlsEnabled}, modifiers={Keyboard.Modifiers}, last={string.Join(",", handler.Commands.TakeLast(8))})");
        session.Controller.ReportStopFailure(Texts.Error_Connection); await Layout();
        Check(window.ErrorIndicator.IsVisible && AutomationProperties.GetName(window.ErrorIndicator).Contains(Texts.Error_Connection) && !window.Status.IsVisible, "Slim camera error inaccessible");
        window.ErrorIndicator.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(window.ErrorIndicator.ToolTip is ToolTip { IsOpen: true }, "Keyboard/click error details unavailable");
        ((ToolTip)window.ErrorIndicator.ToolTip).IsOpen = false;
        Screenshot(window, System.IO.Path.Combine(output, "responsive-error.png"));
        await window.RecallAsync(1); await Layout(); Check(!window.ErrorIndicator.IsVisible, "Recovered error indicator remained visible");

        var area = window.CurrentWorkArea();
        window.Left = area.Left + 20; window.Top = area.Top + 20;
        await Resize(700, 520, CameraView.Full, PresetArrangement.Tiles);
        var saved = new Rect(window.Left, window.Top, window.Width, window.Height);
        await Resize(360, 240, CameraView.Compact, PresetArrangement.Tiles);
        int restoreCommands = handler.Commands.Count;
        window.RestoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Layout();
        Check(window.CurrentLayout.View == CameraView.Full && new Rect(window.Left, window.Top, window.Width, window.Height) == saved, "One-click restore lost last full size/position");
        Check(handler.Commands.Count == restoreCommands, "Restore requested camera motion");
        NativeSizingChecks(window, handler);
        // Slowly traverse the full-view boundary in both directions. Reflow may
        // change content once per direction; it must never change the window.
        int transitions = 0; var previous = window.CurrentLayout;
        foreach (double width in Enumerable.Range(0, 32).Select(i => 560d + i).Concat(Enumerable.Range(0, 32).Reverse().Select(i => 560d + i)))
        {
            window.Width = width; await Layout();
            Check(Math.Abs(window.Width - width) < 1 && window.Height == saved.Height, "Boundary traversal resized the outer window");
            if (window.CurrentLayout != previous) transitions++;
            previous = window.CurrentLayout;
        }
        Check(transitions <= 3 && handler.Commands.Count == restoreCommands, "Boundary traversal oscillated or sent camera commands");
        // A restricted work area must still allow one-click access to settings.
        window.MaxWidth = 420; window.MaxHeight = 330; await Layout();
        window.RestoreFull(); await Layout();
        Check(window.CurrentLayout.View == CameraView.Full && window.SettingsButton.IsVisible && window.Width <= 420 && window.Height <= 330, "Constrained full restore failed");
        window.MaxWidth = area.Width; window.MaxHeight = area.Height;
        await Resize(700, 520, CameraView.Full, PresetArrangement.Tiles);
        window.Left = -10000; window.Top = -10000;

        window.FontSize = 21;
        await Resize(430, 310, CameraView.Compact, PresetArrangement.Tiles);
        Check(Descendants(window.Presets).OfType<TextBlock>().All(t => t.FontSize == 21), "Slim view shrank enlarged text");
        Screenshot(window, System.IO.Path.Combine(output, "responsive-large-text.png"));
        window.FontSize = 14;
        var host = new Grid(); window.Content = null; host.Children.Add(window.LayoutRoot); window.Content = host;
        // Presentation scaling exercises the same DIP budget as a per-monitor
        // DPI change, without changing the user's Windows display settings.
        foreach (double scale in new[] { 1d, 1.25, 1.5, 2 })
        {
            window.LayoutRoot.LayoutTransform = new ScaleTransform(scale, scale);
            await Resize(360 * scale, 230 * scale + 40, CameraView.Compact, PresetArrangement.Tiles);
            Screenshot(window, System.IO.Path.Combine(output, $"responsive-scale-{scale.ToString(CultureInfo.InvariantCulture)}.png"));
        }
        window.LayoutRoot.LayoutTransform = Transform.Identity;
        await Layout();
        await window.Input.PressKeyAsync(Key.Left); window.Close(); await Layout();
        Check(!window.IsVisible && handler.Commands.Last() == "focusstop&5", "Slim close failed to stop/close");

        var m = new CameraLayoutMetrics(68, 32, 28, 554, 390);
        var full = new CameraLayout(CameraView.Full, PresetArrangement.Tiles);
        var slim = m.Select(new(553, 390), full);
        Check(slim.View != CameraView.Full && m.Select(new(559, 395), slim).View != CameraView.Full && m.Select(new(566, 402), slim).View == CameraView.Full, "Full-view hysteresis missing");
        var compact = new CameraLayout(CameraView.Compact, PresetArrangement.Tiles);
        var minimal = m.Select(new(240, 190), compact);
        Check(m.Select(new(m.TileWidth + 3 * m.Key + 20, 200), minimal).View == CameraView.Minimal && m.Select(new(m.TileWidth + 3 * m.Key + 30, 210), minimal).View == CameraView.Compact, "Compact hysteresis missing");
        var strip = new CameraLayout(CameraView.Compact, PresetArrangement.Strip);
        double stripBoundary = m.TileHeight + 2 * m.Row;
        Check(m.Select(new(620, stripBoundary + 4), strip).Arrangement == PresetArrangement.Strip && m.Select(new(620, stripBoundary + 4), compact).Arrangement == PresetArrangement.Tiles, "Arrangement hysteresis missing");
        Check(m.Select(new(m.StripWidth, m.StripHeight), minimal).Arrangement == PresetArrangement.Strip, "Minimum strip size selected an overflowing tile block");
        using var demo = new CameraSession(true);
        for (int i = 0; i < CameraSession.Buttons.Length; i++)
        { demo.SaveMapping(CameraSession.Buttons[i], new string('W', 32), 20 + i); await demo.Service.SavePreset(20 + i); }
        demo.SaveAutomatic(new() { StartPreset = 1, ExitPreset = 9 });
        var demoWindow = new MainWindow(demo); ShowOffscreen(demoWindow); await Layout();
        var demoState = demo.Demo.State;
        foreach (var (width, height, arrangement) in new[] { (360d, 240d, PresetArrangement.Tiles), (620d, 145d, PresetArrangement.Strip), (160d, 460d, PresetArrangement.Column), (240d, 215d, PresetArrangement.Tiles), (620d, 85d, PresetArrangement.Strip), (160d, 310d, PresetArrangement.Column) })
        {
            demoWindow.Width = width; demoWindow.Height = height; await Layout();
            Check(demoWindow.CurrentLayout.Arrangement == arrangement && Math.Abs(demoWindow.Width - width) < 1 && Math.Abs(demoWindow.Height - height) < 1 && !demoWindow.DemoPreview.IsVisible, "Demo prevented slim sizing or kept preview");
            Check(demo.Demo.State == demoState, "Demo resize triggered preset/motion/automation");
            Check(demoWindow.ControlsScroll.ScrollableWidth == 0 && demoWindow.ControlsScroll.ScrollableHeight == 0, "Long demo names forced essential controls to scroll");
            Screenshot(demoWindow, System.IO.Path.Combine(output, $"responsive-demo-{demoWindow.CurrentLayout.View}-{arrangement}.png"));
        }
        demoWindow.Close(); await Layout();
        Check(demo.Demo.State.Action == Texts.Format(Texts.Demo_CustomPreset, 26), "Slim close lost mapped exit preset");
        Theme.Apply(false);
    }

    private static void NativeSizingChecks(MainWindow window, RecordingHandler handler)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        double frameWidth = window.ActualWidth - window.LayoutRoot.ActualWidth;
        double frameHeight = window.ActualHeight - window.LayoutRoot.ActualHeight;
        var m = new CameraLayoutMetrics(68, 32, 30, 554, 390);
        int commands = handler.Commands.Count;
        for (int edge = 1; edge <= 8; edge++)
        {
            var rect = new NativeSizingRect { Left = 100, Top = 100, Right = 100 + (int)(200 * dpi.DpiScaleX), Bottom = 100 + (int)(85 * dpi.DpiScaleY) };
            var before = rect;
            IntPtr data = Marshal.AllocHGlobal(Marshal.SizeOf<NativeSizingRect>());
            try
            {
                Marshal.StructureToPtr(rect, data, false);
                SendMessage(new WindowInteropHelper(window).Handle, 0x0214, new(edge), data);
                rect = Marshal.PtrToStructure<NativeSizingRect>(data);
                double width = (rect.Right - rect.Left) / dpi.DpiScaleX - frameWidth;
                double height = (rect.Bottom - rect.Top) / dpi.DpiScaleY - frameHeight;
                Check(height >= m.MinimumHeight(width) - 1, "Native sizing allowed an unreachable minimal layout");
                if (edge is 1 or 2) Check(rect.Top == before.Top && rect.Bottom == before.Bottom, "Horizontal drag unexpectedly changed height");
                if (edge is 3 or 6) Check(rect.Left == before.Left && rect.Right == before.Right, "Vertical drag unexpectedly changed width");
            }
            finally { Marshal.FreeHGlobal(data); }
        }
        Check(handler.Commands.Count == commands, "Native resize constraint sent camera commands");
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSizingRect { internal int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam);
}
