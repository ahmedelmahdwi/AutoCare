using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Enums;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;
using C__project_Auto_Care.Reports;

namespace C__project_Auto_Care.Core.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IDbHelper _dbHelper;
        private readonly IInvoiceRepository _invRepo;
        private readonly IRepairOrderRepository _roRepo;
        private readonly IRepairOrderServiceRepository _rosRepo;
        private readonly IRepairOrderSparePartRepository _rospRepo;
        private readonly ITechnicianSettlementRepository _tsRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IAuditRepository _auditRepo;

        public InvoiceService(
            IDbHelper dbHelper,
            IInvoiceRepository invRepo,
            IRepairOrderRepository roRepo,
            IRepairOrderServiceRepository rosRepo,
            IRepairOrderSparePartRepository rospRepo,
            ITechnicianSettlementRepository tsRepo,
            ICustomerRepository customerRepo,
            IVehicleRepository vehicleRepo,
            IAuditRepository auditRepo)
        {
            _dbHelper = dbHelper;
            _invRepo = invRepo;
            _roRepo = roRepo;
            _rosRepo = rosRepo;
            _rospRepo = rospRepo;
            _tsRepo = tsRepo;
            _customerRepo = customerRepo;
            _vehicleRepo = vehicleRepo;
            _auditRepo = auditRepo;
        }

        public InvoiceCalculationResult Calculate(decimal servicesTotal, decimal partsTotal, decimal discountAmount, decimal taxRatePercent = 0m, decimal paidAmount = 0m)
        {
            if (servicesTotal < 0) servicesTotal = 0;
            if (partsTotal < 0) partsTotal = 0;
            decimal subTotal = servicesTotal + partsTotal;
            if (discountAmount < 0) discountAmount = 0;
            if (discountAmount > subTotal) discountAmount = subTotal;

            decimal taxableAmount = subTotal - discountAmount;
            decimal taxAmount = 0;
            if (taxRatePercent > 0)
            {
                taxAmount = Math.Round(taxableAmount * (taxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);
            }

            decimal finalDue = taxableAmount + taxAmount;
            if (paidAmount < 0) paidAmount = 0;
            decimal remaining = Math.Max(0, finalDue - paidAmount);

            string tafqeet = TafqeetHelper.ConvertToArabicWords(finalDue);
            string qrData = QrCodeGenerator.GenerateZatcaTlvBase64(
                "مركز أوتو كير لصيانة السيارات - AutoCare",
                "310000000000003",
                DateTime.Now,
                finalDue,
                taxAmount);

            return new InvoiceCalculationResult
            {
                ServicesTotal = servicesTotal,
                SparePartsTotal = partsTotal,
                DiscountAmount = discountAmount,
                TaxRate = taxRatePercent,
                TaxAmount = taxAmount,
                FinalDueAmount = finalDue,
                PaidAmount = paidAmount,
                RemainingAmount = remaining,
                TafqeetArabicWords = tafqeet,
                ZatcaQrCodeBase64 = qrData
            };
        }

        public OperationResult<string> CreateFinalInvoice(string repairOrderNumber, decimal netTotal, decimal discountAmount, string paymentMethod, string createdByUsername)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult<string>.Fail("رقم أمر الإصلاح غير موجود.");

            if (netTotal < 0)
                return OperationResult<string>.Fail("قيمة الفاتورة لا يمكن أن تكون سالبة.");

            if (string.IsNullOrWhiteSpace(paymentMethod))
                paymentMethod = PaymentMethod.Cash.ToDisplayArabic();

            string user = string.IsNullOrWhiteSpace(createdByUsername) ? (UserSession.Username ?? "admin") : createdByUsername;
            string invoiceNumber = null;

            try
            {
                DataTable dtServices = _rosRepo.GetServicesByRepairOrder(repairOrderNumber);

                var groupedByTech = dtServices.AsEnumerable()
                    .Where(r => r["TechnicianID"] != DBNull.Value)
                    .GroupBy(r => new
                    {
                        TechnicianID = Convert.ToInt32(r["TechnicianID"]),
                        TechnicianName = r["TechnicianName"]?.ToString() ?? "",
                        CommissionPercent = r.Table.Columns.Contains("CommissionPercent") && r["CommissionPercent"] != DBNull.Value
                            ? Convert.ToDecimal(r["CommissionPercent"])
                            : 0m
                    });

                // Check idempotency: if invoice already created for this repair order
                DataTable dtInvoice = _invRepo.GetInvoiceByRepairOrder(repairOrderNumber);
                if (dtInvoice != null && dtInvoice.Rows.Count > 0)
                {
                    invoiceNumber = dtInvoice.Rows[0]["InvoiceNumber"]?.ToString() ?? "";
                    return OperationResult<string>.Ok(invoiceNumber, "الفاتورة تم إنشاؤها مسبقاً لهذا الأمر.");
                }

                // Atomic execution using Unit of Work pattern
                using (var uow = AppServiceLocator.CreateUnitOfWork())
                {
                    uow.ExecuteInTransaction(u =>
                    {
                        invoiceNumber = u.Invoices.GenerateNewInvoiceNumber();
                        u.Invoices.InsertInvoice(invoiceNumber, repairOrderNumber, user, netTotal, discountAmount, paymentMethod);

                        // Insert settlements only if not already recorded for this order
                        if (u.TechnicianSettlements.GetSettlementsCount(repairOrderNumber) == 0)
                        {
                            foreach (var group in groupedByTech)
                            {
                                decimal serviceTotal = group.Sum(r =>
                                    r["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(r["UnitPrice"]) : 0m);
                                decimal commissionAmount = Math.Round(serviceTotal * (group.Key.CommissionPercent / 100m), 2, MidpointRounding.AwayFromZero);
                                u.TechnicianSettlements.InsertSettlement(
                                    repairOrderNumber,
                                    group.Key.TechnicianID,
                                    serviceTotal,
                                    group.Key.CommissionPercent,
                                    commissionAmount);
                            }
                        }

                        DateTime compDate = DateTime.Now;
                        DataTable dtExistingRo = u.RepairOrders.GetRepairOrderByNumber(repairOrderNumber);
                        if (dtExistingRo != null && dtExistingRo.Rows.Count > 0 && dtExistingRo.Rows[0]["CompletionDate"] != DBNull.Value)
                        {
                            compDate = Convert.ToDateTime(dtExistingRo.Rows[0]["CompletionDate"]);
                        }
                        u.RepairOrders.UpdateOrderStatus(repairOrderNumber, RepairOrderStatus.Completed.ToDbString(), compDate);
                    });
                }

                _auditRepo?.Log("إنشاء فاتورة", "Invoices", invoiceNumber,
                    $"إنشاء فاتورة نهائية رقم {invoiceNumber} لأمر الإصلاح {repairOrderNumber} بقيمة {netTotal}", user);

                return OperationResult<string>.Ok(invoiceNumber, "تم إنشاء الفاتورة النهائية بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail("حدث خطأ أثناء حفظ الفاتورة في قاعدة البيانات:\n" + ex.Message);
            }
        }

        public List<CompletedOrderWithoutInvoiceDto> GetCompletedOrdersWithoutInvoice(string searchTerm = "")
        {
            try
            {
                return _invRepo.GetCompletedOrdersWithoutInvoiceList(searchTerm ?? "");
            }
            catch (Exception ex)
            {
                AppLogger.Error("InvoiceService.GetCompletedOrdersWithoutInvoice failed", ex);
                return new List<CompletedOrderWithoutInvoiceDto>();
            }
        }

        public List<IssuedInvoiceSummaryDto> GetIssuedInvoices(string searchTerm = "")
        {
            try
            {
                return _invRepo.GetIssuedInvoicesList(searchTerm ?? "");
            }
            catch (Exception ex)
            {
                AppLogger.Error("InvoiceService.GetIssuedInvoices failed", ex);
                return new List<IssuedInvoiceSummaryDto>();
            }
        }

        public OperationResult RevertToInProgress(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رقم أمر الإصلاح مطلوب.");

            try
            {
                _invRepo.RevertToInProgress(repairOrderNumber.Trim());
                _auditRepo?.Log("إعادة للتنفيذ", "RepairOrders", repairOrderNumber.Trim(), "إعادة أمر الإصلاح لحالة قيد التنفيذ", UserSession.Username ?? "admin");
                return OperationResult.Ok("تم إعادة أمر الإصلاح إلى حالة قيد التنفيذ بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تغيير الحالة: " + ex.Message);
            }
        }

        public bool DoesRepairOrderExist(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber)) return false;
            try
            {
                var dt = _roRepo.GetRepairOrderByNumber(repairOrderNumber.Trim());
                return dt != null && dt.Rows.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public FinancialReportSummaryDto GetFinancialReport(DateTime startDate, DateTime endDate)
        {
            var summary = new FinancialReportSummaryDto();
            try
            {
                var invoices = _invRepo.GetInvoicesWithDetailsForReportList(startDate.Date, endDate.Date);
                if (invoices != null)
                {
                    summary.Invoices = invoices;
                    foreach (var inv in invoices)
                    {
                        summary.TotalServices += inv.ServicesTotal;
                        summary.TotalParts += inv.PartsTotal;
                        summary.GrandTotal += inv.GrandTotal;
                    }
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("InvoiceService.GetFinancialReport failed", ex);
            }
            return summary;
        }

        public FinalInvoicePrintDto GetInvoiceForPrint(string invoiceNumber, string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                throw new ArgumentException("رقم أمر الإصلاح مطلوب.");

            var detailsDto = AppServiceLocator.RepairOrderService.GetRepairOrderDetails(repairOrderNumber);

            DataTable dtInvoice = _invRepo.GetInvoiceByRepairOrder(repairOrderNumber);

            string finalInvoiceNumber = dtInvoice.Rows.Count > 0 ? dtInvoice.Rows[0]["InvoiceNumber"]?.ToString() ?? "" : invoiceNumber;
            DateTime invoiceDate = dtInvoice.Rows.Count > 0 ? Convert.ToDateTime(dtInvoice.Rows[0]["InvoiceDate"]) : DateTime.Now;
            decimal paidAmount = dtInvoice.Rows.Count > 0 ? Convert.ToDecimal(dtInvoice.Rows[0]["PaidAmount"]) : 0m;
            decimal discountAmount = dtInvoice.Rows.Count > 0 && dtInvoice.Rows[0].Table.Columns.Contains("DiscountAmount") && dtInvoice.Rows[0]["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(dtInvoice.Rows[0]["DiscountAmount"]) : 0m;
            string paymentMethod = dtInvoice.Rows.Count > 0 && dtInvoice.Rows[0].Table.Columns.Contains("PaymentMethod") && dtInvoice.Rows[0]["PaymentMethod"] != DBNull.Value ? dtInvoice.Rows[0]["PaymentMethod"].ToString() : "نقداً (Cash)";

            return new FinalInvoicePrintDto
            {
                InvoiceNumber = finalInvoiceNumber,
                InvoiceDate = invoiceDate,
                PaidAmount = paidAmount,
                DiscountAmount = discountAmount,
                PaymentMethod = paymentMethod,
                OrderDetails = detailsDto
            };
        }
    }
}

