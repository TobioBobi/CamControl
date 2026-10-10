namespace CamControl
{
    public partial class Form1 : Form
    {
        CamControlService camControlService;
        bool finalClose;
        private readonly HiddenSettingsGesture settingsGesture = new();
        private bool settingsDialogOpen;
        private readonly Label cameraErrorLabel;
        private int cameraErrorVersion;
        private CancellationTokenSource controlCancellation = new();
        private CancellationTokenSource directionCancellation = new();
        private CancellationTokenSource zoomCancellation = new();
        private Task<bool>? stopControlTask;
        private bool positionInProgress;
        private CancellationTokenSource? positionCancellation;
        private bool cameraWindowActive = true;
        private bool keyZoomIsPressed, keyDirectionIsPressed;
        private bool mouseZoomIsPressed, mouseDirectionIsPressed;

        public Form1()
        {
            InitializeComponent();
            camControlService = new CamControlService();
            cameraErrorLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(12, 4, 12, 4),
                ForeColor = Color.DarkRed,
                AutoEllipsis = true,
                AccessibleName = "Kamera-Fehlermeldung"
            };
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + cameraErrorLabel.Height);
            Controls.Add(cameraErrorLabel);
            foreach (var button in new[] { buttonUp, buttonDown, buttonLeft, buttonRight, buttonZoomIn, buttonZoomOut })
                button.MouseCaptureChanged += CameraButton_MouseCaptureChanged;
        }

        private bool CanControlCamera => !IsDisposed && !Disposing && !finalClose && ControlBox &&
            cameraWindowActive && !settingsDialogOpen && stopControlTask == null;

        private void UpdateCameraControls()
        {
            if (IsDisposed || Disposing)
                return;
            foreach (var button in Controls.OfType<Button>().Concat(tableLayoutPanel1.Controls.OfType<Button>()))
                button.Enabled = CanControlCamera && !positionInProgress;
        }

        private void ReportCameraError(string action, string error)
        {
            if (IsDisposed || Disposing || finalClose || !ControlBox)
                return;
            cameraErrorVersion++;
            cameraErrorLabel.Text = $"{action}: {error}\nAdresse ändern: fünfmal schnell auf „CamControl“ links oben klicken.";
        }

        private async Task ExecuteCameraCommandAsync(Func<CancellationToken, Task> command, string action,
            CancellationToken cancellationToken = default)
        {
            if (!CanControlCamera)
                return;
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                controlCancellation.Token, cancellationToken);
            var token = linkedCancellation.Token;
            int errorVersionAtStart = cameraErrorVersion;
            bool succeeded = await CameraCommandRunner.RunAsync(() => command(token),
                error => ReportCameraError(action, error), token);
            if (succeeded && errorVersionAtStart == cameraErrorVersion && CanControlCamera)
                cameraErrorLabel.Text = string.Empty;
        }

        private Task<bool> StopCameraControlAsync()
        {
            // Cancel the entire preset, including its preparatory stop wait.
            positionCancellation?.Cancel();
            return StopManualControlAsync();
        }

        private async Task<bool> StopManualControlAsync()
        {
            // Reset before awaiting: a missing KeyUp/MouseUp must not leave a
            // pressed flag behind. Concurrent lifecycle events share one stop.
            keyDirectionIsPressed = keyZoomIsPressed = false;
            mouseDirectionIsPressed = mouseZoomIsPressed = false;
            if (stopControlTask != null)
                return await stopControlTask;

            var previousCancellation = controlCancellation;
            previousCancellation.Cancel();
            controlCancellation = new CancellationTokenSource();
            var stop = CameraCommandRunner.RunAsync(() => camControlService.StopAll(),
                error => ReportCameraError("Kamerastopp nicht vollständig bestätigt", error));
            stopControlTask = stop;
            UpdateCameraControls();
            try
            {
                return await stop;
            }
            finally
            {
                stopControlTask = null;
                previousCancellation.Dispose();
                UpdateCameraControls();
            }
        }

        private async Task MoveToPositionAsync(Position position)
        {
            if (!CanControlCamera || positionInProgress)
                return;
            using var cancellation = new CancellationTokenSource();
            positionCancellation = cancellation;
            positionInProgress = true;
            UpdateCameraControls();
            if (position == Position.StandBy)
            {
                ActiveControl = null;
                pictureBoxLoaderSmall.BringToFront();
                pictureBoxLoaderSmall.Visible = true;
            }
            try
            {
                // Stop manual motion and cancel queued starts before a preset.
                if (await StopManualControlAsync() && !cancellation.IsCancellationRequested)
                    await ExecuteCameraCommandAsync(token => camControlService.MoveToPosition(position, token),
                        "Position anfahren", cancellation.Token);
            }
            finally
            {
                positionCancellation = null;
                positionInProgress = false;
                if (!IsDisposed && !Disposing)
                {
                    pictureBoxLoaderSmall.Visible = false;
                    UpdateCameraControls();
                }
            }
        }

        protected override void WndProc(ref Message message)
        {
            const int WmNcLeftButtonDown = 0x00A1, WmNcLeftButtonDoubleClick = 0x00A3, HtCaption = 2;
            if ((message.Msg == WmNcLeftButtonDown || message.Msg == WmNcLeftButtonDoubleClick) &&
                message.WParam.ToInt32() == HtCaption)
            {
                int screenX = unchecked((short)(message.LParam.ToInt64() & 0xFFFF));
                int titleWidth = TextRenderer.MeasureText(Text, SystemFonts.SmallCaptionFont).Width +
                    (int)(24 * DeviceDpi / 96F);
                if (screenX >= Left && screenX < Left + titleWidth && !settingsDialogOpen)
                {
                    if (settingsGesture.RegisterClick(Environment.TickCount64))
                        BeginInvoke(new Action(OpenCameraSettings));
                    return;
                }
                settingsGesture.Reset();
            }
            base.WndProc(ref message);
        }

        private async void OpenCameraSettings()
        {
            if (settingsDialogOpen || finalClose || !ControlBox)
                return;
            try
            {
                if (!await PrepareCameraSettingsAsync())
                    return;
                using var dialog = new CameraSettingsDialog(camControlService.CameraIp);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    camControlService.SetCameraIp(dialog.CameraIp);
            }
            finally
            {
                settingsDialogOpen = false;
                settingsGesture.Reset();
                UpdateCameraControls();
            }
        }

        private async Task<bool> PrepareCameraSettingsAsync()
        {
            settingsDialogOpen = true;
            UpdateCameraControls();
            await StopCameraControlAsync();
            return !IsDisposed && !Disposing && !finalClose && ControlBox;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            cameraWindowActive = true;
            UpdateCameraControls();
        }

        protected override async void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            cameraWindowActive = false;
            UpdateCameraControls();
            if (!settingsDialogOpen && !finalClose && ControlBox)
                await StopCameraControlAsync();
        }

        private async void CameraButton_MouseCaptureChanged(object? sender, EventArgs e)
        {
            if (sender is Control { Capture: false } && (mouseDirectionIsPressed || mouseZoomIsPressed))
                await StopCameraControlAsync();
        }

        private async void Form1_Load(object sender, EventArgs e) => await MoveToPositionAsync(Position.BuehneKomplett);

        private async void Form1_FormClosingAsync(object sender, FormClosingEventArgs e)
        {
            if (finalClose)
                return;
            e.Cancel = true;
            if (!ControlBox)
                return;
            ControlBox = false;
            UpdateCameraControls();
            pictureBoxLoader.BringToFront();
            pictureBoxLoader.Visible = true;
            try
            {
                // Do not send another motion command if stopping failed.
                if (await StopCameraControlAsync())
                    await CameraCommandRunner.RunAsync(() => camControlService.MoveToPosition(Position.StandBy), _ => { });
            }
            finally
            {
                finalClose = true;
                Application.Exit();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            positionCancellation?.Cancel();
            controlCancellation.Cancel();
            controlCancellation.Dispose();
            directionCancellation.Cancel();
            directionCancellation.Dispose();
            zoomCancellation.Cancel();
            zoomCancellation.Dispose();
            base.OnFormClosed(e);
        }

        private async void buttonRedner_Click(object sender, EventArgs e) => await MoveToPositionAsync(Position.Redner);
        private async void buttonRednerLeser_Click(object sender, EventArgs e) => await MoveToPositionAsync(Position.Redner_Leser);
        private async void buttonTisch_Click(object sender, EventArgs e) => await MoveToPositionAsync(Position.Tisch);
        private async void buttonKompletteBuehne_Click(object sender, EventArgs e) => await MoveToPositionAsync(Position.BuehneKomplett);
        private async void buttonRednerBildschirm_Click(object sender, EventArgs e) => await MoveToPositionAsync(Position.RednerBildschirm);
        private async void buttonDemoStehend_Click(object sender, EventArgs e) => await MoveToPositionAsync(Position.DemoStehend);
        private async void buttonStandBy_Click(object sender, EventArgs e) => await MoveToPositionAsync(Position.StandBy);

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            Position? position = e.KeyChar switch
            {
                '1' => Position.Redner, '2' => Position.Redner_Leser, '3' => Position.Tisch,
                '4' => Position.BuehneKomplett, '5' => Position.RednerBildschirm,
                '6' => Position.DemoStehend, '9' => Position.StandBy, _ => null
            };
            if (position.HasValue)
                _ = MoveToPositionAsync(position.Value);
        }

        private async Task StartDirectionAsync(Direction direction, bool keyboard)
        {
            if (!CanControlCamera || positionInProgress || (keyboard && keyDirectionIsPressed))
                return;
            if (keyboard) keyDirectionIsPressed = true;
            else mouseDirectionIsPressed = true;
            await ExecuteCameraCommandAsync(token => camControlService.MoveInDirection(direction, token),
                "Kamera bewegen", directionCancellation.Token);
        }

        private async Task StartZoomAsync(Zoom zoom, bool keyboard)
        {
            if (!CanControlCamera || positionInProgress || (keyboard && keyZoomIsPressed))
                return;
            if (keyboard) keyZoomIsPressed = true;
            else mouseZoomIsPressed = true;
            await ExecuteCameraCommandAsync(token => camControlService.Zoom(zoom, token), "Kamera zoomen", zoomCancellation.Token);
        }

        private Task StopDirectionAsync()
        {
            keyDirectionIsPressed = mouseDirectionIsPressed = false;
            directionCancellation.Cancel();
            directionCancellation.Dispose();
            directionCancellation = new CancellationTokenSource();
            return ExecuteCameraCommandAsync(token => camControlService.StopDirectionalMovement(token), "Bewegungsstopp nicht bestätigt");
        }

        private Task StopZoomAsync()
        {
            keyZoomIsPressed = mouseZoomIsPressed = false;
            zoomCancellation.Cancel();
            zoomCancellation.Dispose();
            zoomCancellation = new CancellationTokenSource();
            return ExecuteCameraCommandAsync(token => camControlService.StopZooming(token), "Zoomstopp nicht bestätigt");
        }

        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Add: await StartZoomAsync(Zoom.ZoomIn, true); break;
                case Keys.Subtract: await StartZoomAsync(Zoom.ZoomOut, true); break;
                case Keys.Left: await StartDirectionAsync(Direction.Left, true); break;
                case Keys.Right: await StartDirectionAsync(Direction.Right, true); break;
                case Keys.Up: await StartDirectionAsync(Direction.Up, true); break;
                case Keys.Down: await StartDirectionAsync(Direction.Down, true); break;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Direction? direction = keyData switch
            {
                Keys.Left => Direction.Left, Keys.Right => Direction.Right,
                Keys.Up => Direction.Up, Keys.Down => Direction.Down, _ => null
            };
            if (direction.HasValue)
            {
                _ = StartDirectionAsync(direction.Value, true);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Add:
                case Keys.Subtract:
                    await StopZoomAsync();
                    break;
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    await StopDirectionAsync();
                    break;
            }
        }

        private async void buttonUp_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) await StartDirectionAsync(Direction.Up, false); }
        private async void buttonDown_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) await StartDirectionAsync(Direction.Down, false); }
        private async void buttonLeft_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) await StartDirectionAsync(Direction.Left, false); }
        private async void buttonRight_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) await StartDirectionAsync(Direction.Right, false); }
        private async void buttonZoomIn_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) await StartZoomAsync(Zoom.ZoomIn, false); }
        private async void buttonZoomOut_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) await StartZoomAsync(Zoom.ZoomOut, false); }

        private async void button_MouseUpStopDirectionalMovement(object sender, MouseEventArgs e)
        {
            await StopDirectionAsync();
        }

        private async void button_MouseUpStopZooming(object sender, MouseEventArgs e)
        {
            await StopZoomAsync();
        }
    }
}
