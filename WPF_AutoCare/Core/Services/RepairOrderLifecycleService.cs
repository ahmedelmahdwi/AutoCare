using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Enums;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    /// <summary>
    /// Specialized Domain Service for managing Repair Order lifecycle states and transitions.
    /// Enforces business invariants (Pending -> InProgress -> Completed).
    /// </summary>
    public class RepairOrderLifecycleService : IRepairOrderLifecycleService
    {
        private readonly IRepairOrderRepository _roRepo;
        private readonly IRepairOrderServiceRepository _rosRepo;
        private readonly IRepairOrderSparePartRepository _rospRepo;
        private readonly IAuditRepository _auditRepo;

        public RepairOrderLifecycleService(
            IRepairOrderRepository roRepo,
            IRepairOrderServiceRepository rosRepo,
            IRepairOrderSparePartRepository rospRepo,
            IAuditRepository auditRepo)
        {
            _roRepo = roRepo ?? throw new ArgumentNullException(nameof(roRepo));
            _rosRepo = rosRepo ?? throw new ArgumentNullException(nameof(rosRepo));
            _rospRepo = rospRepo ?? throw new ArgumentNullException(nameof(rospRepo));
            _auditRepo = auditRepo;
        }

        public OperationResult UpdateOrderStatus(string repairOrderNumber, string status)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");

            try
            {
                DateTime? completionDate = (status == "Completed" || status == "مكتمل") ? (DateTime?)DateTime.Now : null;
                _roRepo.UpdateOrderStatus(repairOrderNumber, status, completionDate);
                _auditRepo?.Log("تحديث حالة أمر الإصلاح", "RepairOrders", repairOrderNumber,
                    $"تحديث حالة الأمر إلى {status}", UserSession.Username);
                return OperationResult.Ok("تم تحديث حالة الأمر بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل في تحديث حالة الأمر: " + ex.Message);
            }
        }

        public OperationResult ConfirmOrderServices(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");

            try
            {
                DataTable dtServices = _rosRepo.GetServicesByRepairOrder(repairOrderNumber);
                if (dtServices == null || dtServices.Rows.Count == 0)
                {
                    return OperationResult.Fail("الرجاء اختيار خدمة واحدة على الأقل قبل التأكيد.");
                }

                DataTable dtRo = _roRepo.GetRepairOrderByNumber(repairOrderNumber);
                if (dtRo != null && dtRo.Rows.Count > 0)
                {
                    string currentStatus = dtRo.Rows[0]["Status"]?.ToString() ?? "Pending";
                    if (currentStatus == "Pending" || currentStatus == "معلق")
                    {
                        _roRepo.UpdateRepairOrder(
                            repairOrderNumber,
                            dtRo.Rows[0]["Complaint"]?.ToString() ?? "",
                            "InProgress");
                    }
                }

                _auditRepo?.Log("تأكيد خدمات أمر الإصلاح", "RepairOrders", repairOrderNumber,
                    "تأكيد إضافة الخدمات ونقل الحالة إلى قيد التنفيذ", UserSession.Username);

                return OperationResult.Ok("تم تأكيد إضافة الخدمات وتحديث حالة السيارة إلى قيد التنفيذ بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تأكيد الخدمات: " + ex.Message);
            }
        }

        public OperationResult StartOrderProgress(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");

            try
            {
                DataTable dtServices = _rosRepo.GetServicesByRepairOrder(repairOrderNumber);
                if (dtServices == null || dtServices.Rows.Count == 0)
                {
                    return OperationResult.Fail("لا يمكن تغيير الحالة، يجب إضافة خدمة واحدة على الأقل لأمر الإصلاح.");
                }

                DataTable dtRo = _roRepo.GetRepairOrderByNumber(repairOrderNumber);
                if (dtRo != null && dtRo.Rows.Count > 0)
                {
                    string currentStatus = dtRo.Rows[0]["Status"]?.ToString() ?? "Pending";
                    if (currentStatus == "Pending" || currentStatus == "معلق")
                    {
                        _roRepo.UpdateRepairOrder(
                            repairOrderNumber,
                            dtRo.Rows[0]["Complaint"]?.ToString() ?? "",
                            RepairOrderStatus.InProgress.ToDbString());
                    }
                }

                _auditRepo?.Log("بدء تنفيذ أمر الإصلاح", "RepairOrders", repairOrderNumber,
                    "نقل حالة أمر الإصلاح إلى قيد التنفيذ", UserSession.Username);

                return OperationResult.Ok("تم حفظ البيانات وتحديث حالة السيارة بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء حفظ البيانات: " + ex.Message);
            }
        }

        public OperationResult RevertToPending(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");

            try
            {
                DataTable services = _rosRepo.GetServicesByRepairOrder(repairOrderNumber);
                DataTable spareParts = _rospRepo.GetSparePartsByRepairOrder(repairOrderNumber);

                if ((services != null && services.Rows.Count > 0) || (spareParts != null && spareParts.Rows.Count > 0))
                {
                    return OperationResult.Fail("لا يمكن الرجوع للحالة المعلقة لأن هناك خدمات أو قطع غيار مرتبطة. يرجى حذفها أولاً.");
                }

                _roRepo.UpdateOrderStatus(repairOrderNumber, RepairOrderStatus.Pending.ToDbString(), null);
                _auditRepo?.Log("إرجاع أمر الإصلاح للمعلق", "RepairOrders", repairOrderNumber,
                    "تم إرجاع حالة أمر الإصلاح إلى معلق", UserSession.Username);

                return OperationResult.Ok("تم الرجوع إلى الحالة المعلقة بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء الرجوع للحالة المعلقة: " + ex.Message);
            }
        }

        public OperationResult CompleteRepairOrder(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير صالح.");

            try
            {
                DataTable dtRo = _roRepo.GetRepairOrderByNumber(repairOrderNumber.Trim());
                if (dtRo == null || dtRo.Rows.Count == 0)
                    return OperationResult.Fail("لم يتم العثور على أمر الصيانة المحدد في قاعدة البيانات.");

                string currentStatus = dtRo.Rows[0]["Status"]?.ToString();
                if (currentStatus == RepairOrderStatus.Completed.ToDbString() || currentStatus == "مكتمل")
                    return OperationResult.Fail("أمر الإصلاح المحدد مكتمل بالفعل مسبقاً ولا يمكن إكماله مجدداً.");

                if (!_roRepo.HasServicesOrParts(repairOrderNumber.Trim()))
                    return OperationResult.Fail("لا يمكن إكمال الصيانة دون إضافة خدمات أو قطع غيار.");

                string complaint = dtRo.Rows[0]["Complaint"]?.ToString() ?? "";
                _roRepo.UpdateRepairOrder(repairOrderNumber.Trim(), complaint, RepairOrderStatus.Completed.ToDbString(), DateTime.Now);
                _auditRepo?.Log("إكمال أمر صيانة", "RepairOrders", repairOrderNumber.Trim(), $"إكمال أمر الصيانة {repairOrderNumber}", UserSession.Username ?? "System");
                return OperationResult.Ok("تم إكمال أمر الإصلاح بنجاح.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("RepairOrderLifecycleService.CompleteRepairOrder failed", ex);
                return OperationResult.Fail("حدث خطأ أثناء إكمال أمر الإصلاح: " + ex.Message);
            }
        }

        public OperationResult CancelRepairOrder(string repairOrderNumber, string reason = "")
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");

            try
            {
                string roNum = repairOrderNumber.Trim();
                DataTable dtRo = _roRepo.GetRepairOrderByNumber(roNum);
                if (dtRo == null || dtRo.Rows.Count == 0)
                    return OperationResult.Fail("لم يتم العثور على أمر الإصلاح المحدد.");

                string currentStatus = dtRo.Rows[0]["Status"]?.ToString() ?? "";
                if (currentStatus == "Canceled")
                    return OperationResult.Fail("أمر الإصلاح ملغى بالفعل.");

                bool hasInvoice = AppServiceLocator.InvoiceRepo?.InvoiceExistsByRepairOrder(roNum) ?? false;
                if (hasInvoice)
                {
                    return OperationResult.Fail("لا يمكن إلغاء أمر الإصلاح لوجود فاتورة مرتبطة به. يجب تسوية أو إلغاء الفاتورة أولاً.");
                }

                _roRepo.UpdateOrderStatus(roNum, "Canceled");
                string user = UserSession.Username ?? "System";
                string details = string.IsNullOrWhiteSpace(reason) ? "تم إلغاء أمر الإصلاح" : $"تم إلغاء أمر الإصلاح: {reason}";
                _auditRepo?.Log("إلغاء أمر إصلاح", "RepairOrders", roNum, details, user);

                return OperationResult.Ok($"تم إلغاء أمر الإصلاح ({roNum}) بنجاح وإخراجه من قائمة الورشة النشطة.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("RepairOrderLifecycleService.CancelRepairOrder failed", ex);
                return OperationResult.Fail("حدث خطأ أثناء إلغاء أمر الإصلاح: " + ex.Message);
            }
        }

        public string GetRepairOrderStatus(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber)) return "";
            try
            {
                DataTable dtRo = _roRepo.GetRepairOrderByNumber(repairOrderNumber.Trim());
                if (dtRo != null && dtRo.Rows.Count > 0)
                    return dtRo.Rows[0]["Status"]?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                AppLogger.Error("RepairOrderLifecycleService.GetRepairOrderStatus failed", ex);
            }
            return "";
        }

        public List<CarStatusRowDto> GetCarStatusList(string search = "", string statusFilter = "الكل")
        {
            var list = new List<CarStatusRowDto>();
            try
            {
                DataTable dt = _roRepo.GetAllRepairOrdersWithDetails(search, statusFilter);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string rawStatus = row["Status"]?.ToString() ?? "";
                        string arabicStatus = rawStatus;
                        if (rawStatus == "Pending") arabicStatus = "معلق";
                        else if (rawStatus == "InProgress") arabicStatus = "قيد التنفيذ";
                        else if (rawStatus == "Completed") arabicStatus = "مكتمل";
                        else if (rawStatus == "Canceled") arabicStatus = "ملغى";

                        string entryDate = "";
                        if (row["EntryDate"] != DBNull.Value && row["EntryDate"] != null)
                            entryDate = Convert.ToDateTime(row["EntryDate"]).ToString("yyyy-MM-dd HH:mm");

                        list.Add(new CarStatusRowDto
                        {
                            PlateNumber = row["PlateNumber"]?.ToString() ?? "",
                            VIN = row["VIN"]?.ToString() ?? "",
                            CarName = row["CarName"]?.ToString() ?? "",
                            CarModel = row["CarModel"]?.ToString() ?? "",
                            EntryDate = entryDate,
                            Status = arabicStatus,
                            RawStatus = rawStatus,
                            RoNumber = row["RepairOrderNumber"]?.ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("RepairOrderLifecycleService.GetCarStatusList failed", ex);
            }
            return list;
        }
    }
}
