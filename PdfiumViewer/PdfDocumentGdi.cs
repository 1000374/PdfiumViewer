using Pdfium.Net;
using Pdfium.Net.Native;
using Pdfium.Net.Native.Pdfium;
using Pdfium.Net.Native.Pdfium.Enums;
using Pdfium.Net.Wrapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static PdfDocument Load(IWin32Window owner, string path, string password = null)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            try
            {
                while (true)
                {
                    try
                    {
                        return PdfDocument.Load(path, password);
                    }
                    catch (PdfException ex)
                    {
                        if (owner != null && ex.InnerException.Message == FpdfError.PDF_ERR_PASSWORD.ToString())
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
                throw;
            }
        }

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided buffer.
        /// </summary>
        /// <param name="owner">Window to show any UI for.</param>
        /// <param name="buffer">buffer for the PDF document.</param>
        /// <param name="size"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static PdfDocument Load(IWin32Window owner, byte[] buffer, int size = -1, string password = null)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (buffer.Count() == 0)
                throw new ArgumentNullException(nameof(buffer));
            try
            {
                while (true)
                {
                    try
                    {
                        return PdfDocument.Load(buffer, size, password);
                    }
                    catch (PdfException ex)
                    {
                        if (owner != null && ex.InnerException.Message == FpdfError.PDF_ERR_PASSWORD.ToString())
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
                Array.Clear(buffer, 0, buffer.Length);
                throw;
            }
        }
        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided Stream.
        /// </summary>
        /// <param name="owner">Window to show any UI for.</param>
        /// <param name="stream">stream for the PDF document.</param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static PdfDocument Load(IWin32Window owner, Stream stream, string password = null)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
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
                        if (owner != null && ex.InnerException.Message == FpdfError.PDF_ERR_PASSWORD.ToString())
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
