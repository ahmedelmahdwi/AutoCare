using System.Drawing;
using System.Windows;

namespace C__project_Auto_Care.Reports
{
    public static class PdfExporter
    {
        public static bool SaveWithDialog(Bitmap[] pages, string defaultTitle, Window owner)
        {
            return ReportExporter.SaveWithDialog(pages, defaultTitle, owner, ReportExportFormat.Pdf);
        }

        public static void ExportBitmapsToPdf(Bitmap[] pages, string outputPath)
        {
            ReportExporter.ExportToPdf(pages, outputPath);
        }
    }
}
