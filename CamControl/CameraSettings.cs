using System.Text.Json;

namespace CamControl
{
    internal sealed record CameraTarget(string Ip, string? DeviceId = null);

    internal static class CameraSettings
    {
        private static readonly Mutex WriteLock = new(false, "CamControl.CameraSettings");
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
            => TryLoad(path, out var ip) ? ip : DefaultCameraIp;

        public static bool TryLoad(string? path, out string ip)
        {
            bool loaded = TryLoadTarget(path, out var target);
            ip = target.Ip;
            return loaded;
        }

        public static CameraTarget LoadTarget(string? path = null)
        {
            TryLoadTarget(path, out var target);
            return target;
        }

        internal static bool TryLoadTarget(string? path, out CameraTarget target)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(path ?? FilePath));
                if (settings?.CameraIp != null && TryNormalizeIp(settings.CameraIp, out var ip))
                {
                    target = new(ip, string.IsNullOrWhiteSpace(settings.DeviceId) ? null : settings.DeviceId);
                    return true;
                }
            }
            catch (Exception exception) when (exception is IOException ||
                exception is UnauthorizedAccessException || exception is JsonException)
            {
                // A missing, unreadable or damaged settings file must not prevent startup.
            }
            target = new(DefaultCameraIp);
            return false;
        }

        public static void Save(string ip, string? path = null)
            => SaveTarget(new(ip), path);

        public static void SaveTarget(CameraTarget target, string? path = null)
        {
            if (!TryNormalizeIp(target.Ip, out var normalizedIp))
                throw new ArgumentException(Texts.Camera_InvalidIp, nameof(target));

            WithWriteLock(() => WriteTarget(target with { Ip = normalizedIp }, path ?? FilePath));
        }

        public static void RememberAddress(CameraTarget confirmed, string? path = null)
            => WithWriteLock(() =>
            {
                if (confirmed.DeviceId != null && TryLoadTarget(path, out var saved) && saved.DeviceId == confirmed.DeviceId)
                    WriteTarget(confirmed, path ?? FilePath);
            });

        private static void WithWriteLock(Action write)
        {
            try { WriteLock.WaitOne(); }
            catch (AbandonedMutexException) { /* A stopped writer releases ownership to this caller. */ }
            try { write(); }
            finally { WriteLock.ReleaseMutex(); }
        }

        private static void WriteTarget(CameraTarget target, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(
                    new StoredSettings { CameraIp = target.Ip, DeviceId = target.DeviceId }));
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
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string? DeviceId { get; set; }
        }
    }
}
