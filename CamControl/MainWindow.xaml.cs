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
    private bool finalClose, closing, settingsOpen, demoExpanded;
    private int? lastRequested;
    internal MainWindow(CameraSession session)
    {
        Session = session;
        InitializeComponent();
        Input = new(this, () => Session.Controller, async n => await RecallAsync(n));
        ManualControls.Content = Input.CreatePanel();
        for (int i = 0; i < CameraSession.Buttons.Length; i++)
        {
            int number = CameraSession.Buttons[i];
            var button = new Button { Name = "preset" + number, Style = (Style)FindResource("PresetButton"), Margin = new(i == 6 ? 0 : i % 2 == 0 ? 0 : 4, 0, i == 6 ? 0 : i % 2 == 0 ? 4 : 0, i == 6 ? 0 : 6) };
            var grid = new Grid(); grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            var label = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(label); presetLabels[number] = label;
            var key = new TextBlock { Text = number.ToString(), FontSize = 11, Margin = new(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; key.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); Grid.SetColumn(key, 1); grid.Children.Add(key); button.Content = grid;
            Grid.SetRow(button, i / 2); Grid.SetColumn(button, i % 2); if (i == 6) { Grid.SetColumn(button, 0); Grid.SetColumnSpan(button, 2); }
            button.Click += async (_, _) => await RecallAsync(number);
            buttons[number] = button; Presets.Children.Add(button);
        }
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
            DemoPreview.Visibility = Visibility.Hidden;
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
        finally { settingsOpen = false; DemoPreview.Visibility = Session.IsDemo ? Visibility.Visible : Visibility.Collapsed; Session.Controller.Resume(); await Session.Controller.SetActiveAsync(IsActive); Refresh(); }
    }
    private void UpdateDemo()
    {
        DemoPreview.Visibility = Session.IsDemo ? Visibility.Visible : Visibility.Collapsed;
        DemoPreview.Content = Session.IsDemo ? new DemoCameraView(Session.Demo) : null;
        if (demoExpanded != Session.IsDemo) Height = Math.Min(MaxHeight, Height + (Session.IsDemo ? 212 : -212));
        demoExpanded = Session.IsDemo;
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
