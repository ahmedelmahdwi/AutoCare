using System;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class TechnicianRepository : ITechnicianRepository
    {
        private readonly IDbHelper _dbHelper;

        public TechnicianRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetActiveTechnicians()
        {
            string query = @"
                SELECT TechnicianID, FullName 
                FROM Technicians 
                WHERE IsActive = 1 
                ORDER BY FullName";
            return _dbHelper.ExecuteDataTable(query, null);
        }

        public System.Collections.Generic.List<C__project_Auto_Care.Core.DTOs.TechnicianSummaryDto> GetActiveTechniciansList()
        {
            string query = @"
                SELECT TechnicianID, FullName 
                FROM Technicians 
                WHERE IsActive = 1 
                ORDER BY FullName";
            return _dbHelper.ExecuteList(query, null, r => new C__project_Auto_Care.Core.DTOs.TechnicianSummaryDto
            {
                TechnicianId = Convert.ToInt32(r["TechnicianID"]),
                FullName = r["FullName"]?.ToString()
            });
        }

        public DataTable GetAllTechnicians(string searchTerm = "", string specializationFilter = "")
        {
            // Phase 3 fix: removed CAST(TechnicianID AS NVARCHAR) LIKE which prevents index use.
            // If searchTerm is a pure integer, add a direct TechnicianID = @id filter.
            // Otherwise, search only on FullName (which can use IX_Technicians index via LIKE prefix).
            int parsedId = 0;
            bool isNumericSearch = !string.IsNullOrEmpty(searchTerm) && int.TryParse(searchTerm.Trim(), out parsedId);

            string query;
            System.Collections.Generic.List<SqlParameter> paramList = new System.Collections.Generic.List<SqlParameter>();

            if (isNumericSearch)
            {
                // Exact integer match — uses primary key / index seek on TechnicianID
                query = @"
                    SELECT TechnicianID, FullName, Specialization, CommissionPercent, IsActive
                    FROM Technicians
                    WHERE (TechnicianID = @idSearch OR FullName LIKE @search)
                      AND (@spec = '' OR Specialization = @spec)
                    ORDER BY FullName";
                paramList.Add(new SqlParameter("@idSearch", parsedId));
                paramList.Add(new SqlParameter("@search", "%" + searchTerm.Trim() + "%"));
            }
            else
            {
                query = @"
                    SELECT TechnicianID, FullName, Specialization, CommissionPercent, IsActive
                    FROM Technicians
                    WHERE (@search = '' OR FullName LIKE @search)
                      AND (@spec = '' OR Specialization = @spec)
                    ORDER BY FullName";
                string searchVal = string.IsNullOrEmpty(searchTerm) ? "" : "%" + searchTerm + "%";
                paramList.Add(new SqlParameter("@search", searchVal));
            }

            paramList.Add(new SqlParameter("@spec", specializationFilter ?? ""));
            return _dbHelper.ExecuteDataTable(query, paramList.ToArray());
        }

        public System.Collections.Generic.List<C__project_Auto_Care.Core.DTOs.TechnicianRowDto> GetAllTechniciansList(string searchTerm = "", string specializationFilter = "")
        {
            int parsedId = 0;
            bool isNumericSearch = !string.IsNullOrEmpty(searchTerm) && int.TryParse(searchTerm.Trim(), out parsedId);

            string query;
            System.Collections.Generic.List<SqlParameter> paramList = new System.Collections.Generic.List<SqlParameter>();

            if (isNumericSearch)
            {
                query = @"
                    SELECT t.TechnicianID, t.FullName, t.Specialization, t.CommissionPercent, t.IsActive,
                           ISNULL(t.BranchId, 1) AS BranchId, ISNULL(b.BranchName, N'الفرع الرئيسي') AS BranchName
                    FROM Technicians t
                    LEFT JOIN Branches b ON t.BranchId = b.Id
                    WHERE (t.TechnicianID = @idSearch OR t.FullName LIKE @search)
                      AND (@spec = '' OR t.Specialization = @spec)
                    ORDER BY t.FullName";
                paramList.Add(new SqlParameter("@idSearch", parsedId));
                paramList.Add(new SqlParameter("@search", "%" + searchTerm.Trim() + "%"));
            }
            else
            {
                query = @"
                    SELECT t.TechnicianID, t.FullName, t.Specialization, t.CommissionPercent, t.IsActive,
                           ISNULL(t.BranchId, 1) AS BranchId, ISNULL(b.BranchName, N'الفرع الرئيسي') AS BranchName
                    FROM Technicians t
                    LEFT JOIN Branches b ON t.BranchId = b.Id
                    WHERE (@search = '' OR t.FullName LIKE @search)
                      AND (@spec = '' OR t.Specialization = @spec)
                    ORDER BY t.FullName";
                string searchVal = string.IsNullOrEmpty(searchTerm) ? "" : "%" + searchTerm + "%";
                paramList.Add(new SqlParameter("@search", searchVal));
            }

            paramList.Add(new SqlParameter("@spec", specializationFilter ?? ""));
            return _dbHelper.ExecuteList(query, paramList.ToArray(), r => new C__project_Auto_Care.Core.DTOs.TechnicianRowDto
            {
                TechId = Convert.ToInt32(r["TechnicianID"]),
                TechName = r["FullName"]?.ToString(),
                Specialization = r["Specialization"]?.ToString(),
                CommissionPercent = r["CommissionPercent"] != DBNull.Value ? Convert.ToDecimal(r["CommissionPercent"]) : 0m,
                IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]),
                BranchId = r["BranchId"] != DBNull.Value ? Convert.ToInt32(r["BranchId"]) : 1,
                BranchName = r["BranchName"] != DBNull.Value ? r["BranchName"].ToString() : "الفرع الرئيسي"
            });
        }

        public DataTable GetSpecializations()
        {
            string query = "SELECT DISTINCT Specialization FROM Technicians WHERE Specialization IS NOT NULL ORDER BY Specialization";
            return _dbHelper.ExecuteDataTable(query, null);
        }

        public System.Collections.Generic.List<string> GetSpecializationsList()
        {
            string query = "SELECT DISTINCT Specialization FROM Technicians WHERE Specialization IS NOT NULL ORDER BY Specialization";
            return _dbHelper.ExecuteList(query, null, r => r["Specialization"]?.ToString());
        }

        public bool TechnicianExists(int technicianId)
        {
            string query = "SELECT COUNT(1) FROM Technicians WHERE TechnicianID = @id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", technicianId)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public bool IsTechnicianUsedInOrders(int technicianId)
        {
            string query = @"
                DECLARE @count INT = 0;
                IF OBJECT_ID(N'dbo.RepairOrderService', N'U') IS NOT NULL
                    SELECT @count = @count + COUNT(1) FROM RepairOrderService WHERE TechnicianID = @id;
                IF OBJECT_ID(N'dbo.TechnicianSettlement', N'U') IS NOT NULL
                    SELECT @count = @count + COUNT(1) FROM TechnicianSettlement WHERE TechnicianID = @id;
                SELECT @count;";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", technicianId)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public void AddTechnician(int technicianId, string fullName, string specialization, decimal commissionPercent, bool isActive, int branchId = 1)
        {
            string query = @"
                INSERT INTO Technicians (TechnicianID, FullName, Specialization, CommissionPercent, IsActive, BranchId)
                VALUES (@id, @name, @spec, @comm, @active, @branchId)";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", technicianId),
                new SqlParameter("@name", fullName),
                new SqlParameter("@spec", specialization),
                new SqlParameter("@comm", commissionPercent),
                new SqlParameter("@active", isActive),
                new SqlParameter("@branchId", branchId > 0 ? branchId : 1)
            };

            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void UpdateTechnician(int technicianId, string fullName, string specialization, decimal commissionPercent, bool isActive, int branchId = 1)
        {
            string query = @"
                UPDATE Technicians 
                SET FullName = @name, Specialization = @spec, CommissionPercent = @comm, IsActive = @active, BranchId = @branchId
                WHERE TechnicianID = @id";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", technicianId),
                new SqlParameter("@name", fullName),
                new SqlParameter("@spec", specialization),
                new SqlParameter("@comm", commissionPercent),
                new SqlParameter("@active", isActive),
                new SqlParameter("@branchId", branchId > 0 ? branchId : 1)
            };

            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public DataTable GetTechnicianById(int technicianId)
        {
            string query = "SELECT TechnicianID, FullName, Specialization, CommissionPercent, IsActive, ISNULL(BranchId, 1) AS BranchId FROM Technicians WHERE TechnicianID = @id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", technicianId)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public C__project_Auto_Care.Core.DTOs.TechnicianRowDto GetTechnicianDtoById(int technicianId)
        {
            string query = @"
                SELECT t.TechnicianID, t.FullName, t.Specialization, t.CommissionPercent, t.IsActive, 
                       ISNULL(t.BranchId, 1) AS BranchId, ISNULL(b.BranchName, N'الفرع الرئيسي') AS BranchName 
                FROM Technicians t
                LEFT JOIN Branches b ON t.BranchId = b.Id
                WHERE t.TechnicianID = @id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", technicianId)
            };
            return _dbHelper.ExecuteSingle(query, parameters, r => new C__project_Auto_Care.Core.DTOs.TechnicianRowDto
            {
                TechId = Convert.ToInt32(r["TechnicianID"]),
                TechName = r["FullName"]?.ToString(),
                Specialization = r["Specialization"]?.ToString(),
                CommissionPercent = r["CommissionPercent"] != DBNull.Value ? Convert.ToDecimal(r["CommissionPercent"]) : 0m,
                IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]),
                BranchId = r["BranchId"] != DBNull.Value ? Convert.ToInt32(r["BranchId"]) : 1,
                BranchName = r["BranchName"] != DBNull.Value ? r["BranchName"].ToString() : "الفرع الرئيسي"
            });
        }

        public DataTable GetTechniciansBySpecialization(string specialization)
        {
            string query = "SELECT TechnicianID, FullName, CommissionPercent, Specialization FROM Technicians WHERE IsActive = 1 AND Specialization = @spec ORDER BY FullName";
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@spec", specialization) };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public void DeleteTechnician(int technicianId)
        {
            if (IsTechnicianUsedInOrders(technicianId))
            {
                throw new InvalidOperationException("لا يمكن حذف الفني لوجود أوامر صيانة أو تسويات عمولات مسجلة باسمه.");
            }

            string query = "DELETE FROM Technicians WHERE TechnicianID = @id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", technicianId)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public int GetNextTechnicianId()
        {
            string query = "SELECT ISNULL(MAX(TechnicianID), 0) + 1 FROM Technicians";
            return _dbHelper.ExecuteScalar<int>(query, null);
        }
    }
}
