using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace CamControl;

internal static class Theme
{
    public static void Initialize()
    {
        Apply();
        SystemEvents.UserPreferenceChanged += PreferenceChanged;
        SystemParameters.StaticPropertyChanged += ParametersChanged;
        Application.Current.Exit += (_, _) => { SystemEvents.UserPreferenceChanged -= PreferenceChanged; SystemParameters.StaticPropertyChanged -= ParametersChanged; };
    }
    private static void PreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Application.Current.Dispatcher.BeginInvoke(new Action(Apply));
    private static void ParametersChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Application.Current.Dispatcher.BeginInvoke(new Action(Apply));
    internal static void Apply()
    {
        bool dark = false;
        try { using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"); dark = key?.GetValue("AppsUseLightTheme") is int value && value == 0; }
        catch (System.Security.SecurityException) { }
        Apply(dark);
    }
    internal static void Apply(bool dark)
    {
        string[] keys = ["Canvas", "Surface", "Ink", "Muted", "Line", "Accent", "Hover", "Pressed", "Selection", "Error", "Success"];
        string[] light = ["#F3F5F8", "#FFFFFF", "#182536", "#536174", "#CED6E0", "#1758B8", "#EDF3FC", "#CBDFFF", "#E5EFFF", "#AF2020", "#24653E"];
        string[] night = ["#151A22", "#202733", "#EDF2F9", "#B2BDCC", "#465266", "#91BCFF", "#2A3546", "#385780", "#293F60", "#FFABAB", "#95D9AC"];
        for (int i = 0; i < keys.Length; i++)
        {
            var color = (Color)ColorConverter.ConvertFromString((dark ? night : light)[i]);
            if (SystemParameters.HighContrast)
                color = keys[i] switch { "Canvas" or "Surface" => SystemColors.WindowColor, "Ink" or "Muted" or "Error" or "Success" => SystemColors.WindowTextColor, "Hover" or "Pressed" or "Selection" => SystemColors.ControlColor, _ => SystemColors.HighlightColor };
            Application.Current.Resources[keys[i] + "Brush"] = new SolidColorBrush(color);
        }
    }
}
