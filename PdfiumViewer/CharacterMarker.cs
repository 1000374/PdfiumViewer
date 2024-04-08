using Pdfium.Net.Native.Enums;
using Pdfium.Net.Wrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PdfiumViewer
{
    internal class CharacterMarker : IPdfMarker
    {
        public int Page { get; }
        public List<ObjectInformation> ObjectInformation { get => _objectInformation; }

        private List<ObjectInformation> _objectInformation;
        public CharacterMarker(int page, List<ObjectInformation> objectInformation)
        {
            Page = page;
            _objectInformation = objectInformation;
        }

        public void Draw(PdfRenderer renderer, Graphics graphics)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            using (var pen = new Pen(Color.LightGray, 1))
            using (var penHighlight = new Pen(SystemColors.Highlight, 1))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                pen.DashPattern = new float[] { 5, 5 };

                penHighlight.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                penHighlight.DashPattern = new float[] { 5, 5 };
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                DrawLine(renderer, graphics, ObjectInformation, pen, penHighlight);
            }
        }

        private void TransformRect(Rectangle bounds, PdfRenderer renderer, Graphics graphics, ObjectInformation info, Pen penHighlight)
        {
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;
            var direct = renderer.PointFromPdf(new PdfPoint(Page, info.DirectionEnd));
            var endx = direct.X;
            var endY = direct.Y;
            graphics.DrawRectangle(penHighlight, new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height));
            graphics.DrawLine(penHighlight, centerX, centerY, endx, endY);
            graphics.TranslateTransform(centerX, centerY);
            graphics.RotateTransform(-info.Direction);
            graphics.TranslateTransform(-centerX, -centerY);
            using (var penmovelight = new Pen(SystemColors.Highlight, 1))
            {
                graphics.DrawRectangle(penmovelight, new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height));
            }
            graphics.ResetTransform();
            graphics.Save();
        }

        private void DrawControlPoints(Rectangle bounds, Graphics graphics, bool all = true)
        {
            if (all)
            {
                //Draw a rectangle
                using (Pen pen1 = new Pen(SystemColors.GradientActiveCaption))
                {
                    graphics.DrawRectangle(pen1, new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height));

                }

                //Draw control points
                for (int i = 1; i < 10; i++)
                {
                    graphics.FillRectangle(new SolidBrush(SystemColors.GradientActiveCaption), CharacterHelper.GetHandleRect(i, bounds));
                }
            }
            else
            {
                //Draw a rectangle
                using (Pen pen1 = new Pen(SystemColors.GradientActiveCaption))
                {
                    graphics.DrawRectangle(pen1, new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height));
                }
                //Draw control points
                graphics.FillRectangle(new SolidBrush(SystemColors.GradientActiveCaption), CharacterHelper.GetHandleRect(9, bounds));
            }
        }

        private void DrawLine(PdfRenderer renderer, Graphics graphics, List<ObjectInformation> objects, Pen pen, Pen penHighlight)
        {
            objects.ForEach(info =>
            {
                switch (info.PageObjType)
                {
                    case PdfPageObjType.UnKnown:
                        {

                        }
                        break;
                    case PdfPageObjType.Text:
                        {
                            var bounds = renderer.BoundsFromPdf(new PdfRectangle(Page, info.ObjBounds));
                            if (!info.Highlight && !info.IsSelect)
                            {
                                graphics.DrawRectangle(pen, bounds);
                            }
                            else if (info.Highlight && !info.IsSelect)
                            {
                                graphics.DrawRectangle(penHighlight, bounds);
                            }
                            else
                            {

                                if (info.ObjRectangle != Rectangle.Empty && info.ObjMoveRectangle != Rectangle.Empty)
                                {
                                    graphics.DrawRectangle(penHighlight, info.ObjRectangle);
                                    using (var penmovelight = new Pen(SystemColors.Highlight, 1))
                                    {
                                        graphics.DrawRectangle(penmovelight, info.ObjMoveRectangle);
                                    }
                                }
                                else
                                {
                                    if (info.Direction != -1)
                                    {
                                        TransformRect(bounds, renderer, graphics, info, penHighlight);
                                    }
                                    else
                                    {
                                        DrawControlPoints(bounds, graphics, false);
                                    }
                                }
                            }
                        }
                        break;
                    case PdfPageObjType.Path:
                        {
                            var bounds = renderer.BoundsFromPdf(new PdfRectangle(Page, info.ObjBounds));
                            if (!info.Highlight && !info.IsSelect)
                            {
                                return;
                            }
                            else if (info.Highlight && !info.IsSelect)
                            {
                                graphics.DrawRectangle(penHighlight, bounds);
                            }
                            else
                            {

                                if (info.ObjRectangle != Rectangle.Empty && info.ObjMoveRectangle != Rectangle.Empty)
                                {
                                    graphics.DrawRectangle(penHighlight, info.ObjRectangle);
                                    using (var penmovelight = new Pen(SystemColors.Highlight, 1))
                                    {
                                        graphics.DrawRectangle(penmovelight, info.ObjMoveRectangle);
                                    }
                                }
                                else
                                {
                                    if (info.Direction != -1)
                                    {
                                        TransformRect(bounds, renderer, graphics, info, penHighlight);
                                    }
                                    else
                                    {
                                        DrawControlPoints(bounds, graphics, false);
                                    }
                                }
                            }
                        }
                        break;
                    case PdfPageObjType.Image:
                        {
                            var bounds = renderer.BoundsFromPdf(new PdfRectangle(Page, info.ObjBounds));
                            if (info.Highlight && !info.IsSelect)
                            {
                                graphics.DrawRectangle(penHighlight, bounds);
                            }
                            else if (info.IsSelect)
                            {
                                if (info.ObjRectangle != Rectangle.Empty && info.ObjMoveRectangle != Rectangle.Empty)
                                {
                                    graphics.DrawRectangle(penHighlight, info.ObjRectangle);
                                    using (var penmovelight = new Pen(SystemColors.Highlight, 1))
                                    {
                                        graphics.DrawRectangle(penmovelight, info.ObjMoveRectangle);
                                    }
                                }
                                else
                                {
                                    if (info.Direction != -1)
                                    {
                                        TransformRect(bounds, renderer, graphics, info, penHighlight);
                                    }
                                    else
                                    {
                                        DrawControlPoints(bounds, graphics);
                                    }
                                }
                            }
                            //else
                            //{
                            //    graphics.DrawRectangle(pen, bounds);
                            //}
                        }
                        break;
                    case PdfPageObjType.Shading:
                        {

                        }
                        break;
                    case PdfPageObjType.Form:
                        {

                        }
                        break;
                }
                if (info.SubObjectInformations != null && info.SubObjectInformations.Count > 0)
                {
                    DrawLine(renderer, graphics, info.SubObjectInformations, pen, penHighlight);
                }
            });
        }
    }

    internal class CharacterHelper
    {
        /// <summary>
        /// Get whether the current mouse is in the control handle (1-8), inside the rectangle (0), or not on the rectangle (-1)
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static int GetSelectedHandle(Point p, RectangleF rectangle)
        {
            int index = -1;
            for (int i = 1; i < 9; i++)
            {
                if (GetHandleRect(i, rectangle).Contains(p))
                {
                    index = i;
                    break;
                }
            }
            if (index == -1 && rectangle.Contains(p))
            {
                var rect = CharacterHelper.GetHandleRect(9, rectangle);
                rect.Width = rect.Width * 2;
                rect.Height = rect.Height * 2;
                rect.Offset(-rect.Width / 2 + 3f, -rect.Height / 2 + 3f);
                if (rect.Contains(p))
                {
                    index = 9;
                }
                else
                {
                    index = 0;
                }
            }
            return index;
        }

        /// <summary>
        /// get rectangular range of control points
        /// </summary>
        public static RectangleF GetHandleRect(int index, RectangleF rectangle)
        {
            PointF point = GetHandle(index, rectangle);
            return new RectangleF(point.X - 3, point.Y - 3, 7, 7);
        }

        /// <summary>
        /// Find each point of the rectangle based on the index, clockwise direction
        /// </summary>
        private static PointF GetHandle(int index, RectangleF rectangle)
        {
            float x, y, xCenter, yCenter;

            x = rectangle.X;
            y = rectangle.Y;
            xCenter = rectangle.X + rectangle.Width / 2;
            yCenter = rectangle.Y + rectangle.Height / 2;

            switch (index)
            {
                case 1:
                    x = rectangle.X;
                    y = rectangle.Y;
                    break;
                case 2:
                    x = xCenter;
                    y = rectangle.Y;
                    break;
                case 3:
                    x = rectangle.Right;
                    y = rectangle.Y;
                    break;
                case 4:
                    x = rectangle.Right;
                    y = yCenter;
                    break;
                case 5:
                    x = rectangle.Right;
                    y = rectangle.Bottom;
                    break;
                case 6:
                    x = xCenter;
                    y = rectangle.Bottom;
                    break;
                case 7:
                    x = rectangle.X;
                    y = rectangle.Bottom;
                    break;
                case 8:
                    x = rectangle.X;
                    y = yCenter;
                    break;
                case 9:
                    x = xCenter;
                    y = yCenter;
                    break;
            }
            return new PointF(x, y);
        }


        public static float Distance(float deltaX, float deltaY)
        {
            return (float)Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
        }
        public static float Direction(float deltaX, float deltaY)
        {
            if (deltaX > 0 && deltaY >= 0)
            {
                return (float)(Math.Atan(deltaY / deltaX) * 180 / Math.PI);
            }
            else if (deltaX < 0 && deltaY >= 0)
            {
                return (float)(Math.Atan(deltaY / deltaX) * 180 / Math.PI + 180);
            }
            else if (deltaX < 0 && deltaY <= 0)
            {
                return (float)(Math.Atan(deltaY / deltaX) * 180 / Math.PI + 180);
            }
            else if (deltaX > 0 && deltaY <= 0)
            {
                return (float)(Math.Atan(deltaY / deltaX) * 180 / Math.PI + 360);
            }
            else if (deltaX == 0 && deltaY > 0)
            {
                return 90;
            }
            else if (deltaX == 0 && deltaY < 0)
            {
                return 270;
            }
            else
            {
                return -1;
            }
        }
    }
}
