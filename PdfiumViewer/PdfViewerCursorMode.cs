using System;
using System.Collections.Generic;
using System.Text;

namespace PdfiumViewer
{
    public enum PdfViewerCursorMode
    {
        None = 0,
        Pan,
        TextSelection,
        Cross,   
        Move,
        LeftTop,
        Top,
        RightTop,
        Right,
        RightBottom,
        Bottom,
        LeftBottom,
        Left,
        Rotation
    }
}
