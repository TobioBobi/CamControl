using System.Text.Json;

namespace CamControl;

internal enum Appearance { Dark, Light, System }

internal static class AppearanceSettings
{
    internal static string FilePath => Path.Combine(Path.GetDirectoryName(CameraSettings.FilePath)!, "appearance.json");
    internal static Appearance Load(string? path = null)
    {
        try
        {
            string? value = JsonSerializer.Deserialize<string>(File.ReadAllText(path ?? FilePath));
            if (Enum.TryParse<Appearance>(value, out var appearance) && Enum.IsDefined(appearance)) return appearance;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        return Appearance.Dark;
    }
    internal static void Save(Appearance appearance, string? path = null)
    {
        if (!Enum.IsDefined(appearance)) throw new ArgumentOutOfRangeException(nameof(appearance));
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(appearance.ToString())); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
