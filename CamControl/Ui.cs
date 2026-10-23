using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace CamControl;

internal static class Ui
{
    public static TextBlock Text(string text, string brush = "InkBrush")
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
        block.SetResourceReference(TextBlock.ForegroundProperty, brush);
        return block;
    }
    public static TextBlock Heading(string text) => new() { Text = text, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new(0, 0, 0, 16), TextWrapping = TextWrapping.Wrap };
    public static Button Button(string text, Action action, string name = "")
    {
        var button = new Button { Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, Name = name, Margin = new(0, 4, 0, 4) };
        AutomationProperties.SetName(button, text);
        button.Click += (_, _) => action();
        return button;
    }
    public static ScrollViewer Scroll(UIElement child) => new() { Content = child, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    public static StackPanel Stack(params UIElement[] children) { var panel = new StackPanel(); foreach (var child in children) panel.Children.Add(child); return panel; }
    public static Border Surface(UIElement child, Thickness padding) { var border = new Border { Child = child, Padding = padding, CornerRadius = new(9), BorderThickness = new(1) }; border.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush"); border.SetResourceReference(Border.BorderBrushProperty, "LineBrush"); return border; }
}

public class AppWindow : Window
{
    public AppWindow()
    {
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentUICulture.IetfLanguageTag);
        SetResourceReference(StyleProperty, typeof(Window));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SourceInitialized += (_, _) => Theme.UpdateTitleBar(this);
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/CamControl;component/resources/Camera.ico"));
        Loaded += (_, _) => Fit();
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(Fit);
    }
    private void Fit()
    {
        // Working area in WPF logical units, including per-monitor DPI.
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var monitor = MonitorFromWindow(hwnd, 2);
        var info = new MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return;
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new Point(info.Work.Left, info.Work.Top));
        var bottomRight = transform.Transform(new Point(info.Work.Right, info.Work.Bottom));
        MaxWidth = Math.Max(1, bottomRight.X - topLeft.X); MaxHeight = Math.Max(1, bottomRight.Y - topLeft.Y);
        MinWidth = Math.Min(MinWidth, MaxWidth); MinHeight = Math.Min(MinHeight, MaxHeight);
        if (Width > MaxWidth) Width = MaxWidth;
        if (Height > MaxHeight) Height = MaxHeight;
        // Preserve deliberately offscreen test windows.
        if (Left < bottomRight.X && Left + ActualWidth > topLeft.X && Top < bottomRight.Y && Top + ActualHeight > topLeft.Y)
        { Left = Math.Clamp(Left, topLeft.X, Math.Max(topLeft.X, bottomRight.X - ActualWidth)); Top = Math.Clamp(Top, topLeft.Y, Math.Max(topLeft.Y, bottomRight.Y - ActualHeight)); }
    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}

internal sealed class ConfirmationDialog : AppWindow
{
    internal ConfirmationDialog(Window owner, string message)
    {
        Owner = owner; Title = Texts.Preset_Save; Width = 430; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false;
        var no = Ui.Button(Texts.Common_Cancel, () => DialogResult = false); no.IsCancel = true;
        var yes = Ui.Button(Texts.Preset_Overwrite, () => DialogResult = true);
        var panel = Ui.Stack(Ui.Heading(Texts.Preset_OverwriteTitle), Ui.Text(message), yes, no); panel.Margin = new(20); Content = panel;
        Loaded += (_, _) => no.Focus();
    }
}
