using Pdfium.Net.Wrapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PdfiumViewer.UControl
{
    public enum TextBoxOperate
    {
        Enter,
        ChangeText
    }
    [ToolboxItem(true)]
    public class UcTextBox : TextBox
    {
        public delegate void DeleModifyTextFont(Color color, float size, TextBoxOperate operate);
        public DeleModifyTextFont ModifyTextFontHandler;
        private const int WM_PAINT = 0x000F;
        private const int WM_NCPAINT = 0x0085;

        private Color borderColor = Color.Black;
        [Description("边框颜色")]
        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
                base.Invalidate();
            }
        }

        private int borderWidth = 1;
        [Description("边框宽度")]
        public int BorderWidth
        {
            get { return borderWidth; }
            set
            {
                borderWidth = value;
                base.Invalidate();
            }
        }

        private PositionEnum borderPositionStyle;
        public PositionEnum BorderPositionStyle
        {
            get { return borderPositionStyle; }
            set
            {
                borderPositionStyle = value;
                base.Invalidate();
            }
        }
        public int Page { get; set; }
        public string OrgText { get; set; }

        public PdfPageobject PageObject { get; set; }

        public float FontSize { get; set; }

        public Color FontColor { get; set; }
        public RectangleF ObjBounds { get; set; }
        private TextEditTool textEdit;

        public UcTextBox()
        {
            BorderStyle = BorderStyle.FixedSingle;
            textEdit = new TextEditTool();
            textEdit.ChangeTextFontHandle += ChangeTextFontHandle;
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            SelectAll();
            var p = Parent.Location;
            var point = this.PointToScreen(p);
            SetData(point, this.FontColor, this.FontSize);
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                ModifyTextFontHandler?.Invoke(Color.Black, 0, TextBoxOperate.Enter);
            }
            else if (e.KeyData == Keys.Escape)
            {
                textEdit.Hide();
            }
            base.OnKeyUp(e);
        }
        private void SetData(Point point, Color color, float size)
        {
            textEdit.Hide();
            textEdit.Location = new Point(point.X, point.Y - textEdit.Height - 10);
            textEdit.SetData(color, size);
            textEdit.Show(this.Parent);
        }

        private void ChangeTextFontHandle(Color color, float size)
        {
            textEdit.Hide();
            if (size != -1)
                ModifyTextFontHandler?.Invoke(color, size, TextBoxOperate.ChangeText);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (this.BorderWidth % 2 == 0)
            {
                this.BorderWidth -= 1;
            }
            if (m.Msg == WM_PAINT || m.Msg == WM_NCPAINT)
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {

                    Pen pen = new Pen(this.BorderColor, BorderWidth);
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                    pen.DashPattern = new float[] { 5, 5 };

                    Pen backgroudPen = new Pen(this.BackColor, 1);
                    Pen tempPen = (borderPositionStyle & PositionEnum.Left) == PositionEnum.Left ? pen : backgroudPen;
                    g.DrawLine(tempPen, 0, 0, 0, Size.Height - 1);
                    tempPen = (borderPositionStyle & PositionEnum.Top) == PositionEnum.Top ? pen : backgroudPen;
                    g.DrawLine(tempPen, 0, 0, Size.Width - 1, 0);
                    tempPen = (borderPositionStyle & PositionEnum.Right) == PositionEnum.Right ? pen : backgroudPen;
                    g.DrawLine(tempPen, Size.Width - 1, 0, Size.Width - 1, Size.Height - 1);
                    tempPen = (borderPositionStyle & PositionEnum.Bottom) == PositionEnum.Bottom ? pen : backgroudPen;
                    g.DrawLine(tempPen, 0, Size.Height - 1, Size.Width - 1, Size.Height - 1);
                    pen.Dispose();
                    backgroudPen.Dispose();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            textEdit.ChangeTextFontHandle -= ChangeTextFontHandle;
            textEdit.Dispose();
            textEdit= null;
            PageObject = null;
            base.Dispose(disposing);
        }
    }
    [Flags]
    public enum PositionEnum
    {
        None = 0x00000000,
        Left = 0x00000001,
        Top = 0x00000010,
        Right = 0x00000100,
        Bottom = 0x00001000,
        All = 0x00001111,
    }
}
