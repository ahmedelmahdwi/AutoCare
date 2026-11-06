using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ISparePartRepository _partRepo;
        private readonly IAuditRepository _auditRepo;

        public InventoryService(ISparePartRepository partRepo, IAuditRepository auditRepo)
        {
            _partRepo = partRepo;
            _auditRepo = auditRepo;
        }

        public DataTable GetAllSpareParts(string searchTerm = "")
        {
            return _partRepo.GetAllSpareParts(searchTerm);
        }

        public OperationResult AdjustStock(string partCode, int delta, string reason)
        {
            if (string.IsNullOrWhiteSpace(partCode))
                return OperationResult.Fail("رمز القطعة مطلوب.");

            if (delta == 0)
                return OperationResult.Fail("قيمة التعديل يجب أن تكون مختلفة عن الصفر.");

            try
            {
                int currentStock = _partRepo.GetAvailableStock(partCode);
                if (currentStock + delta < 0)
                {
                    return OperationResult.Fail($"الكمية الحالية ({currentStock}) لا تكفي لإجراء خصم مقداره ({Math.Abs(delta)}).");
                }

                _partRepo.UpdateStock(partCode, delta);

                string username = UserSession.Username ?? "System";
                _auditRepo?.Log(delta > 0 ? "زيادة مخزون" : "خصم مخزون", "SpareParts", partCode,
                    $"تعديل المخزون للقطعة {partCode} بمقدار {delta}. السبب: {reason}. الرصيد الجديد: {currentStock + delta}", username);

                return OperationResult.Ok($"تم تعديل المخزون بنجاح. الرصيد الحالي: {currentStock + delta}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تعديل المخزون: " + ex.Message);
            }
        }

        public int GetLowStockCount()
        {
            return _partRepo.GetLowStockCount();
        }

        public List<SparePartRowDto> GetSparePartsList(string searchTerm = "")
        {
            var list = new List<SparePartRowDto>();
            try
            {
                DataTable dt = _partRepo.GetAllSpareParts(searchTerm?.Trim() ?? "");
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new SparePartRowDto
                        {
                            PartCode = row["PartCode"]?.ToString() ?? "",
                            PartName = row["PartName"]?.ToString() ?? "",
                            Qty = row["QuantityInStock"]?.ToString() ?? "0",
                            BuyingPrice = row["BuyingPrice"] != DBNull.Value ? Convert.ToDecimal(row["BuyingPrice"]).ToString("0.00") : "0.00",
                            SellingPrice = row["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(row["SellingPrice"]).ToString("0.00") : "0.00",
                            MinStock = row["MinimumStock"]?.ToString() ?? "0",
                            Category = row["Category"]?.ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("InventoryService.GetSparePartsList failed", ex);
            }
            return list;
        }

        public OperationResult DeleteSparePart(string partCode)
        {
            if (string.IsNullOrWhiteSpace(partCode))
                return OperationResult.Fail("رمز القطعة مطلوب.");

            try
            {
                _partRepo.DeleteSparePart(partCode.Trim());
                _auditRepo?.Log("حذف قطعة غيار", "SpareParts", partCode.Trim(), $"تم حذف قطعة الغيار ذات الرمز {partCode}", UserSession.Username ?? "System");
                return OperationResult.Ok("تم حذف قطعة الغيار بنجاح.");
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء محاولة الحذف: " + ex.Message);
            }
        }

        public SparePartRowDto GetSparePartByCode(string partCode)
        {
            if (string.IsNullOrWhiteSpace(partCode)) return null;
            try
            {
                DataTable dt = _partRepo.GetSparePartByCode(partCode.Trim());
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return new SparePartRowDto
                    {
                        PartCode = row["PartCode"]?.ToString() ?? "",
                        PartName = row["PartName"]?.ToString() ?? "",
                        Qty = row["QuantityInStock"]?.ToString() ?? "0",
                        BuyingPrice = row["BuyingPrice"] != DBNull.Value ? Convert.ToDecimal(row["BuyingPrice"]).ToString("0.00") : "0.00",
                        SellingPrice = row["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(row["SellingPrice"]).ToString("0.00") : "0.00",
                        MinStock = row["MinimumStock"]?.ToString() ?? "0",
                        Category = row["Category"]?.ToString() ?? ""
                    };
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("InventoryService.GetSparePartByCode failed", ex);
            }
            return null;
        }

        public List<string> GetDistinctCategories()
        {
            try
            {
                return _partRepo.GetDistinctCategories();
            }
            catch (Exception ex)
            {
                AppLogger.Error("InventoryService.GetDistinctCategories failed", ex);
                return new List<string>();
            }
        }

        public OperationResult AddSparePart(string partCode, string partName, int quantity, int minStock, decimal buyingPrice, decimal sellingPrice, string location, string category)
        {
            if (string.IsNullOrWhiteSpace(partCode))
                return OperationResult.Fail("رمز قطعة الغيار مطلوب.");
            if (string.IsNullOrWhiteSpace(partName))
                return OperationResult.Fail("اسم قطعة الغيار مطلوب.");

            try
            {
                if (_partRepo.PartCodeExists(partCode.Trim()))
                    return OperationResult.Fail("رمز قطعة الغيار هذا موجود بالفعل مسبقاً. الرجاء استخدام رمز آخر.");

                _partRepo.AddSparePart(partCode.Trim(), partName.Trim(), quantity, minStock, buyingPrice, sellingPrice, location ?? "", category ?? "");
                _auditRepo?.Log("إضافة قطعة غيار", "SpareParts", partCode.Trim(), $"إضافة قطعة: {partName} - كمية: {quantity} - شراء: {buyingPrice:N2} - بيع: {sellingPrice:N2}", UserSession.Username ?? "System");
                return OperationResult.Ok("تم إضافة قطعة الغيار بنجاح.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("InventoryService.AddSparePart failed", ex);
                return OperationResult.Fail("حدث خطأ أثناء إضافة القطعة: " + ex.Message);
            }
        }

        public OperationResult UpdateSparePart(string partCode, string partName, int quantity, int minStock, decimal buyingPrice, decimal sellingPrice, string location, string category)
        {
            if (string.IsNullOrWhiteSpace(partCode))
                return OperationResult.Fail("رمز قطعة الغيار مطلوب.");
            if (string.IsNullOrWhiteSpace(partName))
                return OperationResult.Fail("اسم قطعة الغيار مطلوب.");

            try
            {
                _partRepo.UpdateSparePart(partCode.Trim(), partName.Trim(), quantity, minStock, buyingPrice, sellingPrice, location ?? "", category ?? "");
                _auditRepo?.Log("تعديل قطعة غيار", "SpareParts", partCode.Trim(), $"تعديل قطعة: {partName} - كمية: {quantity} - شراء: {buyingPrice:N2} - بيع: {sellingPrice:N2}", UserSession.Username ?? "System");
                return OperationResult.Ok("تم تعديل قطعة الغيار بنجاح.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("InventoryService.UpdateSparePart failed", ex);
                return OperationResult.Fail("حدث خطأ أثناء تعديل القطعة: " + ex.Message);
            }
        }

        public List<SparePartCatalogItemDto> GetSparePartsByCategory(string category)
        {
            var list = new List<SparePartCatalogItemDto>();
            if (string.IsNullOrWhiteSpace(category)) return list;
            try
            {
                DataTable dt = _partRepo.GetSparePartsByCategory(category.Trim());
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new SparePartCatalogItemDto
                        {
                            PartCode = row["PartCode"]?.ToString() ?? "",
                            PartName = row["PartName"]?.ToString() ?? "",
                            Category = row["Category"]?.ToString() ?? "",
                            Price = row["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(row["SellingPrice"]) : 0m,
                            QuantityInStock = row["QuantityInStock"] != DBNull.Value ? Convert.ToInt32(row["QuantityInStock"]) : 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("InventoryService.GetSparePartsByCategory failed", ex);
            }
            return list;
        }

        public InventoryReportSummaryDto GetInventoryReport(string search = "", bool lowStockOnly = false)
        {
            var summary = new InventoryReportSummaryDto();
            try
            {
                DataTable dt = _partRepo.GetAllSpareParts(search?.Trim() ?? "");
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        int qty = row["QuantityInStock"] != DBNull.Value ? Convert.ToInt32(row["QuantityInStock"]) : 0;
                        int minStock = row["MinimumStock"] != DBNull.Value ? Convert.ToInt32(row["MinimumStock"]) : 0;
                        bool isLow = qty <= minStock;

                        if (lowStockOnly && !isLow) continue;

                        decimal buyingPrice = row["BuyingPrice"] != DBNull.Value ? Convert.ToDecimal(row["BuyingPrice"]) : 0m;
                        decimal sellingPrice = row["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(row["SellingPrice"]) : 0m;

                        summary.Items.Add(new InventoryReportItemDto
                        {
                            PartCode = row["PartCode"]?.ToString() ?? "",
                            PartName = row["PartName"]?.ToString() ?? "",
                            Quantity = qty,
                            MinimumStock = minStock,
                            BuyingPrice = buyingPrice,
                            SellingPrice = sellingPrice,
                            Category = row.Table.Columns.Contains("Category") ? (row["Category"]?.ToString() ?? "") : "",
                            Location = row.Table.Columns.Contains("Location") ? (row["Location"]?.ToString() ?? "") : ""
                        });

                        summary.TotalQuantity += qty;
                        if (isLow) summary.LowStockCount++;
                        summary.TotalInventoryValue += qty * buyingPrice;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("InventoryService.GetInventoryReport failed", ex);
            }
            return summary;
        }

        public List<TopPartItemDto> GetTopSpareParts(DateTime startDate, DateTime endDate, int topN)
        {
            var list = new List<TopPartItemDto>();
            try
            {
                DataTable dt = _partRepo.GetTopSpareParts(startDate.Date, endDate.Date, topN);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new TopPartItemDto
                        {
                            PartCode = row["PartCode"]?.ToString() ?? "",
                            PartName = row["PartName"]?.ToString() ?? "",
                            Quantity = row["TotalQuantity"] != DBNull.Value ? Convert.ToInt32(row["TotalQuantity"]) : 0,
                            TotalRevenue = row["TotalRevenue"] != DBNull.Value ? Convert.ToDecimal(row["TotalRevenue"]) : 0m,
                            StockLeft = row["StockLeft"] != DBNull.Value ? Convert.ToInt32(row["StockLeft"]) : 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("InventoryService.GetTopSpareParts failed", ex);
            }
            return list;
        }
    }
}
