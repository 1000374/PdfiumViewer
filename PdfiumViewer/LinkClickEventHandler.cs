using Pdfium.Net.Wrapper;
using System.ComponentModel;

#pragma warning disable 1591

namespace PdfiumViewer
{
    public class LinkClickEventArgs : HandledEventArgs
    {
        /// <summary>
        /// Gets the link that was clicked.
        /// </summary>
        public PageLink Link { get; private set; }
        
        public LinkClickEventArgs(PageLink link)
        {
            Link = link;
        }
    }

    public delegate void LinkClickEventHandler(object sender, LinkClickEventArgs e);
}
