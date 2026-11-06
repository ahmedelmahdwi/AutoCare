using System;
using System.Data;
using System.Windows;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Reports;

namespace C__project_Auto_Care.Reports
{
    public static class ReportsBuilders
    {
        public static void ShowFinancial(DateTime start, DateTime end, Window owner)
        {
            try
            {
                ReportDocument doc = BuildFinancial(start, end);
                if (doc == null)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد فواتير في الفترة المحددة", "التقرير", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return;
                }
                ReportPreviewWindow.Show(doc, owner, "تقرير الإيرادات والحسابات المالية");
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء فتح التقرير: " + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
            }
        }

        public static bool ExportFinancialToPdf(DateTime start, DateTime end, Window owner)
        {
            try
            {
                ReportDocument doc = BuildFinancial(start, end);
                if (doc == null)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد فواتير في الفترة المحددة للتصدير", "تصدير PDF", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return false;
                }
                var pages = ReportRenderer.Render(doc);
                bool res = ReportExporter.SaveWithDialog(pages, "التقرير_المالي_والإيرادات", owner, ReportExportFormat.Pdf);
                foreach (var p in pages) p.Dispose();
                return res;
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير التقرير المالي:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static bool ExportFinancialToExcel(DateTime start, DateTime end, Window owner)
        {
            try
            {
                var invRepo = new InvoiceRepository(AppServiceLocator.DbHelper);
                DataTable invoiceData = invRepo.GetInvoicesForCrystalReport(start.Date, end.Date);
                if (invoiceData == null || invoiceData.Rows.Count == 0)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد فواتير في الفترة المحددة للتصدير", "تصدير إكسيل", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return false;
                }
                return ExcelExporter.ExportFinancialReport(invoiceData, start.Date, end.Date, owner);
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير التقرير المالي إلى إكسيل:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static ReportDocument BuildFinancial(DateTime start, DateTime end)
        {
            start = start.Date;
            end = end.Date;

            var invRepo = new InvoiceRepository(AppServiceLocator.DbHelper);
            var rosRepo = new RepairOrderServiceRepository(AppServiceLocator.DbHelper);
            var rospRepo = new RepairOrderSparePartRepository(AppServiceLocator.DbHelper);

            DataTable invoiceData = invRepo.GetInvoicesForCrystalReport(start, end);
            if (invoiceData.Rows.Count == 0) return null;

            var table = new ReportTableBlock
            {
                Title = "تفاصيل الفواتير"
            };
            table
                .AddColumn("رقم الفاتورة", ReportAlign.Right, 1.3)
                .AddColumn("رقم أمر الإصلاح", ReportAlign.Right, 1.3)
                .AddColumn("التاريخ", ReportAlign.Center, 1.0)
                .AddColumn("المُعد بواسطة", ReportAlign.Right, 1.2)
                .AddColumn("قيمة الخدمات", ReportAlign.Right, 1.0)
                .AddColumn("قيمة القطع", ReportAlign.Right, 1.0)
                .AddColumn("الإجمالي", ReportAlign.Right, 1.0);

            decimal sumServices = 0m;
            decimal sumParts = 0m;
            decimal sumTotal = 0m;

            foreach (DataRow row in invoiceData.Rows)
            {
                string invoiceNumber = row["InvoiceNumber"]?.ToString() ?? "";
                string repairOrderNumber = row["RepairOrderNumber"]?.ToString() ?? "";
                DateTime invoiceDate = Convert.ToDateTime(row["InvoiceDate"]);
                string username = row["CreatedByUsername"]?.ToString() ?? "";
                string fullName = row["FullName"]?.ToString() ?? "";
                string createdBy = string.IsNullOrEmpty(fullName) ? username : fullName;

                decimal servicesTotal = 0m;
                DataTable services = rosRepo.GetServicesByRepairOrder(repairOrderNumber);
                foreach (DataRow svc in services.Rows)
                    servicesTotal += Convert.ToDecimal(svc["UnitPrice"]);

                decimal partsTotal = 0m;
                DataTable parts = rospRepo.GetSparePartsByRepairOrder(repairOrderNumber);
                foreach (DataRow part in parts.Rows)
                    partsTotal += Convert.ToInt32(part["Quantity"]) * Convert.ToDecimal(part["UnitPrice"]);

                decimal rowTotal = servicesTotal + partsTotal;

                sumServices += servicesTotal;
                sumParts += partsTotal;
                sumTotal += rowTotal;

                table.AddRow(
                    invoiceNumber,
                    repairOrderNumber,
                    invoiceDate.ToString("dd/MM/yyyy"),
                    createdBy,
                    ReportRenderer.Money(servicesTotal),
                    ReportRenderer.Money(partsTotal),
                    ReportRenderer.Money(rowTotal));
            }

            table.TotalsRow = new[]
            {
                invoiceData.Rows.Count + " فاتورة",
                "",
                "",
                "",
                ReportRenderer.Money(sumServices),
                ReportRenderer.Money(sumParts),
                ReportRenderer.Money(sumTotal)
            };

            ReportDocument doc = new ReportDocument();

            doc.Add(new ReportTitleBlock("التقرير المالي والإيرادات"));
            doc.Add(new ReportSubtitleBlock("AutoCare - نظام إدارة مراكز صيانة السيارات"));
            doc.Add(new ReportDividerBlock());

            doc.Add(new ReportInfoBlock()
                .Add("الفترة من", start.ToString("dd/MM/yyyy"))
                .Add("إلى", end.ToString("dd/MM/yyyy"))
                .Add("عدد الفواتير", invoiceData.Rows.Count.ToString()));

            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            doc.Add(table);

            doc.Add(new ReportSpacerBlock(10));
            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            doc.Add(new ReportTotalsLineBlock("إجمالي قيمة الخدمات:  " + ReportRenderer.Money(sumServices)));
            doc.Add(new ReportTotalsLineBlock("إجمالي قيمة قطع الغيار:  " + ReportRenderer.Money(sumParts)));
            doc.Add(new ReportTotalsLineBlock("إجمالي الإيرادات:  " + ReportRenderer.Money(sumTotal)));

            return doc;
        }

        public static void ShowInventory(DataTable sparePartsData, Window owner)
        {
            if (sparePartsData == null || sparePartsData.Rows.Count == 0)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد بيانات للطباعة", "طباعة", C__project_Auto_Care.Theme.WMsgIcon.Warning, owner);
                return;
            }
            try
            {
                ReportPreviewWindow.Show(BuildInventory(sparePartsData), owner, "تقرير المخزون والمستودع");
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء الطباعة: " + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
            }
        }

        public static bool ExportInventoryToPdf(DataTable sparePartsData, Window owner)
        {
            if (sparePartsData == null || sparePartsData.Rows.Count == 0)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد بيانات لتصديرها", "تصدير PDF", C__project_Auto_Care.Theme.WMsgIcon.Warning, owner);
                return false;
            }
            try
            {
                ReportDocument doc = BuildInventory(sparePartsData);
                var pages = ReportRenderer.Render(doc);
                bool res = ReportExporter.SaveWithDialog(pages, "تقرير_المخزون_والمستودع", owner, ReportExportFormat.Pdf);
                foreach (var p in pages) p.Dispose();
                return res;
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير تقرير المخزون:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static bool ExportInventoryToExcel(DataTable sparePartsData, Window owner)
        {
            if (sparePartsData == null || sparePartsData.Rows.Count == 0)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد بيانات لتصديرها إلى إكسيل", "تصدير إكسيل", C__project_Auto_Care.Theme.WMsgIcon.Warning, owner);
                return false;
            }
            try
            {
                return ExcelExporter.ExportInventoryReport(sparePartsData, owner);
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير تقرير المخزون إلى إكسيل:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static ReportDocument BuildInventory(DataTable sparePartsData)
        {
            var table = new ReportTableBlock
            {
                Title = "جرد قطع الغيار"
            };
            table
                .AddColumn("كود القطعة", ReportAlign.Center, 1.2)
                .AddColumn("اسم القطعة", ReportAlign.Right, 3.0)
                .AddColumn("الكمية بالمخزون", ReportAlign.Center, 1.0)
                .AddColumn("الحد الأدنى", ReportAlign.Center, 1.0)
                .AddColumn("سعر الشراء", ReportAlign.Right, 1.0)
                .AddColumn("الحالة", ReportAlign.Center, 1.0);

            int totalQty = 0;
            decimal totalValue = 0m;

            foreach (DataRow srcRow in sparePartsData.Rows)
            {
                string partCode = srcRow["PartCode"]?.ToString() ?? "";
                string partName = srcRow["PartName"]?.ToString() ?? "";
                int qty = Convert.ToInt32(srcRow["QuantityInStock"]);
                int minStock = Convert.ToInt32(srcRow["MinimumStock"]);
                decimal buyingPrice = Convert.ToDecimal(srcRow["BuyingPrice"]);

                totalQty += qty;
                totalValue += qty * buyingPrice;

                string stockStatus = qty == 0 ? "نفذ المخزون" : (qty <= minStock ? "منخفض" : "متوفر");

                table.AddRow(partCode, partName, qty.ToString(), minStock.ToString(),
                    ReportRenderer.Money(buyingPrice), stockStatus);
            }

            table.TotalsRow = new[]
            {
                "",
                "الإجمالي",
                totalQty.ToString(),
                "",
                ReportRenderer.Money(totalValue),
                sparePartsData.Rows.Count + " قطعة"
            };

            ReportDocument doc = new ReportDocument();

            doc.Add(new ReportTitleBlock("تقرير المخزون"));
            doc.Add(new ReportSubtitleBlock("AutoCare - نظام إدارة مراكز صيانة السيارات"));
            doc.Add(new ReportDividerBlock());

            doc.Add(new ReportInfoBlock()
                .Add("عدد القطع", sparePartsData.Rows.Count.ToString())
                .Add("إجمالي قيمة المخزون", ReportRenderer.Money(totalValue)));

            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            doc.Add(table);

            return doc;
        }

        public static void ShowTopServices(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var serviceRepo = new ServiceRepository(AppServiceLocator.DbHelper);
                DataTable topServices = serviceRepo.GetTopServices(start.Date, end.Date, topN);
                if (topServices == null || topServices.Rows.Count == 0)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد خدمات في الفترة المحددة", "التقرير", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return;
                }
                ReportPreviewWindow.Show(BuildTopServices(start, end, topN, topServices), owner, "تقرير أكثر الخدمات طلباً");
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("فشل في جلب بيانات الخدمات من قاعدة البيانات: " + ex.Message,
                    "خطأ في البيانات", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
            }
        }

        public static bool ExportTopServicesToPdf(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var serviceRepo = new ServiceRepository(AppServiceLocator.DbHelper);
                DataTable topServices = serviceRepo.GetTopServices(start.Date, end.Date, topN);
                if (topServices == null || topServices.Rows.Count == 0)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد خدمات في الفترة المحددة للتصدير", "تصدير PDF", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return false;
                }
                ReportDocument doc = BuildTopServices(start, end, topN, topServices);
                var pages = ReportRenderer.Render(doc);
                bool res = ReportExporter.SaveWithDialog(pages, "تقرير_أكثر_الخدمات_طلبا", owner, ReportExportFormat.Pdf);
                foreach (var p in pages) p.Dispose();
                return res;
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير التقرير:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static bool ExportTopServicesToExcel(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var serviceRepo = new ServiceRepository(AppServiceLocator.DbHelper);
                DataTable topServices = serviceRepo.GetTopServices(start.Date, end.Date, topN);
                if (topServices == null || topServices.Rows.Count == 0)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد خدمات في الفترة المحددة للتصدير", "تصدير إكسيل", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return false;
                }
                return ExcelExporter.ExportTopReport(topServices, "أكثر الخدمات طلباً", start.Date, end.Date, owner);
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير التقرير إلى إكسيل:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static ReportDocument BuildTopServices(DateTime start, DateTime end, int topN, DataTable topServices)
        {
            var table = new ReportTableBlock
            {
                Title = "أكثر الخدمات طلباً"
            };
            table
                .AddColumn("الترتيب", ReportAlign.Center, 0.7)
                .AddColumn("كود الخدمة", ReportAlign.Center, 1.2)
                .AddColumn("اسم الخدمة", ReportAlign.Right, 3.0)
                .AddColumn("عدد الطلبات", ReportAlign.Center, 1.0)
                .AddColumn("إجمالي الإيراد", ReportAlign.Right, 1.2);

            decimal totalRevenue = 0m;
            int totalCount = 0;
            int rank = 1;
            foreach (DataRow row in topServices.Rows)
            {
                string code = row["ServiceCode"]?.ToString() ?? "";
                string name = row["ServiceName"]?.ToString() ?? "";
                int count = Convert.ToInt32(row["Count"]);
                decimal revenue = Convert.ToDecimal(row["TotalRevenue"]);
                totalRevenue += revenue;
                totalCount += count;
                table.AddRow(rank.ToString(), code, name, count.ToString(), ReportRenderer.Money(revenue));
                rank++;
            }

            table.TotalsRow = new[] { "", "", "الإجمالي", totalCount.ToString(), ReportRenderer.Money(totalRevenue) };

            ReportDocument doc = new ReportDocument();

            doc.Add(new ReportTitleBlock("تقرير أكثر الخدمات أداءً"));
            doc.Add(new ReportSubtitleBlock("AutoCare - نظام إدارة مراكز صيانة السيارات"));
            doc.Add(new ReportDividerBlock());

            doc.Add(new ReportInfoBlock()
                .Add("الفترة من", start.ToString("dd/MM/yyyy"))
                .Add("إلى", end.ToString("dd/MM/yyyy"))
                .Add("أعلى " + topN + " خدمة", topServices.Rows.Count.ToString()));

            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            doc.Add(table);

            return doc;
        }

        public static void ShowTopParts(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var sparePartRepo = new SparePartRepository(AppServiceLocator.DbHelper);
                DataTable topParts = sparePartRepo.GetTopSpareParts(start.Date, end.Date, topN);
                if (topParts == null || topParts.Rows.Count == 0)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد قطع غيار في الفترة المحددة", "التقرير", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return;
                }
                ReportPreviewWindow.Show(BuildTopParts(start, end, topN, topParts), owner, "تقرير أكثر قطع الغيار طلباً");
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("فشل في جلب بيانات قطع الغيار من قاعدة البيانات: " + ex.Message,
                    "خطأ في البيانات", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
            }
        }

        public static bool ExportTopPartsToPdf(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var sparePartRepo = new SparePartRepository(AppServiceLocator.DbHelper);
                DataTable topParts = sparePartRepo.GetTopSpareParts(start.Date, end.Date, topN);
                if (topParts == null || topParts.Rows.Count == 0)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد قطع غيار في الفترة المحددة للتصدير", "تصدير PDF", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return false;
                }
                ReportDocument doc = BuildTopParts(start, end, topN, topParts);
                var pages = ReportRenderer.Render(doc);
                bool res = ReportExporter.SaveWithDialog(pages, "تقرير_أكثر_قطع_الغيار_طلبا", owner, ReportExportFormat.Pdf);
                foreach (var p in pages) p.Dispose();
                return res;
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير التقرير:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static bool ExportTopPartsToExcel(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var sparePartRepo = new SparePartRepository(AppServiceLocator.DbHelper);
                DataTable topParts = sparePartRepo.GetTopSpareParts(start.Date, end.Date, topN);
                if (topParts == null || topParts.Rows.Count == 0)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد قطع غيار في الفترة المحددة للتصدير", "تصدير إكسيل", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return false;
                }
                return ExcelExporter.ExportTopReport(topParts, "أكثر قطع الغيار طلباً", start.Date, end.Date, owner);
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير التقرير إلى إكسيل:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static ReportDocument BuildTopParts(DateTime start, DateTime end, int topN, DataTable topParts)
        {
            var table = new ReportTableBlock
            {
                Title = "أكثر قطع الغيار استهلاكاً"
            };
            table
                .AddColumn("الترتيب", ReportAlign.Center, 0.7)
                .AddColumn("كود القطعة", ReportAlign.Center, 1.2)
                .AddColumn("اسم القطعة", ReportAlign.Right, 3.2)
                .AddColumn("الكمية المباعة", ReportAlign.Center, 1.0)
                .AddColumn("إجمالي الإيراد", ReportAlign.Right, 1.1)
                .AddColumn("المتبقي بالمخزون", ReportAlign.Center, 1.0);

            decimal totalRevenue = 0m;
            int totalQuantity = 0;
            int rank = 1;
            foreach (DataRow row in topParts.Rows)
            {
                string code = row["PartCode"]?.ToString() ?? "";
                string name = row["PartName"]?.ToString() ?? "";
                int quantity = Convert.ToInt32(row["TotalQuantity"]);
                decimal revenue = Convert.ToDecimal(row["TotalRevenue"]);
                int stockLeft = Convert.ToInt32(row["StockLeft"]);
                totalRevenue += revenue;
                totalQuantity += quantity;
                table.AddRow(rank.ToString(), code, name, quantity.ToString(), ReportRenderer.Money(revenue), stockLeft.ToString());
                rank++;
            }

            table.TotalsRow = new[] { "", "", "الإجمالي", totalQuantity.ToString(), ReportRenderer.Money(totalRevenue), "" };

            ReportDocument doc = new ReportDocument();

            doc.Add(new ReportTitleBlock("تقرير أكثر قطع الغيار استهلاكاً"));
            doc.Add(new ReportSubtitleBlock("AutoCare - نظام إدارة مراكز صيانة السيارات"));
            doc.Add(new ReportDividerBlock());

            doc.Add(new ReportInfoBlock()
                .Add("الفترة من", start.ToString("dd/MM/yyyy"))
                .Add("إلى", end.ToString("dd/MM/yyyy"))
                .Add("أعلى " + topN + " قطعة", topParts.Rows.Count.ToString()));

            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            doc.Add(table);

            return doc;
        }

        public static void ShowTopCombined(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var serviceRepo = new ServiceRepository(AppServiceLocator.DbHelper);
                var sparePartRepo = new SparePartRepository(AppServiceLocator.DbHelper);
                DataTable topServices = serviceRepo.GetTopServices(start.Date, end.Date, topN);
                DataTable topParts = sparePartRepo.GetTopSpareParts(start.Date, end.Date, topN);

                bool hasServices = topServices != null && topServices.Rows.Count > 0;
                bool hasParts = topParts != null && topParts.Rows.Count > 0;

                if (!hasServices && !hasParts)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد بيانات خدمات أو قطع غيار في الفترة المحددة", "التقرير", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return;
                }
                ReportPreviewWindow.Show(BuildTopCombined(start, end, topN, topServices, topParts), owner, "تقرير الأكثر طلباً واستهلاكاً (الخدمات وقطع الغيار)");
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("فشل في جلب البيانات من قاعدة البيانات: " + ex.Message,
                    "خطأ في البيانات", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
            }
        }

        public static bool ExportTopCombinedToPdf(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var serviceRepo = new ServiceRepository(AppServiceLocator.DbHelper);
                var sparePartRepo = new SparePartRepository(AppServiceLocator.DbHelper);
                DataTable topServices = serviceRepo.GetTopServices(start.Date, end.Date, topN);
                DataTable topParts = sparePartRepo.GetTopSpareParts(start.Date, end.Date, topN);

                bool hasServices = topServices != null && topServices.Rows.Count > 0;
                bool hasParts = topParts != null && topParts.Rows.Count > 0;

                if (!hasServices && !hasParts)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد بيانات خدمات أو قطع غيار في الفترة المحددة للتصدير", "تصدير PDF", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return false;
                }
                ReportDocument doc = BuildTopCombined(start, end, topN, topServices, topParts);
                var pages = ReportRenderer.Render(doc);
                bool res = ReportExporter.SaveWithDialog(pages, "تقرير_الأكثر_طلبا_واستهلاكا", owner, ReportExportFormat.Pdf);
                foreach (var p in pages) p.Dispose();
                return res;
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير التقرير:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static bool ExportTopCombinedToExcel(DateTime start, DateTime end, int topN, Window owner)
        {
            try
            {
                var serviceRepo = new ServiceRepository(AppServiceLocator.DbHelper);
                var sparePartRepo = new SparePartRepository(AppServiceLocator.DbHelper);
                DataTable topServices = serviceRepo.GetTopServices(start.Date, end.Date, topN);
                DataTable topParts = sparePartRepo.GetTopSpareParts(start.Date, end.Date, topN);

                bool hasServices = topServices != null && topServices.Rows.Count > 0;
                bool hasParts = topParts != null && topParts.Rows.Count > 0;

                if (!hasServices && !hasParts)
                {
                    C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد بيانات خدمات أو قطع غيار في الفترة المحددة للتصدير", "تصدير إكسيل", C__project_Auto_Care.Theme.WMsgIcon.Info, owner);
                    return false;
                }

                // If only one has data, export that one; otherwise export services as primary
                DataTable exportDt = hasServices ? topServices : topParts;
                string cat = hasServices ? "الخدمات الأكثر طلباً" : "قطع الغيار الأكثر استهلاكاً";
                return ExcelExporter.ExportTopReport(exportDt, cat, start.Date, end.Date, owner);
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تصدير التقرير إلى إكسيل:\n" + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return false;
            }
        }

        public static ReportDocument BuildTopCombined(DateTime start, DateTime end, int topN, DataTable topServices, DataTable topParts)
        {
            var doc = new ReportDocument();
            doc.Add(new ReportTitleBlock("تقرير الأكثر طلباً واستهلاكاً"));
            doc.Add(new ReportSubtitleBlock("AutoCare - الخدمات وقطع الغيار الأكثر طلباً"));
            doc.Add(new ReportDividerBlock());

            doc.Add(new ReportInfoBlock()
                .Add("الفترة من", start.ToString("dd/MM/yyyy"))
                .Add("إلى", end.ToString("dd/MM/yyyy"))
                .Add("الحد الأقصى لكل قائمة", topN.ToString()));

            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            if (topServices != null && topServices.Rows.Count > 0)
            {
                var serviceTable = new ReportTableBlock
                {
                    Title = "أولاً: أكثر الخدمات طلباً وإنجازاً"
                };
                serviceTable
                    .AddColumn("الترتيب", ReportAlign.Center, 0.7)
                    .AddColumn("كود الخدمة", ReportAlign.Center, 1.2)
                    .AddColumn("اسم الخدمة", ReportAlign.Right, 3.0)
                    .AddColumn("عدد الطلبات", ReportAlign.Center, 1.0)
                    .AddColumn("إجمالي الإيراد", ReportAlign.Right, 1.2);

                decimal totalRevenue = 0m;
                int totalCount = 0;
                int rank = 1;
                foreach (DataRow row in topServices.Rows)
                {
                    string code = row["ServiceCode"]?.ToString() ?? "";
                    string name = row["ServiceName"]?.ToString() ?? "";
                    int count = Convert.ToInt32(row["Count"]);
                    decimal revenue = Convert.ToDecimal(row["TotalRevenue"]);
                    totalRevenue += revenue;
                    totalCount += count;
                    serviceTable.AddRow(rank.ToString(), code, name, count.ToString(), ReportRenderer.Money(revenue));
                    rank++;
                }

                serviceTable.TotalsRow = new[] { "", "", "إجمالي الخدمات", totalCount.ToString(), ReportRenderer.Money(totalRevenue) };
                doc.Add(serviceTable);
                doc.Add(new ReportSpacerBlock(10));
            }

            if (topParts != null && topParts.Rows.Count > 0)
            {
                var partsTable = new ReportTableBlock
                {
                    Title = "ثانياً: أكثر قطع الغيار استهلاكاً"
                };
                partsTable
                    .AddColumn("الترتيب", ReportAlign.Center, 0.7)
                    .AddColumn("كود القطعة", ReportAlign.Center, 1.2)
                    .AddColumn("اسم القطعة", ReportAlign.Right, 3.2)
                    .AddColumn("الكمية المباعة", ReportAlign.Center, 1.0)
                    .AddColumn("إجمالي الإيراد", ReportAlign.Right, 1.1)
                    .AddColumn("المتبقي بالمخزون", ReportAlign.Center, 1.0);

                decimal totalRevenue = 0m;
                int totalQuantity = 0;
                int rank = 1;
                foreach (DataRow row in topParts.Rows)
                {
                    string code = row["PartCode"]?.ToString() ?? "";
                    string name = row["PartName"]?.ToString() ?? "";
                    int quantity = Convert.ToInt32(row["TotalQuantity"]);
                    decimal revenue = Convert.ToDecimal(row["TotalRevenue"]);
                    int stockLeft = Convert.ToInt32(row["StockLeft"]);
                    totalRevenue += revenue;
                    totalQuantity += quantity;
                    partsTable.AddRow(rank.ToString(), code, name, quantity.ToString(), ReportRenderer.Money(revenue), stockLeft.ToString());
                    rank++;
                }

                partsTable.TotalsRow = new[] { "", "", "إجمالي قطع الغيار", totalQuantity.ToString(), ReportRenderer.Money(totalRevenue), "" };
                doc.Add(partsTable);
            }

            return doc;
        }
    }
}