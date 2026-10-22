using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace CamControl;

internal static class Theme
{
    internal static Appearance Preference { get; private set; } = AppearanceSettings.Load();
    private static bool isDark;
    internal static void SetPreference(Appearance preference, string? path = null)
    {
        AppearanceSettings.Save(preference, path);
        Preference = preference;
        Apply();
    }
    public static void Initialize()
    {
        Apply();
        SystemEvents.UserPreferenceChanged += PreferenceChanged;
        SystemParameters.StaticPropertyChanged += ParametersChanged;
        Application.Current.Exit += (_, _) => { SystemEvents.UserPreferenceChanged -= PreferenceChanged; SystemParameters.StaticPropertyChanged -= ParametersChanged; };
    }
    private static void PreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Application.Current.Dispatcher.BeginInvoke(new Action(Apply));
    private static void ParametersChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Focus and keyboard-cue changes also raise this event; they do not change the palette.
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SystemParameters.HighContrast))
            Application.Current.Dispatcher.BeginInvoke(new Action(Apply));
    }
    internal static void Apply()
    {
        bool dark = false;
        try { using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"); dark = key?.GetValue("AppsUseLightTheme") is int value && value == 0; }
        catch (System.Security.SecurityException) { }
        Apply(Preference == Appearance.Dark || Preference == Appearance.System && dark);
    }
    internal static void Apply(bool dark)
    {
        isDark = dark && !SystemParameters.HighContrast;
        string[] keys = ["Canvas", "Surface", "Ink", "Muted", "Line", "Accent", "Hover", "Pressed", "Selection", "Error", "Success"];
        string[] light = ["#F3F5F8", "#FFFFFF", "#182536", "#536174", "#CED6E0", "#1758B8", "#EDF3FC", "#CBDFFF", "#E5EFFF", "#AF2020", "#24653E"];
        string[] night = ["#151A22", "#202733", "#EDF2F9", "#B2BDCC", "#465266", "#91BCFF", "#2A3546", "#385780", "#293F60", "#FFABAB", "#95D9AC"];
        bool changed = false;
        for (int i = 0; i < keys.Length; i++)
        {
            var color = (Color)ColorConverter.ConvertFromString((dark ? night : light)[i]);
            if (SystemParameters.HighContrast)
                color = keys[i] switch { "Canvas" or "Surface" => SystemColors.WindowColor, "Ink" or "Muted" or "Error" or "Success" => SystemColors.WindowTextColor, "Hover" or "Pressed" or "Selection" => SystemColors.ControlColor, _ => SystemColors.HighlightColor };
            string key = keys[i] + "Brush";
            if (Application.Current.Resources[key] is SolidColorBrush current && current.Color == color) continue;
            Application.Current.Resources[key] = new SolidColorBrush(color);
            changed = true;
        }
        // Replacing identical brushes invalidates every DynamicResource consumer;
        // touching DWM on every notification can also flash the entire frame.
        if (changed)
            foreach (Window window in Application.Current.Windows) UpdateTitleBar(window);
    }
    internal static void UpdateTitleBar(Window window)
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        int enabled = isDark ? 1 : 0;
        // Unsupported on older Windows versions; the system frame remains usable.
        _ = DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
    }
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
