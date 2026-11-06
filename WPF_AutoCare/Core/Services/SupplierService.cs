using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepo;
        private readonly IAuditRepository _auditRepo;

        public SupplierService(ISupplierRepository supplierRepo, IAuditRepository auditRepo)
        {
            _supplierRepo = supplierRepo;
            _auditRepo = auditRepo;
        }

        public DataTable GetAllSuppliers(string search = "")
        {
            return _supplierRepo.GetAllSuppliers(search);
        }

        public OperationResult AddSupplier(string name, string contact, string phone, string address)
        {
            if (string.IsNullOrWhiteSpace(name))
                return OperationResult.Fail("اسم المورد مطلوب.");

            try
            {
                int newId = _supplierRepo.AddSupplier(name.Trim(), contact?.Trim(), phone?.Trim(), address?.Trim());
                string user = UserSession.Username ?? "System";
                _auditRepo?.Log("إضافة مورد", "Suppliers", newId.ToString(), $"إضافة مورد جديد: {name} (هاتف: {phone})", user);
                return OperationResult.Ok("تمت إضافة المورد بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل في إضافة المورد: " + ex.Message);
            }
        }

        public OperationResult RecordPurchase(int supplierId, string invoiceNumber, decimal paidAmount, List<PurchaseItemDto> items, string notes)
        {
            if (supplierId <= 0)
                return OperationResult.Fail("يرجى اختيار المورد.");

            if (items == null || items.Count == 0)
                return OperationResult.Fail("يجب إضافة قطعة واحدة على الأقل لفاتورة الشراء.");

            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                    return OperationResult.Fail($"كمية القطعة {item.PartCode} يجب أن تكون أكبر من الصفر.");
                if (item.UnitCost < 0)
                    return OperationResult.Fail($"تكلفة شراء القطعة {item.PartCode} لا يمكن أن تكون سالبة.");
            }

            try
            {
                decimal total = items.Sum(i => i.TotalCost);
                string invNum = string.IsNullOrWhiteSpace(invoiceNumber) ? _supplierRepo.GenerateNewPurchaseInvoiceNumber() : invoiceNumber.Trim();
                string createdBy = UserSession.Username ?? "System";

                _supplierRepo.CreatePurchaseInvoice(invNum, supplierId, total, notes, createdBy, items);

                return OperationResult.Ok($"تم تسجيل فاتورة الشراء بنجاح برقم: {invNum}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تسجيل فاتورة الشراء: " + ex.Message);
            }
        }

        public DataTable GetSupplierLedger(int supplierId)
        {
            return _supplierRepo.GetAllPurchaseInvoices();
        }

        public string GenerateNewPurchaseInvoiceNumber()
        {
            try
            {
                return _supplierRepo.GenerateNewPurchaseInvoiceNumber();
            }
            catch (Exception ex)
            {
                AppLogger.Error("SupplierService.GenerateNewPurchaseInvoiceNumber failed", ex);
                return "PINV-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }
        }

        public List<PurchaseInvoiceDetailDto> GetPurchaseInvoiceDetails(string invoiceNumber)
        {
            var list = new List<PurchaseInvoiceDetailDto>();
            if (string.IsNullOrWhiteSpace(invoiceNumber)) return list;

            try
            {
                DataTable dt = _supplierRepo.GetPurchaseInvoiceDetails(invoiceNumber.Trim());
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        int qty = r["Quantity"] != DBNull.Value ? Convert.ToInt32(r["Quantity"]) : 0;
                        decimal cost = r["UnitCost"] != DBNull.Value ? Convert.ToDecimal(r["UnitCost"]) : 0m;
                        decimal tot = r["TotalCost"] != DBNull.Value ? Convert.ToDecimal(r["TotalCost"]) : (qty * cost);

                        list.Add(new PurchaseInvoiceDetailDto
                        {
                            PartCode = r["PartCode"]?.ToString() ?? "",
                            PartName = r["PartName"]?.ToString() ?? "",
                            Quantity = qty,
                            UnitCost = cost,
                            TotalCost = tot
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("SupplierService.GetPurchaseInvoiceDetails failed", ex);
            }
            return list;
        }

        public List<SupplierSummaryDto> GetSuppliersList(string search = "")
        {
            var list = new List<SupplierSummaryDto>();
            try
            {
                DataTable dt = _supplierRepo.GetAllSuppliers(search?.Trim() ?? "");
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        decimal bal = r["Balance"] != DBNull.Value ? Convert.ToDecimal(r["Balance"]) : 0m;
                        string created = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]).ToString("yyyy-MM-dd") : "-";

                        list.Add(new SupplierSummaryDto
                        {
                            SupplierID = Convert.ToInt32(r["SupplierID"]),
                            SupplierName = r["SupplierName"]?.ToString() ?? "",
                            ContactPerson = r["ContactPerson"]?.ToString() ?? "-",
                            PhoneNumber = r["PhoneNumber"]?.ToString() ?? "-",
                            Address = r["Address"]?.ToString() ?? "-",
                            Balance = bal.ToString("N2") + " د.ل",
                            CreatedAt = created
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("SupplierService.GetSuppliersList failed", ex);
            }
            return list;
        }

        public List<PurchaseInvoiceSummaryDto> GetPurchaseInvoicesList(string search = "")
        {
            var list = new List<PurchaseInvoiceSummaryDto>();
            try
            {
                DataTable dt = _supplierRepo.GetAllPurchaseInvoices(search?.Trim() ?? "");
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        decimal tot = r["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(r["TotalAmount"]) : 0m;
                        string invDate = r["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(r["InvoiceDate"]).ToString("yyyy-MM-dd HH:mm") : "-";

                        list.Add(new PurchaseInvoiceSummaryDto
                        {
                            PurchaseInvoiceNumber = r["PurchaseInvoiceNumber"]?.ToString() ?? "",
                            InvoiceDate = invDate,
                            SupplierName = r["SupplierName"]?.ToString() ?? "",
                            TotalAmount = tot.ToString("N2") + " د.ل",
                            Notes = r["Notes"]?.ToString() ?? "-",
                            CreatedBy = r["CreatedBy"]?.ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("SupplierService.GetPurchaseInvoicesList failed", ex);
            }
            return list;
        }

        public OperationResult UpdateSupplier(int supplierId, string name, string contact, string phone, string address)
        {
            if (supplierId <= 0)
                return OperationResult.Fail("رقم المورد غير صالح.");
            if (string.IsNullOrWhiteSpace(name))
                return OperationResult.Fail("اسم المورد مطلوب.");

            try
            {
                _supplierRepo.UpdateSupplier(supplierId, name.Trim(), contact?.Trim(), phone?.Trim(), address?.Trim());
                string user = UserSession.Username ?? "System";
                _auditRepo?.Log("تعديل مورد", "Suppliers", supplierId.ToString(), $"تعديل بيانات المورد {name}", user);
                return OperationResult.Ok("تم تعديل بيانات المورد بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل في تعديل المورد: " + ex.Message);
            }
        }

        public SupplierSummaryDto GetSupplierById(int supplierId)
        {
            try
            {
                DataRow r = _supplierRepo.GetSupplierById(supplierId);
                if (r == null) return null;
                return new SupplierSummaryDto
                {
                    SupplierID = Convert.ToInt32(r["SupplierID"]),
                    SupplierName = r["SupplierName"]?.ToString() ?? "",
                    ContactPerson = r["ContactPerson"]?.ToString() ?? "",
                    PhoneNumber = r["PhoneNumber"]?.ToString() ?? "",
                    Address = r["Address"]?.ToString() ?? "",
                    Balance = (r["Balance"] != DBNull.Value ? Convert.ToDecimal(r["Balance"]) : 0m).ToString("N2"),
                    CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]).ToString("yyyy-MM-dd") : ""
                };
            }
            catch (Exception ex)
            {
                AppLogger.Error("SupplierService.GetSupplierById failed", ex);
                return null;
            }
        }

        public OperationResult DeleteSupplier(int supplierId, string supplierName = "")
        {
            if (supplierId <= 0)
                return OperationResult.Fail("رقم المورد غير صالح.");

            try
            {
                _supplierRepo.DeleteSupplier(supplierId);
                _auditRepo?.Log("حذف مورد", "Suppliers", supplierId.ToString(), $"حذف المورد: {supplierName}", UserSession.Username ?? "System");
                return OperationResult.Ok("تم حذف المورد بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("تعذر حذف المورد: " + ex.Message);
            }
        }
    }
}

