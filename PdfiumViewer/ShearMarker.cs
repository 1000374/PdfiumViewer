using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace PdfiumViewer
{
    internal class ShearMarker : IPdfMarker
    {
        private Rectangle _currRect;
        public int Page { get; }
        public Rectangle Rectangle { get => _currRect; set => _currRect = value; }
        public Rectangle MoveRectangle { get; set; }
        public ShearMarker(int page, Rectangle currRect)
        {
            Page = page;
            _currRect = currRect;
        }
        public void Draw(PdfRenderer renderer, Graphics graphics)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            using (var pen = new Pen(SystemColors.GradientActiveCaption, 1))
            using (var penHighlight = new Pen(SystemColors.Highlight, 1))
            {
                var rect = renderer.BoundsFromPdf(new Pdfium.Net.Wrapper.PdfRectangle(Page, _currRect));

                if (MoveRectangle != Rectangle.Empty)
                {
                    var moveRect = renderer.BoundsFromPdf(new Pdfium.Net.Wrapper.PdfRectangle(Page, MoveRectangle));
                    graphics.DrawRectangle(penHighlight, moveRect);
                }

                //Draw a rectangle
                using (Pen pen1 = new Pen(SystemColors.GradientActiveCaption))
                {
                    graphics.DrawRectangle(pen1, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
                }
                //Draw control points
                for (int i = 1; i < 9; i++)
                {
                    graphics.FillRectangle(new SolidBrush(SystemColors.GradientActiveCaption), CharacterHelper.GetHandleRect(i, rect));
                }
                DrawInfoPanel(graphics, rect);
            }
        }
        /// <summary>
        /// Draw Panel Information
        /// </summary>
        private void DrawInfoPanel(Graphics g, Rectangle rect)
        {
            int x = ((rect.X + 102) > rect.Width) ? (rect.X - 102) : rect.X + 2;
            int y = ((rect.Y - 38) > 0) ? (rect.Y - 38) : rect.Y + 2;
            string zone = "pixel:" + Math.Abs(rect.Width).ToString() + " x " + Math.Abs(rect.Height).ToString();
            string mm = "mm:" + Math.Round(Math.Abs(rect.Width) / 3.78, 0) + " x " + Math.Round(Math.Abs(rect.Height) / 3.78, 0);

            using (var infoPanelPic = new Bitmap(100, 36))
            using (var graphics = Graphics.FromImage(infoPanelPic))
            using (var font = new Font("Microsoft YaHei", 9f))
            {
                graphics.DrawString(zone, font, Brushes.Blue, new PointF(7, 4));
                graphics.DrawString(mm, font, Brushes.Blue, new PointF(7, 20));
                g.DrawImageUnscaled(infoPanelPic, x, y);
            }
        }
    }
}
