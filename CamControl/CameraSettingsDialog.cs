using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;

namespace CamControl;

internal sealed class CameraSettingsDialog : AppWindow
{
    private enum SettingsPage { Camera, Presets, Automatic, Demo, Help, About, Appearance, Language }
    private readonly CameraSession? session;
    private readonly string? settingsPath;
    internal readonly TextBox Address;
    internal readonly Button SaveAddress, DemoAction, SaveAutomatic;
    internal readonly ComboBox StartPreset = new() { Name = "startPreset" }, ExitPreset = new() { Name = "exitPreset" };
    internal readonly ComboBox AppearanceChoice = new() { Name = "appearanceChoice" };
    internal readonly Button SaveAppearance;
    internal readonly ComboBox LanguageChoice = new() { Name = "languageChoice" };
    internal readonly Button SaveLanguage;
    internal readonly ListBox Navigation = new() { Name = "settingsNavigation" };
    private readonly ContentControl page = new();
    private readonly TextBlock feedback = Ui.Text(""), automaticFeedback = Ui.Text(""), supportFeedback = Ui.Text("");
    private readonly StackPanel presets = new();
    private readonly Button close;
    private readonly Dictionary<SettingsPage, FrameworkElement> pages = new();
    private AutomaticPositions savedAutomatic;
    private bool refreshing;
    public string CameraIp { get; private set; }
    public bool? RequestedDemoMode { get; private set; }
    internal event Action<string>? PresetStopFailed;

    internal CameraSettingsDialog(string cameraIp, CameraSession? session = null, string? settingsPath = null, string? appearancePath = null, string? languagePath = null)
    {
        this.session = session; this.settingsPath = settingsPath; CameraIp = cameraIp;
        savedAutomatic = session?.Automatic ?? new();
        Title = Texts.Settings_Title; Width = 760; Height = 570; MinWidth = 600; MinHeight = 400; ShowInTaskbar = false;
        Address = new() { Text = cameraIp, Name = "cameraAddress", MaxLength = 64, Margin = new(0, 0, 0, 8) };
        AutomationProperties.SetName(Address, Texts.Camera_Address);
        SaveAddress = Ui.Button(Texts.Common_Save, SaveCamera, "saveConfiguration"); SaveAddress.HorizontalAlignment = HorizontalAlignment.Left;
        close = Ui.Button(Texts.Common_Close, Close); close.IsCancel = true;
        DemoAction = Ui.Button(session?.IsDemo == true ? Texts.Demo_SwitchCamera : Texts.Demo_Start, () => { RequestedDemoMode = session?.IsDemo != true; DialogResult = true; }, "demoAction");
        SaveAutomatic = Ui.Button(Texts.Automatic_Save, StoreAutomatic, "saveAutomaticPositions"); SaveAutomatic.HorizontalAlignment = HorizontalAlignment.Left;
        pages[SettingsPage.Camera] = Ui.Stack(Ui.Heading(Texts.Settings_Camera), Ui.Text(Texts.Camera_Address), Address, SaveAddress, feedback,
            Ui.Text(Texts.Camera_Requirements, "MutedBrush"));
        pages[SettingsPage.Presets] = Ui.Stack(Ui.Heading(Texts.Settings_Presets), Ui.Text(Texts.Preset_Description, "MutedBrush"), presets);
        pages[SettingsPage.Automatic] = Ui.Stack(Ui.Heading(Texts.Settings_Automatic), Ui.Text(Texts.Automatic_Description, "MutedBrush"), Ui.Text(Texts.Automatic_Start), StartPreset, Ui.Text(Texts.Automatic_Exit), ExitPreset, SaveAutomatic, automaticFeedback);
        pages[SettingsPage.Demo] = Ui.Stack(Ui.Heading(Texts.Settings_Demo), Ui.Text(session?.IsDemo == true ? Texts.Demo_Active : Texts.Demo_Description), DemoAction);
        pages[SettingsPage.Help] = Ui.Stack(Ui.Heading(Texts.Settings_Help), ReadOnlyText(CameraHelp.Text, "cameraHelp"));
        var support = Ui.Button(Texts.About_Support, () =>
        {
            try { Process.Start(new ProcessStartInfo(AppInformation.ProjectUrl) { UseShellExecute = true }); }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { supportFeedback.Text = Texts.Format(Texts.About_BrowserFailed, e.Message); }
        });
        supportFeedback.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush");
        pages[SettingsPage.About] = Ui.Stack(Ui.Heading(Texts.Settings_About), Ui.Text(Texts.Format(Texts.About_Details, AppInformation.Version, AppInformation.Description, AppInformation.Publisher)), support, supportFeedback, ReadOnlyText(AppInformation.PrivacyText, "privacyInformation"));
        AppearanceChoice.Items.Add(new AppearanceChoiceItem(Appearance.Dark, Texts.Appearance_Dark));
        AppearanceChoice.Items.Add(new AppearanceChoiceItem(Appearance.Light, Texts.Appearance_Light));
        AppearanceChoice.Items.Add(new AppearanceChoiceItem(Appearance.System, Texts.Common_System));
        Appearance selectedAppearance = Theme.Preference;
        AppearanceChoice.SelectedItem = AppearanceChoice.Items.Cast<AppearanceChoiceItem>().Single(item => item.Value == selectedAppearance);
        var appearanceFeedback = Ui.Text("");
        SaveAppearance = Ui.Button(Texts.Appearance_Save, () =>
        {
            if (AppearanceChoice.SelectedItem is not AppearanceChoiceItem choice) return;
            try
            {
                Theme.SetPreference(choice.Value, appearancePath); selectedAppearance = choice.Value;
                SaveAppearance!.IsEnabled = false; appearanceFeedback.Text = Texts.Appearance_Saved;
                appearanceFeedback.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            { appearanceFeedback.Text = Texts.Format(Texts.Appearance_SaveFailed, e.Message); appearanceFeedback.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); }
        }, "saveAppearance");
        SaveAppearance.IsEnabled = false;
        AppearanceChoice.SelectionChanged += (_, _) => SaveAppearance.IsEnabled = AppearanceChoice.SelectedItem is AppearanceChoiceItem item && item.Value != selectedAppearance;
        AutomationProperties.SetName(AppearanceChoice, Texts.Appearance_Scheme);
        pages[SettingsPage.Appearance] = Ui.Stack(Ui.Heading(Texts.Settings_Appearance), Ui.Text(Texts.Appearance_Scheme), AppearanceChoice, SaveAppearance, appearanceFeedback,
            Ui.Text(Texts.Appearance_Description, "MutedBrush"));
        LanguageChoice.Items.Add(new LanguageChoiceItem(AppLanguage.System, Texts.Common_System));
        LanguageChoice.Items.Add(new LanguageChoiceItem(AppLanguage.German, "Deutsch"));
        LanguageChoice.Items.Add(new LanguageChoiceItem(AppLanguage.English, "English"));
        LanguageChoice.Items.Add(new LanguageChoiceItem(AppLanguage.Spanish, "Español"));
        AppLanguage selectedLanguage = LanguageSettings.Load(languagePath);
        LanguageChoice.SelectedItem = LanguageChoice.Items.Cast<LanguageChoiceItem>().Single(item => item.Value == selectedLanguage);
        var languageFeedback = Ui.Text("");
        SaveLanguage = Ui.Button(Texts.Language_Save, () =>
        {
            if (LanguageChoice.SelectedItem is not LanguageChoiceItem choice) return;
            try
            {
                LanguageSettings.Save(choice.Value, languagePath); selectedLanguage = choice.Value;
                SaveLanguage!.IsEnabled = false; languageFeedback.Text = Texts.Language_Saved;
                languageFeedback.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            { languageFeedback.Text = Texts.Format(Texts.Language_SaveFailed, e.Message); languageFeedback.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); }
        }, "saveLanguage");
        SaveLanguage.IsEnabled = false;
        LanguageChoice.SelectionChanged += (_, _) => SaveLanguage.IsEnabled = LanguageChoice.SelectedItem is LanguageChoiceItem item && item.Value != selectedLanguage;
        AutomationProperties.SetName(LanguageChoice, Texts.Settings_Language);
        pages[SettingsPage.Language] = Ui.Stack(Ui.Heading(Texts.Settings_Language), LanguageChoice, SaveLanguage, languageFeedback,
            Ui.Text(Texts.Language_Description, "MutedBrush"));
        foreach (var id in pages.Keys) Navigation.Items.Add(new ListBoxItem { Content = new TextBlock { Text = PageTitle(id), TextWrapping = TextWrapping.Wrap }, Tag = id });
        Navigation.SelectionChanged += (_, _) => { var key = (SettingsPage)((ListBoxItem)Navigation.SelectedItem).Tag; if (page.Content is ScrollViewer previous) previous.Content = null; page.Content = Ui.Scroll(pages[key]); UpdateAddress(); };
        var body = new Grid(); body.ColumnDefinitions.Add(new() { Width = new(185) }); body.ColumnDefinitions.Add(new());
        Navigation.Margin = new(8); Navigation.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled); body.Children.Add(Navigation);
        page.Margin = new(18); Grid.SetColumn(page, 1); body.Children.Add(page);
        var actions = new DockPanel { Margin = new(12, 8, 12, 8), LastChildFill = false }; DockPanel.SetDock(close, Dock.Right); actions.Children.Add(close);
        var layout = new DockPanel(); DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions); layout.Children.Add(body); Content = layout;
        Address.TextChanged += (_, _) => UpdateAddress();
        StartPreset.SelectionChanged += (_, _) => UpdateAutomatic(); ExitPreset.SelectionChanged += (_, _) => UpdateAutomatic();
        RefreshPresets(); RefreshAutomatic(); Navigation.SelectedIndex = 0;
    }

    private static string PageTitle(SettingsPage page) => page switch
    {
        SettingsPage.Camera => Texts.Settings_Camera,
        SettingsPage.Presets => Texts.Settings_Presets,
        SettingsPage.Automatic => Texts.Settings_Automatic,
        SettingsPage.Demo => Texts.Settings_Demo,
        SettingsPage.Help => Texts.Settings_Help,
        SettingsPage.About => Texts.Settings_About,
        SettingsPage.Appearance => Texts.Settings_Appearance,
        SettingsPage.Language => Texts.Settings_Language,
        _ => throw new ArgumentOutOfRangeException(nameof(page))
    };
    private static TextBox ReadOnlyText(string text, string name) => new() { Name = name, Text = text, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, BorderThickness = new(0), Background = System.Windows.Media.Brushes.Transparent, Padding = new(0), AcceptsReturn = true };
    private bool HasAddressChange => CameraSettings.TryNormalizeIp(Address.Text, out string ip) && (!CameraSettings.TryNormalizeIp(CameraIp, out var current) || current != ip);
    private void UpdateAddress()
    {
        bool onCamera = (Navigation.SelectedItem as ListBoxItem)?.Tag is SettingsPage.Camera;
        bool edited = Address.Text.Trim() != CameraIp;
        SaveAddress.IsEnabled = HasAddressChange; SaveAddress.IsDefault = onCamera && HasAddressChange;
        DemoAction.IsEnabled = !edited;
        pages[SettingsPage.Presets].IsEnabled = !edited;
        pages[SettingsPage.Automatic].IsEnabled = !edited && session != null;
        close.Content = edited && onCamera ? Texts.Common_Cancel : Texts.Common_Close;
    }
    internal void SaveCamera()
    {
        if ((Navigation.SelectedItem as ListBoxItem)?.Tag is not SettingsPage.Camera || !HasAddressChange) return;
        CameraSettings.TryNormalizeIp(Address.Text, out var ip);
        try { CameraSettings.Save(ip, settingsPath); CameraIp = ip; DialogResult = true; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { feedback.Text = Texts.Format(Texts.Camera_SaveFailed, e.Message); feedback.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); }
    }
    private void RefreshPresets()
    {
        presets.Children.Clear();
        if (session == null) { presets.Children.Add(Ui.Text(Texts.Camera_SetupFirst)); return; }
        foreach (int number in CameraSession.Buttons)
        {
            var row = new Grid { Margin = new(0, 0, 0, 6) }; row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = Texts.Format(Texts.Preset_Row, PresetSettings.Caption(number, session.Names[number]), session.Numbers[number]), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 10, 0) });
            var edit = Ui.Button(Texts.Common_Edit, () =>
            {
                var editor = new PresetEditorDialog(session, number) { Owner = this };
                editor.CloseStopFailed += error => PresetStopFailed?.Invoke(error);
                editor.ShowDialog(); RefreshPresets(); RefreshAutomatic();
            }, "editPreset" + number);
            Grid.SetColumn(edit, 1); row.Children.Add(edit); presets.Children.Add(row);
        }
    }
    private sealed record PresetChoice(int? Number, string Caption) { public override string ToString() => Caption; }
    private sealed record AppearanceChoiceItem(Appearance Value, string Caption) { public override string ToString() => Caption; }
    private sealed record LanguageChoiceItem(AppLanguage Value, string Caption) { public override string ToString() => Caption; }
    private static int? Selection(ComboBox combo) => (combo.SelectedItem as PresetChoice)?.Number;
    internal void RefreshAutomatic()
    {
        refreshing = true;
        try
        {
            foreach (var (combo, saved) in new[] { (StartPreset, savedAutomatic.StartPreset), (ExitPreset, savedAutomatic.ExitPreset) })
            {
                int? selected = combo.Items.Count == 0 ? saved : Selection(combo);
                combo.Items.Clear(); combo.Items.Add(new PresetChoice(null, Texts.Automatic_None));
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
        try { session.SaveAutomatic(choices); savedAutomatic = choices; automaticFeedback.Text = Texts.Automatic_Saved; automaticFeedback.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush"); UpdateAutomatic(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { automaticFeedback.Text = Texts.Format(Texts.Automatic_SaveFailed, e.Message); automaticFeedback.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush"); }
    }
}
