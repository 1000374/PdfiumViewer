# Pdfium.Net
A Pdfium wrapper inherits all the functions of PdfiumViewer, and on this basis adds new editing and other operations of pdf. For a long time,Pdfium can not be used to edit pdf under.NET. Pdfium.Net encapsulates all functions of Pdfium, but some functions have not been tested due to limited energy.

~~~PdfDocumentTests~~~
 public class PdfDocumentTests
 {
     [Test()]
     public void LoadTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/about_blank.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
             Assert.IsTrue(!doc.Document.IsNull);
     }

     [Test()]
     public void RenderTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/annotation_highlight_long_content.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
             for (int i = 0; i < doc.PageCount; i++)
             {
                 using (var image = doc.Render(i, (int)doc.PageSizes[i].Width * 4 / 3, (int)doc.PageSizes[i].Height * 4 / 3, 96, 96, PdfRotation.Rotate0, PdfRenderFlags.Annotations | PdfRenderFlags.CorrectFromDpi))
                 {
                     Assert.IsNotNull(image);
                 }
             }
     }
     [Test()]
     public void RenderTest1()
     {
         var pathPdf = "./Pdfium.NetTests/resources/annotation_highlight_long_content.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
             for (int i = 0; i < doc.PageCount; i++)
             {
                 using (var image = doc.Render(
                     i,
                     (int)doc.PageSizes[i].Width,
                     (int)doc.PageSizes[i].Height,
                     16, // x of the top/left of clipping rectangle
                    283, // y of the top/left point of clipping rectangle
                    555, // width of clipping reactangle
                    316, // height of clipping reactangle
                    PdfRotation.Rotate0, // no rotation
                    PdfRenderFlags.None // no render flags
                                        ))
                 {
                     Assert.AreEqual(555, image.Width);
                 }
             }
     }
     [Test()]
     public void RenderThumbnailTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/simple_thumbnail.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
             for (int i = 0; i < doc.PageCount; i++)
             {
                 var image = doc.RenderThumbnail(i);
                 Assert.IsNotNull(image);
             }
     }

     [Test()]
     public void ImportNPagesToOneTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/rectangles_multi_pages.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         {
             var doc1 = doc.ImportNPagesToOne(612 * 2, 792);
             Assert.AreEqual(612 * 2, doc1.GetPage(0).Width);
         }
     }

     [Test()]
     public void GetPdfTextTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/text_render_mode.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         {
             var text = doc.GetPdfText(0);
             Assert.AreEqual("fill\r\nstroke\0", text);
         }
     }

     [Test()]
     public void GetBoundedTextTest()
     {
         //page: 0,x: 235,y: 238,x1: 327,y1: 287 Text: fill
         var pathPdf = "./Pdfium.NetTests/resources/text_render_mode.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         {
             var text = doc.GetBoundedText(0, 235, 238, 327, 287);
             Assert.AreEqual("fill", text);
         }
     }

     [Test()]
     public void CreateNewTest()
     {
         using (var doc = PdfDocument.CreateNew())
         {
             Assert.IsTrue(!doc.Document.IsNull);
         }
     }

     [Test()]
     public void CreateNewPageTest()
     {
         using (var doc = PdfDocument.CreateNew())
         {
             doc.CreateNewPage(0, 612, 792);
             Assert.AreEqual(612, doc.Document.LoadPage(0).Width);
         }
     }

     [Test()]
     public void AddStringTest()
     {
         using (var doc = PdfDocument.CreateNew())
         {
             var fontPath = @"c:\Windows\fonts\simhei.ttf";
             var cSharpString = "!you ok 这是第一句。 这是第二行。a you ok";
             var afont = File.ReadAllBytes(fontPath);
             doc.CreateNewPage(0, 612, 792);
             doc.AddString(0, cSharpString, 10, 50, 12, Color.Black, afont);
             doc.Save("./Pdfium.NetTests/AddString.pdf");
         }
     }
     [Test()]
     public void AddStringTest1()
     {
         using (var doc = PdfDocument.CreateNew())
         {
             var cSharpString = "!you ok 这是第一句。 这是第二行。a you ok";
             doc.CreateNewPage(0, 612, 792);
             doc.AddString(0, cSharpString, 10, 50, 12, Color.Black, StandardPDFFonts.TimesNewRoman);
             doc.Save("./Pdfium.NetTests/AddString1.pdf");
         }
     }
     [Test()]
     public void AddStringTest2()
     {
         using (var doc = PdfDocument.CreateNew())
         {
             var fontPath = @"c:\Windows\fonts\simhei.ttf";
             var cSharpString = "!you ok 这是第一句。 这是第二行。a you ok 你好 abcdefghijklmnopqrstuvwxyz1234567890-*/+~`@#$%^&*(){}[];:'|?,，。.！!";
             doc.CreateNewPage(0, 612, 792);
             doc.AddString(0, cSharpString, 10, 50, 12, Color.Black, fontPath, true);
             doc.Save("./Pdfium.NetTests/AddString2.pdf");
         }
     }
     [Test()]
     public void AddImageTest()
     {
         using (var doc = PdfDocument.CreateNew())
         {
             var imagePath = "./Pdfium.NetTests/resources/mona_lisa.jpg";
             doc.CreateNewPage(0, 612, 792);
             var stream = new MemoryStream(File.ReadAllBytes(imagePath));
             doc.AddImage(0, stream, 50, 100);
             doc.Save("./Pdfium.NetTests/AddImage.pdf");
         }
     }

     [Test()]
     public void WaterMarkTest()
     {
         using (var doc = PdfDocument.CreateNew())
         {
             doc.CreateNewPage(0, 612, 792);
             var fontPath = @"c:\Windows\fonts\simhei.ttf";
             var cSharpString = "我是水印";
             doc.WaterMark(cSharpString, 20, Color.FromArgb(50, 255, 0, 0), File.ReadAllBytes(fontPath), totleHeight: 120);
             doc.Save("./Pdfium.NetTests/WaterMark.pdf");
         }
     }

     [Test()]
     public void WaterMarkTest1()
     {
         using (var doc = PdfDocument.CreateNew())
         {
             doc.CreateNewPage(0, 612, 792);
             var fontPath = @"c:\Windows\fonts\simhei.ttf";
             var cSharpString = "我是水印";
             doc.WaterMark(cSharpString, 20, Color.FromArgb(50, 255, 0, 0), fontPath,true, totleHeight: 120,render_mode:FPDF_TEXT_RENDERMODE.STROKE);
             doc.Save("./Pdfium.NetTests/WaterMark1.pdf");
         }
     }
     [Test()]
     public void GetTextObjFontTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/fontText.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         {
             var font = doc.GetTextObjFont(0, "你", out float size);
             if (!font.IsNull)
             {
                 var emb = font.IsEmbedded;
                 var cSharpString = "你好啊,abc";
                 doc.AddString(0, cSharpString, 10, 50, size, Color.Black, font);
                 doc.Save("./Pdfium.NetTests/TextObjFont.pdf");
             }
         }
     }
 }

~~~~~~

~~~PdfSupportTests~~~

 public class PdfSupportTests
 {
     [Test()]
     public void GetPDFPageTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/viewer_ref.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         {
             for (int i = 0; i < doc.PageCount; i++)
             {
                 var doci = PdfSupport.GetPDFPage(doc, i + 1);
                 Assert.AreEqual(1, doci.PageCount);
             }
         }
     }

     [Test()]
     public void GetPDFPageTest1()
     {
         var pathPdf = "./Pdfium.NetTests/resources/viewer_ref.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         {
             var doci = PdfSupport.GetPDFPage(doc, "1-3");
             Assert.AreEqual(3, doci.PageCount);
         }
     }

     [Test()]
     public void MergePDFTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/viewer_ref.pdf";
         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         using (var doc1 = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         {
             var doci = PdfSupport.MergePDF(doc, doc1);
             Assert.AreEqual(10, doci.PageCount);
         }
     }

     [Test()]
     public void ImportPageTest()
     {
         var pathPdf = "./Pdfium.NetTests/resources/viewer_ref.pdf";
         var pathPdf1 = "./Pdfium.NetTests/resources/zero_length_stream.pdf";

         using (var doc = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf))))
         using (var doc1 = PdfDocument.Load(new MemoryStream(File.ReadAllBytes(pathPdf1))))
         {
             var doci = PdfSupport.ImportPage(doc, doc1, 1);
             var w = doc1.PageSizes[0].Width;
             var w1 = doci.PageSizes[1].Width;
             Assert.AreEqual(w, w1);
         }
     }
 }

~~~~~~