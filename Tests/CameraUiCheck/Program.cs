using CamControl;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Globalization;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    [STAThread]
    private static int Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/CamControl;component/Theme.xaml") });
        int exit = 0;
        app.Dispatcher.BeginInvoke(async () =>
        {
            try
            {
                ThemeChecks(); await ControllerChecks();
                foreach (string language in new[] { "de-DE", "en-US", "es-ES" })
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                    await WindowChecks();
                }
                Console.WriteLine($"Passed {checks} WPF/controller checks (German, English and Spanish).");
            }
            catch (Exception e) { Console.Error.WriteLine(e); exit = 1; }
            finally { app.Shutdown(); }
        });
        app.Run();
        return exit;
    }
    private static void ThemeChecks()
    {
        string[] keys = ["Canvas", "Surface", "Ink", "Muted", "Line", "Accent", "Hover", "Pressed", "Selection", "Error", "Success"];
        foreach (bool dark in new[] { false, true, false })
        {
            Theme.Apply(dark);
            var brushes = keys.ToDictionary(key => key, key => Application.Current.Resources[key + "Brush"]);
            Theme.Apply(dark);
            Check(keys.All(key => ReferenceEquals(brushes[key], Application.Current.Resources[key + "Brush"])),
                "Unchanged theme replaced shared brushes and invalidated the entire window");
        }
    }
    private static async Task ControllerChecks()
    {
        using var handler = new RecordingHandler(); using var client = new HttpClient(handler);
        using var service = new CamControlService(client, "192.168.188.24");
        using var controller = new CameraController(service);
        await controller.StartDirectionAsync(Direction.Left); await controller.StartDirectionAsync(Direction.Right);
        Check(handler.Commands.SequenceEqual(["left&10&10"]), "Held direction repeated or changed while held");
        await controller.StartZoomAsync(Zoom.ZoomIn);
        Check(handler.Commands.Last() == "zoomin&5", "Independent zoom did not start");
        await controller.StopDirectionAsync();
        Check(handler.Commands.Last() == "ptzstop&10&10" && controller.IsMoving, "Direction release failed or stopped independent zoom");
        await controller.StopZoomAsync(); Check(!controller.IsMoving && handler.Commands.Last() == "zoomstop&5", "Zoom release failed");

        handler.Commands.Clear(); await controller.RecallAsync(20);
        Check(handler.Commands.Count == 4 && handler.Commands.Take(3).ToHashSet().SetEquals(["ptzstop&10&10", "zoomstop&5", "focusstop&5"]) && handler.Commands.Last() == "poscall&20", "Preset did not stop before custom recall");
        handler.Commands.Clear(); await controller.SaveAsync(0);
        Check(handler.Commands.Last() == "posset&0" && handler.Commands.Count == 4, "Preset save changed protocol or skipped stops");

        await controller.StartDirectionAsync(Direction.Right); await controller.SetActiveAsync(false);
        Check(!controller.IsMoving && !controller.ControlsEnabled && handler.Commands.Last() == "focusstop&5", "Focus loss did not stop all channels");
        int count = handler.Commands.Count; await controller.StartZoomAsync(Zoom.ZoomIn); Check(handler.Commands.Count == count, "Inactive window sent movement");
        await controller.SetActiveAsync(true); await controller.SuspendAsync(); count = handler.Commands.Count;
        await controller.RecallAsync(1); Check(handler.Commands.Count == count, "Settings suspension allowed a preset"); controller.Resume();

        // Release before a delayed start completes must cancel it and send stop.
        var pendingStart = new TaskCompletionSource<HttpResponseMessage>();
        handler.Respond = (command, token) => command == "up&10&10" ? pendingStart.Task.WaitAsync(token) : Ok();
        Task start = controller.StartDirectionAsync(Direction.Up);
        Check(!start.IsCompleted, "Delayed start test did not delay");
        await controller.StopDirectionAsync(); await start;
        Check(handler.Commands.Last() == "ptzstop&10&10" && controller.Error == "", "Early release failed or deliberate cancellation reported error");

        // Cancel a preset while preparatory stops are pending; no recall may leak.
        var pendingStop = new TaskCompletionSource<HttpResponseMessage>();
        handler.Respond = (command, _) => command == "ptzstop&10&10" ? pendingStop.Task : Ok();
        handler.Commands.Clear(); Task<bool> preset = controller.RecallAsync(3);
        Check(!controller.ControlsEnabled, "Preset left controls active");
        Task deactivate = controller.SetActiveAsync(false);
        await controller.SetActiveAsync(true);
        Check(!controller.ControlsEnabled, "Reactivation bypassed a pending stop");
        pendingStop.SetResult(new(HttpStatusCode.OK)); await deactivate; await preset;
        Check(!handler.Commands.Any(c => c.StartsWith("poscall")) && controller.ControlsEnabled, "Cancelled preparation leaked recall or left controls locked");

        // Cancellation of an in-flight recall must also block overlapping presets.
        var pendingPreset = new TaskCompletionSource<HttpResponseMessage>();
        handler.Respond = (command, token) => command.StartsWith("poscall") ? pendingPreset.Task.WaitAsync(token) : Ok();
        handler.Commands.Clear(); preset = controller.RecallAsync(1); await controller.RecallAsync(2);
        Check(handler.Commands.Count(c => c.StartsWith("poscall")) == 1, "Overlapping preset was sent");
        await controller.SuspendAsync(); await preset; controller.Resume();
        Check(controller.ControlsEnabled && controller.Error == "", "Cancelled recall did not recover");

        // Stop failure must attempt every stop and prevent the preset.
        handler.Respond = (command, _) => command.StartsWith("ptzstop") ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)) : Ok();
        handler.Commands.Clear(); await controller.RecallAsync(1);
        Check(handler.Commands.Count == 3 && controller.Error.Contains("503") && controller.ControlsEnabled, "Failed stop skipped channels, moved anyway, or failed to recover");
        using (var closing = new CameraController(service))
        {
            handler.Commands.Clear(); await closing.CloseAsync(9);
            Check(handler.Commands.Count == 3 && !handler.Commands.Any(c => c.StartsWith("poscall")), "Failed close stop allowed exit preset");
        }

        // An old successful response must not hide a newer error.
        var oldSuccess = new TaskCompletionSource<HttpResponseMessage>();
        handler.Respond = (command, token) => command.StartsWith("left") ? oldSuccess.Task.WaitAsync(token) : command.StartsWith("zoomin") ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)) : Ok();
        start = controller.StartDirectionAsync(Direction.Left); await controller.StartZoomAsync(Zoom.ZoomIn);
        Check(controller.Error.Contains("400"), "New HTTP error missing"); oldSuccess.SetResult(new(HttpStatusCode.OK)); await start;
        Check(controller.Error.Contains("400"), "Old success erased newer error");
        handler.Respond = (_, _) => Ok(); await controller.StopAsync(); await controller.RecallAsync(6);
        Check(controller.Error == "" && controller.ControlsEnabled, "Recovery failed after network error");

        using (var closing = new CameraController(service))
        {
            handler.Commands.Clear(); await closing.CloseAsync(9);
            Check(handler.Commands.Count == 4 && handler.Commands.Last() == "poscall&9", "Successful close did not call exit after stops");
        }
        // Separate channels: a slow zoom must not delay a direction stop.
        var slowZoom = new TaskCompletionSource<HttpResponseMessage>();
        handler.Respond = (command, token) => command == "zoomin&5" ? slowZoom.Task.WaitAsync(token) : Ok();
        Task zoom = controller.StartZoomAsync(Zoom.ZoomIn); await controller.StartDirectionAsync(Direction.Down); await controller.StopDirectionAsync();
        Check(!zoom.IsCompleted && handler.Commands.Last() == "ptzstop&10&10", "Slow zoom delayed direction stop");
        await controller.StopZoomAsync(); await zoom;

        // Concurrent lifecycle stops share one transport sequence.
        pendingStop = new(); handler.Commands.Clear(); handler.Respond = (command, _) => command.StartsWith("ptzstop") ? pendingStop.Task : Ok();
        Task<bool> a = controller.StopAsync(), b = controller.StopAsync(); Check(ReferenceEquals(a, b) && handler.Commands.Count == 3, "Lifecycle stops were duplicated"); pendingStop.SetResult(new(HttpStatusCode.OK)); await a;
    }

    private static async Task WindowChecks()
    {
        using var session = new CameraSession(true);
        session.SaveMapping(1, new string('W', 32), 20);
        Check(session.Names[1].Length == 32 && session.Numbers[1] == 20, "Session mapping missing");
        var window = new MainWindow(session); ShowOffscreen(window); await Layout();
        Check(window.Width <= 640 && window.Height < 700 && window.DemoPreview.IsVisible, "Compact main/demo layout missing");
        Check(Descendants(window).OfType<Button>().Count(b => b.Name.StartsWith("preset")) == 7, "Seven preset buttons missing");
        Check(Descendants(window).OfType<Button>().Count(b => b.Name.StartsWith("move")) == 4 && Descendants(window).OfType<Button>().Count(b => b.Name.StartsWith("Zoom")) == 2, "Direction or zoom controls missing");
        Check(window.SettingsButton.IsEnabled && window.ManualControls.ActualWidth >= 150, "Settings inaccessible or motion targets undersized");
        void CheckControlAlignment()
        {
            var lastPreset = Descendants(window).OfType<Button>().Single(b => b.Name == "preset9");
            double presetBottom = lastPreset.TranslatePoint(new Point(0, lastPreset.ActualHeight), window).Y;
            double panelBottom = window.ManualPanel.TranslatePoint(new Point(0, window.ManualPanel.ActualHeight), window).Y;
            Check(Math.Abs(presetBottom - panelBottom) <= 1, "Preset 9 and manual panel do not share a bottom edge");
            var bounds = window.SettingsButton.TransformToAncestor(window.Header).TransformBounds(VisualTreeHelper.GetDescendantBounds(window.SettingsButton));
            Check(bounds.Top >= 2 && bounds.Bottom <= window.Header.ActualHeight - 2, "Settings button drawing is clipped by header");
        }
        CheckControlAlignment();
        await session.Controller.SetActiveAsync(true);
        await window.Input.PressKeyAsync(Key.Right); Check(session.Demo.State.Moving, "WPF input did not start direction");
        await window.Input.PressKeyAsync(Key.Right); await window.Input.ReleaseKeyAsync(Key.Right); Check(!session.Demo.State.Moving, "WPF key release did not stop");
        await window.Input.PressKeyAsync(Key.Add); await window.Input.PressKeyAsync(Key.Left); Check(session.Demo.State.Zooming && session.Demo.State.Moving, "Keyboard channels not independent");
        await window.Input.ReleaseKeyAsync(Key.Left); Check(!session.Demo.State.Moving && session.Demo.State.Zooming, "PTZ release stopped wrong channel");
        await window.Input.ReleaseKeyAsync(Key.Add); Check(!session.Demo.State.Zooming, "Numpad zoom release failed");
        await session.Controller.SetActiveAsync(false); window.Input.Reset(); Check(!session.Demo.State.Moving, "Deactivation did not clear demo motion");
        await session.Controller.SetActiveAsync(true);
        var controls = Descendants(window).OfType<Button>().ToArray(); var left = controls.Single(b => b.Name == "moveLeft");
        var presetContents = controls.Where(b => b.Name.StartsWith("preset")).ToDictionary(b => b, b => b.Content);
        left.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent });
        Check(session.Demo.State.Moving && CameraInput.GetIsHeld(left), "Mouse press did not start or show held state");
        left.ReleaseMouseCapture(); await Layout(); Check(!session.Demo.State.Moving && !CameraInput.GetIsHeld(left), "Lost capture did not stop motion");
        var keySource = PresentationSource.FromVisual(window);
        window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, keySource!, Environment.TickCount, Key.D3) { RoutedEvent = Keyboard.PreviewKeyDownEvent }); await Layout();
        Check(session.Demo.State.Action == "Preset 3" && window.Status.Text.Contains(Texts.Format(Texts.Main_Requested, session.Names[3])), "Preset keyboard route did not recall or report request");
        Check(Descendants(window).OfType<Button>().Single(b => b.Name == "preset3").Tag as string == "Requested", "Requested preset highlight missing");
        Check(presetContents.All(pair => ReferenceEquals(pair.Key.Content, pair.Value)), "Motion or preset recall rebuilt preset content");
        var presetButton = controls.Single(b => b.Name == "preset6");
        presetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Layout();
        Check(session.Demo.State.Action == "Preset 6" && presetButton.Tag as string == "Requested", "Preset click did not recall or highlight position");
        Check(presetContents.All(pair => ReferenceEquals(pair.Key.Content, pair.Value)), "Preset click rebuilt preset content");

        var settings = new CameraSettingsDialog(session.Service.CameraIp, session); ShowOffscreen(settings); await Layout();
        Check(settings.Navigation.Items.Count == 8 && !settings.SaveAddress.IsEnabled, "Settings sections or save guard missing");
        settings.Address.Text = "invalid"; Check(!settings.SaveAddress.IsEnabled && !settings.DemoAction.IsEnabled, "Invalid IP allowed save or mode switch");
        settings.Address.Text = "192.168.188.25"; Check(settings.SaveAddress.IsEnabled && settings.SaveAddress.IsDefault, "Valid changed address not editable");
        settings.Navigation.SelectedIndex = 4; await Layout(); Check(!settings.SaveAddress.IsDefault, "Help retained default camera save");
        Check(Descendants(settings).OfType<TextBox>().Any(t => t.Name == "cameraHelp" && t.IsReadOnly && t.Text.Contains("HTTP-CGI")), "Help missing/read-write");
        settings.Navigation.SelectedIndex = 5; await Layout(); Check(Descendants(settings).OfType<TextBox>().Any(t => t.Name == "privacyInformation" && t.Text == AppInformation.PrivacyText && t.IsReadOnly), "Privacy text missing or editable");
        settings.Navigation.SelectedIndex = 0; settings.Address.Text = settings.CameraIp; Check(!settings.SaveAddress.IsEnabled, "Revert failed to disable save");
        settings.Navigation.SelectedIndex = 2;
        Check(settings.StartPreset.Items.Count == 8 && settings.ExitPreset.Items.Count == 8 && !settings.SaveAutomatic.IsEnabled, "Automatic choices missing/default changed");
        settings.StartPreset.SelectedIndex = 3; settings.ExitPreset.SelectedIndex = 7; Check(settings.SaveAutomatic.IsEnabled && session.Automatic.StartPreset == null, "Automatic changes applied before save");
        settings.StoreAutomatic(); Check(session.Automatic.StartPreset == 3 && session.Automatic.ExitPreset == 9 && !settings.SaveAutomatic.IsEnabled, "Automatic choices not saved");
        settings.RefreshAutomatic(); Check(!settings.SaveAutomatic.IsEnabled, "Refreshing names changed saved automation");
        settings.Navigation.SelectedIndex = 1; await Layout();
        Check(Descendants(settings).OfType<Button>().Count(b => b.Name.StartsWith("editPreset")) == 7, "Preset editors inaccessible");
        double demoHeight = window.Height;
        for (int i = 0; i < 2; i++)
        {
            var completion = new TaskCompletionSource();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            timer.Tick += (_, _) =>
            {
                var dialog = Application.Current.Windows.OfType<CameraSettingsDialog>().FirstOrDefault(w => w.Owner == window);
                if (dialog == null) return;
                timer.Stop(); Check(!session.Demo.State.Moving && !session.Demo.State.Zooming, "Opening settings did not stop demo"); dialog.Close(); completion.SetResult();
            };
            timer.Start(); window.SettingsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await completion.Task; await Layout();
            Check(window.Height == demoHeight && window.DemoPreview.IsVisible, "Repeated settings changed demo height or hid preview");
        }
        await session.Controller.SetActiveAsync(true);

        var editor = new PresetEditorDialog(session, 2); ShowOffscreen(editor); await Layout(); await editor.Controller.SetActiveAsync(true);
        editor.PresetName.Text = "Testposition"; editor.PresetNumber.Text = "20"; Check(!editor.Store.IsEnabled, "Duplicate number allowed preset save");
        editor.PresetNumber.Text = "90"; Check(!editor.SaveMapping.IsEnabled, "Reserved number allowed");
        editor.PresetNumber.Text = "21"; Check(editor.SaveMapping.IsEnabled && !editor.Recall.IsEnabled, "Unsaved mapping allowed recall");
        var before = session.Demo.State;
        Check(editor.SaveName() && session.Names[2] == "Testposition" && session.Numbers[2] == 21 && before == session.Demo.State, "Mapping save moved camera or failed");
        await editor.Controller.StartZoomAsync(Zoom.ZoomIn); session.Demo.Advance(TimeSpan.FromMilliseconds(500));
        await editor.RunPositionAsync(true); var saved = session.Demo.State;
        await editor.Controller.RecallAsync(4); await editor.RunPositionAsync(false);
        Check(session.Demo.State.X == saved.X && session.Demo.State.Zoom == saved.Zoom && !session.Demo.State.Moving && !session.Demo.State.Zooming, "Editor save/recall did not preserve position or stop");
        await editor.Input.PressKeyAsync(Key.Down); editor.Close(); await Layout();
        Check(!session.Demo.State.Moving && !editor.IsVisible, "One editor close did not stop and close");

        // Render the actual WPF windows for review, including enlarged text.
        string output = System.IO.Path.GetFullPath("artifacts/ui-preview/" + CultureInfo.CurrentUICulture.Name); System.IO.Directory.CreateDirectory(output);
        Screenshot(window, System.IO.Path.Combine(output, "main-demo.png"));
        settings.Navigation.SelectedIndex = 1; await Layout(); Screenshot(settings, System.IO.Path.Combine(output, "settings-presets.png"));
        window.DemoPreview.Visibility = Visibility.Collapsed; window.Height = 468; await Layout();
        Screenshot(window, System.IO.Path.Combine(output, "main-compact.png"));
        var scroll = Descendants(window).OfType<ScrollViewer>().First();
        Check(scroll.ScrollableHeight == 0 && scroll.ScrollableWidth == 0, $"Default compact window requires scrolling: {scroll.ScrollableWidth} x {scroll.ScrollableHeight}");
        // Exercise wrapping in all languages, independent of translation length.
        session.Controller.ReportStopFailure(Texts.Error_Connection + " " + Texts.Error_Timeout); await Layout();
        Check(window.Status.ActualHeight > 20, "Regression did not reproduce a wrapped camera error");
        Check(scroll.ScrollableHeight == 0 && scroll.ScrollableWidth == 0, $"Wrapped camera error introduced scrollbars: {scroll.ScrollableWidth} x {scroll.ScrollableHeight}");
        Theme.Apply(true); await Layout(); Screenshot(window, System.IO.Path.Combine(output, "main-dark-error.png"));
        await session.Controller.SetActiveAsync(true); await session.Controller.RecallAsync(3);
        window.FontSize = 21; window.Width = 580; await Layout();
        CheckControlAlignment();
        Check(scroll.ExtentHeight > 0 && window.SettingsButton.IsVisible && window.Status.IsVisible, "Large text hid settings/status");
        Screenshot(window, System.IO.Path.Combine(output, "main-large-text.png"));
        Check(Descendants(window.ManualControls).OfType<Button>().Where(b => b.Name.StartsWith("Zoom")).All(b => b.ActualHeight >= 58), "Large text shrank zoom controls");
        window.FontSize = 14; window.Width = 636; await Layout();
        Screenshot(window, System.IO.Path.Combine(output, "main-long-name.png"));
        string[] examples = ["Redner", "Redner & Leser", "Tisch", "Gesamte Bühne", "Redner & Bildschirm", "Demo stehend", "Standby"];
        for (int i = 0; i < CameraSession.Buttons.Length; i++) { int n = CameraSession.Buttons[i]; session.SaveMapping(n, examples[i], session.Numbers[n]); }
        window.Refresh(); await Layout(); Screenshot(window, System.IO.Path.Combine(output, "main-compact.png"));
        Check(presetContents.All(pair => ReferenceEquals(pair.Key.Content, pair.Value)), "Renaming presets replaced their content");
        for (int i = 0; i < CameraSession.Buttons.Length; i++)
        {
            var button = controls.Single(b => b.Name == "preset" + CameraSession.Buttons[i]);
            Check(Descendants(button).OfType<TextBlock>().Any(text => text.Text == examples[i]), "Reused preset label did not update after renaming");
        }
        Theme.Apply(true); await Layout(); Screenshot(window, System.IO.Path.Combine(output, "main-dark.png"));
        Check(((SolidColorBrush)window.Background).Color == ((SolidColorBrush)Application.Current.Resources["CanvasBrush"]).Color, "Window did not follow dark palette");
        var darkSettings = new CameraSettingsDialog(session.Service.CameraIp, session); ShowOffscreen(darkSettings); darkSettings.Navigation.SelectedIndex = 2; await Layout();
        darkSettings.StartPreset.IsDropDownOpen = true; await Layout(); Screenshot(darkSettings, System.IO.Path.Combine(output, "settings-dark.png")); darkSettings.StartPreset.IsDropDownOpen = false;
        Check(((SolidColorBrush)darkSettings.Address.Foreground).Color == ((SolidColorBrush)Application.Current.Resources["InkBrush"]).Color, "Input did not follow dark palette");
        darkSettings.Close(); Theme.Apply(false);
        var allSettings = new CameraSettingsDialog(session.Service.CameraIp, session); ShowOffscreen(allSettings);
        for (int i = 0; i < allSettings.Navigation.Items.Count; i++) { allSettings.Navigation.SelectedIndex = i; await Layout(); Screenshot(allSettings, System.IO.Path.Combine(output, $"settings-{i}.png")); }
        allSettings.Close();
        string appearancePath = System.IO.Path.Combine(output, "appearance-test.json");
        var previousAppearance = Theme.Preference;
        var appearanceDialog = new CameraSettingsDialog(session.Service.CameraIp, session, appearancePath: appearancePath); ShowOffscreen(appearanceDialog); appearanceDialog.Navigation.SelectedIndex = 6; await Layout();
        Check(!appearanceDialog.SaveAppearance.IsEnabled && appearanceDialog.AppearanceChoice.Items.Count == 3, "Appearance selection/default save state missing");
        appearanceDialog.AppearanceChoice.SelectedIndex = previousAppearance == Appearance.Dark ? 1 : 0;
        Check(appearanceDialog.SaveAppearance.IsEnabled, "Appearance change did not enable save");
        appearanceDialog.SaveAppearance.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Layout();
        Check(!appearanceDialog.SaveAppearance.IsEnabled && AppearanceSettings.Load(appearancePath) == Theme.Preference, "Appearance did not persist");
        Check(((SolidColorBrush)window.Background).Color == ((SolidColorBrush)Application.Current.Resources["CanvasBrush"]).Color, "Changing appearance did not update the main window");
        appearanceDialog.Close(); Theme.SetPreference(previousAppearance, appearancePath); System.IO.File.Delete(appearancePath);
        string languagePath = System.IO.Path.Combine(output, "language-test.json"); System.IO.File.Delete(languagePath);
        var languageDialog = new CameraSettingsDialog(session.Service.CameraIp, session, languagePath: languagePath);
        ShowOffscreen(languageDialog); languageDialog.Navigation.SelectedIndex = 7; await Layout();
        Check(languageDialog.LanguageChoice.Items.Count == 4 && languageDialog.LanguageChoice.SelectedIndex == 0 && !languageDialog.SaveLanguage.IsEnabled, "Language selection/default save state missing");
        Check(languageDialog.LanguageChoice.Items[3].ToString() == "Español", "Spanish choice is not discoverable by its native name");
        var uiCulture = CultureInfo.CurrentUICulture;
        var names = session.Names.ToDictionary(p => p.Key, p => p.Value);
        // Exercise all explicit preferences, including saving Spanish from English.
        int nextLanguageIndex = uiCulture.TwoLetterISOLanguageName switch { "de" => 2, "en" => 3, _ => 1 };
        var nextLanguage = nextLanguageIndex switch { 2 => AppLanguage.English, 3 => AppLanguage.Spanish, _ => AppLanguage.German };
        languageDialog.LanguageChoice.SelectedIndex = nextLanguageIndex;
        Check(languageDialog.SaveLanguage.IsEnabled && !System.IO.File.Exists(languagePath), "Language applied before saving");
        languageDialog.SaveLanguage.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Layout();
        Check(!languageDialog.SaveLanguage.IsEnabled && LanguageSettings.Load(languagePath) == nextLanguage, "Language preference was not saved");
        Check(CultureInfo.CurrentUICulture.Equals(uiCulture) && names.All(p => session.Names[p.Key] == p.Value), "Saving language changed the current UI or user preset names");
        Check(Descendants(languageDialog).OfType<TextBlock>().Any(t => t.Text == Texts.Language_Saved), "Restart guidance missing after language save");
        Screenshot(languageDialog, System.IO.Path.Combine(output, "settings-language-saved.png")); languageDialog.Close();
        var reopenedLanguage = new CameraSettingsDialog(session.Service.CameraIp, session, languagePath: languagePath);
        Check(reopenedLanguage.LanguageChoice.SelectedIndex == nextLanguageIndex, "Reopened settings lost pending language");
        reopenedLanguage.LanguageChoice.SelectedIndex = 0;
        reopenedLanguage.SaveLanguage.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(LanguageSettings.Load(languagePath) == AppLanguage.System, "Could not restore automatic language");
        reopenedLanguage.Close(); System.IO.File.Delete(languagePath);
        var failedLanguage = new CameraSettingsDialog(session.Service.CameraIp, session, languagePath: output);
        failedLanguage.LanguageChoice.SelectedIndex = 1; failedLanguage.SaveLanguage.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(failedLanguage.SaveLanguage.IsEnabled && LanguageSettings.Load(output) == AppLanguage.System, "Failed language save applied preference or prevented retry");
        failedLanguage.Close();
        var expected = uiCulture.TwoLetterISOLanguageName switch
        {
            "de" => (Settings: "Einstellungen", Privacy: "Datenschutz für CamControl"),
            "es" => (Settings: "Ajustes", Privacy: "Privacidad de CamControl"),
            _ => (Settings: "Settings", Privacy: "Privacy for CamControl")
        };
        Check(window.SettingsButton.Content as string == "⚙ " + expected.Settings, "XAML did not use the selected language");
        Check(settings.Title == expected.Settings, "C# dialog did not use the selected language");
        Check(AppInformation.PrivacyText.StartsWith(expected.Privacy), "Privacy text used the wrong language");
        Check(string.Equals(window.Language.IetfLanguageTag, uiCulture.IetfLanguageTag, StringComparison.OrdinalIgnoreCase), "WPF language did not follow UI culture");
        var editorPreview = new PresetEditorDialog(session, 1); ShowOffscreen(editorPreview); await Layout(); Screenshot(editorPreview, System.IO.Path.Combine(output, "preset-editor.png"));
        await editorPreview.Controller.SetActiveAsync(true); await editorPreview.Controller.StartDirectionAsync(Direction.Left);
        typeof(Window).GetMethod("OnDeactivated", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(editorPreview, [EventArgs.Empty]); await Layout();
        Check(!session.Demo.State.Moving && !editorPreview.Controller.ControlsEnabled, "Editor deactivation wiring did not stop input"); editorPreview.Close(); await Layout();
        settings.Close(); window.Close(); await Layout();
        Check(!window.IsVisible && !session.Demo.State.Moving && !session.Demo.State.Zooming, "Main close left demo moving");

        // Modal IP save/cancel checks write only to an isolated test file.
        string path = System.IO.Path.Combine(output, "settings-test.json"); System.IO.File.Delete(path);
        var saveDialog = new CameraSettingsDialog("192.168.188.24", settingsPath: path);
        saveDialog.ContentRendered += (_, _) => { saveDialog.Address.Text = "192.168.188.025"; saveDialog.SaveCamera(); };
        Check(saveDialog.ShowDialog() == true && CameraSettings.Load(path) == "192.168.188.25", "Modal save did not persist normalized IP");
        var cancelDialog = new CameraSettingsDialog("192.168.188.25", settingsPath: path);
        cancelDialog.ContentRendered += (_, _) => { cancelDialog.Address.Text = "192.168.188.26"; cancelDialog.Close(); };
        cancelDialog.ShowDialog(); Check(CameraSettings.Load(path) == "192.168.188.25", "Cancel persisted edits"); System.IO.File.Delete(path);
        var failedDialog = new CameraSettingsDialog("192.168.188.24", settingsPath: output);
        failedDialog.ContentRendered += (_, _) => { failedDialog.Address.Text = "192.168.188.25"; failedDialog.SaveCamera(); Check(failedDialog.CameraIp == "192.168.188.24" && failedDialog.IsVisible, "Failed write applied address or closed dialog"); failedDialog.Close(); }; failedDialog.ShowDialog();
        var demoDialog = new CameraSettingsDialog("192.168.188.24", settingsPath: path);
        demoDialog.ContentRendered += (_, _) => demoDialog.DemoAction.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(demoDialog.ShowDialog() == true && demoDialog.RequestedDemoMode == true && !System.IO.File.Exists(path), "Demo action saved camera configuration");
        using var startupSession = new CameraSession(true);
        startupSession.SaveMapping(2, "Startposition", 50); await startupSession.Service.SavePreset(50);
        startupSession.SaveAutomatic(new() { StartPreset = 2, ExitPreset = 9 });
        var startupWindow = new MainWindow(startupSession); ShowOffscreen(startupWindow); await Layout();
        Check(startupSession.Demo.State.Action == Texts.Format(Texts.Demo_CustomPreset, 50), "Startup ignored custom preset mapping");
        startupWindow.Close(); await Layout(); Check(startupSession.Demo.State.Action == "Preset 9", "Exit preset not applied by window lifecycle");
        var welcome = new StartupChoiceDialog(); welcome.ContentRendered += (_, _) => Descendants(welcome).OfType<Button>().Single(b => (b.Content as TextBlock)?.Text == Texts.Welcome_Demo).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(welcome.ShowDialog() == true && welcome.UseDemo, "First-run demo choice failed");
    }
    private static void ShowOffscreen(Window window) { window.ShowInTaskbar = false; window.ShowActivated = false; window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = -10000; window.Top = -10000; window.Show(); }
    private static async Task Layout() { await Dispatcher.Yield(DispatcherPriority.ApplicationIdle); }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); yield return child; foreach (var descendant in Descendants(child)) yield return descendant; }
    }
    private static void Screenshot(Window window, string path)
    {
        var element = (FrameworkElement)window.Content; element.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual(); using (var dc = background.RenderOpen()) dc.DrawRectangle(window.Background, null, new Rect(0, 0, element.ActualWidth, element.ActualHeight)); bitmap.Render(background); bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = System.IO.File.Create(path); encoder.Save(file);
    }
    private static Task<HttpResponseMessage> Ok() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    private sealed class RecordingHandler : HttpMessageHandler
    {
        internal readonly List<string> Commands = new();
        internal Func<string, CancellationToken, Task<HttpResponseMessage>> Respond = (_, _) => Ok();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { string command = request.RequestUri!.Query.Replace("?ptzcmd&", ""); Commands.Add(command); return Respond(command, token); }
    }
}
