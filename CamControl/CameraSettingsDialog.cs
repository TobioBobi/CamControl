namespace CamControl
{
    internal sealed class CameraSettingsDialog : Form
    {
        private readonly TextBox address;
        public string CameraIp { get; private set; }

        public CameraSettingsDialog(string cameraIp)
        {
            CameraIp = cameraIp;
            Text = "Kamera-Einstellungen";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7F, 15F);
            ClientSize = new Size(350, 155);

            var label = new Label { Text = "IP-Adresse der Kamera:", AutoSize = true, Location = new Point(16, 16) };
            address = new TextBox { Text = cameraIp, Location = new Point(16, 42), Size = new Size(318, 23), TabIndex = 0 };
            var hint = new Label { Text = "Die Änderung gilt sofort und bleibt gespeichert.", AutoSize = true, Location = new Point(16, 76) };
            var save = new Button { Text = "Speichern", Location = new Point(146, 112), Size = new Size(90, 27), TabIndex = 1 };
            var cancel = new Button { Text = "Abbrechen", Location = new Point(244, 112), Size = new Size(90, 27), TabIndex = 2, DialogResult = DialogResult.Cancel };
            save.Click += Save_Click;
            Controls.AddRange(new Control[] { label, address, hint, save, cancel });
            AcceptButton = save;
            CancelButton = cancel;
            Shown += (_, _) => { address.Focus(); address.SelectAll(); };
        }

        private void Save_Click(object? sender, EventArgs e)
        {
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
                CameraSettings.Save(ip);
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
