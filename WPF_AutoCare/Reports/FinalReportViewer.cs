using System;
using System.Data;
using System.Drawing;
using System.Windows;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Reports;
using C__project_Auto_Care.Theme;

namespace C__project_Auto_Care.Reports
{
    public static class FinalReportViewer
    {
        public static void Show(string invoiceNumber, string repairOrderNumber, Window owner = null)
        {
            try
            {
                ReportDocument doc = BuildDocument(invoiceNumber, repairOrderNumber);
                string title = !string.IsNullOrWhiteSpace(invoiceNumber) ? ("معاينة الفاتورة الضريبية - " + invoiceNumber) : "معاينة الفاتورة الضريبية";
                ReportPreviewWindow.Show(doc, owner, title);
            }
            catch (Exception ex)
            {
                WMessageBox.Show("حدث خطأ أثناء تحميل الفاتورة:\n" + ex.Message, "خطأ", WMsgIcon.Error, owner);
            }
        }

        public static bool ExportToPdf(string invoiceNumber, string repairOrderNumber, Window owner = null)
        {
            try
            {
                ReportDocument doc = BuildDocument(invoiceNumber, repairOrderNumber);
                Bitmap[] pages = ReportRenderer.Render(doc);
                string defaultTitle = !string.IsNullOrWhiteSpace(invoiceNumber) ? ("فاتورة_" + invoiceNumber) : ("أمر_إصلاح_" + repairOrderNumber);
                bool result = ReportExporter.SaveWithDialog(pages, defaultTitle, owner, ReportExportFormat.Pdf);
                foreach (var bmp in pages) bmp.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                WMessageBox.Show("حدث خطأ أثناء تصدير الفاتورة إلى PDF:\n" + ex.Message, "خطأ", WMsgIcon.Error, owner);
                return false;
            }
        }

        public static ReportDocument BuildDocument(string invoiceNumber, string repairOrderNumber)
        {
            var roRepo = new RepairOrderRepository(AppServiceLocator.DbHelper);
            var rosRepo = new RepairOrderServiceRepository(AppServiceLocator.DbHelper);
            var rospRepo = new RepairOrderSparePartRepository(AppServiceLocator.DbHelper);
            var invRepo = new InvoiceRepository(AppServiceLocator.DbHelper);
            var vehicleRepo = new VehicleRepository(AppServiceLocator.DbHelper);
            var customerRepo = new CustomerRepository(AppServiceLocator.DbHelper);

            DataTable dtRo = roRepo.GetRepairOrderByNumber(repairOrderNumber);
            if (dtRo.Rows.Count == 0)
                throw new Exception("لم يتم العثور على أمر الإصلاح.");

            string plateNumber = dtRo.Rows[0]["PlateNumber"]?.ToString() ?? "";

            DataTable dtVehicle = vehicleRepo.GetVehicleByPlateNumber(plateNumber);
            string phoneNumber = "";
            string fullName = "";
            string carName = "";
            string carModel = "";
            string vin = "";

            if (dtVehicle.Rows.Count > 0)
            {
                phoneNumber = dtVehicle.Rows[0]["CustomerPhone"]?.ToString() ?? "";
                carName = dtVehicle.Rows[0]["CarName"]?.ToString() ?? "";
                carModel = dtVehicle.Rows[0]["CarModel"]?.ToString() ?? "";
                vin = dtVehicle.Rows[0]["VIN"]?.ToString() ?? "";

                DataTable dtCustomer = customerRepo.GetCustomerByPhone(phoneNumber);
                if (dtCustomer.Rows.Count > 0)
                    fullName = dtCustomer.Rows[0]["FullName"]?.ToString() ?? "";
            }

            DataTable dtInvoice = invRepo.GetInvoiceByRepairOrder(repairOrderNumber);
            DataTable dtServices = rosRepo.GetServicesByRepairOrder(repairOrderNumber);
            DataTable dtParts = rospRepo.GetSparePartsByRepairOrder(repairOrderNumber);

            string finalInvoiceNumber = dtInvoice.Rows.Count > 0 ? dtInvoice.Rows[0]["InvoiceNumber"]?.ToString() ?? "" : invoiceNumber;
            DateTime invoiceDate = dtInvoice.Rows.Count > 0 ? Convert.ToDateTime(dtInvoice.Rows[0]["InvoiceDate"]) : DateTime.Now;
            decimal paidAmount = dtInvoice.Rows.Count > 0 ? Convert.ToDecimal(dtInvoice.Rows[0]["PaidAmount"]) : 0m;
            decimal discountAmount = dtInvoice.Rows.Count > 0 && dtInvoice.Columns.Contains("DiscountAmount") && dtInvoice.Rows[0]["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(dtInvoice.Rows[0]["DiscountAmount"]) : 0m;
            decimal taxRate = dtInvoice.Rows.Count > 0 && dtInvoice.Columns.Contains("TaxRate") && dtInvoice.Rows[0]["TaxRate"] != DBNull.Value ? Convert.ToDecimal(dtInvoice.Rows[0]["TaxRate"]) : 0m;
            decimal taxAmount = dtInvoice.Rows.Count > 0 && dtInvoice.Columns.Contains("TaxAmount") && dtInvoice.Rows[0]["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(dtInvoice.Rows[0]["TaxAmount"]) : 0m;
            string paymentMethod = dtInvoice.Rows.Count > 0 && dtInvoice.Columns.Contains("PaymentMethod") && dtInvoice.Rows[0]["PaymentMethod"] != DBNull.Value ? dtInvoice.Rows[0]["PaymentMethod"].ToString() : "نقداً (Cash)";
            string receptionist = dtRo.Rows[0]["ReceptionistUsername"]?.ToString() ?? "";
            string status = dtRo.Rows[0]["Status"]?.ToString() ?? "";
            string complaint = dtRo.Rows[0]["Complaint"]?.ToString() ?? "";

            DateTime entryDate = Convert.ToDateTime(dtRo.Rows[0]["EntryDate"]);
            DateTime expectedDate = dtRo.Rows[0]["ExpectedCompletionDate"] != DBNull.Value
                ? Convert.ToDateTime(dtRo.Rows[0]["ExpectedCompletionDate"])
                : DateTime.MinValue;
            DateTime completionDate = dtRo.Rows[0]["CompletionDate"] != DBNull.Value
                ? Convert.ToDateTime(dtRo.Rows[0]["CompletionDate"])
                : DateTime.MinValue;

            decimal servicesTotal = 0m;
            var servicesTable = new ReportTableBlock { Title = "الخدمات المقدمة" };
            servicesTable
                .AddColumn("كود الخدمة", ReportAlign.Right, 1.2)
                .AddColumn("اسم الخدمة", ReportAlign.Right, 3.0)
                .AddColumn("السعر (د.ل)", ReportAlign.Right, 1.0);

            foreach (DataRow svc in dtServices.Rows)
            {
                string serviceCode = svc["ServiceCode"]?.ToString() ?? "";
                string serviceName = svc["ServiceName"]?.ToString() ?? "";
                decimal unitPrice = Convert.ToDecimal(svc["UnitPrice"]);
                servicesTotal += unitPrice;
                servicesTable.AddRow(serviceCode, serviceName, ReportRenderer.Money(unitPrice));
            }
            servicesTable.TotalsRow = new[] { "", "إجمالي الخدمات", ReportRenderer.Money(servicesTotal) };

            var partsTable = new ReportTableBlock { Title = "قطع الغيار" };
            partsTable
                .AddColumn("كود القطعة", ReportAlign.Right, 1.2)
                .AddColumn("اسم القطعة", ReportAlign.Right, 3.0)
                .AddColumn("الكمية", ReportAlign.Center, 0.8)
                .AddColumn("سعر الوحدة (د.ل)", ReportAlign.Right, 1.0)
                .AddColumn("الإجمالي (د.ل)", ReportAlign.Right, 1.2);

            decimal partsTotal = 0m;
            foreach (DataRow part in dtParts.Rows)
            {
                string partCode = part["PartCode"]?.ToString() ?? "";
                string partName = part["PartName"]?.ToString() ?? "";
                int qty = Convert.ToInt32(part["Quantity"]);
                decimal unitPrice = Convert.ToDecimal(part["UnitPrice"]);
                partsTotal += qty * unitPrice;
                partsTable.AddRow(partCode, partName, qty.ToString(), ReportRenderer.Money(unitPrice), ReportRenderer.Money(qty * unitPrice));
            }

            partsTable.TotalsRow = new[] { "", "إجمالي قطع الغيار", "", "", ReportRenderer.Money(partsTotal) };

            // Accurate financial calculations
            decimal subTotal = servicesTotal + partsTotal;
            if (discountAmount < 0m) discountAmount = 0m;
            if (discountAmount > subTotal) discountAmount = subTotal;
            decimal netBeforeTax = subTotal - discountAmount;
            if (taxRate > 0 && taxAmount == 0)
                taxAmount = Math.Round(netBeforeTax * (taxRate / 100m), 2, MidpointRounding.AwayFromZero);
            decimal grandTotal = netBeforeTax + taxAmount;
            if (paidAmount < 0m) paidAmount = 0m;
            decimal remaining = Math.Max(0m, grandTotal - paidAmount);
            string tafqeet = TafqeetHelper.ConvertToArabicWords(grandTotal, "دينار", "درهم");

            ReportDocument doc = new ReportDocument();

            doc.Add(new ReportTitleBlock("فاتورة ضريبية نهائية"));
            doc.Add(new ReportSubtitleBlock("AutoCare - نظام إدارة مراكز صيانة السيارات"));
            doc.Add(new ReportDividerBlock());

            doc.Add(new ReportInfoBlock()
                .Add("رقم الفاتورة", string.IsNullOrEmpty(finalInvoiceNumber) ? "-" : finalInvoiceNumber)
                .Add("تاريخ الفاتورة", invoiceDate.ToString("dd/MM/yyyy HH:mm"))
                .Add("رقم أمر الإصلاح", repairOrderNumber)
                .Add("طريقة الدفع", paymentMethod)
                .Add("تاريخ الدخول", entryDate.ToString("dd/MM/yyyy"))
                .Add("تاريخ الانتهاء", completionDate != DateTime.MinValue ? completionDate.ToString("dd/MM/yyyy") : "-"));

            doc.Add(new ReportDividerBlock());

            doc.Add(new ReportInfoBlock()
                .Add("اسم العميل", string.IsNullOrEmpty(fullName) ? phoneNumber : fullName)
                .Add("رقم الهاتف", phoneNumber)
                .Add("السيارة", string.IsNullOrEmpty(carName) ? "-" : carName + (string.IsNullOrEmpty(carModel) ? "" : " - موديل " + carModel))
                .Add("رقم اللوحة", plateNumber)
                .Add("رقم الهيكل (VIN)", string.IsNullOrEmpty(vin) ? "-" : vin)
                .Add("موظف الاستقبال", string.IsNullOrEmpty(receptionist) ? "-" : receptionist));

            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            if (dtServices.Rows.Count > 0)
            {
                doc.Add(servicesTable);
                doc.Add(new ReportSpacerBlock(8));
            }

            if (dtParts.Rows.Count > 0)
            {
                doc.Add(partsTable);
                doc.Add(new ReportSpacerBlock(8));
            }

            doc.Add(new ReportSpacerBlock(6));
            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            doc.Add(new ReportTotalsLineBlock("إجمالي قيمة الخدمات:  " + ReportRenderer.Money(servicesTotal)));
            doc.Add(new ReportTotalsLineBlock("إجمالي قيمة قطع الغيار:  " + ReportRenderer.Money(partsTotal)));
            doc.Add(new ReportTotalsLineBlock("المجموع قبل الخصم (SubTotal):  " + ReportRenderer.Money(subTotal)));
            if (discountAmount > 0)
            {
                doc.Add(new ReportTotalsLineBlock("قيمة الخصم الممنوح:  " + ReportRenderer.Money(discountAmount)));
                doc.Add(new ReportTotalsLineBlock("المبلغ الخاضع للضريبة:  " + ReportRenderer.Money(netBeforeTax)));
            }
            if (taxAmount > 0 || taxRate > 0)
            {
                doc.Add(new ReportTotalsLineBlock($"ضريبة القيمة المضافة ({taxRate:0.##}%):  " + ReportRenderer.Money(taxAmount)));
            }
            doc.Add(new ReportTotalsLineBlock("صافي المبلغ المستحق (Final Due):  " + ReportRenderer.Money(grandTotal)));
            doc.Add(new ReportTotalsLineBlock("المبلغ المدفوع:  " + ReportRenderer.Money(paidAmount)));
            doc.Add(new ReportTotalsLineBlock("المبلغ المتبقي:  " + ReportRenderer.Money(remaining)));
            doc.Add(new ReportSpacerBlock(4));
            doc.Add(new ReportTotalsLineBlock("المبلغ كتابة:  " + tafqeet));

            try
            {
                string qrData = QrCodeGenerator.GenerateZatcaTlvBase64(
                    "مركز أوتو كير لصيانة السيارات - AutoCare",
                    "310000000000003",
                    invoiceDate,
                    grandTotal,
                    taxAmount);
                System.Drawing.Bitmap qrBmp = QrCodeGenerator.GenerateQrBitmap(qrData, 120);
                doc.Add(new ReportSpacerBlock(8));
                doc.Add(new ReportQrCodeBlock(qrBmp, 100, "رمز الفاتورة الإلكترونية المعتمد - ZATCA E-Invoice QR"));
            }
            catch { }

            return doc;
        }
    }
}