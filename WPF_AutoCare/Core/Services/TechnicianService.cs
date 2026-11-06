using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    public class TechnicianService : ITechnicianService
    {
        private readonly ITechnicianRepository _techRepo;
        private readonly ITechnicianSettlementRepository _settlementRepo;
        private readonly IAuditRepository _auditRepo;

        public TechnicianService(ITechnicianRepository techRepo, ITechnicianSettlementRepository settlementRepo, IAuditRepository auditRepo)
        {
            _techRepo = techRepo ?? throw new ArgumentNullException(nameof(techRepo));
            _settlementRepo = settlementRepo ?? throw new ArgumentNullException(nameof(settlementRepo));
            _auditRepo = auditRepo;
        }

        public DataTable GetActiveTechnicians()
        {
            return _techRepo.GetActiveTechnicians();
        }

        public DataTable GetAllTechnicians(string searchTerm = "", string specializationFilter = "")
        {
            return _techRepo.GetAllTechnicians(searchTerm, specializationFilter);
        }

        public OperationResult AddTechnician(int technicianId, string fullName, string specialization, decimal commissionPercent, bool isActive, int branchId = 1)
        {
            if (technicianId <= 0)
                technicianId = _techRepo.GetNextTechnicianId();

            if (string.IsNullOrWhiteSpace(fullName))
                return OperationResult.Fail("اسم الفني مطلوب.");
            if (commissionPercent < 0 || commissionPercent > 100)
                return OperationResult.Fail("نسبة العمولة يجب أن تكون بين 0% و 100%.");

            if (_techRepo.TechnicianExists(technicianId))
                return OperationResult.Fail($"الفني بالرقم ({technicianId}) موجود مسبقاً.");

            try
            {
                _techRepo.AddTechnician(technicianId, fullName.Trim(), specialization?.Trim() ?? "", commissionPercent, isActive, branchId);
                _auditRepo?.Log(UserSession.Username ?? "System", "إضافة فني", "Technicians", technicianId.ToString(), $"الاسم: {fullName}");
                return OperationResult.Ok("تم تسجيل الفني بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء إضافة الفني: " + ex.Message);
            }
        }

        public OperationResult UpdateTechnician(int technicianId, string fullName, string specialization, decimal commissionPercent, bool isActive, int branchId = 1)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return OperationResult.Fail("اسم الفني مطلوب.");
            if (commissionPercent < 0 || commissionPercent > 100)
                return OperationResult.Fail("نسبة العمولة يجب أن تكون بين 0% و 100%.");

            try
            {
                _techRepo.UpdateTechnician(technicianId, fullName.Trim(), specialization?.Trim() ?? "", commissionPercent, isActive, branchId);
                _auditRepo?.Log(UserSession.Username ?? "System", "تعديل فني", "Technicians", technicianId.ToString(), $"الاسم: {fullName}");
                return OperationResult.Ok("تم تحديث بيانات الفني بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تعديل بيانات الفني: " + ex.Message);
            }
        }

        public OperationResult DeleteTechnician(int technicianId)
        {
            if (_techRepo.IsTechnicianUsedInOrders(technicianId))
            {
                return OperationResult.Fail("لا يمكن حذف الفني لوجود أوامر صيانة أو تسويات عمولات مسجلة باسمه. يمكنك إلغاء تفعيله بدلاً من ذلك.");
            }

            try
            {
                _techRepo.DeleteTechnician(technicianId);
                _auditRepo?.Log(UserSession.Username ?? "System", "حذف فني", "Technicians", technicianId.ToString(), "تم حذف الفني");
                return OperationResult.Ok("تم حذف الفني بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("تعذر حذف الفني: " + ex.Message);
            }
        }

        public DataTable GetSettlements(int? technicianId = null, string status = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            return _settlementRepo.GetSettlements(technicianId, status, startDate, endDate);
        }

        public List<SettlementRecordDto> GetSettlementRecords(int? technicianId = null, string status = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            var list = new List<SettlementRecordDto>();
            try
            {
                DataTable dt = _settlementRepo.GetSettlements(technicianId, status, startDate, endDate);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string completionDate = "";
                        if (row.Table.Columns.Contains("CompletionDate") && row["CompletionDate"] != DBNull.Value)
                            completionDate = Convert.ToDateTime(row["CompletionDate"]).ToString("yyyy-MM-dd");

                        string dbStatus = row.Table.Columns.Contains("Status") ? (row["Status"]?.ToString() ?? "") : "";
                        string displayStatus = (dbStatus == "Paid" || dbStatus == "مسوى") ? "مسوى" : "معلق";

                        list.Add(new SettlementRecordDto
                        {
                            TechId = row.Table.Columns.Contains("TechnicianID") ? (row["TechnicianID"]?.ToString() ?? "") : "",
                            TechName = row.Table.Columns.Contains("TechnicianName") ? (row["TechnicianName"]?.ToString() ?? "") : "",
                            RoNumber = row.Table.Columns.Contains("RepairOrderNumber") ? (row["RepairOrderNumber"]?.ToString() ?? "") : "",
                            CompletionDate = completionDate,
                            ServiceTotal = (row.Table.Columns.Contains("ServiceTotal") && row["ServiceTotal"] != DBNull.Value) ? Convert.ToDecimal(row["ServiceTotal"]).ToString("F2") : "0.00",
                            CommissionRate = (row.Table.Columns.Contains("CommissionRate") && row["CommissionRate"] != DBNull.Value) ? Convert.ToDecimal(row["CommissionRate"]).ToString("F2") + "%" : "0%",
                            CommissionAmount = (row.Table.Columns.Contains("CommissionAmount") && row["CommissionAmount"] != DBNull.Value) ? Convert.ToDecimal(row["CommissionAmount"]).ToString("F2") : "0.00",
                            Status = displayStatus,
                            RawStatus = dbStatus,
                            SettlementId = row.Table.Columns.Contains("SettlementID") ? (row["SettlementID"]?.ToString() ?? "") : ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("TechnicianService.GetSettlementRecords failed", ex);
            }
            return list;
        }

        public List<TechnicianSummaryDto> GetActiveTechniciansList()
        {
            try
            {
                return _techRepo.GetActiveTechniciansList();
            }
            catch (Exception ex)
            {
                AppLogger.Error("TechnicianService.GetActiveTechniciansList failed", ex);
                return new List<TechnicianSummaryDto>();
            }
        }

        public OperationResult SettleCommissions(IEnumerable<string> settlementIds)
        {
            if (settlementIds == null)
                return OperationResult.Fail("لم يتم تحديد أي تسويات للصرف.");

            var idList = new List<string>(settlementIds);
            if (idList.Count == 0)
                return OperationResult.Fail("يرجى تحديد تسوية واحدة على الأقل للصرف.");

            try
            {
                _settlementRepo.SettleMultiple(idList);
                _auditRepo?.Log(UserSession.Username ?? "System", "صرف عمولات", "TechnicianSettlements", "Multiple", $"تم تسوية وصرف {idList.Count} مستحقات عمولة");
                return OperationResult.Ok($"تم صرف وتسوية {idList.Count} عمولة بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء ترحيل التسويات: " + ex.Message);
            }
        }

        public List<string> GetSpecializations()
        {
            try
            {
                return _techRepo.GetSpecializationsList();
            }
            catch (Exception ex)
            {
                AppLogger.Error("TechnicianService.GetSpecializations failed", ex);
                return new List<string>();
            }
        }

        public List<TechnicianRowDto> GetTechniciansList(string searchTerm = "", string specializationFilter = "")
        {
            try
            {
                return _techRepo.GetAllTechniciansList(searchTerm, specializationFilter);
            }
            catch (Exception ex)
            {
                AppLogger.Error("TechnicianService.GetTechniciansList failed", ex);
                return new List<TechnicianRowDto>();
            }
        }

        public TechnicianRowDto GetTechnicianById(int technicianId)
        {
            if (technicianId <= 0) return null;
            try
            {
                return _techRepo.GetTechnicianDtoById(technicianId);
            }
            catch (Exception ex)
            {
                AppLogger.Error("TechnicianService.GetTechnicianById failed", ex);
                return null;
            }
        }

        public int GetNextTechnicianId()
        {
            try
            {
                return _techRepo.GetNextTechnicianId();
            }
            catch (Exception ex)
            {
                AppLogger.Error("TechnicianService.GetNextTechnicianId failed", ex);
                return 1;
            }
        }
    }
}

