namespace CamControl
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            bool useDemo = args.Contains("--demo", StringComparer.OrdinalIgnoreCase);
            if (!useDemo && !CameraSettings.TryLoad(null, out _))
            {
                using var startup = new StartupChoiceDialog();
                if (startup.ShowDialog() != DialogResult.OK)
                    return;
                useDemo = startup.UseDemo;
            }
            Application.Run(new Form1(useDemo));
        }
    }
}
