using System.Reflection;

namespace CamControl
{
    internal static class AppInformation
    {
        internal const string Publisher = "TobioBobi";
        internal static string Description => Texts.About_Description;
        internal const string ProjectUrl = "https://github.com/TobioBobi/CamControl";
        internal static string Version => typeof(AppInformation).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
            ?? typeof(AppInformation).Assembly.GetName().Version?.ToString() ?? Texts.Common_Unknown;

        internal static string PrivacyText
        {
            get
            {
                string resource = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
                {
                    "de" => "CamControl.PrivacyPolicy",
                    "es" => "CamControl.PrivacyPolicy.es",
                    _ => "CamControl.PrivacyPolicy.en"
                };
                using var stream = typeof(AppInformation).Assembly.GetManifestResourceStream(resource)
                    ?? throw new InvalidOperationException(Texts.About_PrivacyMissing);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }
    }
}
