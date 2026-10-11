namespace CamControl
{
    internal sealed class CameraSettingsDialog : Form
    {
        private readonly TextBox address;
        private readonly Button edit, save, cancel;
        private readonly Label hint;
        private readonly TabControl pages;
        private readonly Button demoAction;
        private readonly string? settingsPath;
        private bool editing;
        private TabPage? presetsPage;
        private readonly FlowLayoutPanel presetLayout;
        private readonly Dictionary<int, string>? presetNames;
        private ComboBox[] automaticSelectors = Array.Empty<ComboBox>();
        private bool refreshingAutomaticChoices;
        public string CameraIp { get; private set; }
        public bool? RequestedDemoMode { get; private set; }
        internal event Action<string>? PresetStopFailed;

        public CameraSettingsDialog(string cameraIp, string? settingsPath = null, bool demoModeActive = false,
            CamControlService? service = null, Dictionary<int, string>? presetNames = null,
            Action<int, string>? savePresetName = null, DemoCamera? demo = null)
        {
            CameraIp = cameraIp;
            this.settingsPath = settingsPath;
            this.presetNames = presetNames;
            Text = "Einstellungen";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7F, 15F);
            ClientSize = new Size(600, 480);
            Padding = new Padding(16);

            address = new TextBox
            {
                Name = "cameraAddress", Text = cameraIp, Dock = DockStyle.Top,
                ReadOnly = true, TabStop = false, AccessibleName = "IP-Adresse der Kamera"
            };
            hint = new Label
            {
                AutoSize = true, MaximumSize = new Size(460, 0), Dock = DockStyle.Top,
                Text = "Die Konfiguration ist vor Änderungen geschützt. Zum Ändern „Konfiguration bearbeiten“ wählen.",
                Margin = new Padding(0, 12, 0, 0)
            };
            pages = new TabControl { Name = "settingsPages", Dock = DockStyle.Fill, Multiline = true };
            pages.TabPages.Add(CreateCameraPage());
            pages.TabPages.Add(CreateHelpPage());
            var demoPage = new TabPage("Demo-Modus") { Padding = new Padding(16) };
            var demoLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            demoLayout.Controls.Add(new Label
            {
                Text = demoModeActive
                    ? "Demo-Modus aktiv. Es werden keine Befehle an eine echte Kamera gesendet. Zum Wechseln die Kamera-Adresse prüfen und speichern."
                    : "Ohne Kamera ausprobieren: Eine schematische Bühne zeigt Bewegung, Zoom und Presets. Die gespeicherte Kamera-Adresse bleibt erhalten.",
                AutoSize = true, MaximumSize = new Size(490, 0)
            });
            demoAction = new Button
            {
                Name = "demoAction", AutoSize = true, Margin = new Padding(0, 16, 0, 0),
                Text = demoModeActive ? "Kamera einrichten und Demo beenden" : "Demo starten"
            };
            demoAction.Click += (_, _) =>
            {
                if (demoModeActive)
                {
                    RequestedDemoMode = false;
                    Edit_Click(null, EventArgs.Empty);
                }
                else
                {
                    RequestedDemoMode = true;
                    DialogResult = DialogResult.OK;
                }
            };
            demoLayout.Controls.Add(demoAction);
            demoPage.Controls.Add(demoLayout);
            pages.TabPages.Add(demoPage);
            presetsPage = new TabPage("Buttons & Presets") { Padding = new Padding(12) };
            presetLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true
            };
            presetLayout.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(500, 0),
                Text = service == null ? "Bitte zuerst die Kamera einrichten oder den Demo-Modus starten."
                    : "„Bearbeiten …“ öffnet die Steuerung und das gezielte Speichern für jeden Button."
            });
            if (service != null && presetNames != null && savePresetName != null)
                foreach (int number in new[] { 1, 2, 3, 4, 5, 6, 9 })
                {
                    var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
                    var label = new Label { Text = $"{number}: {presetNames[number]}", Width = 330, Height = 30, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
                    var button = new Button { Text = "Bearbeiten …", AutoSize = true, AccessibleName = $"Preset {number} bearbeiten" };
                    button.Click += (_, _) =>
                    {
                        using var editor = new PresetEditorDialog(service, (Position)number, presetNames[number],
                            value => { savePresetName(number, value); label.Text = $"{number}: {presetNames[number]}"; RefreshAutomaticChoices(); }, demo);
                        editor.CloseStopFailed += error => PresetStopFailed?.Invoke(error);
                        editor.ShowDialog(this);
                    };
                    row.Controls.AddRange(new Control[] { label, button });
                    presetLayout.Controls.Add(row);
                }
            presetsPage.Controls.Add(presetLayout);
            pages.TabPages.Add(presetsPage);
            pages.TabPages.Add(CreateInfoPage());

            edit = new Button { Name = "editConfiguration", Text = "Konfiguration bearbeiten", AutoSize = true, TabIndex = 0 };
            save = new Button { Name = "saveConfiguration", Text = "Speichern", AutoSize = true, Enabled = false, TabIndex = 1 };
            cancel = new Button { Text = "Schließen", AutoSize = true, DialogResult = DialogResult.Cancel, TabIndex = 2 };
            edit.Click += Edit_Click;
            save.Click += Save_Click;
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Margin = new Padding(0, 12, 0, 0)
            };
            actions.Controls.AddRange(new Control[] { cancel, save, edit });
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(pages, 0, 0);
            layout.Controls.Add(actions, 0, 1);
            Controls.Add(layout);
            CancelButton = cancel;
            // Enter cannot unlock editing or save while viewing the configuration.
            AcceptButton = null;
        }

        internal void ConfigureAutomaticPositions(AutomaticPositions current, Action<AutomaticPositions> saveChoices)
        {
            var group = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 18, 0, 0) };
            group.Controls.Add(new Label { AutoSize = true, Text = "Automatische Positionen (optional)" });
            var start = new ComboBox { Name = "startPreset", DropDownStyle = ComboBoxStyle.DropDownList, Width = 440 };
            var exit = new ComboBox { Name = "exitPreset", DropDownStyle = ComboBoxStyle.DropDownList, Width = 440 };
            group.Controls.Add(new Label { AutoSize = true, Text = "Beim Start automatisch anfahren:" });
            group.Controls.Add(start);
            group.Controls.Add(new Label { AutoSize = true, Text = "Beim Beenden automatisch anfahren:" });
            group.Controls.Add(exit);
            automaticSelectors = new[] { start, exit };
            RefreshAutomaticChoices();
            start.SelectedItem = start.Items.Cast<PresetChoice>().Single(item => item.Number == current.StartPreset);
            exit.SelectedItem = exit.Items.Cast<PresetChoice>().Single(item => item.Number == current.ExitPreset);
            var saveAutomatic = new Button { Name = "saveAutomaticPositions", Text = "Automatik speichern", AutoSize = true, Enabled = false };
            var feedback = new Label { AutoSize = true, MaximumSize = new Size(470, 0) };
            int? savedStart = current.StartPreset, savedExit = current.ExitPreset;
            bool HasChanges() => (start.SelectedItem as PresetChoice)?.Number != savedStart ||
                (exit.SelectedItem as PresetChoice)?.Number != savedExit;
            void SelectionChanged(object? sender, EventArgs e)
            {
                if (refreshingAutomaticChoices) return;
                saveAutomatic.Enabled = HasChanges();
                feedback.ForeColor = SystemColors.ControlText;
                feedback.Text = saveAutomatic.Enabled ? "Auswahl geändert. Erst „Automatik speichern“ übernimmt sie." : "";
            }
            start.SelectedIndexChanged += SelectionChanged;
            exit.SelectedIndexChanged += SelectionChanged;
            saveAutomatic.Click += (_, _) =>
            {
                if (!HasChanges()) return;
                try
                {
                    saveChoices(new() { StartPreset = ((PresetChoice)start.SelectedItem!).Number, ExitPreset = ((PresetChoice)exit.SelectedItem!).Number });
                    savedStart = ((PresetChoice)start.SelectedItem!).Number;
                    savedExit = ((PresetChoice)exit.SelectedItem!).Number;
                    saveAutomatic.Enabled = false;
                    feedback.ForeColor = Color.DarkGreen;
                    feedback.Text = "Auswahl gespeichert. Ungespeicherte Änderungen werden beim Schließen verworfen.";
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                { feedback.ForeColor = Color.DarkRed; feedback.Text = "Auswahl konnte nicht gespeichert werden: " + e.Message; }
            };
            group.Controls.Add(saveAutomatic);
            group.Controls.Add(feedback);
            presetLayout.Controls.Add(group);
        }

        private sealed record PresetChoice(int? Number, string Caption)
        {
            public override string ToString() => Caption;
        }

        private void RefreshAutomaticChoices()
        {
            refreshingAutomaticChoices = true;
            try
            {
                foreach (var selector in automaticSelectors)
                {
                    int? selected = (selector.SelectedItem as PresetChoice)?.Number;
                    selector.Items.Clear();
                    selector.Items.Add(new PresetChoice(null, "Keine Position"));
                    foreach (int number in new[] { 1, 2, 3, 4, 5, 6, 9 })
                        selector.Items.Add(new PresetChoice(number, $"{presetNames![number]} (Preset {number})"));
                    selector.SelectedItem = selector.Items.Cast<PresetChoice>().Single(item => item.Number == selected);
                }
            }
            finally { refreshingAutomaticChoices = false; }
        }

        private TabPage CreateCameraPage()
        {
            var page = new TabPage("Kamera") { Padding = new Padding(16) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int row = 0; row < 3; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text = "IP-Adresse der Kamera:", AutoSize = true }, 0, 0);
            layout.Controls.Add(address, 0, 1);
            layout.Controls.Add(hint, 0, 2);
            page.Controls.Add(layout);
            return page;
        }

        private void Edit_Click(object? sender, EventArgs e)
        {
            pages.SelectedIndex = 0;
            editing = true;
            if (presetsPage != null) presetsPage.Enabled = false;
            demoAction.Enabled = false;
            address.ReadOnly = false;
            address.TabStop = true;
            edit.Enabled = false;
            save.Enabled = true;
            cancel.Text = "Abbrechen";
            hint.Text = "Änderungen gelten erst nach „Speichern“. „Abbrechen“ verwirft die Änderungen.";
            AcceptButton = save;
            address.Focus();
            address.SelectAll();
        }

        private static TabPage CreateHelpPage()
        {
            var page = new TabPage("Hilfe & Voraussetzungen") { Padding = new Padding(8) };
            var help = new RichTextBox
            {
                Name = "cameraHelp", Text = CameraHelp.Text,
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window, DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.Vertical, WordWrap = true,
                AccessibleName = "Hilfe und Voraussetzungen", TabStop = true
            };
            page.Controls.Add(help);
            return page;
        }

        private static TabPage CreateInfoPage()
        {
            var page = new TabPage("Info & Datenschutz") { Padding = new Padding(8) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label
            {
                Name = "appInformation", AutoSize = true, MaximumSize = new Size(510, 0),
                Text = $"CamControl · Version {AppInformation.Version}\nSteuerung kompatibler PTZ-Kameras über HTTP\nHerausgeber: {AppInformation.Publisher}",
                Margin = new Padding(3, 3, 3, 12)
            }, 0, 0);
            var link = new LinkLabel { Name = "projectSupport", Text = "Projekt & Support auf GitHub", AutoSize = true, Margin = new Padding(3, 0, 3, 8) };
            var feedback = new Label { AutoSize = true, MaximumSize = new Size(510, 0), ForeColor = Color.DarkRed };
            link.LinkClicked += (_, _) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppInformation.ProjectUrl) { UseShellExecute = true });
                    feedback.Text = "";
                }
                catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
                { feedback.Text = "Der Browser konnte nicht geöffnet werden: " + e.Message; }
            };
            layout.Controls.Add(link, 0, 1);
            layout.Controls.Add(feedback, 0, 2);
            layout.Controls.Add(new RichTextBox
            {
                Name = "privacyInformation", Text = AppInformation.PrivacyText,
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window, DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.Vertical, WordWrap = true,
                AccessibleName = "Datenschutz für CamControl"
            }, 0, 3);
            page.Controls.Add(layout);
            return page;
        }

        private void Save_Click(object? sender, EventArgs e)
        {
            if (!editing)
                return;
            if (!CameraSettings.TryNormalizeIp(address.Text, out var ip))
            {
                MessageBox.Show(this, "Bitte eine gültige IPv4-Adresse eingeben, zum Beispiel 192.168.188.24.",
                    "Ungültige Adresse", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                address.Focus();
                address.SelectAll();
                return;
            }
            try
            {
                CameraSettings.Save(ip, settingsPath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                MessageBox.Show(this, "Die Einstellung konnte nicht gespeichert werden.\n" + exception.Message,
                    "Speichern fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            CameraIp = ip;
            DialogResult = DialogResult.OK;
        }
    }
}
