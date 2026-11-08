using System.Windows;
using System.Windows.Interop;

namespace CamControl;

public partial class MainWindow
{
    private string? placementPath;
    private bool placementReady;
    private bool startMaximized;

    private void InitializeWindowPlacement(string? path)
    {
        placementPath = path;
        RestoreWindowPlacement();
        SourceInitialized += (_, _) =>
        {
            FitPlacement();
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(PlacementDisplayChanged);
        };
        Loaded += (_, _) =>
        {
            placementReady = true;
            // The HWND must have acquired its normal restore rectangle before
            // maximizing; doing this during SourceInitialized loses that rectangle.
            if (startMaximized) WindowState = WindowState.Maximized;
        };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(FitPlacement);
    }

    private void RestoreWindowPlacement()
    {
        restoring = true;
        try
        {
            var saved = Session.IsDemo ? null : WindowPlacement.Load(placementPath);
            WindowState = WindowState.Normal;
            fullBounds = saved?.FullBounds.ToRect() ?? new Rect(0, 0, 636, 468);
            constrainedFullSize = null;
            CurrentLayout = new(CameraView.Full, PresetArrangement.Tiles);
            if (saved != null)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                SetPlacementBounds(saved.NormalBounds.ToRect());
            }
            else
            {
                Width = 636; Height = 468;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                if (IsLoaded)
                {
                    var area = CurrentWorkArea();
                    Left = area.Left + (area.Width - Width) / 2;
                    Top = area.Top + (area.Height - Height) / 2;
                }
            }
            if (IsLoaded) FitPlacement();
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
        WindowPlacement.Save(new(WindowBounds.FromRect(bounds), WindowBounds.FromRect(fullBounds),
            WindowState == WindowState.Maximized), placementPath);
    }

    private IntPtr PlacementDisplayChanged(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message is 0x007E or 0x001A) Dispatcher.BeginInvoke(FitPlacement); // display/work-area changes
        return IntPtr.Zero;
    }
}
