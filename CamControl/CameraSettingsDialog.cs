using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;

namespace CamControl;

internal sealed class CameraSettingsDialog : AppWindow
{
    private readonly CameraSession? session;
    private readonly string? settingsPath;
    internal readonly TextBox Address;
    internal readonly Button SaveAddress, DemoAction, SaveAutomatic;
    internal readonly ComboBox StartPreset = new() { Name = "startPreset" }, ExitPreset = new() { Name = "exitPreset" };
    internal readonly ListBox Navigation = new() { Name = "settingsNavigation" };
    private readonly ContentControl page = new();
    private readonly TextBlock feedback = Ui.Text(""), automaticFeedback = Ui.Text(""), supportFeedback = Ui.Text("");
    private readonly StackPanel presets = new();
    private readonly Button close;
    private readonly Dictionary<string, FrameworkElement> pages = new();
    private AutomaticPositions savedAutomatic;
    private bool refreshing;
    public string CameraIp { get; private set; }
    public bool? RequestedDemoMode { get; private set; }
    internal event Action<string>? PresetStopFailed;

    internal CameraSettingsDialog(string cameraIp, CameraSession? session = null, string? settingsPath = null)
    {
        this.session = session; this.settingsPath = settingsPath; CameraIp = cameraIp;
        savedAutomatic = session?.Automatic ?? new();
        Title = "Einstellungen"; Width = 760; Height = 570; MinWidth = 600; MinHeight = 400; ShowInTaskbar = false;
        Address = new() { Text = cameraIp, Name = "cameraAddress", MaxLength = 64, Margin = new(0, 0, 0, 8) };
        AutomationProperties.SetName(Address, "IP-Adresse der Kamera");
        SaveAddress = Ui.Button("Speichern", SaveCamera, "saveConfiguration"); SaveAddress.HorizontalAlignment = HorizontalAlignment.Left;
        close = Ui.Button("Schließen", Close); close.IsCancel = true;
        DemoAction = Ui.Button(session?.IsDemo == true ? "Zur Kamera wechseln" : "Demo starten", () => { RequestedDemoMode = session?.IsDemo != true; DialogResult = true; }, "demoAction");
        SaveAutomatic = Ui.Button("Automatik speichern", StoreAutomatic, "saveAutomaticPositions"); SaveAutomatic.HorizontalAlignment = HorizontalAlignment.Left;
        pages["Kamera"] = Ui.Stack(Ui.Heading("Kamera"), Ui.Text("IP-Adresse der Kamera"), Address, SaveAddress, feedback,
            Ui.Text("IPv4 · HTTP auf Port 80 · Kamera und PC müssen im erreichbaren Netzwerk sein.", "MutedBrush"));
        pages["Buttons & Presets"] = Ui.Stack(Ui.Heading("Buttons & Presets"), Ui.Text("Namen, Kamerapreset-Nummern und Positionen bearbeiten.", "MutedBrush"), presets);
        pages["Automatische Positionen"] = Ui.Stack(Ui.Heading("Automatische Positionen"), Ui.Text("Optional eine Position beim Start und beim Beenden aufrufen. Ohne Auswahl gibt es keine automatische Fahrt.", "MutedBrush"), Ui.Text("Beim Start"), StartPreset, Ui.Text("Beim Beenden"), ExitPreset, SaveAutomatic, automaticFeedback);
        pages["Demo-Modus"] = Ui.Stack(Ui.Heading("Demo-Modus"), Ui.Text(session?.IsDemo == true ? "Demo aktiv · Keine Netzwerkbefehle. Änderungen gelten nur für diese Sitzung." : "Bewegung, Zoom und Presets ohne Kamera testen. Deine Kameraeinstellungen bleiben erhalten."), DemoAction);
        pages["Hilfe & Voraussetzungen"] = Ui.Stack(Ui.Heading("Hilfe & Voraussetzungen"), ReadOnlyText(CameraHelp.Text, "cameraHelp"));
        var support = Ui.Button("Projekt & Support auf GitHub ↗", () =>
        {
            try { Process.Start(new ProcessStartInfo(AppInformation.ProjectUrl) { UseShellExecute = true }); }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { supportFeedback.Text = "Der Browser konnte nicht geöffnet werden: " + e.Message; }
        });
        supportFeedback.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush");
        pages["Info & Datenschutz"] = Ui.Stack(Ui.Heading("Info & Datenschutz"), Ui.Text($"CamControl · Version {AppInformation.Version}\n{AppInformation.Description}\nHerausgeber: {AppInformation.Publisher}"), support, supportFeedback, ReadOnlyText(AppInformation.PrivacyText, "privacyInformation"));
        foreach (string title in pages.Keys) Navigation.Items.Add(new ListBoxItem { Content = new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap }, Tag = title });
        Navigation.SelectionChanged += (_, _) => { var key = (string)((ListBoxItem)Navigation.SelectedItem).Tag; if (page.Content is ScrollViewer previous) previous.Content = null; page.Content = Ui.Scroll(pages[key]); UpdateAddress(); };
        var body = new Grid(); body.ColumnDefinitions.Add(new() { Width = new(185) }); body.ColumnDefinitions.Add(new());
        Navigation.Margin = new(8); Navigation.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled); body.Children.Add(Navigation);
        page.Margin = new(18); Grid.SetColumn(page, 1); body.Children.Add(page);
        var actions = new DockPanel { Margin = new(12, 8, 12, 8), LastChildFill = false }; DockPanel.SetDock(close, Dock.Right); actions.Children.Add(close);
        var layout = new DockPanel(); DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions); layout.Children.Add(body); Content = layout;
        Address.TextChanged += (_, _) => UpdateAddress();
        StartPreset.SelectionChanged += (_, _) => UpdateAutomatic(); ExitPreset.SelectionChanged += (_, _) => UpdateAutomatic();
        RefreshPresets(); RefreshAutomatic(); Navigation.SelectedIndex = 0;
    }
    private static TextBox ReadOnlyText(string text, string name) => new() { Name = name, Text = text, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, BorderThickness = new(0), Background = System.Windows.Media.Brushes.Transparent, Padding = new(0), AcceptsReturn = true };
    private bool HasAddressChange => CameraSettings.TryNormalizeIp(Address.Text, out string ip) && (!CameraSettings.TryNormalizeIp(CameraIp, out var current) || current != ip);
    private void UpdateAddress()
    {
        bool onCamera = Navigation.SelectedIndex == 0;
        bool edited = Address.Text.Trim() != CameraIp;
        SaveAddress.IsEnabled = HasAddressChange; SaveAddress.IsDefault = onCamera && HasAddressChange;
        DemoAction.IsEnabled = !edited;
        pages["Buttons & Presets"].IsEnabled = !edited;
        pages["Automatische Positionen"].IsEnabled = !edited && session != null;
        close.Content = edited && onCamera ? "Abbrechen" : "Schließen";
    }
    internal void SaveCamera()
    {
        if (Navigation.SelectedIndex != 0 || !HasAddressChange) return;
        CameraSettings.TryNormalizeIp(Address.Text, out var ip);
        try { CameraSettings.Save(ip, settingsPath); CameraIp = ip; DialogResult = true; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { feedback.Text = "Die Einstellung konnte nicht gespeichert werden: " + e.Message; feedback.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); }
    }
    private void RefreshPresets()
    {
        presets.Children.Clear();
        if (session == null) { presets.Children.Add(Ui.Text("Zuerst Kamera einrichten oder Demo starten.")); return; }
        foreach (int number in CameraSession.Buttons)
        {
            var row = new Grid { Margin = new(0, 0, 0, 6) }; row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = $"{PresetSettings.Caption(number, session.Names[number])}\nKamerapreset {session.Numbers[number]}", VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 10, 0) });
            var edit = Ui.Button("Bearbeiten …", () =>
            {
                var editor = new PresetEditorDialog(session, number) { Owner = this };
                editor.CloseStopFailed += error => PresetStopFailed?.Invoke(error);
                editor.ShowDialog(); RefreshPresets(); RefreshAutomatic();
            }, "editPreset" + number);
            Grid.SetColumn(edit, 1); row.Children.Add(edit); presets.Children.Add(row);
        }
    }
    private sealed record PresetChoice(int? Number, string Caption) { public override string ToString() => Caption; }
    private static int? Selection(ComboBox combo) => (combo.SelectedItem as PresetChoice)?.Number;
    internal void RefreshAutomatic()
    {
        refreshing = true;
        try
        {
            foreach (var (combo, saved) in new[] { (StartPreset, savedAutomatic.StartPreset), (ExitPreset, savedAutomatic.ExitPreset) })
            {
                int? selected = combo.Items.Count == 0 ? saved : Selection(combo);
                combo.Items.Clear(); combo.Items.Add(new PresetChoice(null, "Keine Position"));
                foreach (int n in CameraSession.Buttons) combo.Items.Add(new PresetChoice(n, PresetSettings.Caption(n, session?.Names[n] ?? $"Preset {n}")));
                combo.SelectedItem = combo.Items.Cast<PresetChoice>().Single(c => c.Number == selected);
            }
        }
        finally { refreshing = false; UpdateAutomatic(); }
    }
    private void UpdateAutomatic()
    {
        if (refreshing) return;
        SaveAutomatic.IsEnabled = session != null && (Selection(StartPreset) != savedAutomatic.StartPreset || Selection(ExitPreset) != savedAutomatic.ExitPreset);
    }
    internal void StoreAutomatic()
    {
        if (session == null || !SaveAutomatic.IsEnabled) return;
        var choices = new AutomaticPositions { StartPreset = Selection(StartPreset), ExitPreset = Selection(ExitPreset) };
        try { session.SaveAutomatic(choices); savedAutomatic = choices; automaticFeedback.Text = "Automatik gespeichert."; automaticFeedback.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush"); UpdateAutomatic(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { automaticFeedback.Text = "Auswahl konnte nicht gespeichert werden: " + e.Message; automaticFeedback.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); }
    }
}
