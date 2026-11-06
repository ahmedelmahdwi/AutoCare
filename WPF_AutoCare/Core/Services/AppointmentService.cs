using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using AutoCare.Domain.Entities;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Core.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IDbHelper _dbHelper;
        private readonly IRepairOrderRepository _roRepo;
        private readonly IAuditRepository _auditRepo;

        public AppointmentService(
            IDbHelper dbHelper = null,
            IRepairOrderRepository roRepo = null,
            IAuditRepository auditRepo = null)
        {
            _dbHelper = dbHelper ?? AppServiceLocator.DbHelper;
            _roRepo = roRepo ?? new RepairOrderRepository(_dbHelper);
            _auditRepo = auditRepo ?? new AuditRepository(_dbHelper);
        }

        public async Task<IReadOnlyList<Appointment>> GetAppointmentsByDateAsync(DateTime date, int? branchId = null)
        {
            var list = new List<Appointment>();
            string sql = @"
                SELECT a.Id, a.AppointmentNumber, a.CustomerPhone, a.CustomerName, a.PlateNumber,
                       a.CarModel, a.TechnicianID, a.BranchId, a.AppointmentDate, a.StartTime, a.EndTime,
                       a.Status, a.Notes, a.RepairOrderNumber, a.CreatedAt, a.IsDeleted,
                       c.FullName AS CustName, v.CarModel AS VehicleModel, t.FullName AS TechName
                FROM Appointments a
                LEFT JOIN Customers c ON a.CustomerPhone = c.PhoneNumber
                LEFT JOIN Vehicles v ON a.PlateNumber = v.PlateNumber
                LEFT JOIN Technicians t ON a.TechnicianID = t.TechnicianID
                WHERE a.IsDeleted = 0
                  AND a.AppointmentDate = @AppointmentDate
                  AND (@BranchId IS NULL OR a.BranchId = @BranchId)
                ORDER BY a.StartTime ASC";

            try
            {
                using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@AppointmentDate", date.Date);
                        cmd.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(MapAppointment(reader));
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public async Task<Appointment> GetByIdAsync(int id)
        {
            string sql = @"
                SELECT a.Id, a.AppointmentNumber, a.CustomerPhone, a.CustomerName, a.PlateNumber,
                       a.CarModel, a.TechnicianID, a.BranchId, a.AppointmentDate, a.StartTime, a.EndTime,
                       a.Status, a.Notes, a.RepairOrderNumber, a.CreatedAt, a.IsDeleted,
                       c.FullName AS CustName, v.CarModel AS VehicleModel, t.FullName AS TechName
                FROM Appointments a
                LEFT JOIN Customers c ON a.CustomerPhone = c.PhoneNumber
                LEFT JOIN Vehicles v ON a.PlateNumber = v.PlateNumber
                LEFT JOIN Technicians t ON a.TechnicianID = t.TechnicianID
                WHERE a.Id = @Id AND a.IsDeleted = 0";

            try
            {
                using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapAppointment(reader);
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> CheckConflictAsync(int technicianId, DateTime date, TimeSpan start, TimeSpan end, int? excludeId = null)
        {
            string sql = @"
                SELECT COUNT(1) FROM Appointments
                WHERE IsDeleted = 0
                  AND TechnicianID = @TechnicianID
                  AND AppointmentDate = @AppointmentDate
                  AND Status NOT IN ('Cancelled', 'NoShow')
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                  AND (@Start < EndTime AND @End > StartTime)";

            try
            {
                using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TechnicianID", technicianId);
                        cmd.Parameters.AddWithValue("@AppointmentDate", date.Date);
                        cmd.Parameters.AddWithValue("@Start", start);
                        cmd.Parameters.AddWithValue("@End", end);
                        cmd.Parameters.AddWithValue("@ExcludeId", (object)excludeId ?? DBNull.Value);

                        var res = await cmd.ExecuteScalarAsync();
                        return res != null && Convert.ToInt32(res) > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> CreateAppointmentAsync(Appointment appointment)
        {
            if (appointment == null) throw new ArgumentNullException(nameof(appointment));

            if (appointment.TechnicianId.HasValue)
            {
                bool hasConflict = await CheckConflictAsync(appointment.TechnicianId.Value, appointment.AppointmentDate, appointment.StartTime, appointment.EndTime);
                if (hasConflict)
                {
                    throw new InvalidOperationException("يوجد تضارب في موعد الفني المحدد لنفس الفترة الزمنية!");
                }
            }

            if (string.IsNullOrWhiteSpace(appointment.AppointmentNumber))
            {
                appointment.AppointmentNumber = await GenerateNextAppointmentNumberAsync();
            }

            string sql = @"
                INSERT INTO Appointments
                    (AppointmentNumber, CustomerPhone, CustomerName, PlateNumber, CarModel,
                     TechnicianID, BranchId, AppointmentDate, StartTime, EndTime,
                     Status, Notes, RepairOrderNumber, CreatedAt, IsDeleted)
                OUTPUT INSERTED.Id
                VALUES
                    (@AppointmentNumber, @CustomerPhone, @CustomerName, @PlateNumber, @CarModel,
                     @TechnicianID, @BranchId, @AppointmentDate, @StartTime, @EndTime,
                     @Status, @Notes, @RepairOrderNumber, @CreatedAt, 0)";

            using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@AppointmentNumber", appointment.AppointmentNumber);
                    cmd.Parameters.AddWithValue("@CustomerPhone", appointment.CustomerPhone ?? "");
                    cmd.Parameters.AddWithValue("@CustomerName", (object)appointment.CustomerName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PlateNumber", appointment.PlateNumber ?? "");
                    cmd.Parameters.AddWithValue("@CarModel", (object)appointment.CarModel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TechnicianID", (object)appointment.TechnicianId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BranchId", appointment.BranchId);
                    cmd.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate.Date);
                    cmd.Parameters.AddWithValue("@StartTime", appointment.StartTime);
                    cmd.Parameters.AddWithValue("@EndTime", appointment.EndTime);
                    cmd.Parameters.AddWithValue("@Status", appointment.Status.ToString());
                    cmd.Parameters.AddWithValue("@Notes", (object)appointment.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RepairOrderNumber", (object)appointment.RepairOrderNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", appointment.CreatedAt);

                    var id = await cmd.ExecuteScalarAsync();
                    appointment.Id = Convert.ToInt32(id);

                    _auditRepo?.Log(
                        "حجز موعد",
                        "Appointments",
                        appointment.AppointmentNumber,
                        $"تم حجز موعد جديد برقم {appointment.AppointmentNumber} للسيارة {appointment.PlateNumber}");

                    return appointment.Id;
                }
            }
        }

        public async Task<bool> CancelAppointmentAsync(int id)
        {
            string sql = "UPDATE Appointments SET Status = 'Cancelled' WHERE Id = @Id AND IsDeleted = 0";
            using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    int rows = await cmd.ExecuteNonQueryAsync();

                    _auditRepo?.Log("إلغاء موعد", "Appointments", id.ToString(), $"تم إلغاء الموعد المعرف {id}");
                    return rows > 0;
                }
            }
        }

        public async Task<bool> RescheduleAppointmentAsync(int id, DateTime newDate, TimeSpan start, TimeSpan end)
        {
            var apt = await GetByIdAsync(id);
            if (apt == null) return false;

            if (apt.TechnicianId.HasValue)
            {
                bool hasConflict = await CheckConflictAsync(apt.TechnicianId.Value, newDate, start, end, id);
                if (hasConflict)
                {
                    throw new InvalidOperationException("يوجد تعارض في مواعيد الفني في التوقيت الجديد!");
                }
            }

            string sql = "UPDATE Appointments SET AppointmentDate = @AppointmentDate, StartTime = @StartTime, EndTime = @EndTime WHERE Id = @Id AND IsDeleted = 0";
            using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@AppointmentDate", newDate.Date);
                    cmd.Parameters.AddWithValue("@StartTime", start);
                    cmd.Parameters.AddWithValue("@EndTime", end);
                    int rows = await cmd.ExecuteNonQueryAsync();

                    _auditRepo?.Log("إعادة جدولة موعد", "Appointments", apt.AppointmentNumber, $"تم تغيير موعد {apt.AppointmentNumber} إلى {newDate:yyyy-MM-dd}");
                    return rows > 0;
                }
            }
        }

        public async Task<string> ConvertAppointmentToRepairOrderAsync(int appointmentId, string receptionistUsername)
        {
            var apt = await GetByIdAsync(appointmentId);
            if (apt == null)
                throw new InvalidOperationException("الموعد غير موجود بالمنظومة.");

            if (!apt.CanConvertToOrder)
                throw new InvalidOperationException($"لا يمكن تحويل هذا الموعد؛ حالته الحالية: {apt.StatusArabic}");

            string newRoNumber = _roRepo.GenerateNewRepairOrderNumber();
            string complaint = string.IsNullOrWhiteSpace(apt.Notes) ? "صيانة دورية محجوزة مسبقاً" : apt.Notes;

            using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Ensure Customer and Vehicle records exist so RepairOrder FK succeeds
                        EnsureCustomerAndVehicleExist(conn, trans, apt);

                        // 2. Insert Repair Order
                        _roRepo.AddRepairOrder(
                            newRoNumber,
                            apt.PlateNumber,
                            receptionistUsername,
                            complaint,
                            "Pending",
                            DateTime.Now.AddDays(1),
                            conn,
                            trans);

                        // 2. Update Appointment to InProgress and link RepairOrderNumber
                        string updateSql = @"
                            UPDATE Appointments 
                            SET Status = 'InProgress',
                                RepairOrderNumber = @RoNum 
                            WHERE Id = @Id";

                        using (var cmd = new SqlCommand(updateSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@RoNum", newRoNumber);
                            cmd.Parameters.AddWithValue("@Id", appointmentId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        trans.Commit();

                        _auditRepo?.Log(
                            "تحويل موعد لأمر صيانة",
                            "RepairOrders",
                            newRoNumber,
                            $"تم تحويل الموعد {apt.AppointmentNumber} بنجاح إلى كرت صيانة رقم {newRoNumber}");

                        return newRoNumber;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        private async Task<string> GenerateNextAppointmentNumberAsync()
        {
            string prefix = "APT-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            string sql = "SELECT COUNT(1) FROM Appointments WHERE AppointmentNumber LIKE @Pattern";

            int count = 1;
            try
            {
                using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Pattern", prefix + "%");
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            count = Convert.ToInt32(result) + 1;
                        }
                    }
                }
            }
            catch { }

            return prefix + count.ToString("D3");
        }

        private static Appointment MapAppointment(SqlDataReader reader)
        {
            var apt = new Appointment
            {
                Id = reader.GetInt32(0),
                AppointmentNumber = reader.GetString(1),
                CustomerPhone = reader.GetString(2),
                CustomerName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                PlateNumber = reader.GetString(4),
                CarModel = reader.IsDBNull(5) ? "" : reader.GetString(5),
                TechnicianId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                BranchId = reader.GetInt32(7),
                AppointmentDate = reader.GetDateTime(8),
                StartTime = reader.GetTimeSpan(9),
                EndTime = reader.GetTimeSpan(10),
                Notes = reader.IsDBNull(12) ? "" : reader.GetString(12),
                RepairOrderNumber = reader.IsDBNull(13) ? null : reader.GetString(13),
                CreatedAt = reader.GetDateTime(14),
                IsDeleted = reader.GetBoolean(15)
            };

            string statusStr = reader.GetString(11);
            if (Enum.TryParse<AppointmentStatus>(statusStr, true, out var status))
            {
                apt.Status = status;
            }

            // Joined columns
            if (reader.FieldCount > 16 && !reader.IsDBNull(16)) apt.CustomerName = reader.GetString(16);
            if (reader.FieldCount > 17 && !reader.IsDBNull(17)) apt.CarModel = reader.GetString(17);
            if (reader.FieldCount > 18 && !reader.IsDBNull(18)) apt.TechnicianName = reader.GetString(18);

            return apt;
        }

        private void EnsureCustomerAndVehicleExist(SqlConnection conn, SqlTransaction trans, Appointment apt)
        {
            if (string.IsNullOrWhiteSpace(apt?.PlateNumber)) return;

            string phone = string.IsNullOrWhiteSpace(apt.CustomerPhone) ? "0910000000" : apt.CustomerPhone.Trim();
            string name = string.IsNullOrWhiteSpace(apt.CustomerName) ? "عميل موعد" : apt.CustomerName.Trim();
            string plate = apt.PlateNumber.Trim().ToUpperInvariant();
            string model = string.IsNullOrWhiteSpace(apt.CarModel) ? "غير محدد" : apt.CarModel.Trim();

            // 1. Ensure Customer exists
            string checkCust = "SELECT COUNT(1) FROM Customers WHERE PhoneNumber = @phone";
            using (var cmd = new SqlCommand(checkCust, conn, trans))
            {
                cmd.Parameters.AddWithValue("@phone", phone);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    string insertCust = "INSERT INTO Customers (PhoneNumber, FullName, CreatedAt, IsDeleted) VALUES (@phone, @name, GETDATE(), 0)";
                    using (var insCmd = new SqlCommand(insertCust, conn, trans))
                    {
                        insCmd.Parameters.AddWithValue("@phone", phone);
                        insCmd.Parameters.AddWithValue("@name", name);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }

            // 2. Ensure Vehicle exists
            string checkVeh = "SELECT COUNT(1) FROM Vehicles WHERE PlateNumber = @plate";
            using (var cmd = new SqlCommand(checkVeh, conn, trans))
            {
                cmd.Parameters.AddWithValue("@plate", plate);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    string insertVeh = @"
                        INSERT INTO Vehicles (PlateNumber, VIN, CustomerPhone, CarName, CarModel, Notes, CreatedAt, IsDeleted)
                        VALUES (@plate, NULL, @phone, @model, YEAR(GETDATE()), 'تم إنشاؤها تلقائياً عند تحويل الموعد لكرت صيانة', GETDATE(), 0)";
                    using (var insCmd = new SqlCommand(insertVeh, conn, trans))
                    {
                        insCmd.Parameters.AddWithValue("@plate", plate);
                        insCmd.Parameters.AddWithValue("@phone", phone);
                        insCmd.Parameters.AddWithValue("@model", model);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
