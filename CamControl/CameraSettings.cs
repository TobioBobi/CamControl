using System.Text.Json;

namespace CamControl
{
    internal static class CameraSettings
    {
        public const string DefaultCameraIp = "192.168.188.24";
        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CamControl", "settings.json");

        public static bool TryNormalizeIp(string input, out string ip)
        {
            ip = string.Empty;
            var parts = input.Trim().Split('.');
            if (parts.Length != 4 || parts.Any(part => part.Length == 0 || part.Length > 3 ||
                part.Any(character => character < '0' || character > '9') ||
                !byte.TryParse(part, out _)))
                return false;

            ip = string.Join(".", parts.Select(part => byte.Parse(part).ToString()));
            return true;
        }

        public static string Load(string? path = null)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(path ?? FilePath));
                if (settings?.CameraIp != null && TryNormalizeIp(settings.CameraIp, out var ip))
                    return ip;
            }
            catch (Exception exception) when (exception is IOException ||
                exception is UnauthorizedAccessException || exception is JsonException)
            {
                // A missing, unreadable or damaged settings file must not prevent startup.
            }
            return DefaultCameraIp;
        }

        public static void Save(string ip, string? path = null)
        {
            if (!TryNormalizeIp(ip, out var normalizedIp))
                throw new ArgumentException("Ungültige IPv4-Adresse.", nameof(ip));

            path ??= FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(
                    new StoredSettings { CameraIp = normalizedIp }));
                File.Move(temporaryPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private sealed class StoredSettings
        {
            public string? CameraIp { get; set; }
        }
    }
}
