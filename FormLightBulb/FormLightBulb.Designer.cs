namespace FormLightBulb
{
    partial class FormLightBulb
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pbDoor = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbDoor)).BeginInit();
            this.SuspendLayout();
            // 
            // pbDoor
            // 
            this.pbDoor.Image = global::FormLightBulb.Properties.Resources.porta_fechada;
            this.pbDoor.Location = new System.Drawing.Point(13, 15);
            this.pbDoor.Margin = new System.Windows.Forms.Padding(4);
            this.pbDoor.Name = "pbDoor";
            this.pbDoor.Size = new System.Drawing.Size(302, 285);
            this.pbDoor.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbDoor.TabIndex = 1;
            this.pbDoor.TabStop = false;
            this.pbDoor.Click += new System.EventHandler(this.pbLightbulb_Click);
            // 
            // FormLightBulb
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(328, 313);
            this.Controls.Add(this.pbDoor);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormLightBulb";
            this.Text = "Light Bulb";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormLightBulb_FormClosing);
            this.Load += new System.EventHandler(this.FormLightBulb_Load);
            this.Shown += new System.EventHandler(this.FormLightBulb_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pbDoor)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbDoor;
    }
}
