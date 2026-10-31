using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace CamControl;

public partial class MainWindow
{
    private ManualControlPanel manual = null!;
    private bool layoutQueued, layoutInitialized, restoring;
    private CameraLayoutMetrics? appliedMetrics;
    private Size? constrainedFullSize;
    private Rect fullBounds = new(0, 0, 636, 468);
    internal CameraLayout CurrentLayout { get; private set; } = new(CameraView.Full, PresetArrangement.Tiles);
    private CameraLayoutMetrics Metrics()
    {
        var sample = new TextBlock { Text = "9 Wâ€¦", FontFamily = FontFamily, FontSize = FontSize };
        sample.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
        double key = Math.Max(30, Math.Ceiling(sample.DesiredSize.Height + 8));
        double row = Math.Max(32, Math.Ceiling(sample.DesiredSize.Height + 12));
        double scale = Math.Max(1, FontSize / 14);
        return new(Math.Max(68, Math.Ceiling(sample.DesiredSize.Width + 20)), row, key, 554 * scale, 420 * scale);
    }
    private void InitializeResponsiveLayout()
    {
        LayoutRoot.SizeChanged += (_, _) => QueueLayout();
        LocationChanged += (_, _) => RememberFullBounds();
        SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(ConstrainSizing);
        ApplyLayout(CurrentLayout, Metrics());
        layoutInitialized = true;
    }
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (layoutInitialized && e.Property == FontSizeProperty) QueueLayout();
    }
    private void QueueLayout()
    {
        if (layoutQueued) return;
        layoutQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            layoutQueued = false;
            if (closing) return;
            var metrics = Metrics();
            bool retainFull = constrainedFullSize is { } forced && Math.Abs(ActualWidth - forced.Width) < 1 && Math.Abs(ActualHeight - forced.Height) < 1;
            if (!retainFull) constrainedFullSize = null;
            var next = retainFull ? new CameraLayout(CameraView.Full, PresetArrangement.Tiles) : metrics.Select(LayoutRoot.RenderSize, CurrentLayout);
            if (next != CurrentLayout || metrics != appliedMetrics)
            {
                // StopAsync synchronously clears held state and gates new input
                // before its first await. Never send stops for an idle resize.
                _ = Input.StopForLayoutAsync();
                CurrentLayout = next;
            }
            ApplyLayout(next, metrics);
            RememberFullBounds();
        });
    }
    private void ApplyLayout(CameraLayout layout, CameraLayoutMetrics metrics)
    {
        if (metrics == appliedMetrics && layout == appliedLayout) return;
        appliedMetrics = metrics; appliedLayout = layout;
        bool full = layout.View == CameraView.Full, strip = !full && layout.Arrangement == PresetArrangement.Strip;
        Header.Visibility = StatusPanel.Visibility = PositionsHeading.Visibility = full ? Visibility.Visible : Visibility.Collapsed;
        CompactActions.Visibility = full ? Visibility.Collapsed : Visibility.Visible;
        CompactActions.Width = metrics.Key + 4;
        RestoreButton.MinWidth = RestoreButton.MinHeight = metrics.Key;
        Grid.SetRow(ControlsScroll, strip ? 0 : 1); Grid.SetColumnSpan(ControlsScroll, strip ? 1 : 2);
        ContentArea.Margin = new(full ? 10 : 6, full ? 8 : 6, full ? 10 : 6, full ? 8 : 6);
        ControlsScroll.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        ControlGrid.MinWidth = full ? metrics.FullWidth - 20 : 0;
        ManualPanel.Visibility = layout.View == CameraView.Minimal ? Visibility.Collapsed : Visibility.Visible;
        ManualPanel.Padding = new(full ? 9 : 0); ManualPanel.BorderThickness = new(full ? 1 : 0);
        if (full) ManualPanel.SetResourceReference(BackgroundProperty, "SurfaceBrush"); else ManualPanel.Background = Brushes.Transparent;
        ControlGrid.ColumnDefinitions[1].Width = new(full ? 14 : layout.Arrangement == PresetArrangement.Tiles && layout.View == CameraView.Compact ? 8 : 0);
        ControlGrid.ColumnDefinitions[2].Width = new(full ? 174 * Math.Max(1, FontSize / 14) : layout.Arrangement == PresetArrangement.Tiles && layout.View == CameraView.Compact ? 3 * metrics.Key : 0);
        Grid.SetColumn(ManualPanel, full || layout.Arrangement == PresetArrangement.Tiles ? 2 : 0);
        Grid.SetRow(ManualPanel, !full && layout.Arrangement != PresetArrangement.Tiles ? 1 : 0);
        ManualPanel.Margin = new(0, !full && layout.Arrangement != PresetArrangement.Tiles ? 4 : 0, 0, 0);
        manual.Configure(full, layout.Arrangement, metrics.Key);
        Presets.ColumnDefinitions.Clear(); Presets.RowDefinitions.Clear();
        int columns = full || layout.Arrangement == PresetArrangement.Tiles ? 2 : strip ? 7 : 1;
        int rows = columns == 2 ? 4 : columns == 7 ? 1 : 7;
        for (int i = 0; i < columns; i++) Presets.ColumnDefinitions.Add(new());
        for (int i = 0; i < rows; i++) Presets.RowDefinitions.Add(new() { Height = full ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
        for (int i = 0; i < CameraSession.Buttons.Length; i++)
        {
            var button = buttons[CameraSession.Buttons[i]];
            Grid.SetRow(button, i / columns); Grid.SetColumn(button, i % columns);
            Grid.SetColumnSpan(button, columns == 2 && i == 6 ? 2 : 1);
            button.MinHeight = full ? 64 : metrics.Row - 4;
            button.Padding = new(full ? 12 : 5, full ? 8 : 3, full ? 12 : 5, full ? 8 : 3);
            button.Margin = new(0, 0, columns == 2 && i == 6 || i % columns == columns - 1 ? 0 : 4, full ? i == 6 ? 0 : 6 : i / columns == rows - 1 ? 0 : 4);
        }
        DemoPreview.Visibility = full && Session.IsDemo && !settingsOpen ? Visibility.Visible : Visibility.Collapsed;
    }
    private CameraLayout appliedLayout;
    private void RememberFullBounds()
    {
        if (!layoutInitialized || restoring || WindowState != WindowState.Normal || CurrentLayout.View != CameraView.Full) return;
        var m = Metrics();
        if (LayoutRoot.ActualWidth >= m.FullWidth && LayoutRoot.ActualHeight >= m.FullHeight)
            fullBounds = new(Left, Top, ActualWidth, ActualHeight);
    }
    private void RestoreFullClick(object sender, RoutedEventArgs e) => RestoreFull();
    internal void RestoreFull()
    {
        restoring = true;
        try
        {
            var area = CurrentWorkArea(); var metrics = Metrics();
            double frameWidth = ActualWidth - LayoutRoot.ActualWidth, frameHeight = ActualHeight - LayoutRoot.ActualHeight;
            WindowState = WindowState.Normal;
            Width = Math.Min(Math.Min(MaxWidth, area.Width), Math.Max(fullBounds.Width, metrics.FullWidth + frameWidth + 12));
            Height = Math.Min(Math.Min(MaxHeight, area.Height), Math.Max(fullBounds.Height, metrics.FullHeight + frameHeight + 12));
            Left = Math.Clamp(fullBounds.Left, area.Left, Math.Max(area.Left, area.Right - Width));
            Top = Math.Clamp(fullBounds.Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
            // If the monitor cannot fit a full view, retain access via scrolling.
            CurrentLayout = new(CameraView.Full, PresetArrangement.Tiles);
            constrainedFullSize = new(Width, Height);
            _ = Input.StopForLayoutAsync();
            ApplyLayout(CurrentLayout, metrics);
        }
        finally { restoring = false; }
    }
    // A single rectangular MinWidth/MinHeight would forbid a thin strip or a
    // narrow column. Constrain only the proposed native drag rectangle, never
    // change Width/Height as a side effect of selecting a layout.
    private IntPtr ConstrainSizing(IntPtr hwnd, int message, IntPtr edge, IntPtr data, ref bool handled)
    {
        if (message != 0x0214) return IntPtr.Zero; // WM_SIZING
        var rect = Marshal.PtrToStructure<SizingRect>(data);
        var dpi = VisualTreeHelper.GetDpi(this); var m = Metrics();
        double frameWidth = ActualWidth - LayoutRoot.ActualWidth, frameHeight = ActualHeight - LayoutRoot.ActualHeight;
        double clientHeight = (rect.Bottom - rect.Top) / dpi.DpiScaleY - frameHeight;
        double minimumWidth = edge.ToInt32() is 1 or 2 ? m.MinimumWidth(clientHeight) : Math.Min(m.ColumnWidth, m.TileWidth);
        int minWidth = (int)Math.Ceiling((minimumWidth + frameWidth) * dpi.DpiScaleX);
        if (rect.Right - rect.Left < minWidth)
        { if (edge.ToInt32() is 1 or 4 or 7) rect.Left = rect.Right - minWidth; else rect.Right = rect.Left + minWidth; }
        double clientWidth = (rect.Right - rect.Left) / dpi.DpiScaleX - frameWidth;
        int minHeight = (int)Math.Ceiling((m.MinimumHeight(clientWidth) + frameHeight) * dpi.DpiScaleY);
        if (rect.Bottom - rect.Top < minHeight)
        { if (edge.ToInt32() is 3 or 4 or 5) rect.Top = rect.Bottom - minHeight; else rect.Bottom = rect.Top + minHeight; }
        Marshal.StructureToPtr(rect, data, false); handled = true; return new IntPtr(1);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SizingRect { public int Left, Top, Right, Bottom; }
}
