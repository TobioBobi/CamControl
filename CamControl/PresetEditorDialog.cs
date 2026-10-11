namespace CamControl;

internal sealed class PresetEditorDialog : Form
{
    private readonly CamControlService service;
    private readonly Position position;
    private readonly Action<string> saveName;
    private readonly TextBox name;
    private readonly Label status;
    private readonly FlowLayoutPanel movement;
    private readonly Button recall, store, rename;
    private CancellationTokenSource motionCancellation = new();
    private readonly CancellationTokenSource lifetime = new();
    private Task<bool>? stopping;
    private bool busy, closing, finalClose, motionHeld, active = true;
    private string savedName;
    private bool resourcesDisposed;

    public PresetEditorDialog(CamControlService service, Position position, string currentName,
        Action<string> saveName, DemoCamera? demo = null)
    {
        this.service = service;
        this.position = position;
        this.saveName = saveName;
        savedName = currentName;
        Text = $"Button {(int)position} bearbeiten";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Font;
        AutoScaleDimensions = new SizeF(7, 15);
        ClientSize = new Size(640, demo == null ? 420 : 610);
        Padding = new Padding(16);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 8; i++) layout.RowStyles.Add(new RowStyle(i == 6 ? SizeType.Percent : SizeType.AutoSize, i == 6 ? 100 : 0));
        layout.Controls.Add(new Label { AutoSize = true, Text = $"Buttonname · Kamera-Preset {(int)position}" }, 0, 0);
        var nameRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 };
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        name = new TextBox { Name = "presetName", Text = currentName, MaxLength = 32, Dock = DockStyle.Fill, AccessibleName = "Buttonname" };
        rename = new Button { Name = "savePresetName", Text = "Nur Namen speichern", AutoSize = true };
        rename.Click += (_, _) => SaveName();
        nameRow.Controls.Add(name, 0, 0);
        nameRow.Controls.Add(rename, 1, 0);
        layout.Controls.Add(nameRow, 0, 1);
        layout.Controls.Add(new Label
        {
            AutoSize = true, MaximumSize = new Size(600, 0), Margin = new Padding(3, 12, 3, 8),
            Text = demo == null ? "Kamera ausrichten: Taste gedrückt halten. Bewegungen werden sofort ausgeführt."
                : "DEMO-MODUS · Positionen und Namen gelten nur für diese Sitzung. Taste gedrückt halten."
        }, 0, 2);
        movement = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false };
        AddMotion("←", "Nach links", token => service.MoveInDirection(Direction.Left, token));
        AddMotion("↑", "Nach oben", token => service.MoveInDirection(Direction.Up, token));
        AddMotion("↓", "Nach unten", token => service.MoveInDirection(Direction.Down, token));
        AddMotion("→", "Nach rechts", token => service.MoveInDirection(Direction.Right, token));
        AddMotion("Zoom +", "Vergrößern", token => service.Zoom(Zoom.ZoomIn, token));
        AddMotion("Zoom −", "Verkleinern", token => service.Zoom(Zoom.ZoomOut, token));
        layout.Controls.Add(movement, 0, 3);
        recall = new Button { Name = "recallPreset", Text = "Gespeicherte Position anfahren", AutoSize = true, Margin = new Padding(3, 12, 3, 8) };
        recall.Click += async (_, _) => await RunPositionAsync(false);
        layout.Controls.Add(recall, 0, 4);
        store = new Button { Name = "storePreset", Text = $"Aktuelle Kameraposition für „{currentName}“ speichern", Dock = DockStyle.Top, Height = 48 };
        store.Click += async (_, _) =>
        {
            if (MessageBox.Show(this, $"Die bisherige Position für „{savedName}“ (Preset {(int)position}) wird überschrieben.\nPosition jetzt speichern?",
                "Kameraposition speichern", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                await RunPositionAsync(true);
        };
        layout.Controls.Add(store, 0, 5);
        if (demo != null) layout.Controls.Add(new DemoCameraView(demo) { Dock = DockStyle.Fill, MinimumSize = new Size(0, 180) }, 0, 6);
        status = new Label { Name = "presetStatus", AutoSize = true, MaximumSize = new Size(600, 0), Dock = DockStyle.Top, Margin = new Padding(3, 10, 3, 8) };
        var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        footer.Controls.Add(status);
        footer.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text = "Schließen verwirft ungespeicherte Namen. Ausgeführte Bewegungen und gespeicherte Positionen bleiben bestehen." });
        var close = new Button { Text = "Schließen", AutoSize = true, DialogResult = DialogResult.Cancel };
        footer.Controls.Add(close);
        layout.Controls.Add(footer, 0, 7);
        Controls.Add(layout);
        CancelButton = close;
        AcceptButton = null;
    }

    private void AddMotion(string text, string description, Func<CancellationToken, Task> command)
    {
        var button = new Button { Text = text, AccessibleName = description, Size = new Size(88, 44), TabStop = true };
        button.MouseDown += async (_, e) => { if (e.Button == MouseButtons.Left) await StartMotionAsync(command); };
        button.MouseUp += async (_, _) => { if (motionHeld) await StopAsync(); };
        button.MouseCaptureChanged += async (_, _) => { if (!button.Capture && motionHeld) await StopAsync(); };
        button.KeyDown += async (_, e) =>
        {
            if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await StartMotionAsync(command);
            }
        };
        button.KeyUp += async (_, e) => { if (e.KeyCode is Keys.Space or Keys.Enter) { e.SuppressKeyPress = true; await StopAsync(); } };
        button.Leave += async (_, _) => { if (motionHeld) await StopAsync(); };
        movement.Controls.Add(button);
    }

    private async Task StartMotionAsync(Func<CancellationToken, Task> command)
    {
        if (busy || closing || motionHeld || !active || stopping != null || resourcesDisposed || Disposing || IsDisposed) return;
        motionHeld = true;
        var token = motionCancellation.Token;
        await CameraCommandRunner.RunAsync(() => command(token), ReportError, token);
    }

    private void ReportError(string error)
    {
        if (IsDisposed) return;
        status.ForeColor = Color.DarkRed;
        status.Text = error;
    }

    private void UpdateActions()
    {
        bool enabled = !busy && !closing && stopping == null;
        movement.Enabled = recall.Enabled = store.Enabled = rename.Enabled = name.Enabled = enabled;
    }

    private async Task<bool> StopAsync()
    {
        if (finalClose || IsDisposed || Disposing || resourcesDisposed) return false;
        motionHeld = false;
        if (stopping != null) return await stopping;
        var previous = motionCancellation;
        previous.Cancel();
        motionCancellation = new();
        // Starts cancelled before this stop cannot be queued behind it.
        stopping = CameraCommandRunner.RunAsync(() => service.StopAll(), ReportError);
        UpdateActions();
        try { return await stopping; }
        finally { stopping = null; previous.Dispose(); if (!IsDisposed) UpdateActions(); }
    }

    private async Task RunPositionAsync(bool save)
    {
        if (busy || closing) return;
        busy = true;
        UpdateActions();
        try
        {
            if (!await StopAsync() || closing || !active) return;
            var token = lifetime.Token;
            bool success = await CameraCommandRunner.RunAsync(() => save
                ? service.SavePosition(position, token) : service.MoveToPosition(position, token), ReportError, token);
            if (success && !closing)
            {
                status.ForeColor = Color.DarkGreen;
                status.Text = save ? "Speicherbefehl erfolgreich gesendet. Mit „Gespeicherte Position anfahren“ prüfen."
                    : "Positionsbefehl erfolgreich gesendet.";
            }
        }
        finally { busy = false; if (!IsDisposed) UpdateActions(); }
    }

    private void SaveName()
    {
        if (!PresetSettings.IsValidName(name.Text)) { ReportError("Bitte einen Namen mit 1 bis 32 Zeichen eingeben."); return; }
        try
        {
            saveName(name.Text.Trim());
            savedName = name.Text.Trim();
            store.Text = $"Aktuelle Kameraposition für „{name.Text.Trim()}“ speichern";
            status.ForeColor = Color.DarkGreen;
            status.Text = "Buttonname gespeichert. Die Kameraposition wurde nicht verändert.";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { ReportError("Name konnte nicht gespeichert werden: " + e.Message); }
    }

    protected override async void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        active = false;
        await StopAsync();
    }

    protected override void OnActivated(EventArgs e) { base.OnActivated(e); active = true; }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!name.ContainsFocus)
        {
            Direction? direction = keyData switch
            {
                Keys.Left => Direction.Left, Keys.Right => Direction.Right,
                Keys.Up => Direction.Up, Keys.Down => Direction.Down, _ => null
            };
            if (direction is { } value)
            {
                _ = StartMotionAsync(token => service.MoveInDirection(value, token));
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!name.ContainsFocus && e.Modifiers == Keys.None && e.KeyCode is Keys.Add or Keys.Subtract)
        {
            e.SuppressKeyPress = true;
            await StartMotionAsync(token => service.Zoom(e.KeyCode == Keys.Add ? Zoom.ZoomIn : Zoom.ZoomOut, token));
        }
    }

    protected override async void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (motionHeld && e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Add or Keys.Subtract)
        {
            e.SuppressKeyPress = true;
            await StopAsync();
        }
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (finalClose) return;
        e.Cancel = true;
        if (closing) return;
        closing = true;
        lifetime.Cancel();
        UpdateActions();
        await StopAsync();
        finalClose = true;
        // Demo stops may complete synchronously. Let the initial, cancelled
        // FormClosing event unwind before requesting the final modal close.
        if (!IsDisposed && IsHandleCreated)
            BeginInvoke(new Action(() => { if (!IsDisposed) Close(); }));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !resourcesDisposed)
        {
            resourcesDisposed = true;
            lifetime.Cancel(); lifetime.Dispose(); motionCancellation.Cancel(); motionCancellation.Dispose();
        }
        base.Dispose(disposing);
    }
}
