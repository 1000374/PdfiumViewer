using Pdfium.Net.Native.Enums;
using Pdfium.Net.Native.Pdfium.Structs;
using Pdfium.Net.Wrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;

namespace PdfiumViewer
{
    public class ObjectInformation
    {
        public ObjectInformation(int page, PdfPageobject pdfPageobject, List<int> indexs, PdfPageObjType fpdfPageObj, RectangleF bounds)
        {
            Page = page;
            PdfPageobject= pdfPageobject;
            Indexs = indexs;
            PageObjType = fpdfPageObj;
            ObjBounds = bounds;
        }
        public int Page { get; }
        /// <summary>
        /// |
        /// </summary>
        public List<int> Indexs { get; }

        public PdfPageObjType PageObjType { get; }

        public RectangleF ObjBounds { get; set; }

        public bool Highlight { get; set; }
        public bool IsSelect { get; set; }
        public PdfPageobject PdfPageobject { get; set; }

        public Rectangle ObjRectangle { get; set; }

        public Rectangle ObjMoveRectangle { get; set; }

        public float Direction { get; set; } = -1f;
        public PointF DirectionEnd { get; set; }
        public bool IsObjectValid => PdfPageobject != null && !PdfPageobject.IsNull;
        public List<ObjectInformation> SubObjectInformations { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
