namespace CamControl
{
    public partial class Form1 : Form
    {
        CamControlService camControlService;
        bool finalClose = false;

        public Form1()
        {
            InitializeComponent();
            camControlService = new CamControlService();
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
                pictureBoxLoaderSmall.Visible = true;
                buttonStandBy.Enabled = false;
                await camControlService.MoveToPosition(Position.StandBy);
            }
            finally
            {
                buttonStandBy.Enabled = true;
                pictureBoxLoaderSmall.Visible = false;
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
            await camControlService.MoveToPosition(Position.BuehneKomplett);
        }

        private async void Form1_FormClosingAsync(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (!finalClose)
                {
                    this.ControlBox = false;
                    pictureBoxLoader.Visible = true;
                    e.Cancel = true;
                    await camControlService.MoveToPosition(Position.StandBy);
                }
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