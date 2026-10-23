using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace CamControl;

internal sealed class PresetEditorDialog : AppWindow
{
    private readonly CameraSession session;
    private readonly int buttonNumber;
    internal readonly CameraController Controller;
    internal readonly CameraInput Input;
    internal readonly TextBox PresetName, PresetNumber;
    internal readonly Button SaveMapping, Recall, Store;
    private readonly Button close;
    private readonly FrameworkElement motion;
    internal readonly TextBlock Status = Ui.Text("");
    private int savedNumber;
    private bool closing, finalClose, confirming;
    internal event Action<string>? CloseStopFailed;
    internal PresetEditorDialog(CameraSession session, int buttonNumber)
    {
        this.session = session; this.buttonNumber = buttonNumber; savedNumber = session.Numbers[buttonNumber];
        Controller = new(session.Service);
        Title = Texts.Format(Texts.Preset_EditTitle, buttonNumber); Width = 610; Height = session.IsDemo ? 730 : 540; MinWidth = 510; MinHeight = 400; ShowInTaskbar = false;
        Input = new(this, () => Controller);
        PresetName = new() { Text = session.Names[buttonNumber], MaxLength = 32, Name = "presetName", Margin = new(0, 0, 0, 12) };
        PresetNumber = new() { Text = savedNumber.ToString(CultureInfo.InvariantCulture), MaxLength = 3, Name = "cameraPresetNumber", Margin = new(0, 0, 0, 8) };
        System.Windows.Automation.AutomationProperties.SetName(PresetName, Texts.Preset_Name); System.Windows.Automation.AutomationProperties.SetName(PresetNumber, Texts.Preset_Number);
        SaveMapping = Ui.Button(Texts.Preset_SaveMapping, () => SaveName(), "savePresetName");
        Recall = Ui.Button(Texts.Preset_Recall, async () => await RunPositionAsync(false), "recallPreset");
        Store = Ui.Button(Texts.Preset_SaveButton, async () =>
        {
            if (!Controller.ControlsEnabled || !ValidNumber(out var selected)) return;
            confirming = true; await Controller.SuspendAsync(); Input.Reset();
            try
            {
                bool accepted = new ConfirmationDialog(this, Texts.Format(Texts.Preset_Confirm, selected, PresetName.Text.Trim())).ShowDialog() == true;
                Controller.Resume(); await Controller.SetActiveAsync(IsActive);
                if (accepted) await RunPositionAsync(true);
            }
            finally { confirming = false; Controller.Resume(); UpdateActions(); }
        }, "storePreset");
        motion = Input.CreatePanel();
        var edit = Ui.Stack(Ui.Text(Texts.Preset_NameHint), PresetName, Ui.Text(Texts.Preset_Number), PresetNumber, Ui.Text(Texts.Preset_NumberHint, "MutedBrush"), SaveMapping, Recall, Store);
        var grid = new Grid(); grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new() { Width = new(188) });
        edit.Margin = new(0, 0, 18, 0); grid.Children.Add(edit); Grid.SetColumn(motion, 1); grid.Children.Add(motion);
        var content = Ui.Stack(Ui.Heading(Texts.Format(Texts.Preset_EditTitle, buttonNumber)), grid);
        if (session.IsDemo) content.Children.Add(new DemoCameraView(session.Demo) { Height = 180, Margin = new(0, 16, 0, 0) });
        content.Children.Add(Status); content.Margin = new(18);
        close = Ui.Button(Texts.Common_Close, Close); close.IsCancel = true;
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new(12, 8, 12, 8) }; footer.Children.Add(close);
        var layout = new DockPanel(); DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer); layout.Children.Add(Ui.Scroll(content)); Content = layout;
        Controller.Changed += () => { if (Controller.Error.Length > 0) { Status.Text = Controller.Error; Status.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); } UpdateActions(); };
        PresetName.TextChanged += (_, _) => UpdateActions(); PresetNumber.TextChanged += (_, _) => UpdateActions();
        Activated += async (_, _) => { if (!closing) await Controller.SetActiveAsync(true); };
        Deactivated += async (_, _) => { Input.Reset(); if (!closing) await Controller.SetActiveAsync(false); };
        Closing += ClosingAsync; Closed += (_, _) => Controller.Dispose(); UpdateActions();
    }
    private bool ValidNumber(out int value)
    {
        if (!int.TryParse(PresetNumber.Text, NumberStyles.None, CultureInfo.InvariantCulture, out value) || !PresetSettings.IsValidNumber(value)) return false;
        int selected = value;
        return !session.Numbers.Any(p => p.Key != buttonNumber && p.Value == selected);
    }
    private void UpdateActions()
    {
        bool enabled = Controller.ControlsEnabled && !closing && !confirming;
        PresetName.IsEnabled = PresetNumber.IsEnabled = motion.IsEnabled = enabled;
        SaveMapping.IsEnabled = enabled && PresetSettings.IsValidName(PresetName.Text) && ValidNumber(out _);
        Recall.IsEnabled = enabled && ValidNumber(out int n) && n == savedNumber;
        Store.IsEnabled = SaveMapping.IsEnabled;
    }
    internal bool SaveName()
    {
        if (!PresetSettings.IsValidName(PresetName.Text) || !ValidNumber(out int number)) { Status.Text = Texts.Preset_Invalid; Status.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); return false; }
        try
        {
            session.SaveMapping(buttonNumber, PresetName.Text, number); savedNumber = number;
            Status.Text = Texts.Preset_Saved; Status.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush"); UpdateActions(); return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException) { Status.Text = Texts.Format(Texts.Preset_SaveFailed, e.Message); Status.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); return false; }
    }
    internal async Task RunPositionAsync(bool save)
    {
        if (!Controller.ControlsEnabled || closing) return;
        if (!save && (!ValidNumber(out int n) || n != savedNumber)) return;
        if (save && !SaveName()) return;
        if (await (save ? Controller.SaveAsync(savedNumber) : Controller.RecallAsync(savedNumber)))
        { Status.Text = save ? Texts.Preset_StoreSent : Texts.Preset_RecallSent; Status.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush"); }
    }
    private async void ClosingAsync(object? sender, CancelEventArgs e)
    {
        if (finalClose) return;
        e.Cancel = true;
        if (closing) return;
        closing = true; close.IsEnabled = false; Input.Reset(); UpdateActions();
        if (!await Controller.CloseAsync()) CloseStopFailed?.Invoke(Controller.Error);
        finalClose = true; _ = Dispatcher.BeginInvoke(Close);
    }
}
