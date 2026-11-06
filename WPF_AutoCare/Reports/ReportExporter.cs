using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using C__project_Auto_Care.Theme;
using Microsoft.Win32;

namespace C__project_Auto_Care.Reports
{
    public enum ReportExportFormat
    {
        Pdf,
        WordDocx
    }

    /// <summary>
    /// Unified Standalone Document Exporter for AutoCare.
    /// Exports report pages directly to standard PDF or Microsoft Word (DOCX)
    /// without requiring any external tools or Microsoft Office installation.
    /// </summary>
    public static class ReportExporter
    {
        public static bool SaveWithDialog(Bitmap[] pages, string defaultTitle, Window owner, ReportExportFormat preferredFormat = ReportExportFormat.Pdf)
        {
            if (pages == null || pages.Length == 0)
            {
                WMessageBox.Show("لا توجد صفحات لتصديرها.", "تنبيه", WMsgIcon.Warning, owner);
                return false;
            }

            string sanitizedTitle = SanitizeFileName(defaultTitle);
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmm");

            var sfd = new SaveFileDialog
            {
                Title = "تصدير وحفظ التقرير (PDF أو Word)",
                Filter = "ملف PDF (*.pdf)|*.pdf|مستند وورد Word (*.docx)|*.docx",
                FilterIndex = (preferredFormat == ReportExportFormat.WordDocx) ? 2 : 1,
                DefaultExt = (preferredFormat == ReportExportFormat.WordDocx) ? ".docx" : ".pdf",
                FileName = string.Format("{0}_{1}{2}", sanitizedTitle, timeStamp, (preferredFormat == ReportExportFormat.WordDocx) ? ".docx" : ".pdf")
            };

            if (sfd.ShowDialog(owner) != true)
                return false;

            string ext = Path.GetExtension(sfd.FileName)?.ToLowerInvariant();
            bool isWord = ext == ".docx" || sfd.FilterIndex == 2;

            try
            {
                if (isWord)
                {
                    ExportToWordDocx(pages, sfd.FileName);
                }
                else
                {
                    ExportToPdf(pages, sfd.FileName);
                }

                string formatName = isWord ? "مستند وورد (Word)" : "ملف (PDF)";
                var res = MessageBox.Show(
                    string.Format("تم حفظ التقرير بنجاح بصيغة {0} إلى:\n{1}\n\nهل ترغب في فتح الملف الآن؟", formatName, sfd.FileName),
                    "نجاح التصدير",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information,
                    MessageBoxResult.Yes,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }

                return true;
            }
            catch (Exception ex)
            {
                WMessageBox.Show("فشل حفظ التقرير:\n" + ex.Message, "خطأ في التصدير", WMsgIcon.Error, owner);
                return false;
            }
        }

        // =========================================================================
        // PDF Export Engine (Pure C# ISO-compliant PDF 1.4)
        // =========================================================================
        public static void ExportToPdf(Bitmap[] pages, string outputPath)
        {
            if (pages == null || pages.Length == 0)
                throw new ArgumentException("No pages provided for PDF export.");

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs))
            {
                var offsets = new List<long>();

                byte[] header = Encoding.ASCII.GetBytes("%PDF-1.4\r\n%\xE2\xE3\xCF\xD3\r\n");
                bw.Write(header);

                offsets.Add(fs.Position);
                WriteString(bw, "1 0 obj\r\n<</Type /Catalog /Pages 2 0 R>>\r\nendobj\r\n");

                int totalPages = pages.Length;
                offsets.Add(fs.Position);
                var kids = new StringBuilder();
                for (int i = 0; i < totalPages; i++)
                {
                    int pageObjId = 3 + i * 3;
                    kids.Append(pageObjId + " 0 R ");
                }
                WriteString(bw, string.Format("2 0 obj\r\n<</Type /Pages /Kids [{0}] /Count {1}>>\r\nendobj\r\n", kids.ToString().Trim(), totalPages));

                for (int i = 0; i < totalPages; i++)
                {
                    var bmp = pages[i];
                    int pageObjId = 3 + i * 3;
                    int contentObjId = 3 + i * 3 + 1;
                    int imageObjId = 3 + i * 3 + 2;

                    byte[] jpegBytes;
                    using (var ms = new MemoryStream())
                    {
                        var encoder = GetEncoder(ImageFormat.Jpeg);
                        var encParams = new EncoderParameters(1);
                        encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 93L);
                        bmp.Save(ms, encoder, encParams);
                        jpegBytes = ms.ToArray();
                    }

                    double pageWidth = 595.28;
                    double pageHeight = 841.89;
                    if (bmp.Width > bmp.Height)
                    {
                        pageWidth = 841.89;
                        pageHeight = 595.28;
                    }

                    offsets.Add(fs.Position);
                    string pageStr = string.Format(
                        "{0} 0 obj\r\n<</Type /Page /Parent 2 0 R /MediaBox [0 0 {1:F2} {2:F2}] /Contents {3} 0 R /Resources <</ProcSet [/PDF /ImageC] /XObject <</Im{4} {5} 0 R>>>>>>\r\nendobj\r\n",
                        pageObjId, pageWidth, pageHeight, contentObjId, i + 1, imageObjId);
                    WriteString(bw, pageStr);

                    offsets.Add(fs.Position);
                    string contentData = string.Format("q\r\n{0:F2} 0 0 {1:F2} 0 0 cm\r\n/Im{2} Do\r\nQ\r\n", pageWidth, pageHeight, i + 1);
                    byte[] contentBytes = Encoding.ASCII.GetBytes(contentData);
                    string contentObj = string.Format("{0} 0 obj\r\n<</Length {1}>>\r\nstream\r\n{2}endstream\r\nendobj\r\n",
                        contentObjId, contentBytes.Length, contentData);
                    WriteString(bw, contentObj);

                    offsets.Add(fs.Position);
                    string imageHeader = string.Format(
                        "{0} 0 obj\r\n<</Type /XObject /Subtype /Image /Width {1} /Height {2} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {3}>>\r\nstream\r\n",
                        imageObjId, bmp.Width, bmp.Height, jpegBytes.Length);
                    WriteString(bw, imageHeader);
                    bw.Write(jpegBytes);
                    WriteString(bw, "\r\nendstream\r\nendobj\r\n");
                }

                long startXref = fs.Position;
                int totalObjects = 2 + totalPages * 3;
                WriteString(bw, string.Format("xref\r\n0 {0}\r\n", totalObjects + 1));
                WriteString(bw, "0000000000 65535 f \r\n");
                foreach (long off in offsets)
                {
                    WriteString(bw, string.Format("{0:D10} 00000 n \r\n", off));
                }

                WriteString(bw, string.Format("trailer\r\n<</Size {0} /Root 1 0 R>>\r\nstartxref\r\n{1}\r\n%%EOF\r\n", totalObjects + 1, startXref));
            }
        }

        // =========================================================================
        // Word DOCX Export Engine (Pure C# OpenXML Specification)
        // =========================================================================
        public static void ExportToWordDocx(Bitmap[] pages, string outputPath)
        {
            if (pages == null || pages.Length == 0)
                throw new ArgumentException("No pages provided for Word export.");

            if (File.Exists(outputPath)) File.Delete(outputPath);

            using (var zipStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                // [Content_Types].xml
                var contentTypesEntry = archive.CreateEntry("[Content_Types].xml");
                using (var writer = new StreamWriter(contentTypesEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Default Extension=""jpeg"" ContentType=""image/jpeg""/>
  <Override PartName=""/word/document.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml""/>
</Types>");
                }

                // _rels/.rels
                var relsEntry = archive.CreateEntry("_rels/.rels");
                using (var writer = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""word/document.xml""/>
</Relationships>");
                }

                // word/_rels/document.xml.rels
                var docRelsEntry = archive.CreateEntry("word/_rels/document.xml.rels");
                using (var writer = new StreamWriter(docRelsEntry.Open(), Encoding.UTF8))
                {
                    var relsSb = new StringBuilder();
                    relsSb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">");
                    for (int i = 0; i < pages.Length; i++)
                    {
                        relsSb.AppendFormat(
                            @"<Relationship Id=""rId{0}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"" Target=""media/image{0}.jpeg""/>",
                            i + 1);
                    }
                    relsSb.Append("</Relationships>");
                    writer.Write(relsSb.ToString());
                }

                // word/document.xml & images
                var docSb = new StringBuilder();
                docSb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main""
            xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""
            xmlns:wp=""http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing""
            xmlns:a=""http://schemas.openxmlformats.org/drawingml/2006/main""
            xmlns:pic=""http://schemas.openxmlformats.org/drawingml/2006/picture"">
  <w:body>");

                long maxWidthEmu = 6645660L;
                long maxHeightEmu = 9777730L;

                for (int i = 0; i < pages.Length; i++)
                {
                    var bmp = pages[i];
                    int rId = i + 1;

                    var imgEntry = archive.CreateEntry(string.Format("word/media/image{0}.jpeg", rId));
                    using (var imgStream = imgEntry.Open())
                    {
                        var encoder = GetEncoder(ImageFormat.Jpeg);
                        var encParams = new EncoderParameters(1);
                        encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 93L);
                        bmp.Save(imgStream, encoder, encParams);
                    }

                    double aspect = (double)bmp.Width / (double)bmp.Height;
                    long cx = maxWidthEmu;
                    long cy = (long)(cx / aspect);
                    if (cy > maxHeightEmu)
                    {
                        cy = maxHeightEmu;
                        cx = (long)(cy * aspect);
                    }

                    docSb.AppendFormat(@"
    <w:p>
      <w:pPr><w:jc w:val=""center""/></w:pPr>
      <w:r>
        <w:drawing>
          <wp:inline distT=""0"" distB=""0"" distL=""0"" distR=""0"">
            <wp:extent cx=""{0}"" cy=""{1}""/>
            <wp:effectExtent l=""0"" t=""0"" r=""0"" b=""0""/>
            <wp:docPr id=""{2}"" name=""Page {2}""/>
            <wp:cNvGraphicFramePr>
              <a:graphicFrameLocks noChangeAspect=""1""/>
            </wp:cNvGraphicFramePr>
            <a:graphic>
              <a:graphicData uri=""http://schemas.openxmlformats.org/drawingml/2006/picture"">
                <pic:pic>
                  <pic:nvPicPr>
                    <pic:cNvPr id=""{2}"" name=""Image{2}.jpeg""/>
                    <pic:cNvPicPr/>
                  </pic:nvPicPr>
                  <pic:blipFill>
                    <a:blip r:embed=""rId{2}""/>
                    <a:stretch><a:fillRect/></a:stretch>
                  </pic:blipFill>
                  <pic:spPr>
                    <a:xfrm>
                      <a:off x=""0"" y=""0""/>
                      <a:ext cx=""{0}"" cy=""{1}""/>
                    </a:xfrm>
                    <a:prstGeom prst=""rect""><a:avLst/></a:prstGeom>
                  </pic:spPr>
                </pic:pic>
              </a:graphicData>
            </a:graphic>
          </wp:inline>
        </w:drawing>
      </w:r>
    </w:p>", cx, cy, rId);

                    if (i < pages.Length - 1)
                    {
                        docSb.Append(@"<w:p><w:r><w:br w:type=""page""/></w:r></w:p>");
                    }
                }

                docSb.Append(@"
    <w:sectPr>
      <w:pgSz w:w=""11906"" w:h=""16838""/>
      <w:pgMar w:top=""720"" w:right=""720"" w:bottom=""720"" w:left=""720""/>
    </w:sectPr>
  </w:body>
</w:document>");

                var docEntry = archive.CreateEntry("word/document.xml");
                using (var writer = new StreamWriter(docEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(docSb.ToString());
                }
            }
        }

        private static void WriteString(BinaryWriter bw, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            bw.Write(bytes);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "تقرير";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}
