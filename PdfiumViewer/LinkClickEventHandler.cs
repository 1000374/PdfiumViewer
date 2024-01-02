using Pdfium.Net;
using System.ComponentModel;

#pragma warning disable 1591

namespace PdfiumViewer
{
    public class LinkClickEventArgs : HandledEventArgs
    {
        /// <summary>
        /// Gets the link that was clicked.
        /// </summary>
        public PdfPageLink Link { get; private set; }
        
        public LinkClickEventArgs(PdfPageLink link)
        {
            Link = link;
        }
    }

    public delegate void LinkClickEventHandler(object sender, LinkClickEventArgs e);
}
