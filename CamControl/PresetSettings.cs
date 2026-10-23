using System.Text.Json;

namespace CamControl;

internal static class PresetSettings
{
    public static string FilePath => Path.Combine(Path.GetDirectoryName(CameraSettings.FilePath)!, "buttons.json");
    public static Dictionary<int, string> Defaults() => new()
    {
        [1] = "Preset 1", [2] = "Preset 2", [3] = "Preset 3", [4] = "Preset 4",
        [5] = "Preset 5", [6] = "Preset 6", [9] = "Preset 9"
    };

    public static string Caption(int number, string name) =>
        name == $"Preset {number}" ? name : $"{name} ({number})";

    public static bool IsValidNumber(int number) => number is >= 0 and <= 254 && number is not (>= 90 and <= 99);
    public static Dictionary<int, int> DefaultNumbers() => Defaults().Keys.ToDictionary(key => key, key => key);
    private sealed record Configuration(Dictionary<int, string> Names, Dictionary<int, int> Numbers);

    public static Dictionary<int, int> LoadNumbers(string? path = null)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path ?? FilePath));
            if (document.RootElement.TryGetProperty("Numbers", out var value))
            {
                var numbers = value.Deserialize<Dictionary<int, int>>();
                if (numbers != null && DefaultNumbers().Keys.All(numbers.ContainsKey) &&
                    numbers.Count == 7 && numbers.Values.All(IsValidNumber) && numbers.Values.Distinct().Count() == 7)
                    return numbers;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException) { }
        return DefaultNumbers();
    }

    public static Dictionary<int, string> Load(string? path = null)
    {
        var names = Defaults();
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path ?? FilePath));
            var root = document.RootElement;
            var stored = (root.TryGetProperty("Names", out var value) ? value : root).Deserialize<Dictionary<int, string>>();
            if (stored != null)
                foreach (int number in names.Keys.ToArray())
                    if (stored.TryGetValue(number, out var name) && IsValidName(name)) names[number] = name.Trim();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException) { }
        return names;
    }

    public static bool IsValidName(string? name) => !string.IsNullOrWhiteSpace(name) &&
        name.Trim().Length <= 32 && !name.Any(char.IsControl);

    public static void Save(IReadOnlyDictionary<int, string> names, string? path = null)
        => SaveConfiguration(names, LoadNumbers(path), path);

    public static void SaveConfiguration(IReadOnlyDictionary<int, string> names, IReadOnlyDictionary<int, int> numbers, string? path = null)
    {
        if (numbers.Count != 7 || !DefaultNumbers().Keys.All(numbers.ContainsKey) ||
            !numbers.Values.All(IsValidNumber) || numbers.Values.Distinct().Count() != 7)
            throw new ArgumentException(Texts.Preset_InvalidNumbers);
        var validated = Defaults();
        foreach (int number in validated.Keys.ToArray())
        {
            if (!names.TryGetValue(number, out var name) || !IsValidName(name))
                throw new ArgumentException(Texts.Preset_InvalidName);
            validated[number] = name.Trim();
        }
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new Configuration(validated, new Dictionary<int, int>(numbers))));
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
