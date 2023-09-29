namespace CamControl
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonRedner = new Button();
            buttonRednerLeser = new Button();
            buttonTisch = new Button();
            buttonKompletteBuehne = new Button();
            buttonRednerBildschirm = new Button();
            buttonUp = new Button();
            buttonDown = new Button();
            buttonLeft = new Button();
            buttonRight = new Button();
            buttonStandBy = new Button();
            buttonZoomIn = new Button();
            buttonZoomOut = new Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            buttonDemoStehend = new Button();
            pictureBoxLoader = new PictureBox();
            pictureBoxLoaderSmall = new PictureBox();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLoader).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLoaderSmall).BeginInit();
            SuspendLayout();
            // 
            // buttonRedner
            // 
            buttonRedner.Location = new Point(12, 12);
            buttonRedner.Name = "buttonRedner";
            buttonRedner.Size = new Size(171, 86);
            buttonRedner.TabIndex = 0;
            buttonRedner.Text = "Redner (1)";
            buttonRedner.UseVisualStyleBackColor = true;
            buttonRedner.Click += buttonRedner_Click;
            // 
            // buttonRednerLeser
            // 
            buttonRednerLeser.Location = new Point(189, 12);
            buttonRednerLeser.Name = "buttonRednerLeser";
            buttonRednerLeser.Size = new Size(171, 86);
            buttonRednerLeser.TabIndex = 1;
            buttonRednerLeser.Text = "Redner + Leser (2)";
            buttonRednerLeser.UseVisualStyleBackColor = true;
            buttonRednerLeser.Click += buttonRednerLeser_Click;
            // 
            // buttonTisch
            // 
            buttonTisch.Location = new Point(366, 12);
            buttonTisch.Name = "buttonTisch";
            buttonTisch.Size = new Size(171, 86);
            buttonTisch.TabIndex = 2;
            buttonTisch.Text = "Tisch (3)";
            buttonTisch.UseVisualStyleBackColor = true;
            buttonTisch.Click += buttonTisch_Click;
            // 
            // buttonKompletteBuehne
            // 
            buttonKompletteBuehne.Location = new Point(12, 104);
            buttonKompletteBuehne.Name = "buttonKompletteBuehne";
            buttonKompletteBuehne.Size = new Size(171, 86);
            buttonKompletteBuehne.TabIndex = 3;
            buttonKompletteBuehne.Text = "komplette Bühne (4)";
            buttonKompletteBuehne.UseVisualStyleBackColor = true;
            buttonKompletteBuehne.Click += buttonKompletteBuehne_Click;
            // 
            // buttonRednerBildschirm
            // 
            buttonRednerBildschirm.Location = new Point(189, 104);
            buttonRednerBildschirm.Name = "buttonRednerBildschirm";
            buttonRednerBildschirm.Size = new Size(171, 86);
            buttonRednerBildschirm.TabIndex = 4;
            buttonRednerBildschirm.Text = "Redner + Bildschirm (5)";
            buttonRednerBildschirm.UseVisualStyleBackColor = true;
            buttonRednerBildschirm.Click += buttonRednerBildschirm_Click;
            // 
            // buttonUp
            // 
            buttonUp.Image = Properties.Resources.GlyphUp_16x;
            buttonUp.Location = new Point(608, 12);
            buttonUp.Name = "buttonUp";
            buttonUp.Size = new Size(86, 46);
            buttonUp.TabIndex = 5;
            buttonUp.UseVisualStyleBackColor = true;
            buttonUp.MouseDown += buttonUp_MouseDown;
            buttonUp.MouseUp += button_MouseUpStopDirectionalMovement;
            // 
            // buttonDown
            // 
            buttonDown.Image = Properties.Resources.GlyphDown_16x;
            buttonDown.Location = new Point(608, 64);
            buttonDown.Name = "buttonDown";
            buttonDown.Size = new Size(86, 46);
            buttonDown.TabIndex = 6;
            buttonDown.UseVisualStyleBackColor = true;
            buttonDown.MouseDown += buttonDown_MouseDown;
            buttonDown.MouseUp += button_MouseUpStopDirectionalMovement;
            // 
            // buttonLeft
            // 
            buttonLeft.Image = Properties.Resources.GlyphLeft_16x;
            buttonLeft.Location = new Point(556, 12);
            buttonLeft.Name = "buttonLeft";
            buttonLeft.Size = new Size(46, 98);
            buttonLeft.TabIndex = 7;
            buttonLeft.UseVisualStyleBackColor = true;
            buttonLeft.MouseDown += buttonLeft_MouseDown;
            buttonLeft.MouseUp += button_MouseUpStopDirectionalMovement;
            // 
            // buttonRight
            // 
            buttonRight.Image = Properties.Resources.GlyphRight_16x;
            buttonRight.Location = new Point(700, 12);
            buttonRight.Name = "buttonRight";
            buttonRight.Size = new Size(46, 98);
            buttonRight.TabIndex = 8;
            buttonRight.UseVisualStyleBackColor = true;
            buttonRight.MouseDown += buttonRight_MouseDown;
            buttonRight.MouseUp += button_MouseUpStopDirectionalMovement;
            // 
            // buttonStandBy
            // 
            buttonStandBy.Location = new Point(189, 196);
            buttonStandBy.Name = "buttonStandBy";
            buttonStandBy.Size = new Size(171, 86);
            buttonStandBy.TabIndex = 9;
            buttonStandBy.Text = "Standby (9)";
            buttonStandBy.UseVisualStyleBackColor = true;
            buttonStandBy.Click += buttonStandBy_Click;
            // 
            // buttonZoomIn
            // 
            buttonZoomIn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buttonZoomIn.Location = new Point(3, 3);
            buttonZoomIn.Name = "buttonZoomIn";
            buttonZoomIn.Size = new Size(89, 68);
            buttonZoomIn.TabIndex = 10;
            buttonZoomIn.Text = "Zoom In";
            buttonZoomIn.UseVisualStyleBackColor = true;
            buttonZoomIn.MouseDown += buttonZoomIn_MouseDown;
            buttonZoomIn.MouseUp += button_MouseUpStopZooming;
            // 
            // buttonZoomOut
            // 
            buttonZoomOut.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buttonZoomOut.Location = new Point(98, 3);
            buttonZoomOut.Name = "buttonZoomOut";
            buttonZoomOut.Size = new Size(89, 68);
            buttonZoomOut.TabIndex = 11;
            buttonZoomOut.Text = "Zoom Out";
            buttonZoomOut.UseVisualStyleBackColor = true;
            buttonZoomOut.MouseDown += buttonZoomOut_MouseDown;
            buttonZoomOut.MouseUp += button_MouseUpStopZooming;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(buttonZoomIn, 0, 0);
            tableLayoutPanel1.Controls.Add(buttonZoomOut, 1, 0);
            tableLayoutPanel1.Location = new Point(556, 116);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(190, 74);
            tableLayoutPanel1.TabIndex = 13;
            // 
            // buttonDemoStehend
            // 
            buttonDemoStehend.Location = new Point(366, 104);
            buttonDemoStehend.Name = "buttonDemoStehend";
            buttonDemoStehend.Size = new Size(171, 86);
            buttonDemoStehend.TabIndex = 14;
            buttonDemoStehend.Text = "Demo stehend (6)";
            buttonDemoStehend.UseVisualStyleBackColor = true;
            buttonDemoStehend.Click += buttonDemoStehend_Click;
            // 
            // pictureBoxLoader
            // 
            pictureBoxLoader.Image = Properties.Resources.loader;
            pictureBoxLoader.Location = new Point(12, 12);
            pictureBoxLoader.Name = "pictureBoxLoader";
            pictureBoxLoader.Size = new Size(734, 272);
            pictureBoxLoader.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLoader.TabIndex = 15;
            pictureBoxLoader.TabStop = false;
            pictureBoxLoader.Visible = false;
            // 
            // pictureBoxLoaderSmall
            // 
            pictureBoxLoaderSmall.Image = Properties.Resources.loader;
            pictureBoxLoaderSmall.Location = new Point(189, 196);
            pictureBoxLoaderSmall.Name = "pictureBoxLoaderSmall";
            pictureBoxLoaderSmall.Size = new Size(171, 86);
            pictureBoxLoaderSmall.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLoaderSmall.TabIndex = 16;
            pictureBoxLoaderSmall.TabStop = false;
            pictureBoxLoaderSmall.Visible = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(757, 296);
            Controls.Add(pictureBoxLoaderSmall);
            Controls.Add(pictureBoxLoader);
            Controls.Add(buttonDemoStehend);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(buttonStandBy);
            Controls.Add(buttonRight);
            Controls.Add(buttonLeft);
            Controls.Add(buttonDown);
            Controls.Add(buttonUp);
            Controls.Add(buttonRednerBildschirm);
            Controls.Add(buttonKompletteBuehne);
            Controls.Add(buttonTisch);
            Controls.Add(buttonRednerLeser);
            Controls.Add(buttonRedner);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            KeyPreview = true;
            Name = "Form1";
            Text = "CamControl";
            FormClosing += Form1_FormClosingAsync;
            Load += Form1_Load;
            KeyPress += Form1_KeyPress;
            tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxLoader).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLoaderSmall).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button buttonRedner;
        private Button buttonRednerLeser;
        private Button buttonTisch;
        private Button buttonKompletteBuehne;
        private Button buttonRednerBildschirm;
        private Button buttonUp;
        private Button buttonDown;
        private Button buttonLeft;
        private Button buttonRight;
        private Button buttonStandBy;
        private Button buttonZoomIn;
        private Button buttonZoomOut;
        private TableLayoutPanel tableLayoutPanel1;
        private Button buttonDemoStehend;
        private PictureBox pictureBoxLoader;
        private PictureBox pictureBoxLoaderSmall;
    }
}