using System;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class RepairOrderServiceRepository : IRepairOrderServiceRepository
    {
        private readonly IDbHelper _dbHelper;

        public RepairOrderServiceRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetServicesByRepairOrder(string repairOrderNumber)
        {
            string query = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderService]') AND type in (N'U'))
                    SELECT ros.ServiceCode, s.ServiceName, ros.UnitPrice, ros.TechnicianID, t.FullName AS TechnicianName, t.CommissionPercent
                    FROM RepairOrderService ros
                    INNER JOIN Services s ON ros.ServiceCode = s.ServiceCode
                    LEFT JOIN Technicians t ON ros.TechnicianID = t.TechnicianID
                    WHERE ros.RepairOrderNumber = @orderNum
                ELSE
                    SELECT ros.ServiceCode, s.ServiceName, ros.UnitPrice, ros.TechnicianID, t.FullName AS TechnicianName, t.CommissionPercent
                    FROM RepairOrderServices ros
                    INNER JOIN Services s ON ros.ServiceCode = s.ServiceCode
                    LEFT JOIN Technicians t ON ros.TechnicianID = t.TechnicianID
                    WHERE ros.RepairOrderNumber = @orderNum";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public void AddServiceToOrder(string repairOrderNumber, string serviceCode, int technicianId, decimal unitPrice)
        {
            string query = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderService]') AND type in (N'U'))
                BEGIN
                    IF EXISTS (SELECT 1 FROM RepairOrderService WHERE RepairOrderNumber = @orderNum AND ServiceCode = @serviceCode AND TechnicianID = @techId)
                    BEGIN
                        UPDATE RepairOrderService 
                        SET UnitPrice = @price 
                        WHERE RepairOrderNumber = @orderNum AND ServiceCode = @serviceCode AND TechnicianID = @techId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO RepairOrderService (RepairOrderNumber, ServiceCode, TechnicianID, UnitPrice) 
                        VALUES (@orderNum, @serviceCode, @techId, @price)
                    END
                END
                ELSE
                BEGIN
                    IF EXISTS (SELECT 1 FROM RepairOrderServices WHERE RepairOrderNumber = @orderNum AND ServiceCode = @serviceCode AND TechnicianID = @techId)
                    BEGIN
                        UPDATE RepairOrderServices 
                        SET UnitPrice = @price 
                        WHERE RepairOrderNumber = @orderNum AND ServiceCode = @serviceCode AND TechnicianID = @techId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO RepairOrderServices (RepairOrderNumber, ServiceCode, TechnicianID, UnitPrice) 
                        VALUES (@orderNum, @serviceCode, @techId, @price)
                    END
                END";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@serviceCode", serviceCode),
                new SqlParameter("@techId", technicianId),
                new SqlParameter("@price", unitPrice)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void RemoveServiceFromOrder(string repairOrderNumber, string serviceCode, int technicianId)
        {
            string query = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderService]') AND type in (N'U'))
                    DELETE FROM RepairOrderService 
                    WHERE RepairOrderNumber = @orderNum AND ServiceCode = @serviceCode AND TechnicianID = @techId
                ELSE
                    DELETE FROM RepairOrderServices 
                    WHERE RepairOrderNumber = @orderNum AND ServiceCode = @serviceCode AND TechnicianID = @techId";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@serviceCode", serviceCode),
                new SqlParameter("@techId", technicianId)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void ClearServicesForOrder(string repairOrderNumber)
        {
            string query = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderService]') AND type in (N'U'))
                    DELETE FROM RepairOrderService WHERE RepairOrderNumber = @orderNum
                ELSE
                    DELETE FROM RepairOrderServices WHERE RepairOrderNumber = @orderNum";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }
    }
}
