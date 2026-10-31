using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;

namespace CamControl;

public partial class MainWindow : AppWindow
{
    internal CameraSession Session { get; }
    internal CameraInput Input { get; }
    private readonly Dictionary<int, Button> buttons = new();
    private readonly Dictionary<int, TextBlock> presetLabels = new();
    private bool finalClose, closing, settingsOpen;
    private int? lastRequested;
    internal MainWindow(CameraSession session)
    {
        Session = session;
        InitializeComponent();
        Input = new(this, () => Session.Controller, async n => await RecallAsync(n));
        manual = Input.CreatePanel();
        ManualControls.Content = manual;
        for (int i = 0; i < CameraSession.Buttons.Length; i++)
        {
            int number = CameraSession.Buttons[i];
            var button = new Button { Name = "preset" + number, Style = (Style)FindResource("PresetButton") };
            var grid = new Grid(); grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); grid.ColumnDefinitions.Add(new());
            var key = new TextBlock { Text = number.ToString(), Margin = new(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.NoWrap };
            key.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); grid.Children.Add(key);
            var label = new TextBlock { TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(label, 1); grid.Children.Add(label); presetLabels[number] = label; button.Content = grid;
            button.Click += async (_, _) => await RecallAsync(number);
            buttons[number] = button; Presets.Children.Add(button);
        }
        InitializeResponsiveLayout();
        AttachController(); Refresh();
        Loaded += async (_, _) => { if (Session.Automatic.StartPreset is { } n) await RecallAsync(n); };
        Activated += async (_, _) => { if (!closing) await Session.Controller.SetActiveAsync(true); };
        Deactivated += async (_, _) => { Input.Reset(); if (!settingsOpen && !closing) await Session.Controller.SetActiveAsync(false); };
        Closing += ClosingAsync;
        Closed += (_, _) => Session.Dispose();
    }
    private void AttachController() => Session.Controller.Changed += Refresh;
    internal async Task RecallAsync(int number)
    {
        if (await Session.Controller.RecallAsync(Session.Numbers[number]))
        {
            lastRequested = number;
            foreach (var pair in buttons) pair.Value.Tag = pair.Key == number ? "Requested" : null;
            Refresh();
        }
    }
    internal void Refresh()
    {
        foreach (var (number, button) in buttons)
        {
            presetLabels[number].Text = Session.Names[number];
            button.ToolTip = Session.Names[number];
            AutomationProperties.SetName(button, Texts.Format(Texts.Preset_AccessibleName, Session.Names[number], number));
            button.IsEnabled = Session.Controller.ControlsEnabled && !settingsOpen;
        }
        ManualControls.IsEnabled = Session.Controller.ControlsEnabled && !settingsOpen;
        SettingsButton.IsEnabled = !closing && !settingsOpen;
        ModeLabel.Text = Session.IsDemo ? "CamControl · Demo" : "CamControl";
        Title = Session.IsDemo ? "CamControl – Demo" : "CamControl";
        string requested = lastRequested is { } n ? Texts.Format(Texts.Main_Requested, Session.Names[n]) : "";
        Status.Text = Session.Controller.Error.Length > 0 ? Session.Controller.Error : Session.IsDemo ? Texts.Main_DemoStatus + (requested.Length > 0 ? " · " + requested : "") : requested.Length > 0 ? requested : Texts.Main_CameraStatus;
        Status.SetResourceReference(TextBlock.ForegroundProperty, Session.Controller.Error.Length > 0 ? "ErrorBrush" : "MutedBrush");
    }
    private async void SettingsClick(object sender, RoutedEventArgs e)
    {
        if (settingsOpen || closing) return;
        settingsOpen = true; Input.Reset(); Refresh();
        var previous = Session.Controller;
        try
        {
            await previous.SuspendAsync();
            if (closing) return;
            DemoPreview.Visibility = Visibility.Collapsed;
            var dialog = new CameraSettingsDialog(Session.Service.CameraIp, Session) { Owner = this };
            dialog.PresetStopFailed += Session.Controller.ReportStopFailure;
            if (dialog.ShowDialog() == true)
            {
                Session.Service.SetCameraIp(dialog.CameraIp);
                if (dialog.RequestedDemoMode is { } demo && demo != Session.IsDemo)
                {
                    previous.Changed -= Refresh;
                    Session.SwitchMode(demo); AttachController(); lastRequested = null;
                    foreach (var button in buttons.Values) button.Tag = null;
                }
            }
            UpdateDemo();
        }
        finally { settingsOpen = false; DemoPreview.Visibility = Session.IsDemo && CurrentLayout.View == CameraView.Full ? Visibility.Visible : Visibility.Collapsed; Session.Controller.Resume(); await Session.Controller.SetActiveAsync(IsActive); Refresh(); }
    }
    private void UpdateDemo()
    {
        DemoPreview.Visibility = Session.IsDemo && CurrentLayout.View == CameraView.Full ? Visibility.Visible : Visibility.Collapsed;
        DemoPreview.Content = Session.IsDemo ? new DemoCameraView(Session.Demo) : null;

    }
    private async void ClosingAsync(object? sender, CancelEventArgs e)
    {
        if (finalClose) return;
        e.Cancel = true;
        if (closing) return;
        closing = true; Input.Reset(); Refresh();
        int? target = Session.Automatic.ExitPreset is { } n ? Session.Numbers[n] : null;
        await Session.Controller.CloseAsync(target);
        finalClose = true;
        _ = Dispatcher.BeginInvoke(Close);
    }
    protected override void OnContentRendered(EventArgs e) { base.OnContentRendered(e); UpdateDemo(); }
}
