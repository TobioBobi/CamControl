using System.Globalization;
using System.Text.Json;

namespace CamControl;

internal enum AppLanguage { System, German, English, Spanish }

internal static class LanguageSettings
{
    internal static string FilePath => Path.Combine(Path.GetDirectoryName(CameraSettings.FilePath)!, "language.json");

    internal static AppLanguage Load(string? path = null)
    {
        try
        {
            string? value = JsonSerializer.Deserialize<string>(File.ReadAllText(path ?? FilePath));
            if (Enum.TryParse<AppLanguage>(value, out var language) && Enum.IsDefined(language)) return language;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        return AppLanguage.System;
    }

    internal static void Save(AppLanguage language, string? path = null)
    {
        if (!Enum.IsDefined(language)) throw new ArgumentOutOfRangeException(nameof(language));
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(language.ToString())); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    internal static CultureInfo Resolve(AppLanguage language, CultureInfo systemCulture) =>
        CultureInfo.GetCultureInfo(language switch
        {
            AppLanguage.German => "de",
            AppLanguage.English => "en",
            AppLanguage.Spanish => "es",
            _ => systemCulture.TwoLetterISOLanguageName switch { "de" => "de", "es" => "es", _ => "en" }
        });

    internal static void Initialize(string? path = null)
    {
        // UI language is independent of regional number/date formats and camera protocol.
        var culture = Resolve(Load(path), CultureInfo.CurrentUICulture);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
