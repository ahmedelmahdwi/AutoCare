using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Core.Services
{
    /// <summary>
    /// Specialized Domain Service for querying vehicle service history and checking active repair orders.
    /// Follows Single Responsibility Principle (Clean Architecture).
    /// </summary>
    public class VehicleHistoryService : IVehicleHistoryService
    {
        private readonly IRepairOrderRepository _roRepo;

        public VehicleHistoryService(IRepairOrderRepository roRepo)
        {
            _roRepo = roRepo ?? throw new ArgumentNullException(nameof(roRepo));
        }

        public bool HasActiveRepairOrder(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return false;
            return _roRepo.HasActiveRepairOrder(plateNumber.Trim());
        }

        public DataTable GetVehicleServiceHistory(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return new DataTable();
            return _roRepo.GetVehicleServiceHistory(plateNumber.Trim());
        }

        public List<VehicleServiceHistoryRowDto> GetVehicleServiceHistoryList(string plateNumber)
        {
            var list = new List<VehicleServiceHistoryRowDto>();
            if (string.IsNullOrWhiteSpace(plateNumber)) return list;

            try
            {
                DataTable dt = _roRepo.GetVehicleServiceHistory(plateNumber.Trim());
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        string status = r["Status"]?.ToString() ?? "";
                        string arabicStatus = status == "Completed" ? "مكتمل"
                                            : status == "InProgress" ? "قيد التنفيذ"
                                            : status == "Canceled" ? "ملغى" : "معلق";

                        string entryDate = "";
                        if (r["EntryDate"] != DBNull.Value && r["EntryDate"] != null)
                            entryDate = Convert.ToDateTime(r["EntryDate"]).ToString("yyyy-MM-dd");

                        string mileageStr = "-";
                        if (r["Mileage"] != DBNull.Value && r["Mileage"] != null)
                            mileageStr = Convert.ToInt32(r["Mileage"]).ToString("N0") + " كم";

                        decimal grandTotal = 0;
                        if (r["GrandTotal"] != DBNull.Value && r["GrandTotal"] != null)
                            grandTotal = Convert.ToDecimal(r["GrandTotal"]);

                        string invNum = r["InvoiceNumber"]?.ToString();
                        if (string.IsNullOrEmpty(invNum)) invNum = "-";

                        string payMethod = r["PaymentMethod"]?.ToString();
                        if (string.IsNullOrEmpty(payMethod)) payMethod = "-";

                        list.Add(new VehicleServiceHistoryRowDto
                        {
                            RoNumber = r["RepairOrderNumber"]?.ToString() ?? "",
                            EntryDate = entryDate,
                            Mileage = mileageStr,
                            Status = arabicStatus,
                            Complaint = r["Complaint"]?.ToString() ?? "",
                            InvoiceNumber = invNum,
                            PaymentMethod = payMethod,
                            GrandTotal = grandTotal > 0 ? grandTotal.ToString("N2") + " د.ل" : "-"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("VehicleHistoryService.GetVehicleServiceHistoryList failed", ex);
            }
            return list;
        }
    }
}
