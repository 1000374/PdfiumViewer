using Pdfium.Net.Extension;
using Pdfium.Net.Native.Enums;
using Pdfium.Net.Wrapper;
using PdfiumViewer.UControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace PdfiumViewer
{
    /// <summary>
    /// Control to render PDF documents.
    /// </summary>
    public class PdfRenderer : PanningZoomingScrollControl
    {
        public delegate void DeleBoundedText(int page, int x, int y, int x1, int y1, string txt, bool isEdit);
        public event DeleBoundedText BoundedTextHandler;

        public delegate void DelePdfPoint(PdfPoint point);
        public event DelePdfPoint PdfPointhandler;

        public delegate void DeleBoundedCut(int page, Rectangle rectangle);
        public event DeleBoundedCut BoundedCutHandler;

        public delegate void DeleBoundedEdit(int page, PdfPageobject pdfobject);
        public event DeleBoundedEdit BoundedEditHandler;

        private static readonly Padding PageMargin = new Padding(4);
        private static readonly SolidBrush _textSelectionBrush = new SolidBrush(Color.FromArgb(90, Color.DodgerBlue));

        private int _maxWidth;
        private int _maxHeight;
        private double _documentScaleFactor;
        private bool _disposed;
        private double _scaleFactor;
        private ShadeBorder _shadeBorder = new ShadeBorder();
        private int _suspendPaintCount;
        private ToolTip _toolTip;
        private PdfViewerZoomMode _zoomMode;
        private bool _pageCacheValid;
        private readonly List<PageCache> _pageCache = new List<PageCache>();
        private int _visiblePageStart;
        private int _visiblePageEnd;
        private CachedLink _cachedLink;
        private DragState _dragState;
        private PdfRotation _rotation;
        private List<IPdfMarker>[] _markers;
        private PdfViewerCursorMode _cursorMode = PdfViewerCursorMode.Pan;
        private bool _isSelectingText = false;
        private MouseState _cachedMouseState = null;
        private TextSelectionState _textSelectionState = null;
        private Rectangle _currRect = Rectangle.Empty;

        private PdfOperate _pdfOperate = PdfOperate.None;

        private bool _isEditMinimum = true;
        private UcTextBox _editBox;
        private ObjectInformation _currObjectInformation;
        private Cursor _editCursor;
        /// <summary>
        /// The associated PDF document.
        /// </summary>
        public PdfDocument Document { get; private set; }

        public PdfOperate PdfOperate
        {
            get
            {
                return _pdfOperate;
            }
            set
            {
                _pdfOperate = value;
                PanningEnabled = (value == PdfOperate.None);
                switch (_pdfOperate)
                {
                    case PdfOperate.None:
                        {
                            _cursorMode = PdfViewerCursorMode.Pan;
                        }
                        break;
                    case PdfOperate.Matching:
                        {
                            _cursorMode = PdfViewerCursorMode.Cross;
                        }
                        break;
                    case PdfOperate.Shear:
                        {
                            _cursorMode = PdfViewerCursorMode.Cross;
                        }
                        break;
                    case PdfOperate.Copy:
                        {
                            _cursorMode = PdfViewerCursorMode.TextSelection;
                        }
                        break;
                    case PdfOperate.Edit:
                        {
                            //_isEditMinimum = true;
                            if (_editBox == null)
                            {
                                _editBox = new UcTextBox();
                                _editBox.BorderStyle = BorderStyle.FixedSingle;
                                _editBox.BorderPositionStyle = PositionEnum.None;
                                _editBox.Multiline = true;
                                _editBox.ModifyTextFontHandler += ModifyTextFontHandler;
                            }
                            if (_editCursor == null)
                            {
                                using (var stream = typeof(PanningZoomingScrollControl).Assembly.GetManifestResourceStream(typeof(PanningZoomingScrollControl).Namespace + ".rotation.cur"))
                                {
                                    _editCursor = new Cursor(stream);
                                }
                            }
                        }
                        break;
                }
                RemoveShearMarker(false);
                IniCharacterMarker();
            }
        }

        private void ModifyTextFontHandler(Color color, float size, TextBoxOperate operate)
        {
            switch (operate)
            {
                case TextBoxOperate.Enter:
                    {
                        RemoveEditTextBox();
                    }
                    break;
                case TextBoxOperate.ChangeText:
                    {
                        if (_pdfOperate == PdfOperate.Edit && _editBox != null && this.Controls.Contains(_editBox))
                        {
                            if ((_editBox.FontColor != color || _editBox.FontSize != size) && !string.IsNullOrEmpty(_editBox.Text) && _editBox.PageObject != null && !_editBox.PageObject.IsNull)
                            {
                                var page = Document.Pages[_editBox.Page];
                                var txt = _editBox.Text.Trim(new char[] { ' ', '\n', '\r', '\t' });
                                var posX = _editBox.ObjBounds.Location.X;
                                var posY = _editBox.ObjBounds.Location.Y + _editBox.ObjBounds.Height;
                                if (page.RemoveObject(_editBox.PageObject))
                                {
                                    var pageobject = Document.PageObjCreateTextObj(Document.FontT, size);
                                    var res = pageobject.PageObjSetFillColor(color.R, color.G, color.B, color.A);
                                    res = pageobject.TextSetText(txt);
                                    if (res)
                                    {
                                        pageobject.PageObjTransform(1, 0, 0, 1, posX, posY);
                                        page.InsertObject(pageobject);
                                        if (page.GenerateContent())
                                        {
                                            var rect = pageobject.PageObjGetBounds().ToRectangle();
                                            var infos = Markers.Where(s => s.Page == Page && (s is CharacterMarker)).Select(s => ((CharacterMarker)s).ObjectInformation).FirstOrDefault();
                                            var info = FindObjectInformation(_currObjectInformation.PdfPageobject, infos);
                                            if (info != null)
                                            {
                                                info.PdfPageobject = pageobject;
                                                info.ObjBounds = rect;
                                            }
                                            Invalidate();
                                        }
                                    }
                                }
                            }
                            this.Controls.Remove(_editBox);
                            _editBox.Text = "";
                            _editBox.OrgText = "";
                            _currObjectInformation = null;
                            _cursorMode = PdfViewerCursorMode.None;
                        }
                    }
                    break;
            }

        }
        /// <summary>
        ///  more FPDF_PAGEOBJ_PATH will be effect select,so default edit text and image
        /// </summary>
        public bool IsEditMinimum { get => _isEditMinimum; set => _isEditMinimum = value; }


        /// <summary>
        /// Gets or sets a value indicating whether the user can give the focus to this control using the TAB key.
        /// </summary>
        /// 
        /// <returns>
        /// true if the user can give the focus to the control using the TAB key; otherwise, false. The default is true.Note:This property will always return true for an instance of the <see cref="T:System.Windows.Forms.Form"/> class.
        /// </returns>
        /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
        [DefaultValue(true)]
        public new bool TabStop
        {
            get { return base.TabStop; }
            set { base.TabStop = value; }
        }

        /// <summary>
        /// Gets or sets the currently focused page.
        /// </summary>
        public int Page
        {
            get
            {
                if (Document == null || !_pageCacheValid)
                    return 0;

                int top = -DisplayRectangle.Top;
                int bottom = top + GetScrollClientArea().Height;

                for (int page = 0; page < Document.Pages.Count; page++)
                {
                    var pageCache = _pageCache[page].OuterBounds;
                    if (top - 10 < pageCache.Top)
                    {
                        // If more than 50% of the page is hidden, return the previous page.

                        int hidden = pageCache.Bottom - bottom;
                        if (hidden > 0 && (double)hidden / pageCache.Height > 0.5 && page > 0)
                            return page - 1;

                        return page;
                    }
                }

                return Document.PageCount - 1;
            }
            set
            {
                if (Document == null)
                {
                    SetDisplayRectLocation(new Point(0, 0));
                }
                else
                {
                    int page = Math.Min(Math.Max(value, 0), Document.PageCount - 1);

                    SetDisplayRectLocation(new Point(0, -_pageCache[page].OuterBounds.Top));
                }
            }
        }

        /// <summary>
        /// Get the outer bounds of the page.
        /// </summary>
        /// <param name="page">The page to get the bounds for.</param>
        /// <returns>The bounds of the page.</returns>
        public Rectangle GetOuterBounds(int page)
        {
            if (Document == null || !_pageCacheValid)
                return Rectangle.Empty;

            page = Math.Min(Math.Max(page, 0), Document.PageCount - 1);
            return _pageCache[page].OuterBounds;
        }
        /// <summary>
        /// Get the  BoundsOffset of the page.
        /// </summary>
        /// <param name="page">The page to get the BoundsOffset for.</param>
        /// <returns>The BoundsOffset of the page.</returns>
        public Rectangle GetBoundsOffset(int page)
        {
            if (Document == null || !_pageCacheValid)
                return Rectangle.Empty;

            page = Math.Min(Math.Max(page, 0), Document.PageCount - 1);
            var pageBounds = _pageCache[page].Bounds;
            var offset = GetScrollOffset();
            pageBounds.Offset(offset.Width, offset.Height);
            return pageBounds;
        }
        /// <summary>
        /// Get the  Bounds of the page.
        /// </summary>
        /// <param name="page">The page to get the Bounds for.</param>
        /// <returns>The Bounds of the page.</returns>
        public Rectangle GetBounds(int page)
        {
            if (Document == null || !_pageCacheValid)
                return Rectangle.Empty;

            page = Math.Min(Math.Max(page, 0), Document.PageCount - 1);
            var pageBounds = _pageCache[page].Bounds;
            return pageBounds;
        }
        /// <summary>
        /// Get the image of the page.
        /// </summary>
        /// <param name="page">The page to get the image for.</param>
        /// <returns>The image of the page.</returns>
        public System.Drawing.Image GetImage(int page)
        {
            if (Document == null || !_pageCacheValid)
                return null;

            page = Math.Min(Math.Max(page, 0), Document.PageCount - 1);
            return _pageCache[page].Image;
        }
        /// <summary>
        /// Gets or sets the way the document should be zoomed initially.
        /// </summary>
        public PdfViewerZoomMode ZoomMode
        {
            get { return _zoomMode; }
            set
            {
                _zoomMode = value;
                PerformLayout();
            }
        }

        /// <summary>
        /// Gets or sets the current rotation of the PDF document.
        /// </summary>
        public PdfRotation Rotation
        {
            get { return _rotation; }
            set
            {
                if (Document == null || !_pageCacheValid)
                    return;
                if (_rotation != value)
                {
                    _rotation = value;
                    ResetFromRotation();
                }
            }
        }

        /// <summary>
        /// Indicates whether the user currently has text selected
        /// </summary>
        public bool IsTextSelected
        {
            get
            {
                var state = _textSelectionState?.GetNormalized();
                if (state == null)
                    return false;

                if (state.EndPage < 0 || state.EndIndex < 0)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Gets the currently selected text
        /// </summary>
        public string SelectedText
        {
            get
            {
                var state = _textSelectionState?.GetNormalized();
                if (state == null)
                    return null;

                var sb = new StringBuilder();
                for (int page = state.StartPage; page <= state.EndPage; page++)
                {
                    int start = 0, end = 0;

                    if (page == state.StartPage)
                        start = state.StartIndex;

                    if (page == state.EndPage)
                        end = (state.EndIndex + 1);
                    else
                        end = Document.Pages[page].CountChars();

                    if (page != state.StartPage)
                        sb.AppendLine();

                    sb.Append(Document.Pages[page].GetPdfText(new PageTextSpan(page, start, end - start)));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets a collection with all markers.
        /// </summary>
        public PdfMarkerCollection Markers { get; }

        /// <summary>
        /// Initializes a new instance of the PdfRenderer class.
        /// </summary>
        public PdfRenderer()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);

            TabStop = true;

            _toolTip = new ToolTip();

            Markers = new PdfMarkerCollection();
            Markers.CollectionChanged += Markers_CollectionChanged;
        }

        private void Markers_CollectionChanged(object sender, EventArgs e)
        {
            RedrawMarkers();
        }

        /// <summary>
        /// Converts client coordinates to PDF coordinates.
        /// </summary>
        /// <param name="location">Client coordinates to get the PDF location for.</param>
        /// <returns>The location in a PDF page or a PdfPoint with IsValid false when the coordinates do not match a PDF page.</returns>
        public PdfPoint PointToPdf(Point location)
        {
            if (Document == null)
                return PdfPoint.Empty;

            var offset = GetScrollOffset();
            location.Offset(-offset.Width, -offset.Height);

            for (int page = 0; page < Document.Pages.Count; page++)
            {
                var pageCache = _pageCache[page];
                if (pageCache.OuterBounds.Contains(location))
                {
                    if (pageCache.Bounds.Contains(location))
                    {
                        location = new Point(
                            location.X - pageCache.Bounds.X,
                            location.Y - pageCache.Bounds.Y
                        );
                        var translated = TranslatePointToPdf(pageCache.Bounds.Size, Document.Pages[page].PageSize, location);
                        translated = Document.Pages[page].PointToPdf(new Point((int)translated.X, (int)translated.Y));

                        return new PdfPoint(page, translated);
                    }

                    break;
                }
            }
            return PdfPoint.Empty;
        }

        /// <summary>
        /// Converts a PDF point to a client point.
        /// </summary>
        /// <param name="point">The PDF point to convert.</param>
        /// <returns>The location of the point in client coordinates.</returns>
        public Point PointFromPdf(PdfPoint point)
        {
            var offset = GetScrollOffset();
            var pageBounds = _pageCache[point.Page].Bounds;

            var translated = Document.Pages[point.Page].PointFromPdf(point.Location);
            var location = TranslatePointFromPdf(pageBounds.Size, Document.Pages[point.Page].PageSize, translated);

            return new Point(
                pageBounds.Left + offset.Width + location.X,
                pageBounds.Top + offset.Height + location.Y
            );
        }

        /// <summary>
        /// Converts client coordinates to PDF bounds.
        /// </summary>
        /// <param name="bounds">The client coordinates to convert.</param>
        /// <returns>The PDF bounds.</returns>
        public PdfRectangle BoundsToPdf(Rectangle bounds)
        {
            if (Document == null)
                return PdfRectangle.Empty;

            var offset = GetScrollOffset();
            bounds.Offset(-offset.Width, -offset.Height);

            for (int page = 0; page < Document.Pages.Count; page++)
            {
                var pageCache = _pageCache[page];
                if (pageCache.OuterBounds.Contains(bounds.Location))
                {
                    if (pageCache.Bounds.Contains(bounds.Location))
                    {
                        var topLeft = new Point(
                            bounds.Left - pageCache.Bounds.Left,
                            bounds.Top - pageCache.Bounds.Top
                        );
                        var bottomRight = new Point(
                            bounds.Right - pageCache.Bounds.Left,
                            bounds.Bottom - pageCache.Bounds.Top
                        );

                        var translatedTopLeft = TranslatePointToPdf(pageCache.Bounds.Size, Document.Pages[page].PageSize, topLeft);
                        var translatedBottomRight = TranslatePointToPdf(pageCache.Bounds.Size, Document.Pages[page].PageSize, bottomRight);

                        var translated = Document.Pages[page].RectangleToPdf(
                            new Rectangle(
                                (int)translatedTopLeft.X,
                                (int)translatedTopLeft.Y,
                                (int)(translatedBottomRight.X - translatedTopLeft.X),
                                (int)(translatedBottomRight.Y - translatedTopLeft.Y)
                            )
                        );

                        return new PdfRectangle(page, translated);
                    }
                    break;
                }
            }
            return PdfRectangle.Empty;
        }

        /// <summary>
        /// Converts PDF bounds to client bounds.
        /// </summary>
        /// <param name="bounds">The PDF bounds to convert.</param>
        /// <returns>The bounds of the PDF bounds in client coordinates.</returns>
        public Rectangle BoundsFromPdf(PdfRectangle bounds)
        {
            return BoundsFromPdf(bounds, true);
        }

        private Rectangle BoundsFromPdf(PdfRectangle bounds, bool translateOffset)
        {
            var offset = translateOffset ? GetScrollOffset() : Size.Empty;
            var pageBounds = _pageCache[bounds.Page].Bounds;
            var pageSize = Document.Pages[bounds.Page].PageSize;

            var translated = Document.Pages[bounds.Page].RectangleFromPdf(bounds.Bounds);
            var topLeft = TranslatePointFromPdf(pageBounds.Size, pageSize, new PointF(translated.Left, translated.Top));
            var bottomRight = TranslatePointFromPdf(pageBounds.Size, pageSize, new PointF(translated.Right, translated.Bottom));

            return new Rectangle(
                pageBounds.Left + offset.Width + Math.Min(topLeft.X, bottomRight.X),
                pageBounds.Top + offset.Height + Math.Min(topLeft.Y, bottomRight.Y),
                Math.Abs(bottomRight.X - topLeft.X),
                Math.Abs(bottomRight.Y - topLeft.Y)
            );
        }

        private PointF TranslatePointToPdf(Size size, SizeF pageSize, Point point)
        {
            switch (Rotation)
            {
                case PdfRotation.Rotate90:
                    point = new Point(size.Height - point.Y, point.X);
                    size = new Size(size.Height, size.Width);
                    break;
                case PdfRotation.Rotate180:
                    point = new Point(size.Width - point.X, size.Height - point.Y);
                    break;
                case PdfRotation.Rotate270:
                    point = new Point(point.Y, size.Width - point.X);
                    size = new Size(size.Height, size.Width);
                    break;
            }

            return new PointF(
                ((float)point.X / size.Width) * pageSize.Width,
                ((float)point.Y / size.Height) * pageSize.Height
            );
        }

        private Point TranslatePointFromPdf(Size size, SizeF pageSize, PointF point)
        {
            switch (Rotation)
            {
                case PdfRotation.Rotate90:
                    point = new PointF(pageSize.Height - point.Y, point.X);
                    pageSize = new SizeF(pageSize.Height, pageSize.Width);
                    break;
                case PdfRotation.Rotate180:
                    point = new PointF(pageSize.Width - point.X, pageSize.Height - point.Y);
                    break;
                case PdfRotation.Rotate270:
                    point = new PointF(point.Y, pageSize.Width - point.X);
                    pageSize = new SizeF(pageSize.Height, pageSize.Width);
                    break;
            }

            return new Point(
                (int)((point.X / pageSize.Width) * size.Width),
                (int)((point.Y / pageSize.Height) * size.Height)
            );
        }

        private Size GetScrollOffset()
        {
            var bounds = GetScrollClientArea();
            int maxWidth = (int)(_maxWidth * _scaleFactor) + ShadeBorder.Size.Horizontal + PageMargin.Horizontal;
            int leftOffset = (HScroll ? DisplayRectangle.X : (bounds.Width - maxWidth) / 2) + maxWidth / 2;
            int topOffset = VScroll ? DisplayRectangle.Y : 0;

            return new Size(leftOffset, topOffset);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Layout"/> event.
        /// </summary>
        /// <param name="levent">A <see cref="T:System.Windows.Forms.LayoutEventArgs"/> that contains the event data. </param>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            UpdateScrollbars();
        }

        /// <summary>
        /// Called when the zoom level changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnZoomChanged(EventArgs e)
        {
            base.OnZoomChanged(e);

            UpdateScrollbars();

            RemoveEditTextBox();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch ((e.KeyData) & Keys.KeyCode)
            {
                case Keys.A:
                    if ((e.KeyData & Keys.Modifiers) == Keys.Control)
                        SelectAll();
                    break;
                case Keys.D:
                    if ((e.KeyData & Keys.Modifiers) == Keys.Control)
                        DeSelectAll();
                    break;
                case Keys.C:
                    if ((e.KeyData & Keys.Modifiers) == Keys.Control)
                        CopySelection();
                    break;
                case Keys.Delete:
                    RemoveObject();
                    break;
                case Keys.Escape:
                    if (_pdfOperate == PdfOperate.Shear)
                    {
                        RemoveShearMarker();
                    }
                    break;
            }

            base.OnKeyDown(e);
        }
        public void DeSelectAll()
        {
            if (_pdfOperate == PdfOperate.Copy)
            {
                _textSelectionState = null;
                Invalidate();
            }
        }
        public void SelectAll()
        {
            if (_pdfOperate == PdfOperate.Copy)
            {
                _textSelectionState = new TextSelectionState()
                {
                    StartPage = 0,
                    StartIndex = 0,
                    EndPage = Document.PageCount - 1,
                    EndIndex = Document.Pages[Document.PageCount - 1].CountChars() - 1
                };
                Invalidate();
            }
        }

        public void CopySelection()
        {
            if (_pdfOperate == PdfOperate.Copy)
            {
                var text = SelectedText;
                if (text?.Length > 0)
                    Clipboard.SetText(text);
            }
        }

        public void RemoveObject()
        {
            if (_pdfOperate == PdfOperate.Edit)
            {
                if (_currObjectInformation != null)
                {
                    var info = _currObjectInformation;
                    var obj = info.PdfPageobject;
                    if (!obj.IsNull)
                    {
                        var page = Document.Pages[info.Page];
                        if (page != null && page.RemoveObject(obj))
                        {
                            if (page.GenerateContent())
                            {
                                var markers = Markers?.Where(s => s.Page == info.Page && (s is CharacterMarker)).Select(s => ((CharacterMarker)s).ObjectInformation)?.FirstOrDefault();
                                RemoveMarker(markers);
                                ResetCacheImage(info.Page);
                                _currObjectInformation = null;
                                Invalidate();
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Load a <see cref="IPdfDocument"/> into the control.
        /// </summary>
        /// <param name="document">Document to load.</param>
        public void Load(PdfDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            if (document.PageCount == 0)
                throw new ArgumentException("Document does not contain any pages", "document");

            Document = document;

            SetDisplayRectLocation(new Point(0, 0));

            ReloadDocument();
        }

        private void ReloadDocument()
        {
            _maxWidth = 0;
            _maxHeight = 0;
            _textSelectionState = null;

            foreach (var page in Document.Pages)
            {
                var translated = TranslateSize(page.PageSize);
                _maxWidth = Math.Max((int)translated.Width, _maxWidth);
                _maxHeight = Math.Max((int)translated.Height, _maxHeight);
            }

            _documentScaleFactor = _maxHeight != 0 ? (double)_maxWidth / _maxHeight : 0D;

            _markers = null;

            Markers?.Clear();

            UpdateScrollbars();

            Invalidate();
        }

        private void UpdateScrollbars()
        {
            if (Document == null)
                return;

            UpdateScaleFactor(ScrollBars.Both);

            var bounds = GetScrollClientArea(ScrollBars.Both);

            var documentSize = GetDocumentBounds().Size;

            bool horizontalVisible = documentSize.Width > bounds.Width;

            if (!horizontalVisible)
            {
                UpdateScaleFactor(ScrollBars.Vertical);

                documentSize = GetDocumentBounds().Size;
            }
            _suspendPaintCount++;
            try
            {
                SetDisplaySize(documentSize);
            }
            finally
            {
                _suspendPaintCount--;
            }
            RebuildPageCache();
        }

        private void RebuildPageCache()
        {
            if (Document == null || _suspendPaintCount > 0)
                return;

            if (_pageCache.Count > 0 && _pageCache.Count != Document.PageCount)
            {
                DisposeCache();
            }

            _pageCacheValid = true;

            int maxWidth = (int)(_maxWidth * _scaleFactor) + ShadeBorder.Size.Horizontal + PageMargin.Horizontal;
            int leftOffset = -maxWidth / 2;

            int offset = 0;

            for (int page = 0; page < Document.Pages.Count; page++)
            {
                var size = TranslateSize(Document.Pages[page].PageSize);
                int height = (int)(size.Height * _scaleFactor);
                int fullHeight = height + ShadeBorder.Size.Vertical + PageMargin.Vertical;
                int width = (int)(size.Width * _scaleFactor);
                int maxFullWidth = (int)(_maxWidth * _scaleFactor) + ShadeBorder.Size.Horizontal + PageMargin.Horizontal;
                int fullWidth = width + ShadeBorder.Size.Horizontal + PageMargin.Horizontal;
                int thisLeftOffset = leftOffset + (maxFullWidth - fullWidth) / 2;

                while (_pageCache.Count <= page)
                {
                    _pageCache.Add(new PageCache());
                }

                var pageCache = _pageCache[page];

                if (pageCache.Image != null)
                {
                    pageCache.Image.Dispose();
                    pageCache.Image = null;
                }

                pageCache.CachedLinks = null;
                pageCache.Bounds = new Rectangle(
                    thisLeftOffset + ShadeBorder.Size.Left + PageMargin.Left,
                    offset + ShadeBorder.Size.Top + PageMargin.Top,
                    width,
                    height
                );
                pageCache.OuterBounds = new Rectangle(
                    thisLeftOffset,
                    offset,
                    width + ShadeBorder.Size.Horizontal + PageMargin.Horizontal,
                    height + ShadeBorder.Size.Vertical + PageMargin.Vertical
                );

                offset += fullHeight;
            }
        }

        private List<CachedLink> GetPageLinks(int page)
        {
            var pageCache = _pageCache[page];
            if (pageCache.CachedLinks == null)
            {
                pageCache.CachedLinks = new List<CachedLink>();
                foreach (var link in Document.Pages[page].GetPageLinks().Links)
                {
                    pageCache.CachedLinks.Add(new CachedLink(link, BoundsFromPdf(new PdfRectangle(page, link.Bounds), false)));
                }
            }

            return pageCache.CachedLinks;
        }

        private Rectangle GetScrollClientArea()
        {
            ScrollBars scrollBarsVisible;

            if (HScroll && VScroll)
                scrollBarsVisible = ScrollBars.Both;
            else if (HScroll)
                scrollBarsVisible = ScrollBars.Horizontal;
            else if (VScroll)
                scrollBarsVisible = ScrollBars.Vertical;
            else
                scrollBarsVisible = ScrollBars.None;

            return GetScrollClientArea(scrollBarsVisible);
        }

        private Rectangle GetScrollClientArea(ScrollBars scrollbars)
        {
            return new Rectangle(
                0,
                0,
                scrollbars == ScrollBars.Vertical || scrollbars == ScrollBars.Both ? Width - SystemInformation.VerticalScrollBarWidth : Width,
                scrollbars == ScrollBars.Horizontal || scrollbars == ScrollBars.Both ? Height - SystemInformation.HorizontalScrollBarHeight : Height
            );
        }

        private void UpdateScaleFactor(ScrollBars scrollBars)
        {
            var bounds = GetScrollClientArea(scrollBars);

            // Scale factor determines what we need to multiply the dimensions
            // of the metafile with to get the size in the control.

            var zoomMode = CalculateZoomModeForFitBest(bounds);

            if (zoomMode == PdfViewerZoomMode.FitHeight)
            {
                int height = bounds.Height - ShadeBorder.Size.Vertical - PageMargin.Vertical;

                _scaleFactor = ((double)height / _maxHeight) * Zoom;
            }
            else
            {
                int width = bounds.Width - ShadeBorder.Size.Horizontal - PageMargin.Horizontal;

                _scaleFactor = ((double)width / _maxWidth) * Zoom;
            }
        }

        private PdfViewerZoomMode CalculateZoomModeForFitBest(Rectangle bounds)
        {
            if (ZoomMode != PdfViewerZoomMode.FitBest)
            {
                return ZoomMode;
            }

            var controlScaleFactor = (double)bounds.Width / bounds.Height;

            return controlScaleFactor >= _documentScaleFactor ? PdfViewerZoomMode.FitHeight : PdfViewerZoomMode.FitWidth;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data. </param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Document == null || _suspendPaintCount > 0 || !_pageCacheValid)
                return;

            EnsureMarkers();

            var offset = GetScrollOffset();
            var bounds = GetScrollClientArea();

            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }

            _visiblePageStart = -1;
            _visiblePageEnd = -1;

            for (int page = 0; page < Document.Pages.Count; page++)
            {
                var pageCache = _pageCache[page];
                var rectangle = pageCache.OuterBounds;
                rectangle.Offset(offset.Width, offset.Height);

                if (_visiblePageStart == -1)
                {
                    if (rectangle.Bottom >= 0)
                    {
                        _visiblePageStart = page;
                    }
                    else if (pageCache.Image != null)
                    {
                        pageCache.Image.Dispose();
                        pageCache.Image = null;
                    }
                }

                if (rectangle.Top > bounds.Height)
                {
                    if (_visiblePageEnd == -1)
                        _visiblePageEnd = page - 1;

                    if (pageCache.Image != null)
                    {
                        pageCache.Image.Dispose();
                        pageCache.Image = null;
                    }
                }

                if (e.ClipRectangle.IntersectsWith(rectangle))
                {
                    var pageBounds = pageCache.Bounds;
                    pageBounds.Offset(offset.Width, offset.Height);

                    e.Graphics.FillRectangle(Brushes.White, pageBounds);

                    DrawPageImage(e.Graphics, page, pageBounds);

                    _shadeBorder.Draw(e.Graphics, pageBounds);

                    DrawMarkers(e.Graphics, page);

                    var selectionInfo = _textSelectionState;
                    if (selectionInfo != null)
                        DrawTextSelection(e.Graphics, page, selectionInfo.GetNormalized());

                    if (_currRect != Rectangle.Empty)
                    {
                        DrawRedRectangle(e.Graphics, page);
                    }
                }
            }

            if (_visiblePageStart == -1)
                _visiblePageStart = 0;
            if (_visiblePageEnd == -1)
                _visiblePageEnd = Document.PageCount - 1;
        }

        private void DrawTextSelection(Graphics graphics, int page, TextSelectionState state)
        {
            if (state.EndPage < 0 || state.EndIndex < 0)
                return;

            if (page >= state.StartPage && page <= state.EndPage)
            {
                int start = 0, end = 0;

                if (page == state.StartPage)
                    start = state.StartIndex;

                if (page == state.EndPage)
                    end = (state.EndIndex + 1);
                else
                    end = Document.Pages[page].CountChars();

                Region region = null;
                foreach (var rectangle in Document.Pages[page].GetTextRectangles(start, end - start))
                {
                    var pdfrectangle = new PdfRectangle(page, rectangle);
                    if (region == null)
                        region = new Region(BoundsFromPdf(pdfrectangle));
                    else
                        region.Union(BoundsFromPdf(pdfrectangle));
                }

                if (region != null)
                    graphics.FillRegion(_textSelectionBrush, region);
            }
        }

        private void DrawPageImage(Graphics graphics, int page, Rectangle pageBounds)
        {
            var pageCache = _pageCache[page];

            if (pageCache.Image == null)
                pageCache.Image = Document.Pages[page].Render(pageBounds.Width, pageBounds.Height, graphics.DpiX, graphics.DpiY, Rotation, Pdfium.Net.Wrapper.PdfRenderFlags.Annotations);

            graphics.DrawImageUnscaled(pageCache.Image, pageBounds.Location);
        }
        /// <summary>
        /// Gets the document bounds.
        /// </summary>
        /// <returns>The document bounds.</returns>
        protected override Rectangle GetDocumentBounds()
        {
            if (Document == null)
                return Rectangle.Empty;
            int scaledHeight = 0;

            for (int page = 0; page < Document.Pages.Count; page++)
            {
                var size = TranslateSize(Document.Pages[page].PageSize);
                scaledHeight += (int)(size.Height * _scaleFactor);
            }

            int height = (int)(scaledHeight + (ShadeBorder.Size.Vertical + PageMargin.Vertical) * Document.PageCount);
            int width = (int)(_maxWidth * _scaleFactor + ShadeBorder.Size.Horizontal + PageMargin.Horizontal);

            var center = new Point(
                DisplayRectangle.Width / 2,
                DisplayRectangle.Height / 2
            );

            if (
                DisplayRectangle.Width > ClientSize.Width ||
                DisplayRectangle.Height > ClientSize.Height
            )
            {
                center.X += DisplayRectangle.Left;
                center.Y += DisplayRectangle.Top;
            }

            return new Rectangle(
                center.X - width / 2,
                center.Y - height / 2,
                width,
                height
            );
        }
        /// <summary>
        /// Called whent he cursor changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnSetCursor(SetCursorEventArgs e)
        {
            _cachedLink = null;

            if (_pageCacheValid)
            {
                var offset = GetScrollOffset();

                var location = new Point(
                    e.Location.X - offset.Width,
                    e.Location.Y - offset.Height
                );
                for (int i = Markers.Count - 1; i >= 0; i--)
                {
                    if (Markers[i] is AnnotMarker)
                        Markers.RemoveAt(i);
                }
                for (int page = _visiblePageStart; page <= _visiblePageEnd; page++)
                {
                    foreach (var link in GetPageLinks(page))
                    {
                        if (link.Bounds.Contains(location))
                        {
                            _cachedLink = link;
                            e.Cursor = Cursors.Hand;
                            if (link.Link.LightAnnot != null && !string.IsNullOrEmpty(link.Link.LightAnnot.Content))
                            {
                                //var bounds = new RectangleF( pdfBounds.X1,pdfBounds.Y1, pdfBounds.X2- pdfBounds.X1,pdfBounds.Y1- pdfBounds.Y3);
                                var marker = new AnnotMarker(page, link.Link.LightAnnot, link.Link.Bounds, Zoom);
                                Markers.Add(marker);
                            }
                            return;
                        }
                    }
                }

                if (_cursorMode == PdfViewerCursorMode.TextSelection)
                {
                    var state = GetMouseState(e.Location);
                    if (state.CharacterIndex >= 0)
                    {
                        e.Cursor = Cursors.IBeam;
                        return;
                    }
                }
                else if (_cursorMode == PdfViewerCursorMode.Cross)
                {
                    e.Cursor = Cursors.Cross;
                    return;
                }
                else if (_cursorMode == PdfViewerCursorMode.Move)
                {
                    e.Cursor = Cursors.SizeAll;
                    return;
                }
                else if (_cursorMode == PdfViewerCursorMode.RightTop || _cursorMode == PdfViewerCursorMode.LeftBottom)
                {
                    e.Cursor = Cursors.SizeNESW;
                    return;
                }
                else if (_cursorMode == PdfViewerCursorMode.Top || _cursorMode == PdfViewerCursorMode.Bottom)
                {
                    e.Cursor = Cursors.SizeNS;
                    return;
                }
                else if (_cursorMode == PdfViewerCursorMode.LeftTop || _cursorMode == PdfViewerCursorMode.RightBottom)
                {
                    e.Cursor = Cursors.SizeNWSE;
                    return;
                }
                else if (_cursorMode == PdfViewerCursorMode.Left || _cursorMode == PdfViewerCursorMode.Right)
                {
                    e.Cursor = Cursors.SizeWE;
                    return;
                }
                else if (_cursorMode == PdfViewerCursorMode.Rotation)
                {
                    e.Cursor = _editCursor;
                    return;
                }
                else if (_cursorMode == PdfViewerCursorMode.None)
                {
                    e.Cursor = null;
                    return;
                }
            }
            base.OnSetCursor(e);
        }

        /// <summary>Raises the <see cref="E:System.Windows.Forms.Control.MouseDown" /> event.</summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data. </param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Document == null || e.Button != MouseButtons.Left)
                return;
            HandleMouseDownForLinks(e);

            if (_pdfOperate == PdfOperate.Copy)
            {
                HandleMouseDownForTextSelection(e);
            }
            else if (_pdfOperate == PdfOperate.Matching)
            {
                HandleMouseDownForDrawRedMouseDown(e);
            }
            else if (_pdfOperate == PdfOperate.Edit)
            {
                HandleMouseDownEditMarker(e);
            }
            else if (_pdfOperate == PdfOperate.Shear)
            {
                HandleMouseDownShear(e);
            }
        }
        /// <summary>Raises the <see cref="E:System.Windows.Forms.Control.MouseUp" /> event.</summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data. </param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (Document == null || e.Button != MouseButtons.Left)
                return;
            HandleMouseUpForLinks(e);

            if (_pdfOperate == PdfOperate.Copy)
            {
                HandleMouseUpForTextSelection(e);
            }
            else if (_pdfOperate == PdfOperate.Matching)
            {
                HandleMouseUpDrawRedRectangle(e);
            }
            else if (_pdfOperate == PdfOperate.Edit)
            {
                HandleMouseUpEditMarker(e);
            }
            else if (_pdfOperate == PdfOperate.Shear)
            {
                HandleMouseUpShear(e);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Document == null)
                return;
            var pdfPoint = this.PointToPdf(e.Location);
            PdfPointhandler?.Invoke(pdfPoint);
            if (_pdfOperate == PdfOperate.Copy)
            {
                HandleMouseMoveForTextSelection(e);
            }
            else if (_pdfOperate == PdfOperate.Matching)
            {
                HandleMouseMoveDrawRedRectangle(e);
            }
            else if (_pdfOperate == PdfOperate.Edit && Markers.Count > 0)
            {
                HandleMouseMoveEditMarker(e);
            }
            else if (_pdfOperate == PdfOperate.Shear)
            {
                HandleMouseMoveShear(e);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (Document == null || e.Button != MouseButtons.Left)
                return;
            if (_pdfOperate == PdfOperate.Copy)
            {
                HandleMouseDoubleClickForTextSelection(e);
            }
            else if (_pdfOperate == PdfOperate.Edit)
            {
                if (_currObjectInformation.PageObjType == PdfPageObjType.Text)
                {
                    var obj = _currObjectInformation.PdfPageobject;
                    var rect = BoundsFromPdf(new PdfRectangle(_currObjectInformation.Page, _currObjectInformation.ObjBounds));
                    var txt = obj.TextObjGetText(Document.Pages[_currObjectInformation.Page].PageText);
                    var fontSize = obj.TextObjGetFontSize();
                    var fontColor = Color.Black;
                    try { fontColor = obj.PageObjGetFillColor(); }
                    catch
                    {

                    }
                    var pdfFont = obj.TextObjGetFont();
                    var fontName = pdfFont.FontName;
                    var zoom = 1.3;
                    _editBox.Bounds = new Rectangle(rect.Location, new Size((int)(rect.Width * zoom), (int)(rect.Height * zoom)));
                    _editBox.Text = txt;
                    _editBox.OrgText = txt;
                    _editBox.FontSize = fontSize;
                    _editBox.FontColor = fontColor;
                    _editBox.ObjBounds = _currObjectInformation.ObjBounds;
                    _editBox.Font = new Font(fontName, (int)(fontSize * _scaleFactor / 1.5));
                    _editBox.PageObject = obj;
                    _editBox.Page = _currObjectInformation.Page;
                    this.Controls.Add(_editBox);
                    _editBox.BringToFront();
                    _editBox.Focus();
                }
            }
            else if (_pdfOperate == PdfOperate.Shear)
            {
                var shear = GetShearRect();
                if (shear == null)
                    return;
                var rect = shear.Rectangle;
                var bound = Document.Pages[Page].PageSize;
                rect.Y = (int)(bound.Height - rect.Y - rect.Height);

                #region
                //var point1 = this.PointToPdf(new Point(rectMarker.X, rectMarker.Y));
                //var point2 = this.PointToPdf(new Point(rectMarker.X + rectMarker.Width, rectMarker.Y + rectMarker.Height));
                //var bound = Document.Pages[Page].PageSize;
                //var rect = new Rectangle();
                //var x = (int)point1.Location.X;
                //var y = (int)Math.Abs(bound.Height - point1.Location.Y);
                //var x1 = (int)point2.Location.X;
                //var y1 = (int)Math.Abs(bound.Height - point2.Location.Y);
                //rect.X = Math.Min(x, x1);
                //rect.Y = Math.Min(y, y1);
                //rect.Width = Math.Abs(x1 - x);
                //rect.Height = Math.Abs(y1 - y);
                //if (Math.Abs(x1 - x) * Math.Abs(y1 - y) == 0)
                //    return;
                #endregion

                RemoveShearMarker();
                BoundedCutHandler?.Invoke(Page, rect);
            }
        }

        private void SetCurrObjectInformation(MouseEventArgs e)
        {
            var infos = Markers.Where(s => s.Page == Page && (s is CharacterMarker)).Select(s => ((CharacterMarker)s).ObjectInformation).FirstOrDefault();
            var info = FindObjectInformation(e.Location, infos);
            if (info == null)
                return;

            if (!info.IsSelect)
            {
                ResetObjectInformationHighlight(infos, true);
                info.IsSelect = true;
            }
            _currObjectInformation = info;
        }
        private void RemoveEditTextBox()
        {
            if (_pdfOperate == PdfOperate.Edit && _editBox != null && this.Controls.Contains(_editBox))
            {
                if (_editBox.OrgText != _editBox.Text && _editBox.PageObject != null && !_editBox.PageObject.IsNull)
                {
                    var page = Document.Pages[_editBox.Page];
                    var txt = _editBox.Text.Trim(new char[] { ' ', '\n', '\r', '\t' });
                    if (string.IsNullOrEmpty(txt))
                    {
                        if (page.RemoveObject(_editBox.PageObject))
                        {
                            if (page.GenerateContent())
                                Invalidate();
                        }
                    }
                    else
                    {
                        if (_editBox.PageObject.TextSetText(txt))
                        {
                            if (page.GenerateContent())
                                Invalidate();
                        }
                    }
                }
                this.Controls.Remove(_editBox);
                _editBox.Text = "";
                _editBox.OrgText = "";
                _currObjectInformation = null;
                _cursorMode = PdfViewerCursorMode.None;
            }
        }
        private void HandleMouseDownEditMarker(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            SetCurrObjectInformation(e);
            _currRect.X = e.X;
            _currRect.Y = e.Y;
            if (_currObjectInformation != null && _currObjectInformation.IsObjectValid)
            {
                switch (_cursorMode)
                {
                    case PdfViewerCursorMode.Move:
                    case PdfViewerCursorMode.LeftTop:
                    case PdfViewerCursorMode.Top:
                    case PdfViewerCursorMode.RightTop:
                    case PdfViewerCursorMode.Right:
                    case PdfViewerCursorMode.RightBottom:
                    case PdfViewerCursorMode.Bottom:
                    case PdfViewerCursorMode.LeftBottom:
                    case PdfViewerCursorMode.Left:
                    case PdfViewerCursorMode.Rotation:
                        {
                            //var pdfRect = new PdfRectangle(_currObjectInformation.Page, new Rectangle((int)(_currObjectInformation.ObjMatrix.E), (int)(_currObjectInformation.ObjMatrix.F), (int)_currObjectInformation.ObjMatrix.A, (int)_currObjectInformation.ObjMatrix.D));
                            var pdfRect = new PdfRectangle(_currObjectInformation.Page, _currObjectInformation.PdfPageobject.PageObjGetBounds().ToRectangle());
                            var rect = BoundsFromPdf(pdfRect);
                            //rect = this.RectangleToScreen(rect);
                            _currObjectInformation.ObjRectangle = rect;
                            //ControlPaint.DrawReversibleFrame(rect, Color.White, FrameStyle.Thick);
                            Invalidate();

                            break;
                        }
                }
            }
        }

        private void HandleMouseMoveEditMarker(MouseEventArgs e)
        {
            var infos = Markers.Where(s => s.Page == Page && (s is CharacterMarker)).Select(s => ((CharacterMarker)s).ObjectInformation).FirstOrDefault();
            ResetObjectInformationHighlight(infos);
            var info = FindObjectInformation(e.Location, infos);

            if (info != null)
            {
                if (e.Button != MouseButtons.Left)
                {
                    info.Highlight = true;
                    if (info.PageObjType == PdfPageObjType.Text
                        || info.PageObjType == PdfPageObjType.Path)
                    {
                        if (info.IsSelect)
                        {
                            var bounds = BoundsFromPdf(new PdfRectangle(Page, info.ObjBounds));
                            var controlBlockIndex = CharacterHelper.GetSelectedHandle(e.Location, bounds);

                            switch (controlBlockIndex)
                            {
                                case 0:
                                    _cursorMode = PdfViewerCursorMode.Move;
                                    break;
                                case 9:
                                    _cursorMode = PdfViewerCursorMode.Rotation;
                                    break;
                                default:
                                    _cursorMode = PdfViewerCursorMode.None;
                                    break;
                            }
                        }
                        else
                        {
                            _cursorMode = PdfViewerCursorMode.None;
                        }

                    }
                    else if (info.PageObjType == PdfPageObjType.Image)
                    {
                        if (info.IsSelect)
                        {
                            var bounds = BoundsFromPdf(new PdfRectangle(Page, info.ObjBounds));
                            var controlBlockIndex = CharacterHelper.GetSelectedHandle(e.Location, bounds);

                            switch (controlBlockIndex)
                            {
                                case 0:
                                    _cursorMode = PdfViewerCursorMode.Move;
                                    break;
                                case 1:
                                    _cursorMode = PdfViewerCursorMode.LeftTop;
                                    break;
                                case 2:
                                    _cursorMode = PdfViewerCursorMode.Top;
                                    break;
                                case 3:
                                    _cursorMode = PdfViewerCursorMode.RightTop;
                                    break;
                                case 4:
                                    _cursorMode = PdfViewerCursorMode.Right;
                                    break;
                                case 5:
                                    _cursorMode = PdfViewerCursorMode.RightBottom;
                                    break;
                                case 6:
                                    _cursorMode = PdfViewerCursorMode.Bottom;
                                    break;
                                case 7:
                                    _cursorMode = PdfViewerCursorMode.LeftBottom;
                                    break;
                                case 8:
                                    _cursorMode = PdfViewerCursorMode.Left;
                                    break;
                                case 9:
                                    _cursorMode = PdfViewerCursorMode.Rotation;
                                    break;
                                default:
                                    _cursorMode = PdfViewerCursorMode.None;
                                    break;
                            }
                        }
                        else
                        {
                            _cursorMode = PdfViewerCursorMode.None;
                        }
                    }
                    else
                    {
                        _cursorMode = PdfViewerCursorMode.Move;
                    }
                    Invalidate();
                }
            }
            else
            {
                if (_currRect == Rectangle.Empty)
                    _cursorMode = PdfViewerCursorMode.None;
            }

            if (_currObjectInformation != null && _currRect != Rectangle.Empty && e.Button == MouseButtons.Left)
            {
                var rect = _currObjectInformation.ObjRectangle;
                var point1 = this.PointToPdf(new Point(_currRect.X, _currRect.Y));
                var point2 = this.PointToPdf(e.Location);
                int x = (int)point1.Location.X;
                int y = (int)point2.Location.Y;
                int x1 = (int)point2.Location.X;
                int y1 = (int)point1.Location.Y;
                if (Math.Abs(x1 - x) * Math.Abs(y1 - y) == 0)
                    return;
                var width = x1 - x;
                var height = y1 - y;
                switch (_cursorMode)
                {
                    case PdfViewerCursorMode.Move:
                        {
                            rect.Offset(e.X - _currRect.X, e.Y - _currRect.Y);
                            _currObjectInformation.ObjMoveRectangle = rect;
                            break;
                        }
                    case PdfViewerCursorMode.LeftTop:
                        {
                            _currObjectInformation.ObjMoveRectangle = new Rectangle(rect.X + width, rect.Y + height, rect.Width - width, rect.Height - height);
                            break;
                        }
                    case PdfViewerCursorMode.Top:
                        {
                            _currObjectInformation.ObjMoveRectangle = new Rectangle(rect.X, rect.Y + height, rect.Width, rect.Height - height);
                            break;
                        }
                    case PdfViewerCursorMode.RightTop:
                        {
                            _currObjectInformation.ObjMoveRectangle = new Rectangle(rect.X, rect.Y + height, rect.Width + width, rect.Height - height);
                            break;
                        }
                    case PdfViewerCursorMode.Right:
                        {
                            _currObjectInformation.ObjMoveRectangle = new Rectangle(rect.X, rect.Y, rect.Width + width, rect.Height);
                            break;
                        }
                    case PdfViewerCursorMode.RightBottom:
                        {
                            _currObjectInformation.ObjMoveRectangle = new Rectangle(rect.X, rect.Y, rect.Width + width, rect.Height + height);
                            break;
                        }
                    case PdfViewerCursorMode.Bottom:
                        {
                            _currObjectInformation.ObjMoveRectangle = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height + height);
                            break;
                        }
                    case PdfViewerCursorMode.LeftBottom:
                        {
                            _currObjectInformation.ObjMoveRectangle = new Rectangle(rect.X + width, rect.Y, rect.Width - width, rect.Height + height);
                            break;
                        }
                    case PdfViewerCursorMode.Left:
                        {
                            _currObjectInformation.ObjMoveRectangle = new Rectangle(rect.X + width, rect.Y, rect.Width - width, rect.Height);
                            break;
                        }
                    case PdfViewerCursorMode.Rotation:
                        {
                            _currObjectInformation.Direction = CharacterHelper.Direction(width, -height) - 90;
                            _currObjectInformation.DirectionEnd = point2.Location;
                            break;
                        }
                    default:
                        break;
                }
                Invalidate();
            }
        }

        private void HandleMouseUpEditMarker(MouseEventArgs e)
        {
            if (_currObjectInformation == null)
                return;
            var point1 = this.PointToPdf(new Point(_currRect.X, _currRect.Y));
            var point2 = this.PointToPdf(e.Location);
            int x = (int)point1.Location.X;
            int y = (int)point2.Location.Y;
            int x1 = (int)point2.Location.X;
            int y1 = (int)point1.Location.Y;
            _currRect = Rectangle.Empty;
            _currObjectInformation.ObjRectangle = Rectangle.Empty;
            _currObjectInformation.ObjMoveRectangle = Rectangle.Empty;
            _currObjectInformation.Direction = -1;
            _currObjectInformation.DirectionEnd = PointF.Empty;
            if (Math.Abs(x1 - x) * Math.Abs(y1 - y) == 0)
                return;
            var width = x1 - x;
            var height = y1 - y;

            if (_currObjectInformation.PageObjType == PdfPageObjType.Image
                || _currObjectInformation.PageObjType == PdfPageObjType.Text
                || _currObjectInformation.PageObjType == PdfPageObjType.Path)
            {
                if (_currObjectInformation.IsObjectValid)
                {
                    var obj = _currObjectInformation.PdfPageobject;
                    var matrix = obj.PageObjGetMatrix();
                    bool res = false;
                    switch (_cursorMode)
                    {
                        case PdfViewerCursorMode.Move:
                            {
                                res = obj.SetMatrix(1, 0, 0, 1, width, -height);
                                break;
                            }
                        case PdfViewerCursorMode.LeftTop:
                            {
                                res = obj.SetMatrix(1 - width / matrix.A, 0, 0, 1 - height / matrix.D, width, 0);
                                break;
                            }
                        case PdfViewerCursorMode.Top:
                            {
                                res = obj.SetMatrix(1, 0, 0, 1 - height / matrix.D, 0, 0);
                                break;
                            }
                        case PdfViewerCursorMode.RightTop:
                            {
                                res = obj.SetMatrix(1 + width / matrix.A, 0, 0, 1 - height / matrix.D, 0, 0);
                                break;
                            }
                        case PdfViewerCursorMode.Right:
                            {
                                res = obj.SetMatrix(1 + width / matrix.A, 0, 0, 1, 0, 0);
                                break;
                            }
                        case PdfViewerCursorMode.RightBottom:
                            {
                                res = obj.SetMatrix(1 + width / matrix.A, 0, 0, 1 + height / matrix.D, 0, -height);
                                break;
                            }
                        case PdfViewerCursorMode.Bottom:
                            {
                                res = obj.SetMatrix(1, 0, 0, 1 + height / matrix.D, 0, -height);
                                break;
                            }
                        case PdfViewerCursorMode.LeftBottom:
                            {
                                res = obj.SetMatrix(1 - width / matrix.A, 0, 0, 1 + height / matrix.D, width, -height);
                                break;
                            }
                        case PdfViewerCursorMode.Left:
                            {
                                res = obj.SetMatrix(1 - width / matrix.A, 0, 0, 1, width, 0);
                                break;
                            }
                        case PdfViewerCursorMode.Rotation:
                            {
                                var angle = 90 - CharacterHelper.Direction(width, -height);
                                _currObjectInformation.DirectionEnd = point2.Location;
                                double angleValue = (angle / 180.0d) * Math.PI;

                                res = obj.SetMatrix((float)Math.Cos(angleValue), (float)-Math.Sin(angleValue), (float)Math.Sin(angleValue), (float)Math.Cos(angleValue), 0, 0);
                                break;
                            }
                        default:
                            break;
                    }
                    if (res)
                    {
                        var page = Document.Pages[_currObjectInformation.Page];
                        if (page.GenerateContent())
                        {
                            _currObjectInformation.ObjBounds = _currObjectInformation.PdfPageobject.PageObjGetBounds().ToRectangle();
                            ResetCacheImage(_currObjectInformation.Page);
                            Invalidate();
                            BoundedEditHandler?.Invoke(_currObjectInformation.Page, _currObjectInformation.PdfPageobject);
                        }
                    }
                }
            }
        }
        private void ResetObjectInformationHighlight(List<ObjectInformation> infos, bool resetIsSelect = false)
        {
            if (infos == null)
                return;
            foreach (var item in infos)
            {
                if (resetIsSelect)
                {
                    item.IsSelect = false;
                    RemoveEditTextBox();
                }
                else
                {
                    item.Highlight = false;
                }
                if (item.SubObjectInformations != null)
                {
                    ResetObjectInformationHighlight(item.SubObjectInformations, resetIsSelect);
                }
            }
        }

        private void ResetCacheImage(int page)
        {
            var pageCache = _pageCache[page];

            if (pageCache.Image != null)
            {
                pageCache.Image.Dispose();
                pageCache.Image = null;
            }
        }

        private ObjectInformation FindObjectInformation(Point point, List<ObjectInformation> infos)
        {
            if (infos != null)
            {
                var info1 = infos.Where(s => BoundsFromPdf(new PdfRectangle(s.Page, s.ObjBounds)).Contains(point)).ToList();
                if (infos.Count > 2 && BoundsFromPdf(new PdfRectangle(infos[1].Page, infos[1].ObjBounds)).Contains(point))
                {

                }
                if (info1 != null && info1.Count > 0)
                {
                    {
                        var temp = info1.Where(s => (s.PageObjType != PdfPageObjType.UnKnown && s.PageObjType != PdfPageObjType.Shading && s.PageObjType != PdfPageObjType.Form))?.ToList();
                        if (temp != null && temp.Count >= 1)
                        {
                            temp.Sort((x, y) => x.ObjBounds.Width.CompareTo(y.ObjBounds.Width));
                            return temp.FirstOrDefault();
                        }
                    }
                    var isGo = false;
                    foreach (var item in info1)
                    {
                        if (!(item.PageObjType != PdfPageObjType.UnKnown && item.PageObjType != PdfPageObjType.Shading && item.PageObjType != PdfPageObjType.Form))
                        {
                            isGo = true;
                            break;
                        }
                    }
                    if (isGo)
                    {
                        foreach (var item in infos)
                        {
                            var info = FindObjectInformation(point, item.SubObjectInformations);
                            if (info != null)
                            {
                                return info;
                            }
                        }
                    }
                    else
                    {
                        var temp = info1.Where(s => (s.PageObjType != PdfPageObjType.UnKnown && s.PageObjType != PdfPageObjType.Shading && s.PageObjType != PdfPageObjType.Form))?.ToList();
                        temp.Sort((x, y) => x.ObjBounds.Width.CompareTo(y.ObjBounds.Width));
                        return temp.FirstOrDefault();
                    }
                }
                else
                {
                    foreach (var item in infos)
                    {
                        var info = FindObjectInformation(point, item.SubObjectInformations);
                        if (info != null)
                        {
                            return info;
                        }
                    }
                }
            }
            return null;
        }

        private ObjectInformation FindObjectInformation(PdfPageobject pageobject, List<ObjectInformation> infos)
        {
            if (infos != null)
            {
                var info1 = infos.Where(s => ReferenceEquals(pageobject, s.PdfPageobject));
                if (info1 != null)
                    return info1.FirstOrDefault();
                else
                {
                    foreach (var item in info1)
                    {
                        FindObjectInformation(pageobject, item.SubObjectInformations);
                    }
                }
            }
            return null;
        }

        private void RemoveMarker(List<ObjectInformation> infos)
        {
            for (int i = infos.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_currObjectInformation, infos[i]))
                {
                    infos.Remove(infos[i]);
                    break;
                }
                if (infos[i].SubObjectInformations != null)
                {
                    RemoveMarker(infos[i].SubObjectInformations);
                }
            }
        }

        private void HandleMouseDownForLinks(MouseEventArgs e)
        {
            _dragState = null;

            if (_cachedLink != null)
            {
                _dragState = new DragState
                {
                    Link = _cachedLink.Link,
                    Location = e.Location
                };
            }
        }

        private void HandleMouseUpForLinks(MouseEventArgs e)
        {
            if (_dragState == null)
                return;

            int dx = Math.Abs(e.Location.X - _dragState.Location.X);
            int dy = Math.Abs(e.Location.Y - _dragState.Location.Y);

            var link = _dragState.Link;
            _dragState = null;

            if (link == null)
                return;

            if (dx <= SystemInformation.DragSize.Width && dy <= SystemInformation.DragSize.Height)
            {
                var linkClickEventArgs = new LinkClickEventArgs(link);
                HandleLinkClick(linkClickEventArgs);
            }
        }

        private void HandleLinkClick(LinkClickEventArgs e)
        {
            OnLinkClick(e);

            if (e.Handled)
                return;

            if (e.Link.TargetPage.HasValue)
                Page = e.Link.TargetPage.Value;

            if (e.Link.Uri != null)
            {
                try
                {
                    Process.Start(e.Link.Uri);
                }
                catch
                {
                    // Some browsers (Firefox) will cause an exception to
                    // be thrown (when it auto-updates).
                }
            }
        }

        private void HandleMouseDownForDrawRedMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            _currRect.X = e.X;
            _currRect.Y = e.Y;
        }
        private void HandleMouseMoveDrawRedRectangle(MouseEventArgs e)
        {
            if (!_currRect.IsEmpty)
            {
                _currRect.Width = e.X - _currRect.X;
                _currRect.Height = e.Y - _currRect.Y;
            }
            Invalidate();
        }
        private void HandleMouseUpDrawRedRectangle(MouseEventArgs e)
        {
            var point1 = this.PointToPdf(new Point(_currRect.X, _currRect.Y));
            var point2 = this.PointToPdf(e.Location);
            if (this.Document.Pages[Page].GetPdfEditable())
            {
                int x = (int)point1.Location.X;
                int y = (int)point2.Location.Y;
                int x1 = (int)point2.Location.X;
                int y1 = (int)point1.Location.Y;
                var txt = this.Document.Pages[Page].GetBoundedText(x, y, x1, y1);
                _currRect = Rectangle.Empty;
                if (Math.Abs(x1 - x) * Math.Abs(y1 - y) == 0)
                    return;
                BoundedTextHandler?.Invoke(Page, x, y, x1, y1, txt, true);
            }
            else
            {
                var x = (int)point1.Location.X;
                var y = (int)point1.Location.Y;
                var x1 = (int)point2.Location.X;
                var y1 = (int)point2.Location.Y;
                _currRect = Rectangle.Empty;
                if (Math.Abs(x1 - x) * Math.Abs(y1 - y) == 0)
                    return;
                BoundedTextHandler?.Invoke(Page, x, y, x1, y1, "", false);
            }
        }


        private void HandleMouseDownShear(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            _currRect.X = e.X;
            _currRect.Y = e.Y;
        }
        private void HandleMouseMoveShear(MouseEventArgs e)
        {
            var shear = GetShearRect();
            if (shear == null)
            {
                if (e.Button == MouseButtons.Left)
                {
                    _currRect.Width = e.X - _currRect.X;
                    _currRect.Height = e.Y - _currRect.Y;
                    Invalidate();
                }
            }
            else
            {
                if (e.Button == MouseButtons.None)
                {
                    var rect = BoundsFromPdf(new PdfRectangle(Page, shear.Rectangle));
                    var controlBlockIndex = CharacterHelper.GetSelectedHandle(e.Location, rect);

                    switch (controlBlockIndex)
                    {
                        case 0:
                            _cursorMode = PdfViewerCursorMode.Move;
                            break;
                        case 1:
                            _cursorMode = PdfViewerCursorMode.LeftTop;
                            break;
                        case 2:
                            _cursorMode = PdfViewerCursorMode.Top;
                            break;
                        case 3:
                            _cursorMode = PdfViewerCursorMode.RightTop;
                            break;
                        case 4:
                            _cursorMode = PdfViewerCursorMode.Right;
                            break;
                        case 5:
                            _cursorMode = PdfViewerCursorMode.RightBottom;
                            break;
                        case 6:
                            _cursorMode = PdfViewerCursorMode.Bottom;
                            break;
                        case 7:
                            _cursorMode = PdfViewerCursorMode.LeftBottom;
                            break;
                        case 8:
                            _cursorMode = PdfViewerCursorMode.Left;
                            break;
                        case 9:
                            _cursorMode = PdfViewerCursorMode.Rotation;
                            break;
                        default:
                            _cursorMode = PdfViewerCursorMode.None;
                            break;
                    }
                }
                else if (e.Button == MouseButtons.Left)
                {
                    var rect = GetRectangleWithOffset(shear.Rectangle, e);
                    if (rect == Rectangle.Empty)
                        return;
                    shear.MoveRectangle = rect;
                    Invalidate();
                }
            }
        }

        private void HandleMouseUpShear(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            var shear = GetShearRect();
            if (shear == null)
            {
                if (_currRect == Rectangle.Empty)
                    return;
                var point1 = this.PointToPdf(new Point(_currRect.X, _currRect.Y));
                var point2 = this.PointToPdf(e.Location);

                var rect = new Rectangle();
                int x = (int)point1.Location.X;
                int y = (int)point2.Location.Y;
                int x1 = (int)point2.Location.X;
                int y1 = (int)point1.Location.Y;

                rect.X = Math.Min(x, x1);
                rect.Y = Math.Min(y, y1);
                rect.Width = Math.Abs(x1 - x);
                rect.Height = Math.Abs(y1 - y);

                if (Math.Abs(x1 - x) * Math.Abs(y1 - y) == 0)
                    return;
                RemoveShearMarker();
                var marker = new ShearMarker(Page, rect);
                Markers.Add(marker);
            }
            else
            {
                var rect = GetRectangleWithOffset(shear.Rectangle, e);
                if (rect == Rectangle.Empty)
                    return;
                shear.Rectangle = rect;
                Invalidate();
            }
            _currRect = Rectangle.Empty;
        }

        private Rectangle GetRectangleWithOffset(Rectangle rect, MouseEventArgs e)
        {
            var point1 = this.PointToPdf(new Point(_currRect.X, _currRect.Y));
            var point2 = this.PointToPdf(e.Location);
            int x = (int)point1.Location.X;
            int y = (int)point2.Location.Y;
            int x1 = (int)point2.Location.X;
            int y1 = (int)point1.Location.Y;
            if (Math.Abs(x1 - x) * Math.Abs(y1 - y) == 0)
                return Rectangle.Empty;
            var width = x1 - x;
            var height = y1 - y;
            switch (_cursorMode)
            {
                case PdfViewerCursorMode.Move:
                    {
                        rect.X = rect.X + width;
                        rect.Y = rect.Y - height;
                        break;
                    }
                case PdfViewerCursorMode.LeftTop:
                    {
                        rect.X = rect.X + width;
                        rect.Width = rect.Width - width;
                        rect.Height = rect.Height - height;
                        break;
                    }
                case PdfViewerCursorMode.Top:
                    {
                        rect.Y = rect.Y - height;
                        break;
                    }
                case PdfViewerCursorMode.RightTop:
                    {
                        rect.Width = rect.Width + width;
                        rect.Height = rect.Height - height;
                        break;
                    }
                case PdfViewerCursorMode.Right:
                    {
                        rect.Width = rect.Width + width;
                        break;
                    }
                case PdfViewerCursorMode.RightBottom:
                    {
                        rect.Width = rect.Width + width;
                        rect.Height = rect.Height + height;
                        rect.Y = rect.Y - height;
                        break;
                    }
                case PdfViewerCursorMode.Bottom:
                    {
                        rect.Height = rect.Height + height;
                        rect.Y = rect.Y - height;
                        break;
                    }
                case PdfViewerCursorMode.LeftBottom:
                    {
                        rect.Width = rect.Width - width;
                        rect.Height = rect.Height + height;
                        rect.X = rect.X + width;
                        rect.Y = rect.Y - height;
                        break;
                    }
                case PdfViewerCursorMode.Left:
                    {
                        rect.Width = rect.Width - width;
                        rect.X = rect.X + width;
                        break;
                    }
                default:
                    break;
            }
            return rect;
        }

        private void DrawRedRectangle(Graphics g, int page)
        {
            Pen pen = new Pen(Color.Red, 1.0f);
            g.DrawRectangle(pen, _currRect);
        }

        private void HandleMouseDownForTextSelection(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            var pdfLocation = PointToPdf(e.Location);
            if (!pdfLocation.IsValid)
                return;

            var characterIndex = Document.GetCharacterIndexAtPosition(pdfLocation.Page, pdfLocation.Location, 4f, 4f);

            if (characterIndex >= 0)
            {
                _textSelectionState = new TextSelectionState()
                {
                    StartPage = pdfLocation.Page,
                    StartIndex = characterIndex,
                    EndPage = -1,
                    EndIndex = -1
                };
                _isSelectingText = true;
                Capture = true;
            }
            else
            {
                _isSelectingText = false;
                Capture = false;
                _textSelectionState = null;
            }
        }
        private void HandleMouseUpForTextSelection(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            _isSelectingText = false;
            Capture = false;
            Invalidate();
        }

        private void HandleMouseMoveForTextSelection(MouseEventArgs e)
        {
            if (!_isSelectingText)
                return;

            var mouseState = GetMouseState(e.Location);

            if (mouseState.CharacterIndex >= 0)
            {
                _textSelectionState.EndPage = mouseState.PdfLocation.Page;
                _textSelectionState.EndIndex = mouseState.CharacterIndex;

                Invalidate();
            }
        }

        private void HandleMouseDoubleClickForTextSelection(MouseEventArgs e)
        {
            var pdfLocation = PointToPdf(e.Location);
            if (!pdfLocation.IsValid)
                return;

            if (Document.GetWordAtPosition(pdfLocation.Page, pdfLocation.Location, 4f, 4f, out var word))
            {
                _textSelectionState = new TextSelectionState()
                {
                    StartPage = pdfLocation.Page,
                    EndPage = pdfLocation.Page,
                    StartIndex = word.Offset,
                    EndIndex = word.Offset + word.Length
                };

                Invalidate();
            }
        }

        private MouseState GetMouseState(Point mouseLocation)
        {
            // OnMouseMove and OnSetCursor get invoked a lot, often multiple times in succession for the same point.
            // By just caching the mouse state for the last known position we can save a lot of work.

            var currentState = _cachedMouseState;
            if (currentState?.MouseLocation == mouseLocation)
                return currentState;

            _cachedMouseState = new MouseState()
            {
                MouseLocation = mouseLocation,
                PdfLocation = PointToPdf(mouseLocation)
            };

            if (!_cachedMouseState.PdfLocation.IsValid)
                return _cachedMouseState;

            _cachedMouseState.CharacterIndex = Document.GetCharacterIndexAtPosition(_cachedMouseState.PdfLocation.Page, _cachedMouseState.PdfLocation.Location, 4f, 4f);

            return _cachedMouseState;
        }

        /// <summary>
        /// Occurs when a link in the pdf document is clicked.
        /// </summary>
        [Category("Action")]
        [Description("Occurs when a link in the pdf document is clicked.")]
        public event LinkClickEventHandler LinkClick;

        /// <summary>
        /// Called when a link is clicked.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnLinkClick(LinkClickEventArgs e)
        {
            var handler = LinkClick;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Rotate the PDF document left.
        /// </summary>
        public void RotateLeft()
        {
            Rotation = (PdfRotation)(((int)Rotation + 3) % 4);
        }

        /// <summary>
        /// Rotate the PDF document right.
        /// </summary>
        public void RotateRight()
        {
            Rotation = (PdfRotation)(((int)Rotation + 1) % 4);
        }

        private void ResetFromRotation()
        {
            var offsetX = (double)DisplayRectangle.Left / DisplayRectangle.Width;
            var offsetY = (double)DisplayRectangle.Top / DisplayRectangle.Height;

            ReloadDocument();

            SetDisplayRectLocation(new Point(
                (int)(DisplayRectangle.Width * offsetX),
                (int)(DisplayRectangle.Height * offsetY)
            ));
        }

        private SizeF TranslateSize(SizeF size)
        {
            switch (Rotation)
            {
                case PdfRotation.Rotate90:
                case PdfRotation.Rotate270:
                    return new SizeF(size.Height, size.Width);

                default:
                    return size;
            }
        }
        /// <summary>
        /// Called when the zoom level changes.
        /// </summary>
        /// <param name="zoom">The new zoom level.</param>
        /// <param name="focus">The location to focus on.</param>
        protected override void SetZoom(double zoom, Point? focus)
        {
            //Point location;

            //if (focus.HasValue)
            //{
            //    var bounds = GetDocumentBounds();

            //    location = new Point(
            //        focus.Value.X - bounds.X,
            //        focus.Value.Y - bounds.Y
            //    );
            //}
            //else
            //{
            //    var bounds = _pageCacheValid
            //        ? _pageCache[Page].Bounds
            //        : GetDocumentBounds();

            //    location = new Point(
            //        bounds.X,
            //        bounds.Y
            //    );
            //}

            //double oldScale = Zoom;

            //base.SetZoom(zoom, null);

            //var newLocation = new Point(
            //    (int)(location.X * (zoom / oldScale)),
            //    (int)(location.Y * (zoom / oldScale))
            //);

            //SetDisplayRectLocation(
            //    new Point(
            //        DisplayRectangle.Left - (newLocation.X - location.X),
            //        DisplayRectangle.Top - (newLocation.Y - location.Y)
            //    ),
            //    false
            //);
            base.SetZoom(zoom, null);
            OnDisplayRectangleChanged(EventArgs.Empty);
        }

        private void RedrawMarkers()
        {
            _markers = null;
            Invalidate();
        }

        private void EnsureMarkers()
        {
            if (_markers != null)
                return;

            _markers = new List<IPdfMarker>[_pageCache.Count];

            foreach (var marker in Markers)
            {
                if (marker.Page < 0 || marker.Page >= _markers.Length)
                    continue;

                if (_markers[marker.Page] == null)
                    _markers[marker.Page] = new List<IPdfMarker>();

                _markers[marker.Page].Add(marker);
            }
        }

        private void DrawMarkers(Graphics graphics, int page)
        {
            var markers = _markers[page];
            if (markers == null)
                return;

            foreach (var marker in markers)
            {
                marker.Draw(this, graphics);
            }
        }

        private void IniCharacterMarker()
        {
            if (Document == null || !_pageCacheValid || _pageCache == null)
                return;
            if (_pdfOperate == PdfOperate.Edit)
            {
                for (int i = 0; i < _pageCache.Count; i++)
                {
                    if (_pageCache[i].ObjectInformation == null)
                    {
                        var page = Document.Pages[i];

                        if (!page.IsNull)
                        {
                            if (_pageCache[i].ObjectInformation == null)
                            {
                                var infos = new List<ObjectInformation>();
                                var count = page.GetObjectsCount();
                                for (int j = 0; j < count; j++)
                                {
                                    var obj = page.GetObject(j);
                                    GetObject(obj, infos, i, new List<int> { j }, page.PageText);
                                }
                                _pageCache[i].ObjectInformation = infos;
                                var marker = new CharacterMarker(i, infos);
                                Markers.Add(marker);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < _pageCache.Count; i++)
                {
                    _pageCache[i].ObjectInformation?.Clear();
                    _pageCache[i].ObjectInformation = null;
                }

                for (int i = Markers.Count - 1; i >= 0; i--)
                {
                    if (Markers[i] is CharacterMarker)
                        Markers.RemoveAt(i);
                }
            }
        }

        private void RemoveShearMarker(bool reset = true)
        {
            _currRect = Rectangle.Empty;
            for (int i = Markers.Count - 1; i >= 0; i--)
            {
                if (Markers[i] is ShearMarker)
                    Markers.RemoveAt(i);
            }
            if (reset)
                _cursorMode = PdfViewerCursorMode.Cross;
        }

        private ShearMarker GetShearRect()
        {
            var marker = Markers.FirstOrDefault(s => s is ShearMarker);
            if (marker == null)
                return null;
            return (ShearMarker)marker;
        }

        private void GetObject(PdfPageobject obj, List<ObjectInformation> infos, int ipage, List<int> objIndex, PdfTextpage textpage)
        {
            if (!obj.IsNull)
            {
                var objType = obj.PageObjGetObjType();
                var rect = obj.PageObjGetBounds().ToRectangle();
                var cainfo = new ObjectInformation(ipage, obj, objIndex, objType, rect);
                var sunObjCount = obj.FormObjCountObjects();
                if (sunObjCount > 0)
                {
                    cainfo.SubObjectInformations = new List<ObjectInformation>();
                    for (int sub = 0; sub < sunObjCount; sub++)
                    {
                        var subObj = obj.FormObjGetObject(sub);
                        var indexs = new List<int>(objIndex);
                        indexs.Add(sub);
                        GetObject(subObj, cainfo.SubObjectInformations, ipage, indexs, textpage);
                    }
                }

                if (_isEditMinimum)
                {
                    if (cainfo.PageObjType == PdfPageObjType.Text || cainfo.PageObjType == PdfPageObjType.Image || cainfo.SubObjectInformations?.Count > 0)
                        infos.Add(cainfo);
                }
                else
                {
                    infos.Add(cainfo);
                }
            }
        }

        /// <summary>
        /// Scroll the PDF bounds into view.
        /// </summary>
        /// <param name="bounds">The PDF bounds to scroll into view.</param>
        public void ScrollIntoView(PdfRectangle bounds)
        {
            ScrollIntoView(BoundsFromPdf(bounds));
        }

        /// <summary>
        /// Scroll the client rectangle into view.
        /// </summary>
        /// <param name="rectangle">The client rectangle to scroll into view.</param>
        public void ScrollIntoView(Rectangle rectangle)
        {
            var clientArea = GetScrollClientArea();

            if (rectangle.Top < 0 || rectangle.Bottom > clientArea.Height)
            {
                var displayRectangle = DisplayRectangle;
                int center = rectangle.Top + rectangle.Height / 2;
                int documentCenter = center - displayRectangle.Y;
                int displayCenter = clientArea.Height / 2;
                int offset = documentCenter - displayCenter;

                SetDisplayRectLocation(new Point(
                    displayRectangle.X,
                    -offset
                ));
            }
        }
        internal void Close()
        {
            if (Document == null || !_pageCacheValid)
                return;
            Document?.Close();
            Document = null;
            DisposeCache();
            foreach (var marker in _markers)
            {
                marker?.Clear();
            }
            PdfOperate = PdfOperate.None;
            _dragState = null;
            _cachedLink = null;
            _cachedMouseState = null;
            _textSelectionState = null;
            _currRect = Rectangle.Empty;
            _pageCacheValid = false;
            _visiblePageStart = 0;
            _visiblePageEnd = 0;
            Markers?.Clear();
            _editCursor?.Dispose();
            _editCursor = null;
            if (_editBox != null)
            {
                _editBox.ModifyTextFontHandler -= ModifyTextFontHandler;
                _editBox.Dispose();
                _editBox = null;
            }

            _currObjectInformation?.SubObjectInformations?.Clear();
            _currObjectInformation = null;
        }

        private void DisposeCache()
        {
            _pageCache.ForEach(s => s.Close());
            _pageCache?.Clear();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.Control"/> and its child controls and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_shadeBorder != null)
                {
                    _shadeBorder.Dispose();
                    _shadeBorder = null;
                }

                if (_toolTip != null)
                {
                    _toolTip.Dispose();
                    _toolTip = null;
                }
                Close();

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
