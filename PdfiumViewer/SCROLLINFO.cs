using System.Runtime.InteropServices;

namespace PdfiumViewer
{
    [StructLayout(LayoutKind.Sequential)]
    public class ScrollInfo
    {
        public int cbSize = Marshal.SizeOf(typeof(ScrollInfo));
        public int fMask;
        public int nMin;
        public int nMax;
        public int nPage;
        public int nPos;
        public int nTrackPos;

        public ScrollInfo()
        {
        }

        public ScrollInfo(int mask, int min, int max, int page, int pos)
        {
            fMask = mask;
            nMin = min;
            nMax = max;
            nPage = page;
            nPos = pos;
        }
    }

}
