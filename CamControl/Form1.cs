namespace CamControl
{
    public partial class Form1 : Form
    {
        CamControlService camControlService;
        bool finalClose = false;
        private readonly HiddenSettingsGesture settingsGesture = new HiddenSettingsGesture();
        private bool settingsDialogOpen;

        public Form1()
        {
            InitializeComponent();
            camControlService = new CamControlService();
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
            await camControlService.MoveToPosition(Position.Redner);
        }

        private async void buttonRednerLeser_Click(object sender, EventArgs e)
        {
            await camControlService.MoveToPosition(Position.Redner_Leser);
        }

        private async void buttonTisch_Click(object sender, EventArgs e)
        {
            await camControlService.MoveToPosition(Position.Tisch);
        }

        private async void buttonKompletteBuehne_Click(object sender, EventArgs e)
        {
            await camControlService.MoveToPosition(Position.BuehneKomplett);
        }

        private async void buttonRednerBildschirm_Click(object sender, EventArgs e)
        {
            await camControlService.MoveToPosition(Position.RednerBildschirm);
        }

        private async void buttonStandBy_Click(object sender, EventArgs e)
        {
            try
            {
                this.ActiveControl = null;
                pictureBoxLoaderSmall.BringToFront();
                pictureBoxLoaderSmall.Visible = true;
                buttonStandBy.Enabled = false;
                await camControlService.MoveToPosition(Position.StandBy);
            }
            finally
            {
                buttonStandBy.Enabled = true;
                pictureBoxLoaderSmall.Visible = false;
                buttonStandBy.Focus();
            }
        }

        private async void buttonDemoStehend_Click(object sender, EventArgs e)
        {
            await camControlService.MoveToPosition(Position.DemoStehend);
        }

        #endregion



        #region Directions
        private void buttonUp_MouseDown(object sender, MouseEventArgs e)
        {
            camControlService.MoveInDirection(Direction.Up);
        }

        private void buttonDown_MouseDown(object sender, MouseEventArgs e)
        {
            camControlService.MoveInDirection(Direction.Down);

        }
        private void buttonLeft_MouseDown(object sender, MouseEventArgs e)
        {
            camControlService.MoveInDirection(Direction.Left);

        }
        private void buttonRight_MouseDown(object sender, MouseEventArgs e)
        {
            camControlService.MoveInDirection(Direction.Right);

        }

        private void button_MouseUpStopDirectionalMovement(object sender, MouseEventArgs e)
        {
            camControlService.StopDirectionalMovement();
        }



        #endregion

        #region Form

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                await camControlService.MoveToPosition(Position.BuehneKomplett);
            }
            catch (Exception exception) when (exception is HttpRequestException || exception is TaskCanceledException)
            {
                if (!IsDisposed && !Disposing && !finalClose && ControlBox)
                    MessageBox.Show(this,
                        "Die Kamera konnte nicht erreicht werden. Bitte Netzwerk und Kamera-Adresse prüfen.\n\n" +
                        "Zum Ändern der Adresse fünfmal schnell auf „CamControl“ links oben klicken.",
                        "Kamera nicht erreichbar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void Form1_FormClosingAsync(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (!finalClose)
                {
                    this.ControlBox = false;
                    pictureBoxLoader.BringToFront();
                    pictureBoxLoader.Visible = true;
                    e.Cancel = true;
                    await camControlService.MoveToPosition(Position.StandBy);
                }
            }
            catch (Exception exception) when (exception is HttpRequestException || exception is TaskCanceledException)
            {
                // The application must still close when the configured camera is offline.
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

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Add:
                    if (!keyZoomIsPressed)
                    {
                        keyZoomIsPressed = true;
                        camControlService.Zoom(Zoom.ZoomIn);
                    }
                    break;
                case Keys.Subtract:
                    if (!keyZoomIsPressed)
                    {
                        keyZoomIsPressed = true;
                        camControlService.Zoom(Zoom.ZoomOut);
                    }
                    break;
                case Keys.Left:
                    if (!keyDirectionIsPressed)
                    {
                        keyDirectionIsPressed = true;
                        camControlService.MoveInDirection(Direction.Left);
                    }
                    break;
                case Keys.Right:
                    if (!keyDirectionIsPressed)
                    {
                        keyDirectionIsPressed = true;
                        camControlService.MoveInDirection(Direction.Right);
                    }
                    break;
                case Keys.Up:
                    if (!keyDirectionIsPressed)
                    {
                        keyDirectionIsPressed = true;
                        camControlService.MoveInDirection(Direction.Up);
                    }
                    break;
                case Keys.Down:
                    if (!keyDirectionIsPressed)
                    {
                        keyDirectionIsPressed = true;
                        camControlService.MoveInDirection(Direction.Down);
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
                            camControlService.MoveInDirection(Direction.Left);
                        }
                        break;
                    case Keys.Right:
                        if (!keyDirectionIsPressed)
                        {
                            keyDirectionIsPressed = true;
                            camControlService.MoveInDirection(Direction.Right);
                        }
                        break;
                    case Keys.Up:
                        if (!keyDirectionIsPressed)
                        {
                            keyDirectionIsPressed = true;
                            camControlService.MoveInDirection(Direction.Up);
                        }
                        break;
                    case Keys.Down:
                        if (!keyDirectionIsPressed)
                        {
                            keyDirectionIsPressed = true;
                            camControlService.MoveInDirection(Direction.Down);
                        }
                        break;
                    default: break;

                }
                return true; // Mark the key as handled
            }

            // Call the base class method to handle other keys
            return base.ProcessCmdKey(ref msg, keyData);
        }



        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Add:
                case Keys.Subtract:
                    keyZoomIsPressed = false;
                    camControlService.StopZooming();
                    break;
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    keyDirectionIsPressed = false;
                    camControlService.StopDirectionalMovement();
                    break;
                default: break;
            }
        }



        #endregion

        #region Zoom

        private void buttonZoomOut_MouseDown(object sender, MouseEventArgs e)
        {
            camControlService.Zoom(Zoom.ZoomOut);
        }

        private void buttonZoomIn_MouseDown(object sender, MouseEventArgs e)
        {
            camControlService.Zoom(Zoom.ZoomIn);

        }

        private void button_MouseUpStopZooming(object sender, MouseEventArgs e)
        {
            camControlService.StopZooming();
        }

        #endregion





    }
}
