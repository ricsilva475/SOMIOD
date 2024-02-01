namespace FormSwitch
{
    partial class FormSwitch
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
            this.btnOn = new System.Windows.Forms.Button();
            this.btnOff = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnOn
            // 
            this.btnOn.Location = new System.Drawing.Point(12, 74);
            this.btnOn.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnOn.Name = "btnOn";
            this.btnOn.Size = new System.Drawing.Size(296, 57);
            this.btnOn.TabIndex = 0;
            this.btnOn.Text = "OPEN";
            this.btnOn.UseVisualStyleBackColor = true;
            this.btnOn.Click += new System.EventHandler(this.btnOn_Click);
            // 
            // btnOff
            // 
            this.btnOff.Location = new System.Drawing.Point(12, 183);
            this.btnOff.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnOff.Name = "btnOff";
            this.btnOff.Size = new System.Drawing.Size(296, 57);
            this.btnOff.TabIndex = 1;
            this.btnOff.Text = "CLOSED";
            this.btnOff.UseVisualStyleBackColor = true;
            this.btnOff.Click += new System.EventHandler(this.btnOff_Click);
            // 
            // FormSwitch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(320, 303);
            this.Controls.Add(this.btnOff);
            this.Controls.Add(this.btnOn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.Name = "FormSwitch";
            this.Text = "Switch";
            this.Load += new System.EventHandler(this.FormSwitch_Load);
            this.Shown += new System.EventHandler(this.FormSwitch_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOn;
        private System.Windows.Forms.Button btnOff;
    }
}
