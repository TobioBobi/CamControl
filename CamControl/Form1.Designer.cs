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
            SuspendLayout();
            // 
            // buttonRedner
            // 
            buttonRedner.Location = new Point(189, 12);
            buttonRedner.Name = "buttonRedner";
            buttonRedner.Size = new Size(171, 86);
            buttonRedner.TabIndex = 0;
            buttonRedner.Text = "Redner (1)";
            buttonRedner.UseVisualStyleBackColor = true;
            buttonRedner.Click += buttonRedner_Click;
            // 
            // buttonRednerLeser
            // 
            buttonRednerLeser.Location = new Point(366, 12);
            buttonRednerLeser.Name = "buttonRednerLeser";
            buttonRednerLeser.Size = new Size(171, 86);
            buttonRednerLeser.TabIndex = 1;
            buttonRednerLeser.Text = "Redner + Leser (2)";
            buttonRednerLeser.UseVisualStyleBackColor = true;
            buttonRednerLeser.Click += buttonRednerLeser_Click;
            // 
            // buttonTisch
            // 
            buttonTisch.Location = new Point(12, 104);
            buttonTisch.Name = "buttonTisch";
            buttonTisch.Size = new Size(171, 86);
            buttonTisch.TabIndex = 2;
            buttonTisch.Text = "Tisch (3)";
            buttonTisch.UseVisualStyleBackColor = true;
            buttonTisch.Click += buttonTisch_Click;
            // 
            // buttonKompletteBuehne
            // 
            buttonKompletteBuehne.Location = new Point(189, 104);
            buttonKompletteBuehne.Name = "buttonKompletteBuehne";
            buttonKompletteBuehne.Size = new Size(171, 86);
            buttonKompletteBuehne.TabIndex = 3;
            buttonKompletteBuehne.Text = "komplette Bühne (4)";
            buttonKompletteBuehne.UseVisualStyleBackColor = true;
            buttonKompletteBuehne.Click += buttonKompletteBuehne_Click;
            // 
            // buttonRednerBildschirm
            // 
            buttonRednerBildschirm.Location = new Point(366, 104);
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
            buttonUp.MouseUp += buttonUp_MouseUp;
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
            buttonDown.MouseUp += buttonDown_MouseUp;
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
            buttonLeft.MouseUp += buttonLeft_MouseUp;
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
            buttonRight.MouseUp += buttonRight_MouseUp;
            // 
            // buttonStandBy
            // 
            buttonStandBy.Location = new Point(12, 12);
            buttonStandBy.Name = "buttonStandBy";
            buttonStandBy.Size = new Size(171, 86);
            buttonStandBy.TabIndex = 9;
            buttonStandBy.Text = "Standby (0)";
            buttonStandBy.UseVisualStyleBackColor = true;
            buttonStandBy.Click += buttonStandBy_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(791, 207);
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
            Load += Form1_Load;
            KeyPress += Form1_KeyPress;
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
    }
}