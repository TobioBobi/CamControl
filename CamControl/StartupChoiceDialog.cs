using System.Windows;

namespace CamControl;

internal sealed class StartupChoiceDialog : AppWindow
{
    public bool UseDemo { get; private set; }
    public StartupChoiceDialog()
    {
        Title = Texts.Welcome_Title; Width = 480; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var demo = Ui.Button(Texts.Welcome_Demo, () => { UseDemo = true; DialogResult = true; }); demo.MinHeight = 54;
        var setup = Ui.Button(Texts.Welcome_Setup, () =>
        {
            var settings = new CameraSettingsDialog("") { Owner = this };
            if (settings.ShowDialog() == true) { UseDemo = settings.RequestedDemoMode == true; DialogResult = true; }
        }); setup.MinHeight = 54;
        var close = Ui.Button(Texts.Common_Close, () => DialogResult = false); close.IsCancel = true;
        var panel = Ui.Stack(Ui.Heading(Texts.Welcome_Title), Ui.Text(Texts.Welcome_Description), demo, setup, close); panel.Margin = new(20); Content = Ui.Scroll(panel);
    }
}
