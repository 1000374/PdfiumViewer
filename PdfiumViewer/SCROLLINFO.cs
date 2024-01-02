using System.Runtime.InteropServices;

namespace PdfiumViewer
{
    [StructLayout(LayoutKind.Sequential)]
    public class SCROLLINFO
    {
        public int cbSize = Marshal.SizeOf(typeof(SCROLLINFO));
        public int fMask;
        public int nMin;
        public int nMax;
        public int nPage;
        public int nPos;
        public int nTrackPos;

        public SCROLLINFO()
        {
        }

        public SCROLLINFO(int mask, int min, int max, int page, int pos)
        {
            fMask = mask;
            nMin = min;
            nMax = max;
            nPage = page;
            nPos = pos;
        }
    }

}
