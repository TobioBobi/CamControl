using System.Reflection;

namespace CamControl
{
    internal static class AppInformation
    {
        internal const string Publisher = "Tobias Heer";
        internal const string ProjectUrl = "https://github.com/TobioBobi/CamControl";
        internal static string Version => typeof(AppInformation).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
            ?? typeof(AppInformation).Assembly.GetName().Version?.ToString() ?? "Unbekannt";

        internal static string PrivacyText
        {
            get
            {
                using var stream = typeof(AppInformation).Assembly.GetManifestResourceStream("CamControl.PrivacyPolicy")
                    ?? throw new InvalidOperationException("Datenschutztext fehlt im App-Paket.");
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }
    }
}
