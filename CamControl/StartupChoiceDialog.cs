using System.Windows;

namespace CamControl;

internal sealed class StartupChoiceDialog : AppWindow
{
    public bool UseDemo { get; private set; }
    public StartupChoiceDialog()
    {
        Title = "Willkommen bei CamControl"; Width = 480; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var demo = Ui.Button("Ohne Kamera ausprobieren", () => { UseDemo = true; DialogResult = true; }); demo.MinHeight = 54;
        var setup = Ui.Button("Kamera einrichten", () =>
        {
            var settings = new CameraSettingsDialog("") { Owner = this };
            if (settings.ShowDialog() == true) { UseDemo = settings.RequestedDemoMode == true; DialogResult = true; }
        }); setup.MinHeight = 54;
        var close = Ui.Button("Schließen", () => DialogResult = false); close.IsCancel = true;
        var panel = Ui.Stack(Ui.Heading("Willkommen bei CamControl"), Ui.Text("PTZ-Kamera mit HTTP-CGI einrichten oder ohne Kamera ausprobieren."), demo, setup, close); panel.Margin = new(20); Content = Ui.Scroll(panel);
    }
}
