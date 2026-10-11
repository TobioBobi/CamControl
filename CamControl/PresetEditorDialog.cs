namespace CamControl;

internal sealed class PresetEditorDialog : Form
{
    private readonly CamControlService service;
    private readonly Action<string> saveName;
    private readonly TextBox name;
    private readonly NumericUpDown number;
    private int savedNumber;
    private Action<string, int>? saveConfiguration;
    private Func<int, bool>? numberInUse;

    internal void ConfigurePresetNumber(int current, Action<string, int> save, Func<int, bool> inUse)
    {
        savedNumber = current;
        number.Value = current;
        saveConfiguration = save;
        numberInUse = inUse;
        UpdateActions();
    }
    private readonly Label status;
    private readonly FlowLayoutPanel movement;
    private readonly Button recall, store, rename, close;
    private CancellationTokenSource motionCancellation = new();
    private CancellationTokenSource? positionCancellation;
    internal event Action<string>? CloseStopFailed;
    private Task<bool>? stopping;
    private bool busy, closing, finalClose, motionHeld, active = true;
    private string savedName;
    private bool resourcesDisposed;
    private bool stopRequired = true;

    public PresetEditorDialog(CamControlService service, Position position, string currentName,
        Action<string> saveName, DemoCamera? demo = null)
    {
        this.service = service;
        this.saveName = saveName;
        savedName = currentName;
        savedNumber = (int)position;
        Text = $"Preset {(int)position} bearbeiten";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Font;
        AutoScaleDimensions = new SizeF(7, 15);
        ClientSize = new Size(640, demo == null ? 420 : 610);
        Padding = new Padding(16);

        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 8; i++) layout.RowStyles.Add(new RowStyle(i == 6 && demo != null ? SizeType.Absolute : SizeType.AutoSize, i == 6 && demo != null ? 180 : 0));
        layout.Controls.Add(new Label { AutoSize = true, Text = "Buttonname (1–32 Zeichen)" }, 0, 0);
        var nameRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 };
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        name = new TextBox { Name = "presetName", Text = currentName, MaxLength = 32, Dock = DockStyle.Fill, AccessibleName = "Buttonname" };
        rename = new Button { Name = "savePresetName", Text = "Zuordnung speichern", AutoSize = true };
        rename.Click += (_, _) => SaveName();
        nameRow.Controls.Add(name, 0, 0);
        nameRow.Controls.Add(rename, 1, 0);
        layout.Controls.Add(nameRow, 0, 1);
        var numberRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, Margin = new Padding(3, 12, 3, 8) };
        numberRow.Controls.Add(new Label { AutoSize = true, Text = "Kamerapreset-Nummer", Margin = new Padding(0, 5, 12, 0) });
        number = new NumericUpDown { Name = "cameraPresetNumber", Minimum = 0, Maximum = 254, Value = savedNumber, Width = 80, AccessibleName = "Kamerapreset-Nummer" };
        numberRow.Controls.Add(number);
        numberRow.Controls.Add(new Label { AutoSize = true, Text = "0–89 oder 100–254", Margin = new Padding(12, 5, 0, 0) });
        layout.Controls.Add(numberRow, 0, 2);
        movement = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true };
        AddMotion("←", "Nach links", token => service.MoveInDirection(Direction.Left, token));
        AddMotion("↑", "Nach oben", token => service.MoveInDirection(Direction.Up, token));
        AddMotion("↓", "Nach unten", token => service.MoveInDirection(Direction.Down, token));
        AddMotion("→", "Nach rechts", token => service.MoveInDirection(Direction.Right, token));
        AddMotion("Zoom +", "Vergrößern", token => service.Zoom(Zoom.ZoomIn, token));
        AddMotion("Zoom −", "Verkleinern", token => service.Zoom(Zoom.ZoomOut, token));
        layout.Controls.Add(movement, 0, 3);
        recall = new Button { Name = "recallPreset", Text = "Position anfahren", AutoSize = true, Margin = new Padding(3, 12, 3, 8) };
        recall.Click += async (_, _) => await RunPositionAsync(false);
        layout.Controls.Add(recall, 0, 4);
        store = new Button { Name = "storePreset", Text = "Position speichern", Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 48) };
        store.Click += async (_, _) =>
        {
            if (MessageBox.Show(this, $"Kamerapreset {(int)number.Value} für „{name.Text.Trim()}“ überschreiben?",
                "Position speichern", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                await RunPositionAsync(true);
        };
        layout.Controls.Add(store, 0, 5);
        if (demo != null) layout.Controls.Add(new DemoCameraView(demo) { Dock = DockStyle.Fill, MinimumSize = new Size(0, 180) }, 0, 6);
        status = new Label { Name = "presetStatus", AutoSize = true, MaximumSize = new Size(600, 0), Dock = DockStyle.Top, Margin = new Padding(3, 10, 3, 8) };
        var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        footer.Controls.Add(status);
        close = new Button { Text = "Schließen", AutoSize = true };
        close.Click += (_, _) => Close();
        layout.Controls.Add(footer, 0, 7);
        var scroll = new Panel { Name = "presetEditorContent", Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(layout);
        var container = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        container.Controls.Add(scroll, 0, 0);
        container.Controls.Add(close, 0, 1);
        Controls.Add(container);
        number.ValueChanged += (_, _) => UpdateActions();
        name.TextChanged += (_, _) => UpdateActions();
        CancelButton = close;
        AcceptButton = null;
        WindowLayout.FitToScreen(this);
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
        stopRequired = true;
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
        movement.Enabled = rename.Enabled = name.Enabled = number.Enabled = enabled;
        recall.Enabled = enabled && (int)number.Value == savedNumber;
        store.Enabled = enabled && PresetSettings.IsValidName(name.Text) &&
            PresetSettings.IsValidNumber((int)number.Value) && numberInUse?.Invoke((int)number.Value) != true;
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
        try
        {
            bool stopped = await stopping;
            stopRequired = !stopped;
            return stopped;
        }
        finally { stopping = null; previous.Dispose(); if (!IsDisposed) UpdateActions(); }
    }

    private async Task RunPositionAsync(bool save)
    {
        if (busy || closing || !active || resourcesDisposed) return;
        if (!save && (int)number.Value != savedNumber) return;
        if (save && ((int)number.Value != savedNumber || name.Text.Trim() != savedName) && !SaveName()) return;
        using var cancellation = new CancellationTokenSource();
        positionCancellation = cancellation;
        busy = true;
        UpdateActions();
        try
        {
            if (!await StopAsync() || cancellation.IsCancellationRequested || closing || !active) return;
            var token = cancellation.Token;
            if (!save) stopRequired = true;
            bool success = await CameraCommandRunner.RunAsync(() => save
                ? service.SavePreset(savedNumber, token) : service.MoveToPreset(savedNumber, token), ReportError, token);
            if (success && !closing && !token.IsCancellationRequested)
            {
                status.ForeColor = Color.DarkGreen;
                status.Text = save ? "Speicherbefehl gesendet. Mit „Position anfahren“ prüfen."
                    : "Positionsbefehl gesendet.";
            }
        }
        finally { positionCancellation = null; busy = false; if (!IsDisposed) UpdateActions(); }
    }

    private bool SaveName()
    {
        if (!PresetSettings.IsValidName(name.Text)) { ReportError("Bitte einen Namen mit 1 bis 32 Zeichen eingeben."); return false; }
        int selectedNumber = (int)number.Value;
        if (!PresetSettings.IsValidNumber(selectedNumber)) { ReportError("Nummer 0–89 oder 100–254 wählen."); return false; }
        if (numberInUse?.Invoke(selectedNumber) == true) { ReportError("Diese Nummer ist bereits einem anderen Button zugeordnet."); return false; }
        try
        {
            if (saveConfiguration != null) saveConfiguration(name.Text.Trim(), selectedNumber);
            else saveName(name.Text.Trim());
            savedNumber = selectedNumber;
            savedName = name.Text.Trim();
            status.ForeColor = Color.DarkGreen;
            status.Text = "Zuordnung gespeichert; Kameraposition unverändert.";
            UpdateActions();
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException) { ReportError("Zuordnung konnte nicht gespeichert werden: " + e.Message); return false; }
    }

    protected override async void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        active = false;
        if (closing) return;
        positionCancellation?.Cancel();
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
        close.Enabled = false;
        close.Text = "Wird geschlossen …";
        ControlBox = false;
        status.ForeColor = SystemColors.ControlText;
        status.Text = "Kamera wird gestoppt …";
        positionCancellation?.Cancel();
        UpdateActions();
        if ((stopRequired || stopping != null) && !await StopAsync())
            CloseStopFailed?.Invoke(status.Text);
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
            positionCancellation?.Cancel(); motionCancellation.Cancel(); motionCancellation.Dispose();
        }
        base.Dispose(disposing);
    }
}
