namespace CamControl
{
    internal sealed class CameraSettingsDialog : Form
    {
        private readonly TextBox address;
        private readonly Button edit, save, cancel;
        private readonly Label hint;
        private readonly string? settingsPath;
        private bool editing;
        public string CameraIp { get; private set; }

        public CameraSettingsDialog(string cameraIp, string? settingsPath = null)
        {
            CameraIp = cameraIp;
            this.settingsPath = settingsPath;
            Text = "Einstellungen";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7F, 15F);
            ClientSize = new Size(540, 280);
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
            // Add future pages for camera presets and button labels here once
            // the camera's preset-save API is known. All pages share edit/save/cancel.
            var pages = new TabControl { Name = "settingsPages", Dock = DockStyle.Fill };
            pages.TabPages.Add(CreateCameraPage());

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
            editing = true;
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
