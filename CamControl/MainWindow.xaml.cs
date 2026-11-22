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
    private bool finalClose, closing, settingsOpen, startPresetPending;
    private int? lastRequested;
    internal MainWindow(CameraSession session, string? windowPlacementPath = null)
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
        InitializeWindowPlacement(windowPlacementPath);
        AttachController(); Refresh();
        Loaded += async (_, _) =>
        {
            startPresetPending = Session.Automatic.StartPreset != null;
            if (await Session.Service.ResolveCameraAsync()) await RunStartupPresetAsync();
        };
        Activated += async (_, _) =>
        {
            if (closing) return;
            await Session.Controller.SetActiveAsync(true);
            await RunStartupPresetAsync();
        };
        Deactivated += async (_, _) => { Input.Reset(); if (!settingsOpen && !closing) await Session.Controller.SetActiveAsync(false); };
        Closing += ClosingAsync;
        Closed += (_, _) => Session.Dispose();
    }
    private void AttachController() => Session.Controller.Changed += Refresh;
    private async Task RunStartupPresetAsync()
    {
        if (!startPresetPending || closing || settingsOpen || !Session.Controller.ControlsEnabled) return;
        startPresetPending = false;
        if (Session.Automatic.StartPreset is { } preset) await RecallAsync(preset);
    }
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
        string error = Session.Controller.Error;
        Status.Text = error.Length > 0 ? error
            : !Session.Service.IsCameraAvailable ? Texts.Camera_Discovery_Resolving
            : Session.IsDemo ? Texts.Main_DemoStatus + (requested.Length > 0 ? " · " + requested : "")
            : requested.Length > 0 ? requested : Texts.Main_CameraStatus;
        Status.SetResourceReference(TextBlock.ForegroundProperty, error.Length > 0 ? "ErrorBrush" : "MutedBrush");
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
            var dialog = new CameraSettingsDialog(Session.Target, Session) { Owner = this };
            dialog.PresetStopFailed += Session.Controller.ReportStopFailure;
            if (dialog.ShowDialog() == true)
            {
                startPresetPending = false;
                if (dialog.RequestedDemoMode is { } demo && demo != Session.IsDemo)
                {
                    SaveWindowPlacement();
                    previous.Changed -= Refresh;
                    Session.SwitchMode(demo); AttachController(); lastRequested = null;
                    RestoreWindowPlacement();
                    foreach (var button in buttons.Values) button.Tag = null;
                }
                else if (dialog.RequestedDemoMode == null)
                {
                    Session.ApplyCameraTarget(dialog.Target);
                    lastRequested = null;
                    foreach (var button in buttons.Values) button.Tag = null;
                }
                _ = Session.Service.ResolveCameraAsync();
            }
            UpdateDemo();
        }
        finally
        {
            settingsOpen = false;
            DemoPreview.Visibility = Session.IsDemo && CurrentLayout.View == CameraView.Full ? Visibility.Visible : Visibility.Collapsed;
            Session.Controller.Resume();
            await Session.Controller.SetActiveAsync(IsActive);
            await RunStartupPresetAsync();
            Refresh();
        }
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
        SaveWindowPlacement();
        closing = true; Input.Reset(); Refresh();
        int? target = Session.Automatic.ExitPreset is { } n ? Session.Numbers[n] : null;
        await Session.Controller.CloseAsync(target);
        finalClose = true;
        _ = Dispatcher.BeginInvoke(Close);
    }
    protected override void OnContentRendered(EventArgs e) { base.OnContentRendered(e); UpdateDemo(); }
}
