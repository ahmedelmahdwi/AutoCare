using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Windows;
using C__project_Auto_Care.Theme;
using Microsoft.Win32;

namespace C__project_Auto_Care.Reports
{
    /// <summary>
    /// Pure, Standalone OpenXML Excel (.xlsx) Exporter for AutoCare.
    /// Complies 100% with ECMA-376 / ISO-IEC 29500 standards.
    /// Generates genuine native Excel workbooks without requiring Microsoft Excel,
    /// Office Interop, or third-party DLL dependencies.
    /// Fully supports Arabic Right-To-Left (RTL), custom styles, borders, and number formatting.
    /// </summary>
    public static class ExcelExporter
    {
        #region Style Indices
        // Style (xf) index mappings in xl/styles.xml:
        // 0: Normal text (left/right default)
        // 1: Center regular
        // 2: Header cell (AutoCare Blue #0284C7 fill, White Bold, Centered, Thin border)
        // 3: Title cell (Bold 14pt #0284C7, Centered)
        // 4: Number 2 decimals (#,##0.00, Centered)
        // 5: Total label cell (Bold, Light Gray #F1F5F9 fill, Centered, Border)
        // 6: Total number cell (Bold, #,##0.00, Light Gray #F1F5F9 fill, Centered, Border)
        // 7: Warning cell (Soft Red #FEE2E2 fill, Centered, Thin border)
        // 8: Warning number cell (Soft Red #FEE2E2 fill, #,##0.00, Centered, Thin border)
        // 9: Date cell (yyyy-MM-dd, Centered)
        #endregion

        private static bool SaveAndOpenFile(byte[] xlsxBytes, string defaultFileName, Window owner)
        {
            if (xlsxBytes == null || xlsxBytes.Length == 0)
            {
                WMessageBox.Show("فشل إنشاء ملف الإكسيل.", "خطأ", WMsgIcon.Error, owner);
                return false;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "حفظ تقرير إكسيل (Excel Workbook)",
                Filter = "ملف إكسيل Excel Workbook (*.xlsx)|*.xlsx",
                FileName = string.Format("{0}_{1:yyyyMMdd_HHmm}.xlsx", SanitizeFileName(defaultFileName), DateTime.Now),
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog(owner) != true)
                return false;

            try
            {
                File.WriteAllBytes(saveDialog.FileName, xlsxBytes);

                var res = MessageBox.Show(
                    string.Format("تم تصدير التقرير بنجاح إلى ملف إكسيل (.xlsx):\n{0}\n\nهل ترغب في فتح الملف الآن؟", saveDialog.FileName),
                    "نجاح تصدير إكسيل",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information,
                    MessageBoxResult.Yes,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                }

                return true;
            }
            catch (Exception ex)
            {
                WMessageBox.Show("تعذر حفظ ملف الإكسيل:\n" + ex.Message, "خطأ في الحفظ", WMsgIcon.Error, owner);
                return false;
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "تقرير";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }

        // =========================================================================
        // 1. تصدير التقرير المالي الشامل
        // =========================================================================
        public static bool ExportFinancialReport(DataTable invoiceData, DateTime start, DateTime end, Window owner)
        {
            if (invoiceData == null || invoiceData.Rows.Count == 0)
            {
                WMessageBox.Show("لا توجد فواتير لتصديرها خلال الفترة المحددة.", "تنبيه", WMsgIcon.Info, owner);
                return false;
            }

            try
            {
                var wb = new SimpleXlsxWorkbook("التقرير المالي");
                string title = string.Format("التقرير المالي وإيرادات مركز AutoCare للفترة من {0:yyyy-MM-dd} إلى {1:yyyy-MM-dd}", start, end);
                wb.SetTitle(title, 7);

                string[] headers = { "رقم الفاتورة", "رقم أمر الإصلاح", "تاريخ الفاتورة", "المُعد / المسؤول", "قيمة الخدمات (د.ل)", "قيمة قطع الغيار (د.ل)", "الإجمالي النهائي (د.ل)" };
                double[] widths = { 18, 18, 16, 26, 20, 20, 22 };
                wb.SetHeaders(headers, widths);

                decimal grandServices = 0m;
                decimal grandParts = 0m;
                decimal grandTotal = 0m;

                Data.RepairOrderServiceRepository rosRepo = null;
                Data.RepairOrderSparePartRepository rospRepo = null;
                if (AppServiceLocator.DbHelper != null)
                {
                    rosRepo = new Data.RepairOrderServiceRepository(AppServiceLocator.DbHelper);
                    rospRepo = new Data.RepairOrderSparePartRepository(AppServiceLocator.DbHelper);
                }

                foreach (DataRow dr in invoiceData.Rows)
                {
                    string invNum = dr.Table.Columns.Contains("InvoiceNumber") ? (dr["InvoiceNumber"]?.ToString() ?? "") : "";
                    string roNum = dr.Table.Columns.Contains("RepairOrderNumber") ? (dr["RepairOrderNumber"]?.ToString() ?? "") : "";

                    DateTime dt = DateTime.Now;
                    if (dr.Table.Columns.Contains("InvoiceDate") && dr["InvoiceDate"] != DBNull.Value)
                        dt = Convert.ToDateTime(dr["InvoiceDate"]);
                    else if (dr.Table.Columns.Contains("IssueDate") && dr["IssueDate"] != DBNull.Value)
                        dt = Convert.ToDateTime(dr["IssueDate"]);

                    string personName = "";
                    if (dr.Table.Columns.Contains("FullName") && !string.IsNullOrWhiteSpace(dr["FullName"]?.ToString()))
                        personName = dr["FullName"].ToString();
                    else if (dr.Table.Columns.Contains("CreatedByUsername") && !string.IsNullOrWhiteSpace(dr["CreatedByUsername"]?.ToString()))
                        personName = dr["CreatedByUsername"].ToString();
                    else if (dr.Table.Columns.Contains("CustomerName") && !string.IsNullOrWhiteSpace(dr["CustomerName"]?.ToString()))
                        personName = dr["CustomerName"].ToString();

                    decimal servicesTotal = 0m;
                    if (dr.Table.Columns.Contains("ServicesTotal") && dr["ServicesTotal"] != DBNull.Value)
                    {
                        servicesTotal = Convert.ToDecimal(dr["ServicesTotal"]);
                    }
                    else if (rosRepo != null && !string.IsNullOrWhiteSpace(roNum))
                    {
                        var svcs = rosRepo.GetServicesByRepairOrder(roNum);
                        if (svcs != null)
                        {
                            foreach (DataRow s in svcs.Rows)
                                servicesTotal += Convert.ToDecimal(s["UnitPrice"]);
                        }
                    }

                    decimal partsTotal = 0m;
                    if (dr.Table.Columns.Contains("SparePartsTotal") && dr["SparePartsTotal"] != DBNull.Value)
                    {
                        partsTotal = Convert.ToDecimal(dr["SparePartsTotal"]);
                    }
                    else if (rospRepo != null && !string.IsNullOrWhiteSpace(roNum))
                    {
                        var parts = rospRepo.GetSparePartsByRepairOrder(roNum);
                        if (parts != null)
                        {
                            foreach (DataRow p in parts.Rows)
                                partsTotal += Convert.ToInt32(p["Quantity"]) * Convert.ToDecimal(p["UnitPrice"]);
                        }
                    }

                    decimal rowTotal = 0m;
                    if (dr.Table.Columns.Contains("TotalAmount") && dr["TotalAmount"] != DBNull.Value)
                    {
                        rowTotal = Convert.ToDecimal(dr["TotalAmount"]);
                    }
                    else
                    {
                        rowTotal = servicesTotal + partsTotal;
                    }

                    grandServices += servicesTotal;
                    grandParts += partsTotal;
                    grandTotal += rowTotal;

                    wb.AddRow(new CellData[]
                    {
                        CellData.Text(invNum, 1),
                        CellData.Text(roNum, 1),
                        CellData.Text(dt.ToString("yyyy-MM-dd"), 9),
                        CellData.Text(personName, 0),
                        CellData.Number(servicesTotal, 4),
                        CellData.Number(partsTotal, 4),
                        CellData.Number(rowTotal, 4)
                    });
                }

                // صف المجموع الكلي
                wb.AddRow(new CellData[]
                {
                    CellData.Empty(5),
                    CellData.Empty(5),
                    CellData.Empty(5),
                    CellData.Text("المجموع الكلي:", 5),
                    CellData.Number(grandServices, 6),
                    CellData.Number(grandParts, 6),
                    CellData.Number(grandTotal, 6)
                });

                byte[] bytes = wb.GeneratePackage();
                return SaveAndOpenFile(bytes, "التقرير_المالي", owner);
            }
            catch (Exception ex)
            {
                WMessageBox.Show("حدث خطأ أثناء تصدير التقرير المالي:\n" + ex.Message, "خطأ في التصدير", WMsgIcon.Error, owner);
                return false;
            }
        }

        // =========================================================================
        // 2. تصدير جرد المخزون وقطع الغيار
        // =========================================================================
        public static bool ExportInventoryReport(DataTable partsData, Window owner)
        {
            if (partsData == null || partsData.Rows.Count == 0)
            {
                WMessageBox.Show("لا توجد بيانات لقطع الغيار لتصديرها.", "تنبيه", WMsgIcon.Info, owner);
                return false;
            }

            try
            {
                var wb = new SimpleXlsxWorkbook("جرد المخزون");
                wb.SetTitle("تقرير جرد قطع الغيار والمخزون الحالي - مركز AutoCare", 7);

                string[] headers = { "كود القطعة", "اسم القطعة", "الكمية بالمخزون", "الحد الأدنى", "سعر الشراء (د.ل)", "سعر البيع (د.ل)", "حالة المخزون" };
                double[] widths = { 16, 30, 16, 16, 18, 18, 16 };
                wb.SetHeaders(headers, widths);

                int totalQty = 0;
                decimal totalCostValue = 0m;

                foreach (DataRow dr in partsData.Rows)
                {
                    string partCode = dr.Table.Columns.Contains("PartCode") ? (dr["PartCode"]?.ToString() ?? "") : (dr["PartNumber"]?.ToString() ?? "");
                    string partName = dr["PartName"]?.ToString() ?? "";
                    int qty = Convert.ToInt32(dr["QuantityInStock"]);
                    int minStock = dr.Table.Columns.Contains("MinimumStock") ? Convert.ToInt32(dr["MinimumStock"]) : Convert.ToInt32(dr["MinStockLevel"]);
                    decimal buyPrice = dr.Table.Columns.Contains("BuyingPrice") ? Convert.ToDecimal(dr["BuyingPrice"]) : Convert.ToDecimal(dr["CostPrice"]);
                    decimal sellPrice = dr.Table.Columns.Contains("SellingPrice") ? Convert.ToDecimal(dr["SellingPrice"]) : 0m;

                    totalQty += qty;
                    totalCostValue += qty * buyPrice;

                    bool isLow = qty <= minStock;
                    string status = qty == 0 ? "نفذ المخزون" : (isLow ? "منخفض جداً" : "متوفر");

                    int textStyle = isLow ? 7 : 1;
                    int numStyle = isLow ? 8 : 4;

                    wb.AddRow(new CellData[]
                    {
                        CellData.Text(partCode, textStyle),
                        CellData.Text(partName, isLow ? 7 : 0),
                        CellData.Number(qty, textStyle),
                        CellData.Number(minStock, textStyle),
                        CellData.Number(buyPrice, numStyle),
                        CellData.Number(sellPrice, numStyle),
                        CellData.Text(status, textStyle)
                    });
                }

                // صف الإجمالي
                wb.AddRow(new CellData[]
                {
                    CellData.Empty(5),
                    CellData.Text("إجمالي الكميات وقيمة التكلفة:", 5),
                    CellData.Number(totalQty, 6),
                    CellData.Empty(5),
                    CellData.Number(totalCostValue, 6),
                    CellData.Empty(5),
                    CellData.Empty(5)
                });

                byte[] bytes = wb.GeneratePackage();
                return SaveAndOpenFile(bytes, "جرد_قطع_الغيار", owner);
            }
            catch (Exception ex)
            {
                WMessageBox.Show("حدث خطأ أثناء تصدير جرد المخزون:\n" + ex.Message, "خطأ في التصدير", WMsgIcon.Error, owner);
                return false;
            }
        }

        // =========================================================================
        // 3. تصدير تقرير الأكثر طلباً (Top Services / Parts)
        // =========================================================================
        public static bool ExportTopReport(DataTable data, string categoryName, DateTime start, DateTime end, Window owner)
        {
            if (data == null || data.Rows.Count == 0)
            {
                WMessageBox.Show("لا توجد بيانات لتصديرها.", "تنبيه", WMsgIcon.Info, owner);
                return false;
            }

            try
            {
                var wb = new SimpleXlsxWorkbook("الأكثر طلباً");
                string title = string.Format("تقرير الأكثر طلباً ({0}) للفترة من {1:yyyy-MM-dd} إلى {2:yyyy-MM-dd}", categoryName, start, end);
                wb.SetTitle(title, 4);

                string[] headers = { "م", "البند / الاسم", "عدد مرات الطلب / الاستخدام", "إجمالي القيمة المالية (د.ل)" };
                double[] widths = { 10, 36, 26, 26 };
                wb.SetHeaders(headers, widths);

                int idx = 1;
                decimal sumVal = 0m;
                int sumCount = 0;

                foreach (DataRow dr in data.Rows)
                {
                    string name = dr.Table.Columns.Contains("ItemName") ? dr["ItemName"]?.ToString() : (dr.Table.Columns.Contains("ServiceName") ? dr["ServiceName"]?.ToString() : dr[0]?.ToString());
                    int count = dr.Table.Columns.Contains("UsageCount") ? Convert.ToInt32(dr["UsageCount"]) : (dr.Table.Columns.Contains("Count") ? Convert.ToInt32(dr["Count"]) : 0);
                    decimal total = dr.Table.Columns.Contains("TotalIncome") ? Convert.ToDecimal(dr["TotalIncome"]) : (dr.Table.Columns.Contains("TotalValue") ? Convert.ToDecimal(dr["TotalValue"]) : 0m);

                    sumCount += count;
                    sumVal += total;

                    wb.AddRow(new CellData[]
                    {
                        CellData.Number(idx++, 1),
                        CellData.Text(name, 0),
                        CellData.Number(count, 1),
                        CellData.Number(total, 4)
                    });
                }

                wb.AddRow(new CellData[]
                {
                    CellData.Empty(5),
                    CellData.Text("المجموع:", 5),
                    CellData.Number(sumCount, 6),
                    CellData.Number(sumVal, 6)
                });

                byte[] bytes = wb.GeneratePackage();
                return SaveAndOpenFile(bytes, "الأكثر_طلباً_" + SanitizeFileName(categoryName), owner);
            }
            catch (Exception ex)
            {
                WMessageBox.Show("حدث خطأ أثناء تصدير التقرير:\n" + ex.Message, "خطأ في التصدير", WMsgIcon.Error, owner);
                return false;
            }
        }

        #region OpenXML Workbook Engine
        public class CellData
        {
            public string Value { get; set; }
            public bool IsNumber { get; set; }
            public int StyleIndex { get; set; }

            public static CellData Text(string text, int styleIndex = 0)
            {
                return new CellData { Value = text ?? "", IsNumber = false, StyleIndex = styleIndex };
            }

            public static CellData Number(decimal num, int styleIndex = 4)
            {
                return new CellData { Value = num.ToString(CultureInfo.InvariantCulture), IsNumber = true, StyleIndex = styleIndex };
            }

            public static CellData Number(int num, int styleIndex = 1)
            {
                return new CellData { Value = num.ToString(CultureInfo.InvariantCulture), IsNumber = true, StyleIndex = styleIndex };
            }

            public static CellData Empty(int styleIndex = 0)
            {
                return new CellData { Value = "", IsNumber = false, StyleIndex = styleIndex };
            }
        }

        public class SimpleXlsxWorkbook
        {
            private readonly string _sheetName;
            private string _title;
            private int _titleColSpan = 7;
            private string[] _headers;
            private double[] _colWidths;
            private readonly List<CellData[]> _rows = new List<CellData[]>();

            public SimpleXlsxWorkbook(string sheetName = "ورقة 1")
            {
                _sheetName = string.IsNullOrWhiteSpace(sheetName) ? "ورقة 1" : sheetName;
            }

            public void SetTitle(string title, int colSpan = 7)
            {
                _title = title;
                _titleColSpan = colSpan > 0 ? colSpan : 7;
            }

            public void SetHeaders(string[] headers, double[] colWidths = null)
            {
                _headers = headers;
                _colWidths = colWidths;
            }

            public void AddRow(CellData[] cells)
            {
                _rows.Add(cells);
            }

            public byte[] GeneratePackage()
            {
                using (var ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        // 1. [Content_Types].xml
                        CreateEntry(archive, "[Content_Types].xml",
                            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\r\n" +
                            "  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>\r\n" +
                            "  <Default Extension=\"xml\" ContentType=\"application/xml\"/>\r\n" +
                            "  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>\r\n" +
                            "  <Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>\r\n" +
                            "  <Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>\r\n" +
                            "</Types>");

                        // 2. _rels/.rels
                        CreateEntry(archive, "_rels/.rels",
                            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\r\n" +
                            "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>\r\n" +
                            "</Relationships>");

                        // 3. xl/_rels/workbook.xml.rels
                        CreateEntry(archive, "xl/_rels/workbook.xml.rels",
                            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\r\n" +
                            "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>\r\n" +
                            "  <Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>\r\n" +
                            "</Relationships>");

                        // 4. xl/workbook.xml
                        string safeSheetName = SecurityElement.Escape(_sheetName);
                        CreateEntry(archive, "xl/workbook.xml",
                            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">\r\n" +
                            "  <bookViews><workbookView xWindow=\"0\" yWindow=\"0\" windowWidth=\"20480\" windowHeight=\"10240\"/></bookViews>\r\n" +
                            "  <sheets>\r\n" +
                            string.Format("    <sheet name=\"{0}\" sheetId=\"1\" r:id=\"rId1\"/>\r\n", safeSheetName) +
                            "  </sheets>\r\n" +
                            "</workbook>");

                        // 5. xl/styles.xml
                        CreateEntry(archive, "xl/styles.xml", GenerateStylesXml());

                        // 6. xl/worksheets/sheet1.xml
                        CreateEntry(archive, "xl/worksheets/sheet1.xml", GenerateWorksheetXml());
                    }

                    return ms.ToArray();
                }
            }

            private static void CreateEntry(ZipArchive archive, string entryName, string content)
            {
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
                {
                    writer.Write(content);
                }
            }

            private string GenerateStylesXml()
            {
                // Fonts:
                // 0: Segoe UI 10pt Regular (Default)
                // 1: Segoe UI 11pt Bold White (Header)
                // 2: Segoe UI 14pt Bold AutoCare Blue (#0284C7) (Title)
                // 3: Segoe UI 10.5pt Bold Dark (#0F172A) (Totals)
                // 4: Segoe UI 10pt Bold Red (#DC2626) (Warning)
                //
                // Fills:
                // 0: none
                // 1: gray125
                // 2: AutoCare Blue #0284C7
                // 3: Warning Light Red #FEE2E2
                // 4: Light Gray #F1F5F9 (Totals)
                //
                // Borders:
                // 0: none
                // 1: thin border all around (#CBD5E1)
                //
                // CellXfs:
                // 0: Default Text (Font 0, Fill 0, Border 0, Align Right)
                // 1: Centered Text (Font 0, Fill 0, Border 1, Align Center)
                // 2: Header (Font 1, Fill 2, Border 1, Align Center)
                // 3: Title (Font 2, Fill 0, Border 0, Align Center)
                // 4: Number #,##0.00 (Font 0, Fill 0, Border 1, numFmtId 4, Align Center)
                // 5: Total Label (Font 3, Fill 4, Border 1, Align Center)
                // 6: Total Value #,##0.00 (Font 3, Fill 4, Border 1, numFmtId 4, Align Center)
                // 7: Warning Text (Font 4, Fill 3, Border 1, Align Center)
                // 8: Warning Number #,##0.00 (Font 4, Fill 3, Border 1, numFmtId 4, Align Center)
                // 9: Date (Font 0, Fill 0, Border 1, Align Center)

                return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                    "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\r\n" +
                    "  <numFmts count=\"1\">\r\n" +
                    "    <numFmt numFmtId=\"164\" formatCode=\"#,##0.00\"/>\r\n" +
                    "  </numFmts>\r\n" +
                    "  <fonts count=\"5\">\r\n" +
                    "    <font><sz val=\"10\"/><color rgb=\"FF1E293B\"/><name val=\"Segoe UI\"/></font>\r\n" +
                    "    <font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Segoe UI\"/></font>\r\n" +
                    "    <font><b/><sz val=\"14\"/><color rgb=\"FF0284C7\"/><name val=\"Segoe UI\"/></font>\r\n" +
                    "    <font><b/><sz val=\"10.5\"/><color rgb=\"FF0F172A\"/><name val=\"Segoe UI\"/></font>\r\n" +
                    "    <font><b/><sz val=\"10\"/><color rgb=\"FF991B1B\"/><name val=\"Segoe UI\"/></font>\r\n" +
                    "  </fonts>\r\n" +
                    "  <fills count=\"5\">\r\n" +
                    "    <fill><patternFill patternType=\"none\"/></fill>\r\n" +
                    "    <fill><patternFill patternType=\"gray125\"/></fill>\r\n" +
                    "    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF0284C7\"/></patternFill></fill>\r\n" +
                    "    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFEE2E2\"/></patternFill></fill>\r\n" +
                    "    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF1F5F9\"/></patternFill></fill>\r\n" +
                    "  </fills>\r\n" +
                    "  <borders count=\"2\">\r\n" +
                    "    <border><left/><right/><top/><bottom/><diagonal/></border>\r\n" +
                    "    <border>\r\n" +
                    "      <left style=\"thin\"><color rgb=\"FFCBD5E1\"/></left>\r\n" +
                    "      <right style=\"thin\"><color rgb=\"FFCBD5E1\"/></right>\r\n" +
                    "      <top style=\"thin\"><color rgb=\"FFCBD5E1\"/></top>\r\n" +
                    "      <bottom style=\"thin\"><color rgb=\"FFCBD5E1\"/></bottom>\r\n" +
                    "    </border>\r\n" +
                    "  </borders>\r\n" +
                    "  <cellStyleXfs count=\"1\">\r\n" +
                    "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\r\n" +
                    "  </cellStyleXfs>\r\n" +
                    "  <cellXfs count=\"10\">\r\n" +
                    "    <!-- 0: Regular Right -->\r\n" +
                    "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 1: Regular Center -->\r\n" +
                    "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 2: Header Blue -->\r\n" +
                    "    <xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 3: Title -->\r\n" +
                    "    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 4: Number #,##0.00 -->\r\n" +
                    "    <xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 5: Total Label Gray -->\r\n" +
                    "    <xf numFmtId=\"0\" fontId=\"3\" fillId=\"4\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 6: Total Value #,##0.00 Gray -->\r\n" +
                    "    <xf numFmtId=\"164\" fontId=\"3\" fillId=\"4\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 7: Warning Cell Light Red -->\r\n" +
                    "    <xf numFmtId=\"0\" fontId=\"4\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 8: Warning Number Light Red -->\r\n" +
                    "    <xf numFmtId=\"164\" fontId=\"4\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "    <!-- 9: Date -->\r\n" +
                    "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>\r\n" +
                    "  </cellXfs>\r\n" +
                    "</styleSheet>";
            }

            private string GenerateWorksheetXml()
            {
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n");
                sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\r\n");

                // RTL view enabled for Arab workshops!
                sb.Append("  <sheetViews>\r\n");
                sb.Append("    <sheetView rightToLeft=\"1\" workbookViewId=\"0\"/>\r\n");
                sb.Append("  </sheetViews>\r\n");
                sb.Append("  <sheetFormatPr defaultRowHeight=\"22\"/>\r\n");

                // Columns
                if (_colWidths != null && _colWidths.Length > 0)
                {
                    sb.Append("  <cols>\r\n");
                    for (int i = 0; i < _colWidths.Length; i++)
                    {
                        double w = _colWidths[i] > 0 ? _colWidths[i] : 16.0;
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "    <col min=\"{0}\" max=\"{0}\" width=\"{1:F2}\" customWidth=\"1\"/>\r\n",
                            i + 1, w);
                    }
                    sb.Append("  </cols>\r\n");
                }

                sb.Append("  <sheetData>\r\n");

                int rowIndex = 1;

                // Title row
                if (!string.IsNullOrWhiteSpace(_title))
                {
                    sb.AppendFormat("    <row r=\"{0}\" ht=\"32\" customHeight=\"1\">\r\n", rowIndex);
                    string safeTitle = SecurityElement.Escape(_title);
                    sb.AppendFormat("      <c r=\"A{0}\" s=\"3\" t=\"inlineStr\"><is><t>{1}</t></is></c>\r\n", rowIndex, safeTitle);
                    for (int col = 2; col <= _titleColSpan; col++)
                    {
                        sb.AppendFormat("      <c r=\"{0}{1}\" s=\"3\"/>\r\n", GetColumnLetter(col), rowIndex);
                    }
                    sb.Append("    </row>\r\n");
                    rowIndex++;

                    // Empty spacer row
                    sb.AppendFormat("    <row r=\"{0}\" ht=\"14\" customHeight=\"1\"/>\r\n", rowIndex);
                    rowIndex++;
                }

                // Header row
                if (_headers != null && _headers.Length > 0)
                {
                    sb.AppendFormat("    <row r=\"{0}\" ht=\"26\" customHeight=\"1\">\r\n", rowIndex);
                    for (int i = 0; i < _headers.Length; i++)
                    {
                        string colLetter = GetColumnLetter(i + 1);
                        string safeH = SecurityElement.Escape(_headers[i]);
                        sb.AppendFormat("      <c r=\"{0}{1}\" s=\"2\" t=\"inlineStr\"><is><t>{2}</t></is></c>\r\n",
                            colLetter, rowIndex, safeH);
                    }
                    sb.Append("    </row>\r\n");
                    rowIndex++;
                }

                // Data rows
                foreach (var rowCells in _rows)
                {
                    sb.AppendFormat("    <row r=\"{0}\" ht=\"22\" customHeight=\"1\">\r\n", rowIndex);
                    for (int i = 0; i < rowCells.Length; i++)
                    {
                        var cell = rowCells[i];
                        string colLetter = GetColumnLetter(i + 1);
                        int style = cell.StyleIndex;

                        if (string.IsNullOrEmpty(cell.Value))
                        {
                            sb.AppendFormat("      <c r=\"{0}{1}\" s=\"{2}\"/>\r\n", colLetter, rowIndex, style);
                        }
                        else if (cell.IsNumber)
                        {
                            sb.AppendFormat("      <c r=\"{0}{1}\" s=\"{2}\"><v>{3}</v></c>\r\n",
                                colLetter, rowIndex, style, cell.Value);
                        }
                        else
                        {
                            string safeVal = SecurityElement.Escape(cell.Value);
                            sb.AppendFormat("      <c r=\"{0}{1}\" s=\"{2}\" t=\"inlineStr\"><is><t>{3}</t></is></c>\r\n",
                                colLetter, rowIndex, style, safeVal);
                        }
                    }
                    sb.Append("    </row>\r\n");
                    rowIndex++;
                }

                sb.Append("  </sheetData>\r\n");

                // Merge cells for title
                if (!string.IsNullOrWhiteSpace(_title) && _titleColSpan > 1)
                {
                    sb.Append("  <mergeCells count=\"1\">\r\n");
                    sb.AppendFormat("    <mergeCell ref=\"A1:{0}1\"/>\r\n", GetColumnLetter(_titleColSpan));
                    sb.Append("  </mergeCells>\r\n");
                }

                sb.Append("</worksheet>");
                return sb.ToString();
            }

            private static string GetColumnLetter(int colIndex)
            {
                string letter = "";
                while (colIndex > 0)
                {
                    int mod = (colIndex - 1) % 26;
                    letter = (char)('A' + mod) + letter;
                    colIndex = (colIndex - mod) / 26;
                }
                return letter;
            }
        }
        #endregion
    }
}
