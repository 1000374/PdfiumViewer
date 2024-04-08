using Pdfium.Net.Wrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace PdfiumViewer
{
    class DragState
    {
        public PageLink Link { get; set; }
        public Point Location { get; set; }
    }

    class MouseState
    {
        public Point MouseLocation { get; set; }
        public PdfPoint PdfLocation { get; set; }
        public int CharacterIndex { get; set; }
    }

    class TextSelectionState
    {
        public int StartPage { get; set; }
        public int StartIndex { get; set; }
        public int EndPage { get; set; }
        public int EndIndex { get; set; }

        public TextSelectionState GetNormalized()
        {
            if (EndPage < 0 || EndIndex < 0) // Special case when only start position is known
                return this;

            if (EndPage < StartPage ||
                (StartPage == EndPage && EndIndex < StartIndex))
            {
                // End position is before start position.
                // Swap positions so start is always before end.

                return new TextSelectionState()
                {
                    StartPage = EndPage,
                    StartIndex = EndIndex,
                    EndPage = StartPage,
                    EndIndex = StartIndex
                };
            }

            return this;
        }
    }
}
