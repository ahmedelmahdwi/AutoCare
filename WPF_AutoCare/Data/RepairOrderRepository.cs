using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class RepairOrderRepository : IRepairOrderRepository
    {
        private readonly IDbHelper _dbHelper;

        public RepairOrderRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetAllRepairOrdersWithDetails(string searchTerm = "", string statusFilter = "الكل")
        {
            string query = @"
                SELECT ro.RepairOrderNumber, ro.PlateNumber, v.VIN, v.CarName, v.CarModel, 
                       c.FullName AS CustomerName, c.PhoneNumber AS CustomerPhone, 
                       ro.EntryDate, ro.Complaint, ro.Status AS Status, ro.CompletionDate
                FROM RepairOrders ro
                INNER JOIN Vehicles v ON ro.PlateNumber = v.PlateNumber
                INNER JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                WHERE (c.FullName LIKE @search OR c.PhoneNumber LIKE @search OR v.PlateNumber LIKE @search OR ro.RepairOrderNumber LIKE @search OR @search = '')";

            string dbStatus = null;
            if (statusFilter == "معلق") dbStatus = "Pending";
            else if (statusFilter == "قيد التنفيذ") dbStatus = "InProgress";
            else if (statusFilter == "مكتمل") dbStatus = "Completed";
            else if (statusFilter == "ملغى") dbStatus = "Canceled";

            List<SqlParameter> listParams = new List<SqlParameter>();
            string searchVal = string.IsNullOrEmpty(searchTerm) ? "" : "%" + searchTerm + "%";
            listParams.Add(new SqlParameter("@search", searchVal));

            if (!string.IsNullOrEmpty(dbStatus))
            {
                query += " AND ro.Status = @status";
                listParams.Add(new SqlParameter("@status", dbStatus));
            }
            else if (statusFilter == "الكل")
            {
                query += " AND ro.Status IN ('Pending', 'InProgress')";
            }

            int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;
            if (currentBranch.HasValue)
            {
                query += " AND (ro.BranchId = @branchId OR ro.BranchId IS NULL)";
                listParams.Add(new SqlParameter("@branchId", currentBranch.Value));
            }

            query += " ORDER BY ro.EntryDate DESC";

            return _dbHelper.ExecuteDataTable(query, listParams.ToArray());
        }

        public DataTable GetRepairOrderByNumber(string repairOrderNumber)
        {
            string query = @"
                SELECT RepairOrderNumber, PlateNumber, ReceptionistUsername, Complaint, Status, 
                       EntryDate, CompletionDate, ExpectedCompletionDate,
                       Mileage, FuelLevel, InspectionNotes 
                FROM RepairOrders WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public DataTable GetLatestRepairOrderByPlate(string plateNumber)
        {
            string query = "SELECT TOP 1 RepairOrderNumber, PlateNumber, ReceptionistUsername, Complaint, Status, EntryDate, CompletionDate FROM RepairOrders WHERE PlateNumber = @plate ORDER BY EntryDate DESC";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public bool HasActiveRepairOrder(string plateNumber)
        {
            string query = "SELECT COUNT(1) FROM RepairOrders WHERE PlateNumber = @plate AND Status IN ('Pending', 'InProgress')";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public string GenerateNewRepairOrderNumber()
        {
            // Phase 5 fix: use SQL SEQUENCE for atomic, race-condition-free ID generation.
            // The SEQUENCE (seq_RepairOrderNumber) must be created first by running
            // SQL_Scripts/Phase5_IDSequences.sql in SSMS.
            // Falls back to MAX()+1 if the sequence does not exist yet.
            try
            {
                string seqQuery = @"
                    DECLARE @next INT = NEXT VALUE FOR dbo.seq_RepairOrderNumber;
                    SELECT 'OR-' + FORMAT(@next, 'D3')";
                string result = _dbHelper.ExecuteScalar<string>(seqQuery, null);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }
            catch
            {
                // Sequence not yet created — fall back to legacy MAX() method
            }

            // Legacy fallback: MAX() + 1 (single-user safe)
            string fallbackQuery = "SELECT MAX(RepairOrderNumber) FROM RepairOrders";
            string maxOrder = _dbHelper.ExecuteScalar<string>(fallbackQuery, null);
            if (string.IsNullOrEmpty(maxOrder))
                return "OR-001";

            string numPart = "";
            foreach (char c in maxOrder)
            {
                if (char.IsDigit(c)) numPart += c;
            }
            if (int.TryParse(numPart, out int number))
            {
                number++;
                return "OR-" + number.ToString("D3");
            }
            return "OR-001";
        }

        public int GetOrdersCountByStatus(string status)
        {
            int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;
            string query = "SELECT COUNT(1) FROM RepairOrders WHERE Status = @status";
            var listParams = new List<SqlParameter> { new SqlParameter("@status", status) };

            if (currentBranch.HasValue)
            {
                query += " AND (BranchId = @branchId OR BranchId IS NULL)";
                listParams.Add(new SqlParameter("@branchId", currentBranch.Value));
            }

            try
            {
                return _dbHelper.ExecuteScalar<int>(query, listParams.ToArray());
            }
            catch
            {
                return 0;
            }
        }

        public void AddRepairOrder(string repairOrderNumber, string plateNumber, string receptionistUsername, string complaint, string status, DateTime? expectedDate = null, int? mileage = null, string fuelLevel = null, string inspectionNotes = null) =>
            AddRepairOrder(repairOrderNumber, plateNumber, receptionistUsername, complaint, status, expectedDate, mileage, fuelLevel, inspectionNotes, null, null);

        public void AddRepairOrder(string repairOrderNumber, string plateNumber, string receptionistUsername, string complaint, string status, DateTime? expectedDate, SqlConnection conn, SqlTransaction tran) =>
            AddRepairOrder(repairOrderNumber, plateNumber, receptionistUsername, complaint, status, expectedDate, null, null, null, conn, tran);

        public void AddRepairOrder(string repairOrderNumber, string plateNumber, string receptionistUsername, string complaint, string status, DateTime? expectedDate, int? mileage, string fuelLevel, string inspectionNotes, SqlConnection conn, SqlTransaction tran)
        {
            int branchId = AppServiceLocator.BranchContext?.CurrentBranchId ?? 1;
            string query = @"
                INSERT INTO RepairOrders (RepairOrderNumber, PlateNumber, ReceptionistUsername, Complaint, Status, EntryDate, ExpectedCompletionDate, Mileage, FuelLevel, InspectionNotes, BranchId) 
                VALUES (@orderNum, @plate, @receptionist, @complaint, @status, @entryDate, @expectedDate, @mileage, @fuelLevel, @inspectionNotes, @branchId)";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@plate", plateNumber),
                new SqlParameter("@receptionist", string.IsNullOrEmpty(receptionistUsername) ? (object)DBNull.Value : receptionistUsername),
                new SqlParameter("@complaint", complaint),
                new SqlParameter("@status", status),
                new SqlParameter("@entryDate", DateTime.Now),
                new SqlParameter("@expectedDate", expectedDate.HasValue ? (object)expectedDate.Value : DBNull.Value),
                new SqlParameter("@mileage", mileage.HasValue ? (object)mileage.Value : DBNull.Value),
                new SqlParameter("@fuelLevel", string.IsNullOrEmpty(fuelLevel) ? (object)DBNull.Value : fuelLevel),
                new SqlParameter("@inspectionNotes", string.IsNullOrEmpty(inspectionNotes) ? (object)DBNull.Value : inspectionNotes),
                new SqlParameter("@branchId", branchId)
            };
            int res = _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
            if (res < 0)
            {
                throw new Exception("فشل في إدخال أمر الإصلاح في جدول RepairOrders بقاعدة البيانات.");
            }
        }

        public void UpdateOrderStatus(string repairOrderNumber, string status, DateTime? completionDate = null)
        {
            string query = "UPDATE RepairOrders SET Status = @status, CompletionDate = @completionDate WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@status", status),
                new SqlParameter("@completionDate", completionDate.HasValue ? (object)completionDate.Value : DBNull.Value)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
            AppServiceLocator.AuditRepo?.Log("تغيير حالة أمر الإصلاح", "RepairOrders", repairOrderNumber, "تغيير حالة أمر الإصلاح إلى: " + status);
        }

        public bool UpdateOrderStatusWithConcurrency(string repairOrderNumber, string status, byte[] rowVersion, DateTime? completionDate = null)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber) || rowVersion == null) return false;
            string query = @"
                UPDATE RepairOrders 
                SET Status = @status, CompletionDate = @completionDate 
                WHERE RepairOrderNumber = @orderNum AND RowVersion = @rowVersion";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber.Trim()),
                new SqlParameter("@status", status),
                new SqlParameter("@completionDate", completionDate.HasValue ? (object)completionDate.Value : DBNull.Value),
                new SqlParameter("@rowVersion", rowVersion)
            };
            int affected = _dbHelper.ExecuteNonQuery(query, parameters);
            if (affected > 0)
            {
                AppServiceLocator.AuditRepo?.Log("تغيير حالة أمر الإصلاح بالتزامن", "RepairOrders", repairOrderNumber, "تغيير حالة أمر الإصلاح إلى: " + status);
                return true;
            }
            return false;
        }

        public byte[] GetRepairOrderRowVersion(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber)) return null;
            string query = "SELECT RowVersion FROM RepairOrders WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber.Trim())
            };
            return _dbHelper.ExecuteScalar<byte[]>(query, parameters);
        }

        public void UpdateRepairOrder(string repairOrderNumber, string complaint, string status, DateTime? completionDate = null, int? mileage = null, string fuelLevel = null, string inspectionNotes = null) =>
            UpdateRepairOrder(repairOrderNumber, complaint, status, completionDate, mileage, fuelLevel, inspectionNotes, null, null);

        public void UpdateRepairOrder(string repairOrderNumber, string complaint, string status, DateTime? completionDate, SqlConnection conn, SqlTransaction tran) =>
            UpdateRepairOrder(repairOrderNumber, complaint, status, completionDate, null, null, null, conn, tran);

        public void UpdateRepairOrder(string repairOrderNumber, string complaint, string status, DateTime? completionDate, int? mileage, string fuelLevel, string inspectionNotes, SqlConnection conn, SqlTransaction tran)
        {
            string query = @"
                UPDATE RepairOrders 
                SET Complaint = @complaint, Status = @status, CompletionDate = @completionDate,
                    Mileage = @mileage, FuelLevel = @fuelLevel, InspectionNotes = @inspectionNotes 
                WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@complaint", complaint),
                new SqlParameter("@status", status),
                new SqlParameter("@completionDate", completionDate.HasValue ? (object)completionDate.Value : DBNull.Value),
                new SqlParameter("@mileage", mileage.HasValue ? (object)mileage.Value : DBNull.Value),
                new SqlParameter("@fuelLevel", string.IsNullOrEmpty(fuelLevel) ? (object)DBNull.Value : fuelLevel),
                new SqlParameter("@inspectionNotes", string.IsNullOrEmpty(inspectionNotes) ? (object)DBNull.Value : inspectionNotes)
            };
            _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
        }

        public void DeleteRepairOrder(string repairOrderNumber)
        {
            string checkInvoiceQuery = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Invoices]') AND type in (N'U'))
                    SELECT COUNT(1) FROM Invoices WHERE RepairOrderNumber = @orderNum
                ELSE IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Invoice]') AND type in (N'U'))
                    SELECT COUNT(1) FROM Invoice WHERE RepairOrderNumber = @orderNum
                ELSE
                    SELECT 0";

            string checkServicesQuery = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderService]') AND type in (N'U'))
                    SELECT COUNT(1) FROM RepairOrderService WHERE RepairOrderNumber = @orderNum
                ELSE IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderServices]') AND type in (N'U'))
                    SELECT COUNT(1) FROM RepairOrderServices WHERE RepairOrderNumber = @orderNum
                ELSE
                    SELECT 0";

            string checkPartsQuery = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSparePart]') AND type in (N'U'))
                    SELECT COUNT(1) FROM RepairOrderSparePart WHERE RepairOrderNumber = @orderNum
                ELSE IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSpareParts]') AND type in (N'U'))
                    SELECT COUNT(1) FROM RepairOrderSpareParts WHERE RepairOrderNumber = @orderNum
                ELSE
                    SELECT 0";

            SqlParameter[] checkParams = new SqlParameter[] { new SqlParameter("@orderNum", repairOrderNumber) };
            int invoicesCount = _dbHelper.ExecuteScalar<int>(checkInvoiceQuery, checkParams);
            if (invoicesCount > 0)
            {
                throw new InvalidOperationException("لا يمكن حذف أمر الإصلاح لوجود فاتورة مرتبطة به.");
            }

            SqlParameter[] checkParams2 = new SqlParameter[] { new SqlParameter("@orderNum", repairOrderNumber) };
            int servicesCount = _dbHelper.ExecuteScalar<int>(checkServicesQuery, checkParams2);
            if (servicesCount > 0)
            {
                throw new InvalidOperationException("لا يمكن حذف أمر الإصلاح لوجود خدمات مرتبطة به.");
            }

            SqlParameter[] checkParams3 = new SqlParameter[] { new SqlParameter("@orderNum", repairOrderNumber) };
            int partsCount = _dbHelper.ExecuteScalar<int>(checkPartsQuery, checkParams3);
            if (partsCount > 0)
            {
                throw new InvalidOperationException("لا يمكن حذف أمر الإصلاح لوجود قطع غيار مرتبطة به.");
            }

            string deleteQuery = "DELETE FROM RepairOrders WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] deleteParams = new SqlParameter[] { new SqlParameter("@orderNum", repairOrderNumber) };
            _dbHelper.ExecuteNonQuery(deleteQuery, deleteParams);
            AppServiceLocator.AuditRepo?.Log("حذف كرت صيانة", "RepairOrders", repairOrderNumber, "حذف أمر الإصلاح نهائياً من النظام");
        }

        public bool HasServicesOrParts(string repairOrderNumber)
        {
            string checkServicesQuery = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderService]') AND type in (N'U'))
                    SELECT COUNT(1) FROM RepairOrderService WHERE RepairOrderNumber = @orderNum
                ELSE IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderServices]') AND type in (N'U'))
                    SELECT COUNT(1) FROM RepairOrderServices WHERE RepairOrderNumber = @orderNum
                ELSE
                    SELECT 0";

            string checkPartsQuery = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSparePart]') AND type in (N'U'))
                    SELECT COUNT(1) FROM RepairOrderSparePart WHERE RepairOrderNumber = @orderNum
                ELSE IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSpareParts]') AND type in (N'U'))
                    SELECT COUNT(1) FROM RepairOrderSpareParts WHERE RepairOrderNumber = @orderNum
                ELSE
                    SELECT 0";

            SqlParameter[] p1 = new SqlParameter[] { new SqlParameter("@orderNum", repairOrderNumber) };
            int sc = _dbHelper.ExecuteScalar<int>(checkServicesQuery, p1);

            SqlParameter[] p2 = new SqlParameter[] { new SqlParameter("@orderNum", repairOrderNumber) };
            int pc = _dbHelper.ExecuteScalar<int>(checkPartsQuery, p2);

            return (sc > 0 || pc > 0);
        }

        public DataTable GetVehicleServiceHistory(string plateNumber)
        {
            string query = @"
                SELECT ro.RepairOrderNumber, ro.EntryDate, ro.CompletionDate, ro.Status, 
                       ro.Mileage, ro.FuelLevel, ro.Complaint, ro.InspectionNotes,
                       inv.InvoiceNumber, inv.PaymentMethod, inv.PaidAmount,
                       ISNULL(srv.TotalServices, 0) AS TotalServices,
                       ISNULL(prt.TotalParts, 0) AS TotalParts,
                       ISNULL(srv.TotalServices, 0) + ISNULL(prt.TotalParts, 0) - ISNULL(inv.DiscountAmount, 0) AS GrandTotal
                FROM RepairOrders ro
                LEFT JOIN Invoices inv ON ro.RepairOrderNumber = inv.RepairOrderNumber
                LEFT JOIN (
                    SELECT RepairOrderNumber, SUM(UnitPrice) AS TotalServices
                    FROM RepairOrderService
                    GROUP BY RepairOrderNumber
                ) srv ON ro.RepairOrderNumber = srv.RepairOrderNumber
                LEFT JOIN (
                    SELECT RepairOrderNumber, SUM(Quantity * UnitPrice) AS TotalParts
                    FROM RepairOrderSparePart
                    GROUP BY RepairOrderNumber
                ) prt ON ro.RepairOrderNumber = prt.RepairOrderNumber
                WHERE ro.PlateNumber = @plate
                ORDER BY ro.EntryDate DESC";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }
    }
}
