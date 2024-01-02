using Pdfium.Net.Native.Pdfium;
using Pdfium.Net.Native.Structs;
using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable 1591

namespace PdfiumViewer
{
    public enum HitTest
    {
        Border = HitTestValues.HTBORDER,
        Bottom =HitTestValues.HTBOTTOM,
        BottomLeft = HitTestValues.HTBOTTOMLEFT,
        BottomRight = HitTestValues.HTBOTTOMRIGHT,
        Caption = HitTestValues.HTCAPTION,
        Client = HitTestValues.HTCLIENT,
        CloseButton = HitTestValues.HTCLOSE,
        Error = HitTestValues.HTERROR,
        GrowBox = HitTestValues.HTGROWBOX,
        HelpButton = HitTestValues.HTHELP,
        HorizontalScroll = HitTestValues.HTHSCROLL,
        Left = HitTestValues.HTLEFT,
        MaximizeButton = HitTestValues.HTMAXBUTTON,
        Menu = HitTestValues.HTMENU,
        MinimizeButton = HitTestValues.HTMINBUTTON,
        Nowhere = HitTestValues.HTNOWHERE,
        Object = HitTestValues.HTOBJECT,
        Right = HitTestValues.HTRIGHT,
        SystemMenu = HitTestValues.HTSYSMENU,
        Top = HitTestValues.HTTOP,
        TopLeft = HitTestValues.HTTOPLEFT,
        TopRight = HitTestValues.HTTOPRIGHT,
        Transparent = HitTestValues.HTTRANSPARENT,
        VerticalScroll = HitTestValues.HTVSCROLL
    }
}
