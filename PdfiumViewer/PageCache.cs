using Pdfium.Net.Wrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace PdfiumViewer
{
    class PageCache : IDisposable
    {
        public List<CachedLink> CachedLinks { get; set; }
        public Rectangle Bounds { get; set; }
        public Rectangle OuterBounds { get; set; }
        public Image Image { get; set; }
        public List<ObjectInformation> ObjectInformation { get; set; }
        bool dispose = false;
        private void Dispose(bool disposing)
        {
            if (dispose)
                return;
            if (disposing)
            {
                CachedLinks?.Clear();
                ObjectInformation?.Clear();
            }
            Image?.Dispose();
            dispose = true;
        }
        public void Close()
        {
            Dispose(true);
        }
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PageCache()
        {
            Dispose(false);
        }
    }
    class CachedLink
    {
        public PageLink Link { get; }
        public Rectangle Bounds { get; }

        public CachedLink(PageLink link, Rectangle bounds)
        {
            Link = link;
            Bounds = bounds;
        }
    }
}
