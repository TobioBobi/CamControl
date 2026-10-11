using System.Text.Json;

namespace CamControl;

internal static class PresetSettings
{
    public static string FilePath => Path.Combine(Path.GetDirectoryName(CameraSettings.FilePath)!, "buttons.json");
    public static Dictionary<int, string> Defaults() => new()
    {
        [1] = "Redner", [2] = "Redner + Leser", [3] = "Tisch", [4] = "Komplette Bühne",
        [5] = "Redner + Bildschirm", [6] = "Demo stehend"
    };

    public static Dictionary<int, string> Load(string? path = null)
    {
        var names = Defaults();
        try
        {
            var stored = JsonSerializer.Deserialize<Dictionary<int, string>>(File.ReadAllText(path ?? FilePath));
            if (stored != null)
                foreach (int number in names.Keys.ToArray())
                    if (stored.TryGetValue(number, out var name) && IsValidName(name)) names[number] = name.Trim();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        return names;
    }

    public static bool IsValidName(string? name) => !string.IsNullOrWhiteSpace(name) &&
        name.Trim().Length <= 32 && !name.Any(char.IsControl);

    public static void Save(IReadOnlyDictionary<int, string> names, string? path = null)
    {
        var validated = Defaults();
        foreach (int number in validated.Keys.ToArray())
        {
            if (!names.TryGetValue(number, out var name) || !IsValidName(name))
                throw new ArgumentException("Bitte einen Namen mit 1 bis 32 Zeichen eingeben.");
            validated[number] = name.Trim();
        }
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(validated));
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
