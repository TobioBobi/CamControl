using System.Text.Json;

namespace CamControl;

internal sealed class AutomaticPositions
{
    public int? StartPreset { get; set; }
    public int? ExitPreset { get; set; }
    public static string FilePath => Path.Combine(Path.GetDirectoryName(CameraSettings.FilePath)!, "automatic-positions.json");
    public static bool IsValid(int? number) => number == null || number is >= 1 and <= 6 or 9;

    public static AutomaticPositions Load(string? path = null, bool? existingInstallation = null)
    {
        try
        {
            var value = JsonSerializer.Deserialize<AutomaticPositions>(File.ReadAllText(path ?? FilePath));
            if (value != null)
                return new() { StartPreset = IsValid(value.StartPreset) ? value.StartPreset : null,
                    ExitPreset = IsValid(value.ExitPreset) ? value.ExitPreset : null };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        // Only absent settings migrate the old behavior. Damaged settings fail
        // closed rather than unexpectedly moving a camera.
        return !File.Exists(path ?? FilePath) && (existingInstallation ?? CameraSettings.TryLoad(null, out _))
            ? new() { StartPreset = 4, ExitPreset = 9 } : new();
    }

    public static void Save(AutomaticPositions value, string? path = null)
    {
        if (!IsValid(value.StartPreset) || !IsValid(value.ExitPreset)) throw new ArgumentException("Ungültige Presetnummer.");
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(value)); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
