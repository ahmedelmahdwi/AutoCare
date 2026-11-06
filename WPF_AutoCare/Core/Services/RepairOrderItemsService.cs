using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    /// <summary>
    /// Specialized Domain Service for adding and removing services and spare parts in repair orders.
    /// Handles inventory stock deduction and validation (Clean Architecture).
    /// </summary>
    public class RepairOrderItemsService : IRepairOrderItemsService
    {
        private readonly IRepairOrderServiceRepository _rosRepo;
        private readonly IRepairOrderSparePartRepository _rospRepo;
        private readonly IServiceRepository _serviceRepo;
        private readonly ISparePartRepository _sparePartRepo;
        private readonly IAuditRepository _auditRepo;

        public RepairOrderItemsService(
            IRepairOrderServiceRepository rosRepo,
            IRepairOrderSparePartRepository rospRepo,
            IServiceRepository serviceRepo,
            ISparePartRepository sparePartRepo,
            IAuditRepository auditRepo)
        {
            _rosRepo = rosRepo ?? throw new ArgumentNullException(nameof(rosRepo));
            _rospRepo = rospRepo ?? throw new ArgumentNullException(nameof(rospRepo));
            _serviceRepo = serviceRepo ?? throw new ArgumentNullException(nameof(serviceRepo));
            _sparePartRepo = sparePartRepo ?? throw new ArgumentNullException(nameof(sparePartRepo));
            _auditRepo = auditRepo;
        }

        public OperationResult AddServiceToOrder(string repairOrderNumber, string serviceCode, int technicianId)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");
            if (string.IsNullOrWhiteSpace(serviceCode))
                return OperationResult.Fail("الرجاء اختيار الخدمة أولاً.");
            if (technicianId <= 0)
                return OperationResult.Fail("الرجاء اختيار الفني المسؤول عن الخدمة.");

            try
            {
                DataRow svcRow = _serviceRepo.GetServiceByCode(serviceCode);
                if (svcRow == null)
                    return OperationResult.Fail("الخدمة المحددة غير موجودة.");

                decimal price = Convert.ToDecimal(svcRow["StandardPrice"]);
                string svcName = svcRow["ServiceName"]?.ToString() ?? serviceCode;

                _rosRepo.AddServiceToOrder(repairOrderNumber, serviceCode, technicianId, price);
                _auditRepo?.Log("إضافة خدمة لأمر إصلاح", "RepairOrderServices", repairOrderNumber,
                    $"إضافة خدمة {svcName} ({serviceCode}) للفني {technicianId} بسعر {price:F2}", UserSession.Username);

                return OperationResult.Ok("تمت إضافة الخدمة بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل في إضافة الخدمة: " + ex.Message);
            }
        }

        public OperationResult RemoveServiceFromOrder(string repairOrderNumber, string serviceCode, int technicianId)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber) || string.IsNullOrWhiteSpace(serviceCode))
                return OperationResult.Fail("البيانات غير مكتملة لحذف الخدمة.");

            try
            {
                _rosRepo.RemoveServiceFromOrder(repairOrderNumber, serviceCode, technicianId);
                _auditRepo?.Log("حذف خدمة من أمر إصلاح", "RepairOrderServices", repairOrderNumber,
                    $"حذف الخدمة {serviceCode} المسندة للفني {technicianId}", UserSession.Username);
                return OperationResult.Ok("تم حذف الخدمة بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل في حذف الخدمة: " + ex.Message);
            }
        }

        public OperationResult AddSparePartToOrder(string repairOrderNumber, string partCode, int quantity)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");
            if (string.IsNullOrWhiteSpace(partCode))
                return OperationResult.Fail("الرجاء اختيار قطعة الغيار أولاً.");
            if (quantity <= 0)
                return OperationResult.Fail("الرجاء إدخال كمية صالحة أكبر من الصفر.");

            try
            {
                DataTable dtPart = _sparePartRepo.GetSparePartByCode(partCode);
                if (dtPart == null || dtPart.Rows.Count == 0)
                    return OperationResult.Fail("قطعة الغيار المحددة غير موجودة.");

                DataRow partRow = dtPart.Rows[0];
                int stock = Convert.ToInt32(partRow["QuantityInStock"]);
                decimal price = Convert.ToDecimal(partRow["SellingPrice"]);
                string partName = partRow["PartName"]?.ToString() ?? partCode;

                if (stock < quantity)
                    return OperationResult.Fail($"الكمية المطلوبة غير متوفرة في المخزون (المتوفر حالياً: {stock})");

                _rospRepo.AddSparePartWithStockDeduction(repairOrderNumber, partCode, quantity, price);
                _auditRepo?.Log("إضافة قطعة غيار لأمر إصلاح", "RepairOrderSpareParts", repairOrderNumber,
                    $"إضافة {quantity} من {partName} ({partCode}) وخصمها من المخزون", UserSession.Username);

                return OperationResult.Ok("تمت إضافة قطعة الغيار وخصمها من المخزون بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء إضافة قطعة الغيار: " + ex.Message);
            }
        }

        public OperationResult RemoveSparePartFromOrder(string repairOrderNumber, string partCode, int quantity)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber) || string.IsNullOrWhiteSpace(partCode))
                return OperationResult.Fail("البيانات غير مكتملة لحذف قطعة الغيار.");

            try
            {
                _rospRepo.RemoveSparePartWithStockRestoration(repairOrderNumber, partCode, quantity);
                _auditRepo?.Log("حذف قطعة غيار من أمر إصلاح", "RepairOrderSpareParts", repairOrderNumber,
                    $"حذف {quantity} من {partCode} واستعادة الكمية للمخزون", UserSession.Username);
                return OperationResult.Ok("تم حذف قطعة الغيار واستعادة الكمية بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل في حذف قطعة الغيار: " + ex.Message);
            }
        }

        public List<OrderServiceItemDto> GetOrderServices(string repairOrderNumber)
        {
            var list = new List<OrderServiceItemDto>();
            if (string.IsNullOrWhiteSpace(repairOrderNumber)) return list;
            try
            {
                DataTable dt = _rosRepo.GetServicesByRepairOrder(repairOrderNumber.Trim());
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new OrderServiceItemDto
                        {
                            ServiceCode = row["ServiceCode"]?.ToString() ?? "",
                            ServiceName = row["ServiceName"]?.ToString() ?? "",
                            UnitPrice = row["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(row["UnitPrice"]) : 0m,
                            TechnicianName = row["TechnicianName"]?.ToString() ?? "",
                            TechnicianId = row["TechnicianID"] != DBNull.Value ? Convert.ToInt32(row["TechnicianID"]) : 0,
                            CommissionPercent = row.Table.Columns.Contains("CommissionPercent") && row["CommissionPercent"] != DBNull.Value ? Convert.ToDecimal(row["CommissionPercent"]) : 0m
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("RepairOrderItemsService.GetOrderServices failed", ex);
            }
            return list;
        }

        public List<OrderSparePartItemDto> GetOrderSpareParts(string repairOrderNumber)
        {
            var list = new List<OrderSparePartItemDto>();
            if (string.IsNullOrWhiteSpace(repairOrderNumber)) return list;
            try
            {
                DataTable dt = _rospRepo.GetSparePartsByRepairOrder(repairOrderNumber.Trim());
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new OrderSparePartItemDto
                        {
                            PartCode = row["PartCode"]?.ToString() ?? "",
                            PartName = row["PartName"]?.ToString() ?? "",
                            Quantity = row["Quantity"] != DBNull.Value ? Convert.ToInt32(row["Quantity"]) : 0,
                            UnitPrice = row["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(row["UnitPrice"]) : 0m
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("RepairOrderItemsService.GetOrderSpareParts failed", ex);
            }
            return list;
        }
    }
}
