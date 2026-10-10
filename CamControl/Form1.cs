namespace CamControl
{
    public partial class Form1 : Form
    {
        CamControlService camControlService;
        bool finalClose = false;
        private readonly HiddenSettingsGesture settingsGesture = new HiddenSettingsGesture();
        private bool settingsDialogOpen;
        private readonly Label cameraErrorLabel;
        private int cameraErrorVersion;

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
        }

        private async Task ExecuteCameraCommandAsync(Func<Task> command, string action)
        {
            if (IsDisposed || Disposing || finalClose || !ControlBox)
                return;

            int errorVersionAtStart = cameraErrorVersion;
            bool succeeded = await CameraCommandRunner.RunAsync(command, error =>
            {
                if (!IsDisposed && !Disposing && !finalClose && ControlBox)
                {
                    cameraErrorVersion++;
                    cameraErrorLabel.Text = $"{action}: {error}\nAdresse ändern: fünfmal schnell auf „CamControl“ links oben klicken.";
                }
            });
            // An older successful request must not hide a newer command failure.
            if (succeeded && errorVersionAtStart == cameraErrorVersion &&
                !IsDisposed && !Disposing && !finalClose && ControlBox)
                cameraErrorLabel.Text = string.Empty;
        }

        protected override void WndProc(ref Message message)
        {
            const int WmNcLeftButtonDown = 0x00A1;
            const int WmNcLeftButtonDoubleClick = 0x00A3;
            const int HtCaption = 2;
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
                    // Consume clicks on the title text, including native double-click messages.
                    return;
                }
                settingsGesture.Reset();
            }
            base.WndProc(ref message);
        }

        private void OpenCameraSettings()
        {
            if (settingsDialogOpen || finalClose || !ControlBox)
                return;

            settingsDialogOpen = true;
            try
            {
                using var dialog = new CameraSettingsDialog(camControlService.CameraIp);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    camControlService.SetCameraIp(dialog.CameraIp);
            }
            finally
            {
                settingsDialogOpen = false;
                settingsGesture.Reset();
            }
        }

        #region Positions

        private async void buttonRedner_Click(object sender, EventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveToPosition(Position.Redner), "Position anfahren");
        }

        private async void buttonRednerLeser_Click(object sender, EventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveToPosition(Position.Redner_Leser), "Position anfahren");
        }

        private async void buttonTisch_Click(object sender, EventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveToPosition(Position.Tisch), "Position anfahren");
        }

        private async void buttonKompletteBuehne_Click(object sender, EventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveToPosition(Position.BuehneKomplett), "Position anfahren");
        }

        private async void buttonRednerBildschirm_Click(object sender, EventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveToPosition(Position.RednerBildschirm), "Position anfahren");
        }

        private async void buttonStandBy_Click(object sender, EventArgs e)
        {
            if (!buttonStandBy.Enabled || finalClose || !ControlBox)
                return;
            try
            {
                this.ActiveControl = null;
                pictureBoxLoaderSmall.BringToFront();
                pictureBoxLoaderSmall.Visible = true;
                buttonStandBy.Enabled = false;
                await ExecuteCameraCommandAsync(() => camControlService.MoveToPosition(Position.StandBy), "Position anfahren");
            }
            finally
            {
                if (!IsDisposed && !Disposing && !finalClose)
                {
                    buttonStandBy.Enabled = true;
                    pictureBoxLoaderSmall.Visible = false;
                    if (ControlBox)
                        buttonStandBy.Focus();
                }
            }
        }

        private async void buttonDemoStehend_Click(object sender, EventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveToPosition(Position.DemoStehend), "Position anfahren");
        }

        #endregion



        #region Directions
        private async void buttonUp_MouseDown(object sender, MouseEventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Up), "Kamera bewegen");
        }

        private async void buttonDown_MouseDown(object sender, MouseEventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Down), "Kamera bewegen");

        }
        private async void buttonLeft_MouseDown(object sender, MouseEventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Left), "Kamera bewegen");

        }
        private async void buttonRight_MouseDown(object sender, MouseEventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Right), "Kamera bewegen");

        }

        private async void button_MouseUpStopDirectionalMovement(object sender, MouseEventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.StopDirectionalMovement(), "Bewegungsstopp nicht bestätigt");
        }



        #endregion

        #region Form

        private async void Form1_Load(object sender, EventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.MoveToPosition(Position.BuehneKomplett), "Startposition anfahren");
        }

        private async void Form1_FormClosingAsync(object sender, FormClosingEventArgs e)
        {
            if (finalClose)
                return;
            e.Cancel = true;
            if (!ControlBox)
                return;
            try
            {
                this.ControlBox = false;
                pictureBoxLoader.BringToFront();
                pictureBoxLoader.Visible = true;
                // Closing must complete even if the camera rejects standby or is offline.
                await CameraCommandRunner.RunAsync(() => camControlService.MoveToPosition(Position.StandBy), _ => { });
            }
            finally
            {
                finalClose = true;
                Application.Exit();
            }

        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '9':
                    buttonStandBy_Click(sender, e);
                    break;
                case '1':
                    buttonRedner_Click((object)sender, e);
                    break;
                case '2':
                    buttonRednerLeser_Click((object)sender, e);
                    break;
                case '3':
                    buttonTisch_Click((object)sender, e);
                    break;
                case '4':
                    buttonKompletteBuehne_Click((object)sender, e);
                    break;
                case '5':
                    buttonRednerBildschirm_Click((object)sender, e);
                    break;
                case '6':
                    buttonDemoStehend_Click((object)sender, e);
                    break;
                default: break;
            }

        }

        bool keyZoomIsPressed, keyDirectionIsPressed;

        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Add:
                    if (!keyZoomIsPressed)
                    {
                        keyZoomIsPressed = true;
                        await ExecuteCameraCommandAsync(() => camControlService.Zoom(Zoom.ZoomIn), "Kamera zoomen");
                    }
                    break;
                case Keys.Subtract:
                    if (!keyZoomIsPressed)
                    {
                        keyZoomIsPressed = true;
                        await ExecuteCameraCommandAsync(() => camControlService.Zoom(Zoom.ZoomOut), "Kamera zoomen");
                    }
                    break;
                case Keys.Left:
                    if (!keyDirectionIsPressed)
                    {
                        keyDirectionIsPressed = true;
                        await ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Left), "Kamera bewegen");
                    }
                    break;
                case Keys.Right:
                    if (!keyDirectionIsPressed)
                    {
                        keyDirectionIsPressed = true;
                        await ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Right), "Kamera bewegen");
                    }
                    break;
                case Keys.Up:
                    if (!keyDirectionIsPressed)
                    {
                        keyDirectionIsPressed = true;
                        await ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Up), "Kamera bewegen");
                    }
                    break;
                case Keys.Down:
                    if (!keyDirectionIsPressed)
                    {
                        keyDirectionIsPressed = true;
                        await ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Down), "Kamera bewegen");
                    }
                    break;
                default: break;

            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Down || keyData == Keys.Up ||
                keyData == Keys.Left || keyData == Keys.Right)
            {
                switch (keyData)
                {
                    case Keys.Left:
                        if (!keyDirectionIsPressed)
                        {
                            keyDirectionIsPressed = true;
                            _ = ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Left), "Kamera bewegen");
                        }
                        break;
                    case Keys.Right:
                        if (!keyDirectionIsPressed)
                        {
                            keyDirectionIsPressed = true;
                            _ = ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Right), "Kamera bewegen");
                        }
                        break;
                    case Keys.Up:
                        if (!keyDirectionIsPressed)
                        {
                            keyDirectionIsPressed = true;
                            _ = ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Up), "Kamera bewegen");
                        }
                        break;
                    case Keys.Down:
                        if (!keyDirectionIsPressed)
                        {
                            keyDirectionIsPressed = true;
                            _ = ExecuteCameraCommandAsync(() => camControlService.MoveInDirection(Direction.Down), "Kamera bewegen");
                        }
                        break;
                    default: break;

                }
                return true; // Mark the key as handled
            }

            // Call the base class method to handle other keys
            return base.ProcessCmdKey(ref msg, keyData);
        }



        private async void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Add:
                case Keys.Subtract:
                    keyZoomIsPressed = false;
                    await ExecuteCameraCommandAsync(() => camControlService.StopZooming(), "Zoomstopp nicht bestätigt");
                    break;
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    keyDirectionIsPressed = false;
                    await ExecuteCameraCommandAsync(() => camControlService.StopDirectionalMovement(), "Bewegungsstopp nicht bestätigt");
                    break;
                default: break;
            }
        }



        #endregion

        #region Zoom

        private async void buttonZoomOut_MouseDown(object sender, MouseEventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.Zoom(Zoom.ZoomOut), "Kamera zoomen");
        }

        private async void buttonZoomIn_MouseDown(object sender, MouseEventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.Zoom(Zoom.ZoomIn), "Kamera zoomen");

        }

        private async void button_MouseUpStopZooming(object sender, MouseEventArgs e)
        {
            await ExecuteCameraCommandAsync(() => camControlService.StopZooming(), "Zoomstopp nicht bestätigt");
        }

        #endregion





    }
}
