using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace CamControl;

internal sealed class DemoCameraView : FrameworkElement
{
    private readonly DemoCamera camera;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly Stopwatch clock = new();
    internal DemoCameraView(DemoCamera camera)
    {
        this.camera = camera;
        System.Windows.Automation.AutomationProperties.SetName(this, Texts.Demo_AccessibleName);
        timer.Tick += Tick;
        Loaded += (_, _) => { clock.Restart(); timer.Start(); };
        Unloaded += (_, _) => { timer.Stop(); clock.Stop(); };
        IsVisibleChanged += (_, _) => { clock.Restart(); if (IsVisible) timer.Start(); else timer.Stop(); };
    }
    private void Tick(object? sender, EventArgs e) { camera.Advance(clock.Elapsed); clock.Restart(); InvalidateVisual(); }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 1 || ActualHeight <= 40) return;
        var state = camera.State;
        double w = (ActualWidth - 16) / 2, h = Math.Max(1, ActualHeight - 44);
        var overview = new Rect(0, 20, w, h); var preview = new Rect(w + 16, 20, w, h);
        Label(dc, Texts.Demo_Overview, new(0, 0)); Label(dc, Texts.Format(Texts.Demo_Zoom, state.Zoom), new(w + 16, 0));
        Scene(dc, overview);
        var f = state.Frame;
        dc.DrawRectangle(null, new((Brush)FindResource("AccentBrush"), 2), new Rect(overview.X + f.X * w, overview.Y + f.Y * h, f.Width * w, f.Height * h));
        dc.PushClip(new RectangleGeometry(preview));
        Scene(dc, new Rect(preview.X - f.X * w / f.Width, preview.Y - f.Y * h / f.Height, w / f.Width, h / f.Height)); dc.Pop();
        dc.DrawRectangle(null, new((Brush)FindResource("LineBrush"), 1), preview); Label(dc, Texts.Format(Texts.Demo_Status, state.Action), new(0, ActualHeight - 19));
    }
    private void Label(DrawingContext dc, string text, Point point) => dc.DrawText(new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, (Brush)FindResource("InkBrush"), VisualTreeHelper.GetDpi(this).PixelsPerDip), point);
    private void Scene(DrawingContext dc, Rect bounds)
    {
        dc.DrawRectangle((Brush)FindResource("SurfaceBrush"), null, bounds);
        var pen = new Pen((Brush)FindResource("LineBrush"), .5);
        for (int i = 1; i < 10; i++) { dc.DrawLine(pen, new(bounds.X + bounds.Width * i / 10, bounds.Y), new(bounds.X + bounds.Width * i / 10, bounds.Bottom)); dc.DrawLine(pen, new(bounds.X, bounds.Y + bounds.Height * i / 10), new(bounds.Right, bounds.Y + bounds.Height * i / 10)); }
        foreach (var (x, y, w, h, text) in new[] { (.16, .32, .13, .24, Texts.Demo_Speaker), (.34, .35, .12, .20, Texts.Demo_Reader), (.52, .16, .35, .15, Texts.Demo_Screen), (.50, .62, .30, .17, Texts.Demo_Table), (.70, .36, .12, .23, "Demo") })
        {
            var rect = new Rect(bounds.X + x * bounds.Width, bounds.Y + y * bounds.Height, w * bounds.Width, h * bounds.Height);
            dc.DrawRectangle((Brush)FindResource("SelectionBrush"), pen, rect);
            dc.PushClip(new RectangleGeometry(rect)); Label(dc, text, new(rect.X + 3, rect.Y + 3)); dc.Pop();
        }
    }
}
