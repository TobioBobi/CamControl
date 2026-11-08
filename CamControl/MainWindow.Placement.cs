using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace CamControl;

public partial class MainWindow
{
    private string? placementPath;
    private bool placementReady;
    private bool startMaximized;
    private WindowPlacement? startupPlacement;

    private void InitializeWindowPlacement(string? path)
    {
        placementPath = path;
        RestoreWindowPlacement();
        SourceInitialized += (_, _) =>
        {
            if (startupPlacement is { } saved)
            {
                var position = MoveToSavedMonitor(saved.NormalBounds);
                Left = position.X; Top = position.Y;
            }
            FitPlacement();
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(PlacementDisplayChanged);
        };
        Loaded += (_, _) =>
        {
            placementReady = true;
            FitUsablePlacement();
            // The HWND must have acquired its normal restore rectangle before
            // maximizing; doing this during SourceInitialized loses that rectangle.
            if (startMaximized) WindowState = WindowState.Maximized;
        };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(FitPlacement);
    }

    internal void RestoreWindowPlacement()
    {
        restoring = true;
        try
        {
            var saved = Session.IsDemo ? null : WindowPlacement.Load(placementPath);
            startupPlacement = saved;
            WindowState = WindowState.Normal;
            fullBounds = saved?.FullBounds.ToRect() ?? DefaultWindowBounds;
            fullBoundsDpi = saved is null ? VisualTreeHelper.GetDpi(this) : new(saved.FullBounds.ScaleX, saved.FullBounds.ScaleY);
            constrainedFullSize = null;
            CurrentLayout = new(CameraView.Full, PresetArrangement.Tiles);
            if (saved != null)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                SetPlacementBounds(saved.NormalBounds.ToRect());
                if (IsLoaded)
                {
                    var position = MoveToSavedMonitor(saved.NormalBounds);
                    Left = position.X; Top = position.Y;
                }
            }
            else
            {
                Width = DefaultWindowBounds.Width; Height = DefaultWindowBounds.Height;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                if (IsLoaded)
                {
                    var area = CurrentWorkArea();
                    Left = area.Left + (area.Width - Width) / 2;
                    Top = area.Top + (area.Height - Height) / 2;
                }
            }
            if (IsLoaded) { FitPlacement(); FitUsablePlacement(); }
            startMaximized = saved?.Maximized == true;
            if (IsLoaded && startMaximized) WindowState = WindowState.Maximized;
        }
        finally { restoring = false; }
        QueueLayout();
    }

    private void SetPlacementBounds(Rect bounds)
    {
        Width = bounds.Width; Height = bounds.Height;
        Left = bounds.Left; Top = bounds.Top;
    }

    private void FitPlacement()
    {
        if (WindowState != WindowState.Normal) return;
        var area = CurrentWorkArea();
        MaxWidth = area.Width; MaxHeight = area.Height;
        MinWidth = Math.Min(MinWidth, area.Width); MinHeight = Math.Min(MinHeight, area.Height);
        Width = Math.Clamp(Width, MinWidth, area.Width);
        Height = Math.Clamp(Height, MinHeight, area.Height);
        Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width));
        Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
    }

    private void SaveWindowPlacement()
    {
        if (!placementReady || Session.IsDemo) return;
        RememberFullBounds();
        // RestoreBounds retains the normal rectangle even when closing maximized
        // or minimized. Minimization itself is deliberately never persisted.
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
        WindowPlacement.Save(new(WindowBounds.FromRect(bounds, VisualTreeHelper.GetDpi(this)), WindowBounds.FromRect(fullBounds, fullBoundsDpi),
            WindowState == WindowState.Maximized), placementPath);
    }

    private void FitUsablePlacement()
    {
        // Individually legal width/height values can still be an unusable pair
        // (for example a narrow window too short to hold the preset column).
        var metrics = Metrics();
        if (LayoutRoot.ActualWidth + 1 >= metrics.MinimumWidth(LayoutRoot.ActualHeight + 1) &&
            LayoutRoot.ActualHeight + 1 >= metrics.MinimumHeight(LayoutRoot.ActualWidth + 1)) return;
        Width = DefaultWindowBounds.Width; Height = DefaultWindowBounds.Height;
        FitPlacement();
    }
    private IntPtr PlacementDisplayChanged(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message is 0x007E or 0x001A) Dispatcher.BeginInvoke(FitPlacement); // display/work-area changes
        return IntPtr.Zero;
    }
}
