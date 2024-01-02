using Pdfium.Net;
using Pdfium.Net.Native;
using Pdfium.Net.Native.Pdfium;
using Pdfium.Net.Native.Pdfium.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PdfiumViewer
{
    /// <summary>
    /// Provides functionality to render a PDF document.
    /// </summary>
    public class PdfDocumentGdi
    {
        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided path.
        /// </summary>
        /// <param name="owner">Window to show any UI for.</param>
        /// <param name="path">Path to the PDF document.</param>
        public static PdfDocument Load(IWin32Window owner, string path)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return Load(owner, File.OpenRead(path), null);
        }

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided path.
        /// </summary>
        /// <param name="owner">Window to show any UI for.</param>
        /// <param name="stream">Stream for the PDF document.</param>
        public static PdfDocument Load(IWin32Window owner, Stream stream)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return Load(owner, stream, null);
        }

        private static PdfDocument Load(IWin32Window owner, Stream stream, string password)
        {
            try
            {
                while (true)
                {
                    try
                    {
                        return PdfDocument.Load(stream, password);
                    }
                    catch (PdfException ex)
                    {
                        if (owner != null && ex.Error == FPDF_ERR.PDF_ERR_PASSWORD)
                        {
                            using (var form = new PasswordForm())
                            {
                                if (form.ShowDialog(owner) == DialogResult.OK)
                                {
                                    password = form.Password;
                                    continue;
                                }
                            }
                        }

                        throw;
                    }
                }
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
    }
}
