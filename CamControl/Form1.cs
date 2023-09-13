namespace CamControl
{
    public partial class Form1 : Form
    {
        HttpClient client;

        public Form1()
        {
            InitializeComponent();
            client = new HttpClient();
        }

        private void buttonRedner_Click(object sender, EventArgs e)
        {
            client.GetAsync("http://192.168.188.64/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&1");
        }

        private void buttonRednerLeser_Click(object sender, EventArgs e)
        {
            client.GetAsync("http://192.168.188.64/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&2");
        }

        private void buttonTisch_Click(object sender, EventArgs e)
        {
            client.GetAsync("http://192.168.188.64/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&3");
        }

        private void buttonKompletteBuehne_Click(object sender, EventArgs e)
        {
            client.GetAsync("http://192.168.188.64/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&4");
        }

        private void buttonRednerBildschirm_Click(object sender, EventArgs e)
        {
            client.GetAsync("http://192.168.188.64/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&5");
        }

        private void buttonStandBy_Click(object sender, EventArgs e)
        {
            client.GetAsync("http://192.168.188.64/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&0");
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '0':
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
                default: break;
            }

        }

        private void buttonUp_MouseDown(object sender, MouseEventArgs e)
        {
            MessageBox.Show("mousedown");

        }

        private void buttonUp_MouseUp(object sender, MouseEventArgs e)
        {
            MessageBox.Show("mouseUp");

        }

        private void buttonDown_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void buttonDown_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void buttonLeft_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void buttonLeft_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void buttonRight_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void buttonRight_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}