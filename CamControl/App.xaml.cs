using System.Windows;

namespace CamControl;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LanguageSettings.Initialize();
        Theme.Initialize();
        bool demo = e.Args.Contains("--demo", StringComparer.OrdinalIgnoreCase);
        if (!demo && !CameraSettings.TryLoad(null, out _))
        {
            var welcome = new StartupChoiceDialog();
            if (welcome.ShowDialog() != true) { Shutdown(); return; }
            demo = welcome.UseDemo;
        }
        var main = new MainWindow(new CameraSession(demo));
        MainWindow = main;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        main.Show();
    }
}
