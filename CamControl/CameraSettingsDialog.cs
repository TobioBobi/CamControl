namespace CamControl
{
    internal sealed class CameraSettingsDialog : Form
    {
        private readonly TextBox address;
        private readonly Button save, cancel;
        private readonly TabControl pages;
        private readonly Button demoAction;
        private readonly string? settingsPath;
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
                TabStop = true, AccessibleName = "IP-Adresse der Kamera"
            };
            save = new Button { Name = "saveConfiguration", Text = "Speichern", AutoSize = true, Enabled = false, TabIndex = 1 };
            save.Click += Save_Click;
            pages = new TabControl { Name = "settingsPages", Dock = DockStyle.Fill, Multiline = true };
            pages.TabPages.Add(CreateCameraPage());
            pages.TabPages.Add(CreateHelpPage());
            var demoPage = new TabPage("Demo-Modus") { Padding = new Padding(16) };
            var demoLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            demoLayout.Controls.Add(new Label
            {
                Text = demoModeActive
                    ? "Demo aktiv · Keine Netzwerkbefehle."
                    : "Bewegung, Zoom und Presets ohne Kamera testen. Deine Kameraeinstellungen bleiben erhalten.",
                AutoSize = true, MaximumSize = new Size(490, 0)
            });
            demoAction = new Button
            {
                Name = "demoAction", AutoSize = true, Margin = new Padding(0, 16, 0, 0),
                Text = demoModeActive ? "Zur Kamera wechseln" : "Demo starten"
            };
            demoAction.Click += (_, _) =>
            {
                if (demoModeActive)
                {
                    RequestedDemoMode = false;
                    DialogResult = DialogResult.OK;
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
                Text = service == null ? "Zuerst Kamera einrichten oder Demo starten."
                    : "Buttonnamen und Kamerapositionen bearbeiten.",
            });
            if (service != null && presetNames != null && savePresetName != null)
                foreach (int number in new[] { 1, 2, 3, 4, 5, 6, 9 })
                {
                    var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
                    var label = new Label { Text = PresetSettings.Caption(number, presetNames[number]), Width = 330, Height = 30, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
                    var button = new Button { Text = "Bearbeiten …", AutoSize = true, AccessibleName = $"Preset {number} bearbeiten" };
                    button.Click += (_, _) =>
                    {
                        using var editor = new PresetEditorDialog(service, (Position)number, presetNames[number],
                            value => { savePresetName(number, value); label.Text = PresetSettings.Caption(number, presetNames[number]); RefreshAutomaticChoices(); }, demo);
                        editor.CloseStopFailed += error => PresetStopFailed?.Invoke(error);
                        editor.ShowDialog(this);
                    };
                    row.Controls.AddRange(new Control[] { label, button });
                    presetLayout.Controls.Add(row);
                }
            presetsPage.Controls.Add(presetLayout);
            pages.TabPages.Add(presetsPage);
            pages.TabPages.Add(CreateInfoPage());

            cancel = new Button { Text = "Schließen", AutoSize = true, DialogResult = DialogResult.Cancel, TabIndex = 2 };
            pages.SelectedIndexChanged += (_, _) => UpdateAddressActions();
            address.TextChanged += (_, _) => UpdateAddressActions();
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Margin = new Padding(0, 12, 0, 0)
            };
            actions.Controls.Add(cancel);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(pages, 0, 0);
            layout.Controls.Add(actions, 0, 1);
            Controls.Add(layout);
            CancelButton = cancel;
            // Enter only saves a changed, valid address on the camera page.
            AcceptButton = null;
            WindowLayout.FitToScreen(this);
        }

        internal void ConfigureAutomaticPositions(AutomaticPositions current, Action<AutomaticPositions> saveChoices)
        {
            var group = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 18, 0, 0) };
            group.Controls.Add(new Label { AutoSize = true, Text = "Automatische Positionen (optional)" });
            var start = new ComboBox { Name = "startPreset", DropDownStyle = ComboBoxStyle.DropDownList, Width = 440 };
            var exit = new ComboBox { Name = "exitPreset", DropDownStyle = ComboBoxStyle.DropDownList, Width = 440 };
            group.Controls.Add(new Label { AutoSize = true, Text = "Beim Start:" });
            group.Controls.Add(start);
            group.Controls.Add(new Label { AutoSize = true, Text = "Beim Beenden:" });
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
                feedback.Text = saveAutomatic.Enabled ? "Ungespeicherte Änderungen." : "";
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
                    feedback.Text = "Automatik gespeichert.";
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
                        selector.Items.Add(new PresetChoice(number, PresetSettings.Caption(number, presetNames![number])));
                    selector.SelectedItem = selector.Items.Cast<PresetChoice>().Single(item => item.Number == selected);
                }
            }
            finally { refreshingAutomaticChoices = false; }
        }

        private TabPage CreateCameraPage()
        {
            var page = new TabPage("Kamera") { Padding = new Padding(16), AutoScroll = true };
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int row = 0; row < 3; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text = "IP-Adresse der Kamera:", AutoSize = true }, 0, 0);
            layout.Controls.Add(address, 0, 1);
            var addressActions = new FlowLayoutPanel
            {
                AutoSize = true, Dock = DockStyle.Fill, WrapContents = false,
                Margin = new Padding(0, 16, 0, 0)
            };
            addressActions.Controls.Add(save);
            layout.Controls.Add(addressActions, 0, 2);
            page.Controls.Add(layout);
            return page;
        }

        private bool HasAddressChanges() =>
            CameraSettings.TryNormalizeIp(address.Text, out var ip) &&
            (!CameraSettings.TryNormalizeIp(CameraIp, out var current) || ip != current);

        private void UpdateAddressActions()
        {
            save.Enabled = HasAddressChanges();
            bool changed = address.Text.Trim() != CameraIp;
            if (presetsPage != null) presetsPage.Enabled = !changed;
            demoAction.Enabled = !changed;
            AcceptButton = save.Enabled && pages.SelectedIndex == 0 ? save : null;
            cancel.Text = changed && pages.SelectedIndex == 0 ? "Abbrechen" : "Schließen";
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
                Text = $"CamControl · Version {AppInformation.Version}\n{AppInformation.Description}\nHerausgeber: {AppInformation.Publisher}",
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
            if (!HasAddressChanges() || pages.SelectedIndex != 0)
                return;
            if (!CameraSettings.TryNormalizeIp(address.Text, out var ip))
            {
                MessageBox.Show(this, "Gültige IPv4-Adresse eingeben, z. B. 192.168.1.100.",
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
