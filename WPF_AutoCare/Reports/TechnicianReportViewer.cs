using System;
using System.Data;
using System.Windows;
using C__project_Auto_Care.Reports;

namespace C__project_Auto_Care.Reports
{
    public static class TechnicianReportViewer
    {
        public static void Show(DataTable settlementData, DataTable technicianData, Window owner = null)
        {
            if (settlementData == null || settlementData.Rows.Count == 0)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("لا توجد بيانات لعرضها في التقرير.", "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
                return;
            }

            try
            {
                ReportDocument doc = BuildDocument(settlementData, technicianData);
                ReportPreviewWindow.Show(doc, owner);
            }
            catch (Exception ex)
            {
                C__project_Auto_Care.Theme.WMessageBox.Show("حدث خطأ أثناء تحميل التقرير: " + ex.Message, "خطأ", C__project_Auto_Care.Theme.WMsgIcon.Error, owner);
            }
        }

        public static ReportDocument BuildDocument(DataTable settlementData, DataTable technicianData)
        {
            string technicianName = "فني";
            string specialization = "-";
            string commissionPercent = "-";

            if (technicianData != null && technicianData.Rows.Count > 0)
            {
                DataRow src = technicianData.Rows[0];
                if (technicianData.Columns.Contains("FullName"))
                    technicianName = src["FullName"]?.ToString() ?? "فني";
                if (technicianData.Columns.Contains("Specialization"))
                    specialization = src["Specialization"]?.ToString() ?? "-";
                if (technicianData.Columns.Contains("CommissionPercent"))
                    commissionPercent = Convert.ToDecimal(src["CommissionPercent"]).ToString("0.##") + "%";
            }

            var table = new ReportTableBlock
            {
                Title = "تفاصيل التسويات"
            };
            table
                .AddColumn("رقم التسوية", ReportAlign.Center, 1.1)
                .AddColumn("رقم أمر الإصلاح", ReportAlign.Center, 1.3)
                .AddColumn("إجمالي الخدمات", ReportAlign.Right, 1.0)
                .AddColumn("نسبة العمولة", ReportAlign.Center, 1.0)
                .AddColumn("قيمة العمولة", ReportAlign.Right, 1.0)
                .AddColumn("التاريخ", ReportAlign.Center, 1.1)
                .AddColumn("الحالة", ReportAlign.Center, 0.9);

            decimal sumServiceTotal = 0m;
            decimal sumCommission = 0m;

            foreach (DataRow srcRow in settlementData.Rows)
            {
                string settlementId = srcRow["SettlementID"]?.ToString() ?? "";
                string repairOrderNumber = srcRow["RepairOrderNumber"]?.ToString() ?? "";
                decimal serviceTotal = Convert.ToDecimal(srcRow["ServiceTotal"]);
                decimal rate = Convert.ToDecimal(srcRow["CommissionRate"]);
                decimal amount = Convert.ToDecimal(srcRow["CommissionAmount"]);
                string settledDate = srcRow["SettledDate"] != DBNull.Value
                    ? Convert.ToDateTime(srcRow["SettledDate"]).ToString("dd/MM/yyyy")
                    : "-";
                string status = srcRow["Status"]?.ToString() ?? "";

                sumServiceTotal += serviceTotal;
                sumCommission += amount;

                table.AddRow(settlementId, repairOrderNumber,
                    ReportRenderer.Money(serviceTotal),
                    rate.ToString("0.##") + "%",
                    ReportRenderer.Money(amount),
                    settledDate, status);
            }

            table.TotalsRow = new[]
            {
                "",
                "الإجمالي",
                ReportRenderer.Money(sumServiceTotal),
                "",
                ReportRenderer.Money(sumCommission),
                "",
                ""
            };

            ReportDocument doc = new ReportDocument();

            doc.Add(new ReportTitleBlock("تقرير عمولات الفني"));
            doc.Add(new ReportSubtitleBlock("AutoCare - نظام إدارة مراكز صيانة السيارات"));
            doc.Add(new ReportDividerBlock());

            doc.Add(new ReportInfoBlock()
                .Add("اسم الفني", technicianName)
                .Add("التخصص", specialization)
                .Add("نسبة العمولة", commissionPercent)
                .Add("عدد التسويات", settlementData.Rows.Count.ToString()));

            doc.Add(new ReportDividerBlock());
            doc.Add(new ReportSpacerBlock(6));

            doc.Add(table);

            return doc;
        }
    }
}