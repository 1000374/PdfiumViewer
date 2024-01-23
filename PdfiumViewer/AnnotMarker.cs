using Pdfium.Net;
using Pdfium.Net.Wrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using static System.Windows.Forms.LinkLabel;

namespace PdfiumViewer
{
    public class AnnotMarker : IPdfMarker
    {
        public int Page { get; }
        public AnnotData Annot { get; }
        public RectangleF Bounds { get; }
        public double Zoom { get; }
        public AnnotMarker(int page, AnnotData annot, RectangleF bounds, double zoom)
        {
            Page = page;
            Annot = annot;
            Bounds = bounds;
            Zoom = zoom;
        }

        public void Draw(PdfRenderer renderer, Graphics graphics)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            var author = Annot.Author;
            var content = Annot.Content;
            var fontSize = (Annot.FontSize > 6 ? 6f : Annot.FontSize) * Zoom;
            Font font;
            if (!string.IsNullOrEmpty(Annot.FontName))
                font = new Font(Annot.FontName, (float)fontSize);
            else
            {
                font = new Font("Microsoft YaHei", 12);
            }
            var color = Annot.BackColor.ToArgb() == 0 ? Color.LightYellow : Annot.BackColor;
            var cloum = 50;
            string[] authors = SplitByLenth(author, cloum);
            string[] contents = SplitByLenth(content, cloum);
            float width = 0;
            float height = 0;
            var list = new List<SizeInfo>();
            foreach (var item in authors)
            {
                var size = graphics.MeasureString(item, font);
                list.Add(new SizeInfo(size, item));
                if (width < size.Width)
                    width = size.Width;
                height += size.Height;
            }
            foreach (var item in contents)
            {
                var size = graphics.MeasureString(item, font);
                list.Add(new SizeInfo(size, item));
                if (width < size.Width)
                    width = size.Width;
                height += size.Height;
            }

            var boundsF = new RectangleF(Bounds.X, Bounds.Y + Bounds.Height - height, width, height);
            var bounds = renderer.BoundsFromPdf(new PdfRectangle(Page, boundsF));
            bounds.Width = (int)width + 10;
            bounds.Height = (int)height + 10;
            using (var brush = new SolidBrush(color))
            {
                graphics.FillRectangle(brush, bounds);
            }
            var ly = bounds.Location.Y + 5F;
            foreach (var item in list)
            {
                graphics.DrawString(item.context, font, Brushes.Black, new Point(bounds.Location.X + 5, (int)ly));
                ly += item.size.Height;
            }
            font.Dispose();
            //if (BorderWidth > 0)
            //{
            //    using (var pen = new Pen(BorderColor, BorderWidth))
            //    {
            //        graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            //    }
            //}
        }
        private string[] SplitByLenth(string text, int length)
        {
            if (string.IsNullOrEmpty(text))
                return new string[0];
            int paragraphCount = (int)Math.Ceiling(((double)(text.Length) / length));
            string[] paragraphs = new string[paragraphCount];
            for (int i = 0; i < paragraphs.Length; i++)
            {
                paragraphs[i] = text.Substring(i * length, (text.Length - i * length > length ? length : text.Length - i * length));
            }
            return (paragraphs);
        }
        private class SizeInfo
        {
            public SizeF size { get; set; }
            public string context { get; set; }
            public SizeInfo(SizeF size, string context)
            {
                this.size = size; this.context = context;
            }
        }
    }
}
