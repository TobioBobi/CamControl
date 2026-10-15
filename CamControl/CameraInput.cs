using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Automation;

namespace CamControl;

internal sealed class CameraInput
{
    public static readonly DependencyProperty IsHeldProperty = DependencyProperty.RegisterAttached("IsHeld", typeof(bool), typeof(CameraInput), new PropertyMetadata(false));
    public static bool GetIsHeld(DependencyObject value) => (bool)value.GetValue(IsHeldProperty);
    public static void SetIsHeld(DependencyObject value, bool held) => value.SetValue(IsHeldProperty, held);
    private readonly Func<CameraController> controller;
    private object? directionSource, zoomSource;
    internal CameraInput(Window owner, Func<CameraController> controller, Action<int>? preset = null)
    {
        this.controller = controller;
        owner.PreviewKeyDown += async (_, e) =>
        {
            if (e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase || Keyboard.Modifiers != ModifierKeys.None) return;
            if (IsMotionKey(e.Key)) { e.Handled = true; if (!e.IsRepeat) await PressKeyAsync(e.Key); }
            else if (preset != null && !e.IsRepeat && Number(e.Key) is { } number) { e.Handled = true; preset(number); }
        };
        owner.PreviewKeyUp += async (_, e) => { if (IsMotionKey(e.Key)) { e.Handled = true; await ReleaseKeyAsync(e.Key); } };
    }
    private static int? Number(Key key) => key switch { Key.D1 or Key.NumPad1 => 1, Key.D2 or Key.NumPad2 => 2, Key.D3 or Key.NumPad3 => 3, Key.D4 or Key.NumPad4 => 4, Key.D5 or Key.NumPad5 => 5, Key.D6 or Key.NumPad6 => 6, Key.D9 or Key.NumPad9 => 9, _ => null };
    private static bool IsMotionKey(Key key) => key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Add or Key.Subtract;
    internal Task PressKeyAsync(Key key) => key switch
    {
        Key.Left => StartAsync(key, false, () => controller().StartDirectionAsync(Direction.Left)),
        Key.Right => StartAsync(key, false, () => controller().StartDirectionAsync(Direction.Right)),
        Key.Up => StartAsync(key, false, () => controller().StartDirectionAsync(Direction.Up)),
        Key.Down => StartAsync(key, false, () => controller().StartDirectionAsync(Direction.Down)),
        Key.Add => StartAsync(key, true, () => controller().StartZoomAsync(Zoom.ZoomIn)),
        Key.Subtract => StartAsync(key, true, () => controller().StartZoomAsync(Zoom.ZoomOut)),
        _ => Task.CompletedTask
    };
    internal Task ReleaseKeyAsync(Key key) => ReleaseAsync(key, key is Key.Add or Key.Subtract);
    private Task StartAsync(object source, bool zoom, Func<Task> start)
    {
        if (!controller().ControlsEnabled || (zoom ? zoomSource : directionSource) != null) return Task.CompletedTask;
        if (zoom) zoomSource = source; else directionSource = source;
        if (source is Button button) SetIsHeld(button, true);
        return start();
    }
    private Task ReleaseAsync(object source, bool zoom)
    {
        if (!Equals(zoom ? zoomSource : directionSource, source)) return Task.CompletedTask;
        if (zoom) zoomSource = null; else directionSource = null;
        if (source is Button button) SetIsHeld(button, false);
        return zoom ? controller().StopZoomAsync() : controller().StopDirectionAsync();
    }
    public void Reset() { if (directionSource is Button d) SetIsHeld(d, false); if (zoomSource is Button z) SetIsHeld(z, false); directionSource = zoomSource = null; }
    internal void Bind(Button button, string label, bool zoom, Func<Task> start)
    {
        AutomationProperties.SetName(button, label);
        button.PreviewMouseLeftButtonDown += async (_, e) => { e.Handled = true; button.Focus(); button.CaptureMouse(); await StartAsync(button, zoom, start); };
        button.PreviewMouseLeftButtonUp += async (_, e) => { e.Handled = true; await ReleaseAsync(button, zoom); button.ReleaseMouseCapture(); };
        button.LostMouseCapture += async (_, _) => await ReleaseAsync(button, zoom);
        button.PreviewKeyDown += async (_, e) => { if (e.Key is Key.Space or Key.Enter) { e.Handled = true; if (!e.IsRepeat) await StartAsync(button, zoom, start); } };
        button.PreviewKeyUp += async (_, e) => { if (e.Key is Key.Space or Key.Enter) { e.Handled = true; await ReleaseAsync(button, zoom); } };
        button.LostKeyboardFocus += async (_, _) => await ReleaseAsync(button, zoom);
    }
    internal FrameworkElement CreatePanel()
    {
        var pad = new Grid { Margin = new(0, 0, 0, 12) };
        foreach (double width in new[] { 48d, 54d, 48d }) pad.ColumnDefinitions.Add(new() { Width = new(width) });
        pad.RowDefinitions.Add(new() { Height = new(54) }); pad.RowDefinitions.Add(new() { Height = new(54) });
        void DirectionButton(string text, Direction direction, int column, int row, int span = 1)
        {
            var button = new Button { Content = text, FontSize = 23, Padding = new(4), Margin = new(2), Name = "move" + direction };
            Bind(button, direction switch { Direction.Left => "Nach links", Direction.Right => "Nach rechts", Direction.Up => "Nach oben", _ => "Nach unten" }, false, () => controller().StartDirectionAsync(direction));
            Grid.SetColumn(button, column); Grid.SetRow(button, row); Grid.SetRowSpan(button, span); pad.Children.Add(button);
        }
        DirectionButton("←", Direction.Left, 0, 0, 2); DirectionButton("↑", Direction.Up, 1, 0);
        DirectionButton("↓", Direction.Down, 1, 1); DirectionButton("→", Direction.Right, 2, 0, 2);
        var zoom = new Grid(); zoom.ColumnDefinitions.Add(new()); zoom.ColumnDefinitions.Add(new());
        foreach (var (text, value, column) in new[] { ("−\nHeraus", Zoom.ZoomOut, 0), ("+\nHinein", Zoom.ZoomIn, 1) })
        {
            var button = new Button { Content = new TextBlock { Text = text, TextAlignment = TextAlignment.Center }, MinHeight = 58, Padding = new(4), Margin = new(2), Name = value.ToString() };
            Bind(button, value == Zoom.ZoomIn ? "Zoom hinein" : "Zoom heraus", true, () => controller().StartZoomAsync(value)); Grid.SetColumn(button, column); zoom.Children.Add(button);
        }
        return Ui.Stack(Ui.Text("Bewegen"), pad, Ui.Text("Zoom"), zoom, new TextBlock { Text = "Halten zum Bewegen\nLoslassen stoppt", FontSize = 11, TextAlignment = TextAlignment.Center, Margin = new(0, 10, 0, 0) });
    }
}
