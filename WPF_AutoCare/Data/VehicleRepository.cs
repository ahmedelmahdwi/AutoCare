using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Core.Enums;

namespace C__project_Auto_Care.Data
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly IDbHelper _dbHelper;

        public VehicleRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetAllVehiclesWithCustomers(string searchTerm = "", string statusFilter = "الكل")
        {
            // Phase 3 fix: replaced expensive ROW_NUMBER() OVER (PARTITION BY...) full-table window
            // with CROSS APPLY (SELECT TOP 1) which uses IX_RepairOrders_PlateNumber via index seek
            string query = @"
                SELECT c.PhoneNumber, c.FullName, v.PlateNumber, v.VIN, v.CarName, v.CarModel, v.Notes,
                       ro.RepairOrderNumber, ro.Status, ro.EntryDate, ro.Complaint
                FROM Vehicles v
                INNER JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                OUTER APPLY (
                    SELECT TOP 1 RepairOrderNumber, Status, EntryDate, Complaint
                    FROM RepairOrders ro2
                    WHERE ro2.PlateNumber = v.PlateNumber
                    ORDER BY ro2.EntryDate DESC
                ) ro
                WHERE v.IsDeleted = 0 AND (@search = '' OR c.FullName LIKE @search OR c.PhoneNumber LIKE @search OR v.PlateNumber LIKE @search OR v.CarName LIKE @search OR v.VIN LIKE @search)";

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

            query += " ORDER BY ro.EntryDate DESC";

            return _dbHelper.ExecuteDataTable(query, listParams.ToArray());
        }

        public List<C__project_Auto_Care.Core.DTOs.CustomerVehicleRowDto> GetAllVehiclesWithCustomersList(string searchTerm = "", string statusFilter = "الكل")
        {
            string query = @"
                SELECT c.PhoneNumber, c.FullName, v.PlateNumber, v.VIN, v.CarName, v.CarModel, v.Notes,
                       ro.RepairOrderNumber, ro.Status, ro.EntryDate, ro.Complaint
                FROM Vehicles v
                INNER JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                OUTER APPLY (
                    SELECT TOP 1 ro2.RepairOrderNumber, ro2.Status, ro2.EntryDate, ro2.Complaint
                    FROM RepairOrders ro2
                    WHERE ro2.PlateNumber = v.PlateNumber
                    ORDER BY ro2.EntryDate DESC
                ) ro
                WHERE v.IsDeleted = 0 AND (@search = '' OR c.FullName LIKE @search OR c.PhoneNumber LIKE @search OR v.PlateNumber LIKE @search OR v.CarName LIKE @search OR v.VIN LIKE @search)";

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

            query += " ORDER BY ro.EntryDate DESC";

            return _dbHelper.ExecuteList(query, listParams.ToArray(), r =>
            {
                string status = r["Status"] != DBNull.Value ? r["Status"].ToString() : "";
                string arabicStatus = string.IsNullOrWhiteSpace(status) ? "" : EnumExtensions.ParseRepairOrderStatus(status).ToDisplayArabic();

                string entryDate = "";
                if (r["EntryDate"] != DBNull.Value)
                    entryDate = Convert.ToDateTime(r["EntryDate"]).ToString("yyyy-MM-dd HH:mm");

                return new C__project_Auto_Care.Core.DTOs.CustomerVehicleRowDto
                {
                    PhoneNumber = r["PhoneNumber"] != DBNull.Value ? r["PhoneNumber"].ToString() : "",
                    FullName = r["FullName"] != DBNull.Value ? r["FullName"].ToString() : "",
                    PlateNumber = r["PlateNumber"] != DBNull.Value ? r["PlateNumber"].ToString() : "",
                    CarName = r["CarName"] != DBNull.Value ? r["CarName"].ToString() : "",
                    CarModel = r["CarModel"] != DBNull.Value ? r["CarModel"].ToString() : "",
                    EntryDate = entryDate,
                    Status = arabicStatus,
                    Complaint = r["Complaint"] != DBNull.Value ? r["Complaint"].ToString() : "",
                    RoNumber = r["RepairOrderNumber"] != DBNull.Value ? r["RepairOrderNumber"].ToString() : ""
                };
            });
        }

        public DataTable GetVehicleByPlateNumber(string plateNumber)
        {
            string query = "SELECT PlateNumber, VIN, CustomerPhone, CarName, CarModel, Notes FROM Vehicles WHERE PlateNumber = @plate";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public C__project_Auto_Care.Core.DTOs.VehicleWithCustomerDto GetVehicleWithCustomerDto(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return null;
            string query = @"
                SELECT v.PlateNumber, v.VIN, v.CustomerPhone, v.CarName, v.CarModel, v.Notes, c.FullName AS CustomerName
                FROM Vehicles v
                LEFT JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                WHERE v.PlateNumber = @plate";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber.Trim())
            };
            return _dbHelper.ExecuteSingle(query, parameters, r => new C__project_Auto_Care.Core.DTOs.VehicleWithCustomerDto
            {
                PlateNumber = r["PlateNumber"] != DBNull.Value ? r["PlateNumber"].ToString() : "",
                Vin = r["VIN"] != DBNull.Value ? r["VIN"].ToString() : "",
                CarName = r["CarName"] != DBNull.Value ? r["CarName"].ToString() : "",
                CarModel = r["CarModel"] != DBNull.Value ? r["CarModel"].ToString() : "",
                CustomerPhone = r["CustomerPhone"] != DBNull.Value ? r["CustomerPhone"].ToString() : "",
                CustomerName = r["CustomerName"] != DBNull.Value ? r["CustomerName"].ToString() : "",
                Notes = r["Notes"] != DBNull.Value ? r["Notes"].ToString() : ""
            });
        }

        public C__project_Auto_Care.Core.Entities.Vehicle GetVehicleEntityByPlate(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return null;
            string query = "SELECT PlateNumber, VIN, CustomerPhone, CarName, CarModel, Notes FROM Vehicles WHERE PlateNumber = @plate";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber.Trim())
            };
            return _dbHelper.ExecuteSingle(query, parameters, r => new C__project_Auto_Care.Core.Entities.Vehicle
            {
                PlateNumber = r["PlateNumber"]?.ToString(),
                Vin = r["VIN"]?.ToString(),
                CustomerPhone = r["CustomerPhone"]?.ToString(),
                CarName = r["CarName"]?.ToString(),
                CarModel = r["CarModel"]?.ToString()
            });
        }

        public bool VehicleExists(string plateNumber)
        {
            string query = "SELECT COUNT(1) FROM Vehicles WHERE PlateNumber = @plate";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public bool VinExists(string vin, string excludePlateNumber = null)
        {
            string query;
            List<SqlParameter> listParams = new List<SqlParameter>
            {
                new SqlParameter("@vin", vin)
            };

            if (!string.IsNullOrEmpty(excludePlateNumber))
            {
                query = "SELECT COUNT(1) FROM Vehicles WHERE VIN = @vin AND PlateNumber <> @excludePlate";
                listParams.Add(new SqlParameter("@excludePlate", excludePlateNumber));
            }
            else
            {
                query = "SELECT COUNT(1) FROM Vehicles WHERE VIN = @vin";
            }

            int count = _dbHelper.ExecuteScalar<int>(query, listParams.ToArray());
            return count > 0;
        }

        public void AddVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes) =>
            AddVehicle(plateNumber, vin, customerPhone, carName, carModel, notes, null, null);

        public void AddVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes, SqlConnection conn, SqlTransaction tran)
        {
            string query = "INSERT INTO Vehicles (PlateNumber, VIN, CustomerPhone, CarName, CarModel, Notes) VALUES (@plate, @vin, @phone, @carName, @carModel, @notes)";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber),
                new SqlParameter("@vin", vin),
                new SqlParameter("@phone", customerPhone),
                new SqlParameter("@carName", carName),
                new SqlParameter("@carModel", carModel),
                new SqlParameter("@notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes)
            };
            _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
        }

        public void UpdateVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes) =>
            UpdateVehicle(plateNumber, vin, customerPhone, carName, carModel, notes, null, null);

        public void UpdateVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes, SqlConnection conn, SqlTransaction tran)
        {
            string query = "UPDATE Vehicles SET VIN = @vin, CustomerPhone = @phone, CarName = @carName, CarModel = @carModel, Notes = @notes WHERE PlateNumber = @plate";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber),
                new SqlParameter("@vin", vin),
                new SqlParameter("@phone", customerPhone),
                new SqlParameter("@carName", carName),
                new SqlParameter("@carModel", carModel),
                new SqlParameter("@notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes)
            };
            _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
        }

        public void DeleteVehicle(string plateNumber)
        {
            string checkQuery = "SELECT COUNT(1) FROM RepairOrders WHERE PlateNumber = @plate";
            SqlParameter[] checkParams = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber)
            };
            int count = _dbHelper.ExecuteScalar<int>(checkQuery, checkParams);
            if (count > 0)
            {
                throw new InvalidOperationException("لا يمكن حذف المركبة لوجود أوامر صيانة تابعة لها.");
            }

            string deleteQuery = "DELETE FROM Vehicles WHERE PlateNumber = @plate";
            SqlParameter[] deleteParams = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber)
            };
            _dbHelper.ExecuteNonQuery(deleteQuery, deleteParams);
        }

        public void SoftDeleteVehicle(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return;
            string query = "UPDATE Vehicles SET IsDeleted = 1 WHERE PlateNumber = @plate";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber.Trim())
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void RestoreVehicle(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return;
            string query = "UPDATE Vehicles SET IsDeleted = 0 WHERE PlateNumber = @plate";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@plate", plateNumber.Trim())
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }
    }
}
