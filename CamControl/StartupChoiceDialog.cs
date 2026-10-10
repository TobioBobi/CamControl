namespace CamControl
{
    internal sealed class StartupChoiceDialog : Form
    {
        public bool UseDemo { get; private set; }

        public StartupChoiceDialog()
        {
            Text = "Willkommen bei CamControl";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            AutoScaleDimensions = new SizeF(7, 15);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(500, 270);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(450, 0),
                Text = "Eine PTZ-Kamera steuern oder die Bedienung ohne Kamera ausprobieren. Vor deiner Auswahl werden keine Kamerabefehle gesendet."
            }, 0, 0);
            var demo = new Button { Text = "Ohne Kamera ausprobieren", Dock = DockStyle.Fill, Height = 54, Margin = new Padding(0, 16, 0, 8) };
            var setup = new Button { Text = "Kamera einrichten", Dock = DockStyle.Fill, Height = 54, Margin = new Padding(0, 0, 0, 8) };
            demo.Click += (_, _) => { UseDemo = true; DialogResult = DialogResult.OK; };
            setup.Click += (_, _) =>
            {
                using var settings = new CameraSettingsDialog(CameraSettings.Load());
                if (settings.ShowDialog(this) == DialogResult.OK)
                {
                    UseDemo = settings.RequestedDemoMode == true;
                    DialogResult = DialogResult.OK;
                }
            };
            var close = new Button { Text = "Schließen", AutoSize = true, Anchor = AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(demo, 0, 1);
            layout.Controls.Add(setup, 0, 2);
            layout.Controls.Add(close, 0, 3);
            Controls.Add(layout);
            CancelButton = close;
        }
    }
}
