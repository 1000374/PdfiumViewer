using Pdfium.Net;
using Pdfium.Net.Native.Enums;
using Pdfium.Net.Wrapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PdfiumViewer.Demo
{
    public partial class MainForm : Form
    {
        private SearchForm _searchForm;
        private string fileName;
        List<int> list = new List<int>();
        private string outImagePath;
      
        public MainForm()
        {
            InitializeComponent();
         
            renderToBitmapsToolStripMenuItem.Enabled = false;
            pdfViewer1.Renderer.ContextMenuStrip = pdfViewerContextMenu;

            pdfViewer1.Renderer.DisplayRectangleChanged += Renderer_DisplayRectangleChanged;
            pdfViewer1.Renderer.ZoomChanged += Renderer_ZoomChanged;

            pdfViewer1.Renderer.MouseLeave += Renderer_MouseLeave;
            pdfViewer1.Renderer.PdfPointhandler += Renderer_PdfPointhandler;
            pdfViewer1.Renderer.BoundedTextHandler += Renderer_BoundedTextHandler;
            pdfViewer1.Renderer.BoundedCutHandler += Renderer_BoundedCutHandler;
            ShowPdfLocation(PdfPoint.Empty);

            cutMarginsWhenPrintingToolStripMenuItem.PerformClick();

            _zoom.Text = pdfViewer1.Renderer.Zoom.ToString();
        }

        private void Renderer_BoundedCutHandler(int ipage, Rectangle rectangle)
        {
            MessageBox.Show($"page:{ipage} rectangle:{rectangle.ToString()}");
            string path;
            using (var form = new FolderBrowserDialog())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                path = form.SelectedPath;
            }
            var page = pdfViewer1.Document.Pages[ipage];
            var captureDpiX = 0;
            var captureDpiY = 0;
            using (var image = pdfViewer1.Document.Render(
                     ipage,
                     (int)page.Width,
                     (int)page.Height,
                    rectangle.X, // x of the top/left of clipping rectangle
                    rectangle.Y, // y of the top/left point of clipping rectangle
                    rectangle.Width, // width of clipping reactangle
                    rectangle.Height, // height of clipping reactangle
                    captureDpiX,
                    captureDpiY,
                    PdfRotation.Rotate0, // no rotation
                    PdfRenderFlags.None // no render flags
                                        ))
            {
                image.Save(Path.Combine(path, "Page " + ipage + "Cut.png"));
            }
        }

        private void Renderer_PdfPointhandler(PdfPoint point)
        {
            ShowPdfLocation(point);
        }

        private void Renderer_BoundedTextHandler(int page, int x, int y, int x1, int y1, string txt, bool isEdit)
        {
            MessageBox.Show($"page:{page},x:{x},y:{y},x1:{x1},y1:{y1} Text:{txt}");
        }

        private void Renderer_MouseLeave(object sender, EventArgs e)
        {
            ShowPdfLocation(PdfPoint.Empty);
        }
        private void ShowPdfLocation(PdfPoint point)
        {
            if (!point.IsValid)
            {
                _pageToolStripLabel.Text = null;
                _coordinatesToolStripLabel.Text = null;
            }
            else
            {
                _pageToolStripLabel.Text = (point.Page + 1).ToString();
                _coordinatesToolStripLabel.Text = point.Location.X + "," + point.Location.Y;
            }
        }

        void Renderer_ZoomChanged(object sender, EventArgs e)
        {
            _zoom.Text = pdfViewer1.Renderer.Zoom.ToString();
        }

        void Renderer_DisplayRectangleChanged(object sender, EventArgs e)
        {
            _page.Text = (pdfViewer1.Renderer.Page + 1).ToString();
        }

        private PdfDocument OpenDocument(string fileName)
        {
            try
            {
                var stream = new FileStream(fileName, FileMode.Open);
                return PdfDocumentGdi.Load(this, stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void OpenFile()
        {
            using (var form = new OpenFileDialog())
            {
                form.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
                form.RestoreDirectory = true;
                form.Title = "Open PDF File";

                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                fileName = form.FileName;
                pdfViewer1?.Close();
                GC.Collect(GC.MaxGeneration);
                pdfViewer1.Document = OpenDocument(form.FileName);
                renderToBitmapsToolStripMenuItem.Enabled = true;
                _edit.Checked = false;
                _copy.Checked = false;
                _matching.Checked = false;
                _panningEnabled.Checked = false;
                var doc = pdfViewer1.Document;
                var fontPath = @"c:\Windows\fonts\simhei.ttf";
                doc?.LoadFont(fontPath, isVerification: true);
            }
        }

        private void FitPage(PdfViewerZoomMode zoomMode)
        {
            int page = pdfViewer1.Renderer.Page;
            pdfViewer1.ZoomMode = zoomMode;
            pdfViewer1.Renderer.Zoom = 1;
            pdfViewer1.Renderer.Page = page;
        }

        private void Rotate(PdfRotation rotate)
        {
            // PdfRenderer does not support changes to the loaded document,
            // so we fake it by reloading the document into the renderer.

            int page = pdfViewer1.Renderer.Page;
            var document = pdfViewer1.Document;
            document.RotatePage(page, rotate);
            pdfViewer1.Document = null;
            pdfViewer1.Document = document;
            pdfViewer1.Renderer.Page = page;
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void renderToBitmapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int dpiX;
            int dpiY;

            using (var form = new ExportBitmapsForm())
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;

                dpiX = form.DpiX;
                dpiY = form.DpiY;
            }

            string path;
            using (var form = new FolderBrowserDialog())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                path = form.SelectedPath;
            }
            outImagePath = path;
            var document = pdfViewer1.Document;

            for (int i = 0; i < document.PageCount; i++)
            {
                // 能展示签章 using (var image = document.Render(i, (int)document.PageSizes[i].Width * 4 / 3, (int)document.PageSizes[i].Height * 4 / 3, dpiX, dpiY, PdfRotation.Rotate0, PdfRenderFlags.Annotations))
                // 能根据dpi变化 using (var image = document.Render(i, (int)document.PageSizes[i].Width * 4 / 3, (int)document.PageSizes[i].Height * 4 / 3, dpiX, dpiY, PdfRotation.Rotate0, PdfRenderFlags.CorrectFromDpi))
                using (var image = document.Pages[i].Render((int)document.Pages[i].Width * 4 / 3, (int)document.Pages[i].Height * 4 / 3, dpiX, dpiY, PdfRotation.Rotate0, Pdfium.Net.Wrapper.PdfRenderFlags.Annotations | Pdfium.Net.Wrapper.PdfRenderFlags.CorrectFromDpi))// false))
                {
                    image.Save(Path.Combine(path, "Page " + i + ".png"));
                }
            }
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.Page--;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.Page++;
        }

        private void cutMarginsWhenPrintingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cutMarginsWhenPrintingToolStripMenuItem.Checked = true;
            shrinkToMarginsWhenPrintingToolStripMenuItem.Checked = false;

            pdfViewer1.DefaultPrintMode = PdfPrintMode.CutMargin;
        }

        private void shrinkToMarginsWhenPrintingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shrinkToMarginsWhenPrintingToolStripMenuItem.Checked = true;
            cutMarginsWhenPrintingToolStripMenuItem.Checked = false;

            pdfViewer1.DefaultPrintMode = PdfPrintMode.ShrinkToMargin;
        }

        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new PrintPreviewDialog())
            {
                form.Document = pdfViewer1.Document.CreatePrintDocument(pdfViewer1.DefaultPrintMode);
                form.ShowDialog(this);
            }
        }

        private void _fitWidth_Click(object sender, EventArgs e)
        {
            FitPage(PdfViewerZoomMode.FitWidth);
        }

        private void _fitHeight_Click(object sender, EventArgs e)
        {
            FitPage(PdfViewerZoomMode.FitHeight);
        }

        private void _fitBest_Click(object sender, EventArgs e)
        {
            FitPage(PdfViewerZoomMode.FitBest);
        }

        private void _page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;

                int page;
                if (int.TryParse(_page.Text, out page))
                    pdfViewer1.Renderer.Page = page - 1;
            }
        }

        private void _zoom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;

                float zoom;
                if (float.TryParse(_zoom.Text, out zoom))
                    pdfViewer1.Renderer.Zoom = zoom;
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.ZoomIn();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.ZoomOut();
        }

        private void _rotateLeft_Click(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.RotateLeft();
        }

        private void _rotateRight_Click(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.RotateRight();
        }

        private void _hideToolbar_Click(object sender, EventArgs e)
        {
            pdfViewer1.ShowToolbar = _showToolbar.Checked;
        }

        private void _hideBookmarks_Click(object sender, EventArgs e)
        {
            pdfViewer1.ShowBookmarks = _showBookmarks.Checked;
        }

        private void deleteCurrentPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // PdfRenderer does not support changes to the loaded document,
            // so we fake it by reloading the document into the renderer.

            int page = pdfViewer1.Renderer.Page;
            var document = pdfViewer1.Document;
            pdfViewer1.Document = null;
            document.DeletePage(page);
            pdfViewer1.Document = document;
            pdfViewer1.Renderer.Page = page;
        }

        private void rotate0ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rotate(PdfRotation.Rotate0);
        }

        private void rotate90ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rotate(PdfRotation.Rotate90);
        }

        private void rotate180ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rotate(PdfRotation.Rotate180);
        }

        private void rotate270ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rotate(PdfRotation.Rotate270);
        }
        private void getrotateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int page = pdfViewer1.Renderer.Page;
            var document = pdfViewer1.Document;
            var rotation = document.Pages[page].Rotation;
            MessageBox.Show(rotation.ToString());
        }
        private void showRangeOfPagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //using (var form = new PageRangeForm(pdfViewer1.Document))
            //{
            //    if (form.ShowDialog(this) == DialogResult.OK)
            //    {
            //        pdfViewer1.Document = form.Document;
            //    }
            //}
        }

        private void informationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Information info = pdfViewer1.Document.GetInformation();
            StringBuilder sz = new StringBuilder();
            sz.AppendLine($"Author: {info.Author}");
            sz.AppendLine($"Creator: {info.Creator}");
            sz.AppendLine($"Keywords: {info.Keywords}");
            sz.AppendLine($"Producer: {info.Producer}");
            sz.AppendLine($"Subject: {info.Subject}");
            sz.AppendLine($"Title: {info.Title}");
            sz.AppendLine($"Create Date: {info.CreationDate}");
            sz.AppendLine($"Modified Date: {info.ModificationDate}");
            sz.AppendLine($"FileVersion: {info.FileVersion}");
            MessageBox.Show(sz.ToString(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void pageInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int page = pdfViewer1.Renderer.Page;
            var info = pdfViewer1.Document?.Pages[page].GetCharacterInformation();
        }
        private void _getTextFromPage_Click(object sender, EventArgs e)
        {
            int page = pdfViewer1.Renderer.Page;
            string text = pdfViewer1.Document?.Pages[page].GetPdfText();
            string caption = string.Format("Page {0} contains {1} character(s):", page + 1, text?.Length);

            if (text?.Length > 128) text = text.Substring(0, 125) + "...\n\n\n\n..." + text.Substring(text.Length - 125);
            MessageBox.Show(this, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_searchForm == null)
            {
                _searchForm = new SearchForm(pdfViewer1.Renderer);
                _searchForm.Disposed += (s, ea) => _searchForm = null;
                _searchForm.Show(this);
            }

            _searchForm.Focus();
        }

        private void printMultiplePagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new PrintMultiplePagesForm(pdfViewer1))
            {
                form.ShowDialog(this);
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.CopySelection();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.SelectAll();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pdfViewer1.Renderer.RemoveObject();
        }
        private void pdfViewerContextMenu_Opening(object sender, CancelEventArgs e)
        {
            copyToolStripMenuItem.Enabled = pdfViewer1.Renderer.IsTextSelected;
        }
        #region Test
        private void splitintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string index;
            using (var form = new SplitForm())
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;
                index = form.pageIndex;
            }
            var document = pdfViewer1.Document;
            try
            {
                var sheet = Convert.ToInt32(index);
                var doc = PdfSupport.GetPage(document, sheet);
                if (doc != null)
                {
                    document.Close();
                    pdfViewer1.Document = null;
                    pdfViewer1.Document = doc;
                }
            }
            catch
            {

            }
        }

        private void mergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var file = "";
            using (var form = new OpenFileDialog())
            {
                form.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
                form.RestoreDirectory = true;
                form.Title = "Open PDF File";

                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    Dispose();
                    return;
                }
                file = form.FileName;
            }
            var document = pdfViewer1.Document;
            document.MergePage(OpenDocument(file));

            pdfViewer1.Document = null;
            pdfViewer1.Document = document;
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var file = "";
            using (var form = new OpenFileDialog())
            {
                form.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
                form.RestoreDirectory = true;
                form.Title = "Open PDF File";

                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    Dispose();
                    return;
                }
                file = form.FileName;
            }
            string index;
            using (var form = new SplitForm())
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;
                index = form.pageIndex;
            }
            var sheet = 0;
            try
            {
                sheet = Convert.ToInt32(index);
            }
            catch { }
            var document = pdfViewer1.Document;
            document.ImportPage(OpenDocument(file), sheet);

            pdfViewer1.Document = null;
            pdfViewer1.Document = document;

        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var file = "";
            using (var form = new OpenFileDialog())
            {
                form.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
                form.RestoreDirectory = true;
                form.Title = "Open PDF File";

                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    Dispose();
                    return;
                }
                file = form.FileName;
            }
            string index;
            using (var form = new SplitForm())
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;
                index = form.pageIndex;
            }
            var sheet = 0;
            try
            {
                sheet = Convert.ToInt32(index);
            }
            catch { }

            var document = pdfViewer1.Document;
            document.DeletePage(sheet);
            document.ImportPage(OpenDocument(file), sheet);

            pdfViewer1.Document = null;
            pdfViewer1.Document = document;
        }
        #region
        void Cutting()
        {
            //int dpiX;
            //int dpiY;

            //using (var form = new ExportBitmapsForm())
            //{
            //    if (form.ShowDialog() != DialogResult.OK)
            //        return;

            //    dpiX = form.DpiX;
            //    dpiY = form.DpiY;
            //}

            //string path;
            //using (var form = new FolderBrowserDialog())
            //{
            //    if (form.ShowDialog(this) != DialogResult.OK)
            //        return;

            //    path = form.SelectedPath;
            //}
            //outImagePath = path;
            //var document = pdfViewer1.Document;

            //for (int i = 0; i < document.PageCount; i++)
            //{
            //    using (var image = document.Render(i, (int)document.PageSizes[i].Width * 4 / 3, (int)document.PageSizes[i].Height * 4 / 3, dpiX, dpiY, false))
            //    {
            //        image.Save(Path.Combine(path, "Page " + i + ".png"));
            //    }
            //}
        }
        ///// <summary>
        ///// 合并PDF
        ///// </summary>
        //private void Merge()
        //{
        //    var doc1 = PdfDocument.Load(this, new MemoryStream(File.ReadAllBytes(@"C:\Users\11\Desktop\7020437019869205\施召良0.pdf")));
        //    var doc2 = PdfDocument.Load(this, new MemoryStream(File.ReadAllBytes(@"C:\Users\11\Desktop\7020437019869205\施召良1.pdf")));
        //    var bytes = doc1.PDFMerge();//(doc1,doc2);
        //    File.WriteAllBytes(@"C:\Users\11\Desktop\11.pdf", bytes);
        //}

        ///// <summary>
        ///// 拆分PDF
        ///// </summary>
        //private void SplitPdf()
        //{
        //    //拆分文件
        //    var bytes = pdfViewer1.Document.GetPage(1);
        //    var bytes1 = pdfViewer1.Document.GetPage("1-");
        //    File.WriteAllBytes(@"C:\Users\11\Desktop\11.pdf", bytes1);
        //}
        /// <summary>
        ///添加了光栅化页面任意区域的方法
        ///添加了将任何页面作为单个 PDF 文件返回的方法（未光栅化）
        /// </summary>
        //void Test1()
        //{

        //int pagenum = 1;

        //// dpi = 96 here for screen display
        //// for printing up it to 240, 300, 600 or whatever
        //var pdoc = pdfViewer1.Document;

        ////rasterize whole page------------------------------
        //var img = pdoc.Render(pagenum, 96, 96, false);

        ////get single page as new pdf doc (byte[])-----------
        //var pdfpage = pdoc.GetPage(pagenum);
        //File.WriteAllBytes(@"C:\Users\11\Desktop\11.pdf", pdfpage);
        ////rasterize page area-------------------------------
        ////todo: move implemenation into pdfdocument class
        //var mb = pdoc.GetPageMediaBox(pagenum);
        ///*get your vals:
        // * starting from pdf page width  & height
        // * get the coords of the crop box
        // * and crop box width & height
        // * */
        //mb.Left = 200;
        //mb.Right = 200;
        //mb.Top = 200;
        //mb.Bottom = 200;
        //pdoc.SetPageMediaBox(pagenum, mb.Left, mb.Right, mb.Top, mb.Bottom);
        ////if pdfpage with is 1000 then cropbox width will now be 600
        ////if pdfpage with is 1500 then cropbox width will now be 1100
        //Bitmap bmp = new Bitmap(600, 1100);
        //var g = Graphics.FromImage(bmp);
        //pdoc.Render(pagenum, g, 96, 96, new Rectangle(0, 0, 600, 1100), false);
        //using (var stream = new MemoryStream())
        //{
        //    bmp.Save(stream, ImageFormat.Png);
        //    //save the stream somewhere, convert to byte[] etc
        //}
        //bmp.Save(@"C:\Users\11\Desktop\11.Png");
        //}

        //private void ImportPdf()
        //{
        //    //var doc1 = PdfDocument.Load(this, new MemoryStream(File.ReadAllBytes(@"C:\Users\11\Desktop\信阳.pdf")));
        //    var doc2 = PdfDocument.Load(this, new MemoryStream(File.ReadAllBytes(@"C:\Users\11\Desktop\7020437019869205\施召良0.pdf")));
        //    var bytes = pdfViewer1.Document.ImportPage(2, doc2);
        //    File.WriteAllBytes(@"C:\Users\11\Desktop\11.pdf", bytes);
        //}
        #endregion

        #endregion

        private void cutingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path;
            using (var form = new FolderBrowserDialog())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                path = form.SelectedPath;
            }
            //x y x1 y1
            // 20 279 573 601
            //20 279 553  322
            //29 326 733  418
            var outputPath = Path.Combine(path, "Page " + 0 + ".png");
            var cropImage = Path.Combine(path, "cropPage " + 0 + ".png");
            if (File.Exists(outputPath))
            {
                using (Image imgPDF = Image.FromFile(outputPath))
                {
                    Rectangle currRect = new Rectangle(
                                          (int)(29),
                                          (int)(326),
                                          (int)(733),
                                          (int)(418));

                    Bitmap bm = new Bitmap(currRect.Width, currRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(bm);
                    g.DrawImage(imgPDF, new Rectangle(0, 0, currRect.Width, currRect.Height), currRect, GraphicsUnit.Pixel);
                    bm.Save(cropImage, System.Drawing.Imaging.ImageFormat.Png);

                    g.Dispose();
                    bm.Dispose();
                }

            }
        }
        private void renderToCutBitmapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path;
            using (var form = new FolderBrowserDialog())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                path = form.SelectedPath;
            }
            var outputPath = Path.Combine(path, "Page " + 0 + "Cut.png");
            //            var renderedPage = pdfViewer1.Document.Render(0,
            //        10000, // width in px
            //        0, // '0' to compute height according to aspect ratio
            //        0, // x of the top/left of clipping rectangle
            //        0, // y of the top/left point of clipping rectangle
            //        1000, // width of clipping reactangle
            //        1000, // height of clipping reactangle
            //        PdfRotation.Rotate0, // no rotation
            //        PdfRenderFlags.None // no render flags
            //);
            //20 279 573 601 
            //20 279 553  322
            //29 326 733  418
            //36 332 720 409
            //X = 16 Y = 283 Width = 555 Height = 316
            var document = pdfViewer1.Document;
            for (int i = 0; i < document.PageCount; i++)
            {
                if (i != 0)
                    break;
                using (var image = document.Render(
                    i,
                    (int)document.Pages[i].Width,
                    (int)document.Pages[i].Height,
                    16, // x of the top/left of clipping rectangle
                   283, // y of the top/left point of clipping rectangle
                   555, // width of clipping reactangle
                   316, // height of clipping reactangle
                   0, 0,
                   PdfRotation.Rotate0, // no rotation
                   Pdfium.Net.Wrapper.PdfRenderFlags.None // no render flags
                                       ))
                {
                    image.Save(Path.Combine(path, "Page " + i + "Cut.png"));
                }
            }

        }
        private void _matching_Click(object sender, EventArgs e)
        {
            _matching.Checked = !_matching.Checked;
            if (_matching.Checked)
            {
                _copy.Checked = false;
                _shear.Checked = false;
                _edit.Checked = false;
                _editMini.Checked = false;
                pdfViewer1.Renderer.PdfOperate = PdfOperate.Matching;
            }
            else
            {
                pdfViewer1.Renderer.PdfOperate = PdfOperate.None;
            }
        }
        private void _copy_Click(object sender, EventArgs e)
        {
            _copy.Checked = !_copy.Checked;
            if (_copy.Checked)
            {
                _shear.Checked = false;
                _shear.Checked = false;
                _edit.Checked = false;
                _editMini.Checked = false;
                pdfViewer1.Renderer.PdfOperate = PdfOperate.Copy;
            }
            else
            {
                pdfViewer1.Renderer.PdfOperate = PdfOperate.None;
            }
        }

        private void _shear_Click(object sender, EventArgs e)
        {
            _shear.Checked = !_shear.Checked;
            if (_shear.Checked)
            {
                _matching.Checked = false;
                _copy.Checked = false;
                _edit.Checked = false;
                _editMini.Checked = false;
                pdfViewer1.Renderer.PdfOperate = PdfOperate.Shear;
            }
            else
            {
                pdfViewer1.Renderer.PdfOperate = PdfOperate.None;
            }
        }

        private void _edit_Click(object sender, EventArgs e)
        {
            _edit.Checked = !_edit.Checked;
            if (_edit.Checked)
            {
                _matching.Checked = false;
                _copy.Checked = false;
                _shear.Checked = false;
                pdfViewer1.Renderer.PdfOperate = PdfOperate.Edit;
            }
            else
            {
                pdfViewer1.Renderer.PdfOperate = PdfOperate.None;
            }
        }
        private void _editMini_Click(object sender, EventArgs e)
        {
            _editMini.Checked = !_editMini.Checked;
            pdfViewer1.Renderer.IsEditMinimum = !_editMini.Checked;
        }
        private void _panningEnabled_Click(object sender, EventArgs e)
        {
            _panningEnabled.Checked = !_panningEnabled.Checked;
            if (_panningEnabled.Checked)
            {
                pdfViewer1.Renderer.PanningEnabled = false;
            }
            else
            {
                pdfViewer1.Renderer.PanningEnabled = true;
            }
        }
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new OpenFileDialog())
            {
                form.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";
                form.RestoreDirectory = true;
                form.Title = "Open PDF File";

                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                fileName = form.FileName;
                var printer = new PdfPrinter("Microsoft Print To PDF");
                //printer.Print(fileName);
                printer.Print(fileName, documentName: "with name");
            }
        }

        private void thumbnailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path;
            using (var form = new FolderBrowserDialog())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                path = form.SelectedPath;
            }
            for (int i = 0; i < pdfViewer1.Document.PageCount; i++)
            {
                var image = pdfViewer1.Document.Pages[i].RenderThumbnail();
                image?.Save(Path.Combine(path, "Page " + i + ".png"));
            }
        }

        private void getFontInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ss = (pdfViewer1.Renderer.Page + 1).ToString();
            //var doc =pdfViewer1.Document;
            //doc.Test();
        }

        private void addTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var cSharpString = "you ok 这是第一句。 这是第二行。a you ok";
            var doc = pdfViewer1.Document;
            var pdfPage = doc.Pages[0];
            pdfPage.AddString(cSharpString, 10, 50, 20, Color.Black);
            pdfViewer1.Document = null;
            pdfViewer1.Document = doc;
        }

        private void addImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var imagePath = "D:\\NaLong\\一些有用的Demo\\pdf\\PdfiumViewer\\pdfium\\testing\\resources\\mona_lisa.jpg";
            var stream = new MemoryStream(File.ReadAllBytes(imagePath));
            var doc = pdfViewer1.Document;
            var pdfPage = doc.Pages[0];
            pdfPage.AddImage(stream, 50, 100);
            pdfViewer1.Document = null;
            pdfViewer1.Document = doc;

        }

        private void addImageObjToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var imagePath = "D:\\NaLong\\一些有用的Demo\\pdf\\PdfiumViewer\\pdfium\\testing\\resources\\mona_lisa.jpg";
            var stream = new MemoryStream(File.ReadAllBytes(imagePath));
            var image = Image.FromStream(stream);
            var doc = pdfViewer1.Document;
            var pdfPage = doc.Pages[0];
            pdfPage.AddImage(image, 200, 100);
            pdfViewer1.Document = null;
            pdfViewer1.Document = doc;
        }
        private void signatureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var count = pdfViewer1.Document.GetSignatureCount();
            if (count > 0)
            {
                MessageBox.Show($"Signature {count}");
                for (int i = 0; i < count; i++)
                {
                    var sign = pdfViewer1.Document.GetSignatureObject(i);
                    MessageBox.Show($"SubFilter:{sign.GetSubFilter()} \r\nTime:{sign.GetTime()} \r\nReason:{sign.GetReason()}");
                }
            }
        }

        private void npageToOneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var doc = pdfViewer1.Document.ImportNPagesToOne(612 * 2, 792);
            pdfViewer1.Document = null;
            pdfViewer1.Document = doc;
        }

        private void createDocumentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var doc = PdfDocument.CreateNew();
            var fontPath = @"c:\Windows\fonts\simhei.ttf";
            doc?.LoadFont(fontPath);
            var cSharpString = "you ok 这是第一句。 这是第二行。a you ok";
            doc.Pages.Insert(0, 612, 792);
            var pdfPage = doc.Pages[0];
            pdfPage.AddString(cSharpString, 10, 50, 12, Color.Black);
            pdfViewer1?.Close();
            pdfViewer1.Document = doc;
        }
        private void createNewPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var cSharpString = "心内1 (东院)";
            var doc = pdfViewer1.Document;
            pdfViewer1.Document = null;
            doc.Pages.Insert(0, 612, 792);
            var pdfPage = doc.Pages[0];
            pdfPage.AddString(cSharpString, 10, 50, 12, Color.FromArgb(128, 255, 0, 0));
            pdfViewer1.Document = doc;
        }

        private void addWaterMarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var cSharpString = "我是水印";
            var doc = pdfViewer1.Document;
            doc.WaterMark(cSharpString, 50, Color.FromArgb(50, 255, 0, 0), totleHeight: 120, render_mode: PdfTextRenderMode.Stroke, strokeColor: Color.FromArgb(50, 255, 0, 0), strokeWidth: 0.1f);
            pdfViewer1.Document = null;
            pdfViewer1.Document = doc;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pdfViewer1.Close();
            GC.Collect(GC.MaxGeneration);
        }
    }
}
