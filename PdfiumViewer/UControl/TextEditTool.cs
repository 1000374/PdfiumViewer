using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PdfiumViewer.UControl
{
    public partial class TextEditTool : Form
    {
        public delegate void DeleChangeTextFont(Color color, float size);
        public DeleChangeTextFont ChangeTextFontHandle;
        public TextEditTool()
        {
            InitializeComponent();
        }
        public void SetData(Color color, float size)
        {
            LbFontColor.BackColor = color;
            CbFontSize.Text = size.ToString();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            ChangeTextFontHandle?.Invoke(LbFontColor.BackColor, Convert.ToSingle(CbFontSize.Text));
        }
        private void btnEsc_Click(object sender, EventArgs e)
        {
            ChangeTextFontHandle?.Invoke(Color.Black, -1);
        }
        private void LbFontColor_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LbFontColor.BackColor = colorDialog.Color;
            }
        }
    }
}
