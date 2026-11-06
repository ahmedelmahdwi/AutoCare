using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class TechnicianSettlementRepository : ITechnicianSettlementRepository
    {
        private readonly IDbHelper _dbHelper;

        public TechnicianSettlementRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public void InsertSettlement(string repairOrderNumber, int technicianId, decimal serviceTotal, decimal commissionRate, decimal commissionAmount) =>
            InsertSettlement(repairOrderNumber, technicianId, serviceTotal, commissionRate, commissionAmount, null, null);

        public void InsertSettlement(string repairOrderNumber, int technicianId, decimal serviceTotal, decimal commissionRate, decimal commissionAmount, SqlConnection conn, SqlTransaction tran)
        {
            string query = @"
                INSERT INTO TechnicianSettlement (RepairOrderNumber, TechnicianID, ServiceTotal, CommissionRate, CommissionAmount, Status)
                VALUES (@orderNum, @techId, @svcTotal, @pct, @amount, 'Pending')";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@techId", technicianId),
                new SqlParameter("@svcTotal", serviceTotal),
                new SqlParameter("@pct", commissionRate),
                new SqlParameter("@amount", commissionAmount)
            };
            _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
        }

        public DataTable GetSettlements(int? technicianId = null, string status = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            string query = @"
                SELECT ts.SettlementID, ts.RepairOrderNumber, ts.TechnicianID,
                       ISNULL(t.FullName, N'غير محدد') AS TechnicianName,
                       ts.ServiceTotal, ts.CommissionRate, ts.CommissionAmount, ts.Status,
                       ts.SettledDate, ro.CompletionDate
                FROM TechnicianSettlement ts
                LEFT JOIN Technicians t ON ts.TechnicianID = t.TechnicianID
                LEFT JOIN RepairOrders ro ON ts.RepairOrderNumber = ro.RepairOrderNumber
                WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (technicianId.HasValue && technicianId.Value > 0)
            {
                query += " AND ts.TechnicianID = @techId";
                parameters.Add(new SqlParameter("@techId", technicianId.Value));
            }

            if (!string.IsNullOrEmpty(status) && status != "الكل")
            {
                if (status == "مسوى" || status == "Paid")
                    query += " AND ts.Status = 'Paid'";
                else if (status == "معلق" || status == "غير مسوى" || status == "Pending" || status == "معلق (غير مسوى)")
                    query += " AND ts.Status = 'Pending'";
            }

            if (startDate.HasValue)
            {
                query += " AND (ro.CompletionDate >= @startDate OR (ro.CompletionDate IS NULL AND (ts.SettledDate >= @startDate OR ro.EntryDate >= @startDate)))";
                parameters.Add(new SqlParameter("@startDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                query += " AND (ro.CompletionDate <= @endDate OR (ro.CompletionDate IS NULL AND (ts.SettledDate <= @endDate OR ro.EntryDate <= @endDate)))";
                parameters.Add(new SqlParameter("@endDate", endDate.Value));
            }

            query += " ORDER BY COALESCE(ro.CompletionDate, ts.SettledDate, ro.EntryDate) DESC, ts.SettlementID DESC";

            return _dbHelper.ExecuteDataTable(query, parameters.ToArray());
        }

        public void UpdateSettlementStatus(string settlementId, string newStatus) =>
            UpdateSettlementStatus(settlementId, newStatus, null, null);

        public void UpdateSettlementStatus(string settlementId, string newStatus, SqlConnection conn, SqlTransaction tran)
        {
            string query = "UPDATE TechnicianSettlement SET Status = @status, SettledDate = CASE WHEN @status = 'Paid' THEN @now ELSE SettledDate END WHERE SettlementID = @id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", settlementId),
                new SqlParameter("@status", newStatus),
                new SqlParameter("@now", DateTime.Now)
            };
            _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
        }

        public void SettleMultiple(IEnumerable<string> settlementIds)
        {
            if (settlementIds == null) return;
            _dbHelper.ExecuteTransaction((conn, tran) =>
            {
                foreach (var id in settlementIds)
                {
                    UpdateSettlementStatus(id, "Paid", conn, tran);
                }
            });
        }

        public void UpdateSettlementsStatusByRepairOrder(string repairOrderNumber, string newStatus)
        {
            string query = "UPDATE TechnicianSettlement SET Status = @status, SettledDate = CASE WHEN @status = 'Paid' THEN @now ELSE SettledDate END WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@status", newStatus),
                new SqlParameter("@now", DateTime.Now)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void DeleteSettlementsByRepairOrder(string repairOrderNumber)
        {
            string query = "DELETE FROM TechnicianSettlement WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public DataTable GetSettlementById(string settlementId)
        {
            string query = @"
                SELECT ts.SettlementID, ts.RepairOrderNumber, ts.TechnicianID, t.FullName AS TechnicianName,
                       ts.ServiceTotal, ts.CommissionRate, ts.CommissionAmount, ts.Status,
                       ro.CompletionDate, ts.SettledDate
                FROM TechnicianSettlement ts
                INNER JOIN Technicians t ON ts.TechnicianID = t.TechnicianID
                INNER JOIN RepairOrders ro ON ts.RepairOrderNumber = ro.RepairOrderNumber
                WHERE ts.SettlementID = @id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", settlementId)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public int GetSettlementsCount(string repairOrderNumber)
        {
            string query = "SELECT COUNT(1) FROM TechnicianSettlement WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            return _dbHelper.ExecuteScalar<int>(query, parameters);
        }

        public bool HasUnsettledSettlements(string repairOrderNumber)
        {
            string query = "SELECT COUNT(1) FROM TechnicianSettlement WHERE RepairOrderNumber = @orderNum AND Status = 'Pending'";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }
    }
}
