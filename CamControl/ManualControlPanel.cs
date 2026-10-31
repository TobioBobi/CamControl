using System.Windows;
using System.Windows.Controls;

namespace CamControl;

// Reposition existing buttons; keep bindings, focus and controller ownership.
internal sealed class ManualControlPanel : Grid
{
    private readonly Grid pad = new(), zoom = new();
    private readonly TextBlock heading = Ui.Text(Texts.Movement_Title), zoomHeading = Ui.Text(Texts.Zoom_Title);
    private readonly TextBlock hint = new() { Text = Texts.Movement_HoldHint, FontSize = 11, TextAlignment = TextAlignment.Center, Margin = new(0, 10, 0, 0) };
    private readonly List<Button> directions = new(), zooms = new();
    internal ManualControlPanel(CameraInput input, Func<CameraController> controller)
    {
        foreach (var (text, value, label) in new[] { ("←", Direction.Left, Texts.Movement_Left), ("↑", Direction.Up, Texts.Movement_Up), ("↓", Direction.Down, Texts.Movement_Down), ("→", Direction.Right, Texts.Movement_Right) })
        {
            var button = new Button { Content = text, Name = "move" + value, ToolTip = label };
            input.Bind(button, label, false, () => controller().StartDirectionAsync(value));
            directions.Add(button); pad.Children.Add(button);
        }
        foreach (var (value, label) in new[] { (Zoom.ZoomOut, Texts.Zoom_Out), (Zoom.ZoomIn, Texts.Zoom_In) })
        {
            var button = new Button { Name = value.ToString(), ToolTip = label };
            input.Bind(button, label, true, () => controller().StartZoomAsync(value));
            zooms.Add(button); zoom.Children.Add(button);
        }
        foreach (var child in new UIElement[] { heading, pad, zoomHeading, zoom, hint }) Children.Add(child);
        Configure(true, PresetArrangement.Tiles, 28);
    }
    internal void Configure(bool full, PresetArrangement arrangement, double key)
    {
        bool inline = !full && arrangement == PresetArrangement.Strip;
        bool cross = full || arrangement == PresetArrangement.Tiles;
        RowDefinitions.Clear(); ColumnDefinitions.Clear();
        for (int i = 0; i < (inline ? 1 : 5); i++) RowDefinitions.Add(new() { Height = GridLength.Auto });
        for (int i = 0; i < (inline ? 2 : 1); i++) ColumnDefinitions.Add(new() { Width = !inline ? new GridLength(1, GridUnitType.Star) : i == 0 ? new GridLength(4 * key + 4) : GridLength.Auto });
        heading.Visibility = zoomHeading.Visibility = hint.Visibility = full ? Visibility.Visible : Visibility.Collapsed;
        Place(heading, 0, 0); Place(pad, inline ? 0 : 1, 0); Place(zoomHeading, 2, 0); Place(zoom, inline ? 0 : 3, inline ? 1 : 0); Place(hint, 4, 0);
        pad.Margin = new(0, 0, inline ? 4 : 0, full ? 12 : inline ? 0 : 4);
        pad.RowDefinitions.Clear(); pad.ColumnDefinitions.Clear();
        for (int i = 0; i < (cross ? 3 : 4); i++) pad.ColumnDefinitions.Add(new() { Width = full ? new GridLength(i == 1 ? 54 : 48) : new GridLength(1, GridUnitType.Star) });
        pad.MinWidth = full ? 150 : (cross ? 3 : 4) * key;
        for (int i = 0; i < (cross ? 2 : 1); i++) pad.RowDefinitions.Add(new() { Height = new(full ? 54 : key) });
        for (int i = 0; i < directions.Count; i++)
        {
            var button = directions[i];
            if (full) button.FontSize = 23; else button.ClearValue(Control.FontSizeProperty);
            button.MinHeight = full ? 36 : key - 2; button.Padding = new(full ? 4 : 1); button.Margin = new(1);
            Place(button, cross && i == 2 ? 1 : 0, cross ? i == 0 ? 0 : i == 3 ? 2 : 1 : i);
            Grid.SetRowSpan(button, cross && i is 0 or 3 ? 2 : 1);
        }
        zoom.ColumnDefinitions.Clear(); zoom.ColumnDefinitions.Add(new()); zoom.ColumnDefinitions.Add(new());
        zoom.MinWidth = inline ? 2 * key : 0;
        zoom.Width = inline ? 2 * key : double.NaN;
        zoom.Margin = new(0, full || inline ? 0 : 4, 0, 0);
        for (int i = 0; i < zooms.Count; i++)
        {
            var button = zooms[i]; button.Content = full ? new TextBlock { Text = i == 0 ? Texts.Zoom_OutButton : Texts.Zoom_InButton, TextAlignment = TextAlignment.Center } : i == 0 ? "−" : "+";
            button.MinHeight = full ? 58 : key - 2; button.Padding = new(full ? 4 : 1); button.Margin = new(1); Place(button, 0, i);
        }
    }
    private static void Place(UIElement element, int row, int column) { SetRow(element, row); SetColumn(element, column); }
}
