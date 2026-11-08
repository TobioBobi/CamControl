using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace CamControl;

internal sealed record WindowBounds(double Left, double Top, double Width, double Height, double ScaleX = 1, double ScaleY = 1)
{
    [JsonIgnore]
    public bool IsValid => double.IsFinite(Left) && double.IsFinite(Top) &&
        double.IsFinite(Width) && double.IsFinite(Height) &&
        Math.Abs(Left) <= 1000000 && Math.Abs(Top) <= 1000000 &&
        Width is >= 152 and <= 100000 && Height is >= 80 and <= 100000 &&
        ScaleX is >= 0.5 and <= 8 && ScaleY is >= 0.5 and <= 8;
    public Rect ToRect() => new(Left, Top, Width, Height);
    public static WindowBounds FromRect(Rect bounds, DpiScale dpi) => new(bounds.Left, bounds.Top, bounds.Width, bounds.Height, dpi.DpiScaleX, dpi.DpiScaleY);
}

internal sealed record WindowPlacement(WindowBounds NormalBounds, WindowBounds FullBounds, bool Maximized)
{
    internal static string FilePath => Path.Combine(Path.GetDirectoryName(CameraSettings.FilePath)!, "window-placement.json");
    public static WindowPlacement? Load(string? path = null)
    {
        try
        {
            var value = JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(path ?? FilePath));
            if (value?.NormalBounds?.IsValid == true && value.FullBounds?.IsValid == true) return value;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        return null;
    }
    // Remembering a window is best-effort; a read-only settings folder must
    // never interfere with camera stops or closing the application.
    public static void Save(WindowPlacement value, string? path = null)
    {
        if (value.NormalBounds?.IsValid != true || value.FullBounds?.IsValid != true) return;
        path ??= FilePath;
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(value));
            File.Move(temporary, path, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }
}
