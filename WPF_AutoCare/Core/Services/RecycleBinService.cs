using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    public interface IRecycleBinService
    {
        Task<List<RecycleBinItemDto>> GetDeletedItemsAsync(string itemTypeFilter = "الكل", string search = "");
        OperationResult RestoreItem(string itemType, string key);
        OperationResult PurgeItem(string itemType, string key);
        OperationResult PurgeAllEligible();
    }

    public class RecycleBinService : IRecycleBinService
    {
        private readonly IDbHelper _dbHelper;
        private readonly IAuditRepository _auditRepo;

        public RecycleBinService(IDbHelper dbHelper, IAuditRepository auditRepo = null)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _auditRepo = auditRepo;
        }

        public async Task<List<RecycleBinItemDto>> GetDeletedItemsAsync(string itemTypeFilter = "الكل", string search = "")
        {
            return await Task.Run(() =>
            {
                var list = new List<RecycleBinItemDto>();
                string s = string.IsNullOrWhiteSpace(search) ? "" : search.Trim();

                string connStr = _dbHelper.GetConnectionString();
                if (string.IsNullOrWhiteSpace(connStr)) return list;

                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // 1. Fetch Soft-Deleted Customers
                    if (itemTypeFilter == "الكل" || itemTypeFilter == "العملاء")
                    {
                        string custSql = @"
                            SELECT c.PhoneNumber, c.FullName, c.CreatedAt,
                                   (SELECT COUNT(*) FROM Vehicles v WHERE v.CustomerPhone = c.PhoneNumber) AS VehicleCount,
                                   (SELECT COUNT(*) FROM Appointments a WHERE a.CustomerPhone = c.PhoneNumber) AS AptCount
                            FROM Customers c
                            WHERE c.IsDeleted = 1";

                        using (var cmd = new SqlCommand(custSql, conn))
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string phone = r["PhoneNumber"]?.ToString() ?? "";
                                string name = r["FullName"]?.ToString() ?? "";
                                int vCount = Convert.ToInt32(r["VehicleCount"]);
                                int aCount = Convert.ToInt32(r["AptCount"]);

                                if (!string.IsNullOrEmpty(s) && !phone.Contains(s) && !name.Contains(s))
                                    continue;

                                bool canPurge = (vCount == 0 && aCount == 0);
                                string reason = canPurge 
                                    ? "مؤهل للحذف النهائي الآمن (لا توجد ارتباطات)" 
                                    : $"محمي: مرتبط بـ {vCount} سيارة مسجلة و {aCount} موعد";

                                DateTime? dt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : (DateTime?)null;

                                list.Add(new RecycleBinItemDto
                                {
                                    ItemType = "عميل",
                                    Key = phone,
                                    DisplayName = name,
                                    Details = $"الهاتف: {phone} | السيارات المرتبطة: {vCount}",
                                    DeletedAt = dt,
                                    CanPurge = canPurge,
                                    StatusReason = reason
                                });
                            }
                        }
                    }

                    // 2. Fetch Soft-Deleted Vehicles
                    if (itemTypeFilter == "الكل" || itemTypeFilter == "المركبات")
                    {
                        string vehSql = @"
                            SELECT v.PlateNumber, v.CarName, v.CarModel, v.CustomerPhone, v.CreatedAt,
                                   (SELECT COUNT(*) FROM RepairOrders ro WHERE ro.PlateNumber = v.PlateNumber) AS RoCount,
                                   (SELECT COUNT(*) FROM Appointments a WHERE a.PlateNumber = v.PlateNumber) AS AptCount
                            FROM Vehicles v
                            WHERE v.IsDeleted = 1";

                        using (var cmd = new SqlCommand(vehSql, conn))
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string plate = r["PlateNumber"]?.ToString() ?? "";
                                string carName = r["CarName"]?.ToString() ?? "";
                                string carModel = r["CarModel"]?.ToString() ?? "";
                                string custPhone = r["CustomerPhone"]?.ToString() ?? "";
                                int roCount = Convert.ToInt32(r["RoCount"]);
                                int aptCount = Convert.ToInt32(r["AptCount"]);

                                string disp = $"{carName} {carModel} ({plate})";
                                if (!string.IsNullOrEmpty(s) && !plate.Contains(s) && !carName.Contains(s) && !custPhone.Contains(s))
                                    continue;

                                bool canPurge = (roCount == 0 && aptCount == 0);
                                string reason = canPurge
                                    ? "مؤهل للحذف النهائي الآمن (بلا كروت صيانة)"
                                    : $"محمي: مرتبط بـ {roCount} كرت صيانة تاريخي";

                                DateTime? dt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : (DateTime?)null;

                                list.Add(new RecycleBinItemDto
                                {
                                    ItemType = "مركبة",
                                    Key = plate,
                                    DisplayName = disp,
                                    Details = $"المالك: {custPhone} | كروت الصيانة: {roCount}",
                                    DeletedAt = dt,
                                    CanPurge = canPurge,
                                    StatusReason = reason
                                });
                            }
                        }
                    }

                    // 3. Fetch Soft-Deleted or Cancelled Appointments
                    if (itemTypeFilter == "الكل" || itemTypeFilter == "الحجوزات")
                    {
                        string aptSql = @"
                            SELECT a.AppointmentNumber, a.CustomerName, a.PlateNumber, a.AppointmentDate, a.Status, a.RepairOrderNumber, a.CreatedAt
                            FROM Appointments a
                            WHERE a.IsDeleted = 1 OR a.Status IN ('Cancelled', 'Canceled')";

                        using (var cmd = new SqlCommand(aptSql, conn))
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string aptNum = r["AppointmentNumber"]?.ToString() ?? "";
                                string custName = r["CustomerName"]?.ToString() ?? "";
                                string plate = r["PlateNumber"]?.ToString() ?? "";
                                string roNum = r["RepairOrderNumber"]?.ToString() ?? "";
                                string status = r["Status"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(s) && !aptNum.Contains(s) && !custName.Contains(s) && !plate.Contains(s))
                                    continue;

                                bool canPurge = string.IsNullOrEmpty(roNum);
                                string reason = canPurge
                                    ? "مؤهل للحذف النهائي (موعد ملغي غير محول)"
                                    : $"محمي: تم تحويله إلى أمر الصيانة رقم {roNum}";

                                DateTime? dt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : (DateTime?)null;

                                list.Add(new RecycleBinItemDto
                                {
                                    ItemType = "موعد",
                                    Key = aptNum,
                                    DisplayName = $"موعد #{aptNum} - {custName}",
                                    Details = $"السيارة: {plate} | الحالة: {status}",
                                    DeletedAt = dt,
                                    CanPurge = canPurge,
                                    StatusReason = reason
                                });
                            }
                        }
                    }
                }

                return list;
            });
        }

        public OperationResult RestoreItem(string itemType, string key)
        {
            if (string.IsNullOrWhiteSpace(itemType) || string.IsNullOrWhiteSpace(key))
                return OperationResult.Fail("معرف السجل غير صالح.");

            try
            {
                string connStr = _dbHelper.GetConnectionString();
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            if (itemType == "عميل")
                            {
                                string sql = "UPDATE Customers SET IsDeleted = 0 WHERE PhoneNumber = @key";
                                using (var cmd = new SqlCommand(sql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@key", key);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else if (itemType == "مركبة")
                            {
                                // If owner is soft deleted, restore owner too!
                                string restoreCust = @"
                                    UPDATE Customers SET IsDeleted = 0 
                                    WHERE PhoneNumber = (SELECT CustomerPhone FROM Vehicles WHERE PlateNumber = @key AND IsDeleted = 1)";
                                using (var cmdCust = new SqlCommand(restoreCust, conn, trans))
                                {
                                    cmdCust.Parameters.AddWithValue("@key", key);
                                    cmdCust.ExecuteNonQuery();
                                }

                                string sql = "UPDATE Vehicles SET IsDeleted = 0 WHERE PlateNumber = @key";
                                using (var cmd = new SqlCommand(sql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@key", key);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else if (itemType == "موعد")
                            {
                                string sql = "UPDATE Appointments SET IsDeleted = 0, Status = 'Confirmed' WHERE AppointmentNumber = @key";
                                using (var cmd = new SqlCommand(sql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@key", key);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                return OperationResult.Fail("نوع السجل غير معروف.");
                            }

                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }

                string user = UserSession.Username ?? "admin";
                _auditRepo?.Log("استعادة من المحذوفات", itemType, key, $"تمت استعادة السجل {key} بنجاح إلى النظام النشط.", user);

                return OperationResult.Ok($"تمت استعادة {itemType} ({key}) بنجاح إلى النظام.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"فشل استعادة السجل: {ex.Message}");
            }
        }

        public OperationResult PurgeItem(string itemType, string key)
        {
            if (string.IsNullOrWhiteSpace(itemType) || string.IsNullOrWhiteSpace(key))
                return OperationResult.Fail("معرف السجل غير صالح.");

            try
            {
                string connStr = _dbHelper.GetConnectionString();
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    if (itemType == "عميل")
                    {
                        // Check dependencies
                        string checkSql = @"
                            SELECT 
                                (SELECT COUNT(*) FROM Vehicles WHERE CustomerPhone = @key) AS VCount,
                                (SELECT COUNT(*) FROM Appointments WHERE CustomerPhone = @key) AS ACount";
                        using (var checkCmd = new SqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@key", key);
                            using (var r = checkCmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    int vc = Convert.ToInt32(r["VCount"]);
                                    int ac = Convert.ToInt32(r["ACount"]);
                                    if (vc > 0 || ac > 0)
                                        return OperationResult.Fail($"لا يمكن الحذف النهائي: العميل مرتبط بـ {vc} سيارة و {ac} موعد.");
                                }
                            }
                        }

                        string delSql = "DELETE FROM Customers WHERE PhoneNumber = @key AND IsDeleted = 1";
                        using (var delCmd = new SqlCommand(delSql, conn))
                        {
                            delCmd.Parameters.AddWithValue("@key", key);
                            delCmd.ExecuteNonQuery();
                        }
                    }
                    else if (itemType == "مركبة")
                    {
                        // Check dependencies
                        string checkSql = @"
                            SELECT 
                                (SELECT COUNT(*) FROM RepairOrders WHERE PlateNumber = @key) AS RoCount,
                                (SELECT COUNT(*) FROM Appointments WHERE PlateNumber = @key) AS ACount";
                        using (var checkCmd = new SqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@key", key);
                            using (var r = checkCmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    int rc = Convert.ToInt32(r["RoCount"]);
                                    int ac = Convert.ToInt32(r["ACount"]);
                                    if (rc > 0 || ac > 0)
                                        return OperationResult.Fail($"لا يمكن الحذف النهائي: المركبة مرتبطة بـ {rc} كرت صيانة تاريخي.");
                                }
                            }
                        }

                        string delSql = "DELETE FROM Vehicles WHERE PlateNumber = @key AND IsDeleted = 1";
                        using (var delCmd = new SqlCommand(delSql, conn))
                        {
                            delCmd.Parameters.AddWithValue("@key", key);
                            delCmd.ExecuteNonQuery();
                        }
                    }
                    else if (itemType == "موعد")
                    {
                        string checkSql = "SELECT RepairOrderNumber FROM Appointments WHERE AppointmentNumber = @key";
                        using (var checkCmd = new SqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@key", key);
                            object roVal = checkCmd.ExecuteScalar();
                            if (roVal != null && roVal != DBNull.Value && !string.IsNullOrWhiteSpace(roVal.ToString()))
                            {
                                return OperationResult.Fail($"لا يمكن الحذف النهائي: الموعد مرتبط بأمر الصيانة {roVal}.");
                            }
                        }

                        string delSql = "DELETE FROM Appointments WHERE AppointmentNumber = @key";
                        using (var delCmd = new SqlCommand(delSql, conn))
                        {
                            delCmd.Parameters.AddWithValue("@key", key);
                            delCmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        return OperationResult.Fail("نوع السجل غير معروف.");
                    }
                }

                string user = UserSession.Username ?? "admin";
                _auditRepo?.Log("حذف نهائي من الأرشيف", itemType, key, $"تم حذف السجل {key} نهائياً وبلا رجعة من قاعدة البيانات.", user);

                return OperationResult.Ok($"تم الحذف النهائي لـ {itemType} ({key}) من قاعدة البيانات بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"فشل الحذف النهائي: {ex.Message}");
            }
        }

        public OperationResult PurgeAllEligible()
        {
            try
            {
                int totalPurged = 0;
                string connStr = _dbHelper.GetConnectionString();
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Appointments
                            string delApt = @"
                                DELETE FROM Appointments 
                                WHERE (IsDeleted = 1 OR Status IN ('Cancelled', 'Canceled'))
                                  AND (RepairOrderNumber IS NULL OR RepairOrderNumber = '')";
                            using (var cmd = new SqlCommand(delApt, conn, trans))
                            {
                                totalPurged += cmd.ExecuteNonQuery();
                            }

                            // 2. Vehicles
                            string delVeh = @"
                                DELETE FROM Vehicles 
                                WHERE IsDeleted = 1
                                  AND NOT EXISTS (SELECT 1 FROM RepairOrders ro WHERE ro.PlateNumber = Vehicles.PlateNumber)
                                  AND NOT EXISTS (SELECT 1 FROM Appointments a WHERE a.PlateNumber = Vehicles.PlateNumber)";
                            using (var cmd = new SqlCommand(delVeh, conn, trans))
                            {
                                totalPurged += cmd.ExecuteNonQuery();
                            }

                            // 3. Customers
                            string delCust = @"
                                DELETE FROM Customers 
                                WHERE IsDeleted = 1
                                  AND NOT EXISTS (SELECT 1 FROM Vehicles v WHERE v.CustomerPhone = Customers.PhoneNumber)
                                  AND NOT EXISTS (SELECT 1 FROM Appointments a WHERE a.CustomerPhone = Customers.PhoneNumber)";
                            using (var cmd = new SqlCommand(delCust, conn, trans))
                            {
                                totalPurged += cmd.ExecuteNonQuery();
                            }

                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }

                string user = UserSession.Username ?? "admin";
                _auditRepo?.Log("تفريغ سلة المحذوفات بالكامل", "RecycleBin", "ALL", $"تم تفريغ كافة السجلات المؤهلة بعدد {totalPurged} سجل.", user);

                return OperationResult.Ok($"تم تفريغ وحذف {totalPurged} سجل مؤهل بنجاح وأمان.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"فشل تفريغ المحذوفات: {ex.Message}");
            }
        }
    }
}
