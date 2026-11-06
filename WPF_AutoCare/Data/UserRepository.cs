using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbHelper _dbHelper;

        public UserRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        private const int Pbkdf2Iterations = 50000;
        private const int SaltByteSize = 16;
        private const int HashByteSize = 32;

        public static string HashPassword(string plainPassword)
        {
            if (plainPassword == null) plainPassword = "";
            byte[] salt = new byte[SaltByteSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            using (var pbkdf2 = new Rfc2898DeriveBytes(plainPassword, salt, Pbkdf2Iterations))
            {
                byte[] hash = pbkdf2.GetBytes(HashByteSize);
                return "PBKDF2$" + Pbkdf2Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string plainPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(plainPassword))
                return false;

            // 1. Check modern PBKDF2 format
            if (storedHash.StartsWith("PBKDF2$", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = storedHash.Split('$');
                if (parts.Length == 4 && int.TryParse(parts[1], out int iterations))
                {
                    try
                    {
                        byte[] salt = Convert.FromBase64String(parts[2]);
                        byte[] expectedHash = Convert.FromBase64String(parts[3]);
                        using (var pbkdf2 = new Rfc2898DeriveBytes(plainPassword, salt, iterations))
                        {
                            byte[] actualHash = pbkdf2.GetBytes(expectedHash.Length);
                            return SlowEquals(expectedHash, actualHash);
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            // 2. Check legacy SHA-256 Base64 hash (44 characters)
            if (IsBase64Sha256(storedHash))
            {
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainPassword));
                    string computed = Convert.ToBase64String(bytes);
                    if (string.Equals(storedHash, computed, StringComparison.Ordinal))
                        return true;
                }
            }

            // 3. Check legacy plain-text match (e.g. freshly seeded database)
            return string.Equals(storedHash, plainPassword, StringComparison.Ordinal);
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static bool IsBase64Sha256(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 44) return false;
            try
            {
                byte[] b = Convert.FromBase64String(s);
                return b.Length == 32;
            }
            catch
            {
                return false;
            }
        }

        public DataTable GetAllUsers(string searchTerm = "", string roleFilter = "")
        {
            string query = @"
                SELECT Username, FullName, Role, IsActive 
                FROM Users 
                WHERE (@search = '' OR Username LIKE @search OR FullName LIKE @search)
                  AND (@role = '' OR Role = @role)
                ORDER BY FullName";

            string searchVal = string.IsNullOrEmpty(searchTerm) ? "" : "%" + searchTerm + "%";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@search", searchVal),
                new SqlParameter("@role", roleFilter ?? "")
            };

            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public System.Collections.Generic.List<C__project_Auto_Care.Core.DTOs.UserListItemDto> GetAllUsersList(string searchTerm = "", string roleFilter = "")
        {
            string query = @"
                SELECT u.Username, u.FullName, u.Role, u.IsActive, 
                       ISNULL(u.BranchId, 1) AS BranchId, ISNULL(b.BranchName, N'الفرع الرئيسي') AS BranchName 
                FROM Users u
                LEFT JOIN Branches b ON u.BranchId = b.Id
                WHERE (@search = '' OR u.Username LIKE @search OR u.FullName LIKE @search)
                  AND (@role = '' OR u.Role = @role)
                ORDER BY u.FullName";

            string searchVal = string.IsNullOrEmpty(searchTerm) ? "" : "%" + searchTerm + "%";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@search", searchVal),
                new SqlParameter("@role", roleFilter ?? "")
            };

            return _dbHelper.ExecuteList(query, parameters, r => new C__project_Auto_Care.Core.DTOs.UserListItemDto
            {
                Username = r["Username"]?.ToString(),
                FullName = r["FullName"]?.ToString(),
                Role = r["Role"]?.ToString(),
                RoleDisplay = r["Role"]?.ToString() == "Admin" ? "مدير النظام" : (r["Role"]?.ToString() == "Accountant" ? "محاسب" : "موظف استقبال"),
                IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]),
                BranchId = r["BranchId"] != DBNull.Value ? Convert.ToInt32(r["BranchId"]) : 1,
                BranchName = r["BranchName"] != DBNull.Value ? r["BranchName"].ToString() : "الفرع الرئيسي"
            });
        }

        public bool UserExists(string username)
        {
            string query = "SELECT COUNT(1) FROM Users WHERE Username = @username";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public bool IsUserUsedInOrders(string username)
        {
            string query = @"
                SELECT COUNT(1) FROM RepairOrders WHERE ReceptionistUsername = @username";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public bool IsUserUsedInInvoices(string username)
        {
            string query = @"
                SELECT COUNT(1) FROM Invoices WHERE CreatedByUsername = @username";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public void AddUser(string username, string fullName, string role, bool isActive, string plainPassword, int branchId = 1)
        {
            string hash = HashPassword(plainPassword);
            string query = @"
                INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, BranchId)
                VALUES (@username, @hash, @fullName, @role, @active, @branchId)";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username),
                new SqlParameter("@hash", hash),
                new SqlParameter("@fullName", fullName),
                new SqlParameter("@role", role),
                new SqlParameter("@active", isActive),
                new SqlParameter("@branchId", branchId > 0 ? branchId : 1)
            };

            _dbHelper.ExecuteNonQuery(query, parameters);
            AppServiceLocator.AuditRepo?.Log("إضافة مستخدم", "Users", username, $"إضافة مستخدم جديد: {fullName} ({role})");
        }

        public void UpdateUser(string username, string fullName, string role, bool isActive, string newPlainPassword = null, int? branchId = null)
        {
            string query;
            var paramList = new System.Collections.Generic.List<SqlParameter>
            {
                new SqlParameter("@username", username),
                new SqlParameter("@fullName", fullName),
                new SqlParameter("@role", role),
                new SqlParameter("@active", isActive)
            };

            string setBranchClause = "";
            if (branchId.HasValue && branchId.Value > 0)
            {
                setBranchClause = ", BranchId = @branchId";
                paramList.Add(new SqlParameter("@branchId", branchId.Value));
            }

            if (!string.IsNullOrEmpty(newPlainPassword))
            {
                string hash = HashPassword(newPlainPassword);
                query = $@"
                    UPDATE Users 
                    SET FullName = @fullName, Role = @role, IsActive = @active, PasswordHash = @hash{setBranchClause}
                    WHERE Username = @username";
                paramList.Add(new SqlParameter("@hash", hash));
            }
            else
            {
                query = $@"
                    UPDATE Users 
                    SET FullName = @fullName, Role = @role, IsActive = @active{setBranchClause}
                    WHERE Username = @username";
            }

            _dbHelper.ExecuteNonQuery(query, paramList.ToArray());
            AppServiceLocator.AuditRepo?.Log("تعديل مستخدم", "Users", username, $"تعديل بيانات المستخدم: {fullName} ({role}) - نشط: {isActive}");
        }

        public void DeleteUser(string username)
        {
            if (IsUserUsedInOrders(username) || IsUserUsedInInvoices(username))
            {
                throw new InvalidOperationException("لا يمكن حذف هذا المستخدم لأنه مرتبط بأوامر صيانة أو فواتير مسجلة في النظام.");
            }

            string query = "DELETE FROM Users WHERE Username = @username";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
            AppServiceLocator.AuditRepo?.Log("حذف مستخدم", "Users", username, $"حذف المستخدم {username} نهائياً من النظام");
        }

        public bool Authenticate(string username, string plainPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(plainPassword))
                return false;

            string query = @"
                SELECT PasswordHash FROM Users 
                WHERE Username = @username AND IsActive = 1";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username)
            };
            DataTable dt = _dbHelper.ExecuteDataTable(query, parameters);
            if (dt == null || dt.Rows.Count == 0)
                return false;

            string storedHash = dt.Rows[0]["PasswordHash"]?.ToString();
            bool isMatched = VerifyPassword(plainPassword, storedHash);

            if (isMatched)
            {
                // Auto-upgrade / sync hash to modern PBKDF2 with current password
                try
                {
                    string upgradedHash = HashPassword(plainPassword);
                    string updateQuery = "UPDATE Users SET PasswordHash = @hash WHERE Username = @username";
                    SqlParameter[] updateParams = new SqlParameter[]
                    {
                        new SqlParameter("@hash", upgradedHash),
                        new SqlParameter("@username", username.Trim())
                    };
                    _dbHelper.ExecuteNonQuery(updateQuery, updateParams);
                }
                catch
                {
                    // Failure to upgrade should not block login
                }
                return true;
            }

            return false;
        }

        public DataTable GetUserData(string username)
        {
            string query = "SELECT Username, FullName, Role, IsActive FROM Users WHERE Username = @username";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public C__project_Auto_Care.Core.DTOs.UserListItemDto GetUserDto(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            string query = "SELECT Username, FullName, Role, IsActive FROM Users WHERE Username = @username";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@username", username.Trim())
            };
            return _dbHelper.ExecuteSingle(query, parameters, r => new C__project_Auto_Care.Core.DTOs.UserListItemDto
            {
                Username = r["Username"]?.ToString(),
                FullName = r["FullName"]?.ToString(),
                Role = r["Role"]?.ToString(),
                RoleDisplay = r["Role"]?.ToString() == "Admin" ? "مدير النظام" : (r["Role"]?.ToString() == "Accountant" ? "محاسب" : "موظف استقبال"),
                IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"])
            });
        }

        public int CountAdminUsers()
        {
            string query = "SELECT COUNT(1) FROM Users WHERE Role = 'Admin'";
            return _dbHelper.ExecuteScalar<int>(query, null);
        }
    }
}
