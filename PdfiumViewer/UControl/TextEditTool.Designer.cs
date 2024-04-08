namespace PdfiumViewer.UControl
{
    partial class TextEditTool
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
            if (disposing && (components != null))
            {
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
            this.btnOk = new System.Windows.Forms.Button();
            this.LbFontColor = new System.Windows.Forms.Label();
            this.CbFontSize = new System.Windows.Forms.ComboBox();
            this.btnEsc = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(126, 10);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(35, 23);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // LbFontColor
            // 
            this.LbFontColor.BackColor = System.Drawing.Color.Black;
            this.LbFontColor.Location = new System.Drawing.Point(98, 10);
            this.LbFontColor.Name = "LbFontColor";
            this.LbFontColor.Size = new System.Drawing.Size(22, 23);
            this.LbFontColor.TabIndex = 4;
            this.LbFontColor.Click += new System.EventHandler(this.LbFontColor_Click);
            // 
            // CbFontSize
            // 
            this.CbFontSize.FormattingEnabled = true;
            this.CbFontSize.Items.AddRange(new object[] {
            "5",
            "5.5",
            "6.5",
            "7.5",
            "8",
            "9",
            "10",
            "10.5",
            "11",
            "12",
            "14",
            "16",
            "18",
            "20",
            "22",
            "24",
            "26",
            "28",
            "34",
            "36",
            "48",
            "72"});
            this.CbFontSize.Location = new System.Drawing.Point(12, 12);
            this.CbFontSize.Name = "CbFontSize";
            this.CbFontSize.Size = new System.Drawing.Size(78, 20);
            this.CbFontSize.TabIndex = 3;
            // 
            // btnEsc
            // 
            this.btnEsc.Location = new System.Drawing.Point(167, 10);
            this.btnEsc.Name = "btnEsc";
            this.btnEsc.Size = new System.Drawing.Size(35, 23);
            this.btnEsc.TabIndex = 6;
            this.btnEsc.Text = "Esc";
            this.btnEsc.UseVisualStyleBackColor = true;
            this.btnEsc.Click += new System.EventHandler(this.btnEsc_Click);
            // 
            // TextEditTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(208, 42);
            this.ControlBox = false;
            this.Controls.Add(this.btnEsc);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.LbFontColor);
            this.Controls.Add(this.CbFontSize);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "TextEditTool";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "TextEditTool";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label LbFontColor;
        private System.Windows.Forms.ComboBox CbFontSize;
        private System.Windows.Forms.Button btnEsc;
    }
}