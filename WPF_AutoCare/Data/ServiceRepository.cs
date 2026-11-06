using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly IDbHelper _dbHelper;

        public ServiceRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetAllServices(string searchTerm = "")
        {
            // Phase 4: Use Full-Text CONTAINS() on ServiceName with LIKE fallback
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string ftsQuery = @"
                    SELECT ServiceCode, ServiceName, StandardPrice, Category
                    FROM Services
                    WHERE CONTAINS(ServiceName, @ftsTerm)
                    ORDER BY ServiceName";
                string likeQuery = @"
                    SELECT ServiceCode, ServiceName, StandardPrice, Category
                    FROM Services
                    WHERE ServiceName LIKE @likeTerm
                    ORDER BY ServiceName";
                string ftsTerm = '"' + searchTerm.Replace('"', ' ').Trim() + '*' + '"';
                string likeTerm = "%" + searchTerm + "%";
                try
                {
                    return _dbHelper.ExecuteDataTable(ftsQuery, new SqlParameter[] { new SqlParameter("@ftsTerm", ftsTerm) });
                }
                catch
                {
                    return _dbHelper.ExecuteDataTable(likeQuery, new SqlParameter[] { new SqlParameter("@likeTerm", likeTerm) });
                }
            }
            else
            {
                string query = "SELECT ServiceCode, ServiceName, StandardPrice, Category FROM Services ORDER BY ServiceName";
                return _dbHelper.ExecuteDataTable(query, null);
            }
        }

        public bool ServiceCodeExists(string serviceCode)
        {
            string query = "SELECT COUNT(1) FROM Services WHERE ServiceCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", serviceCode)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public void AddService(string serviceCode, string serviceName, decimal standardPrice, string category)
        {
            string query = "INSERT INTO Services (ServiceCode, ServiceName, StandardPrice, Category) VALUES (@code, @name, @price, @category)";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", serviceCode),
                new SqlParameter("@name", serviceName),
                new SqlParameter("@price", standardPrice),
                new SqlParameter("@category", category)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void UpdateService(string serviceCode, string serviceName, decimal standardPrice, string category)
        {
            string query = "UPDATE Services SET ServiceName = @name, StandardPrice = @price, Category = @category WHERE ServiceCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", serviceCode),
                new SqlParameter("@name", serviceName),
                new SqlParameter("@price", standardPrice),
                new SqlParameter("@category", category)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void DeleteService(string serviceCode)
        {
            string checkQuery = "SELECT COUNT(1) FROM RepairOrderService WHERE ServiceCode = @code";
            SqlParameter[] checkParams = new SqlParameter[]
            {
                new SqlParameter("@code", serviceCode)
            };
            int count = _dbHelper.ExecuteScalar<int>(checkQuery, checkParams);
            if (count > 0)
            {
                throw new InvalidOperationException("لا يمكن حذف هذه الخدمة لأنها مرتبطة بطلبات إصلاح قائمة.");
            }

            string deleteQuery = "DELETE FROM Services WHERE ServiceCode = @code";
            SqlParameter[] deleteParams = new SqlParameter[]
            {
                new SqlParameter("@code", serviceCode)
            };
            _dbHelper.ExecuteNonQuery(deleteQuery, deleteParams);
        }

        public DataRow GetServiceByCode(string serviceCode)
        {
            string query = "SELECT ServiceCode, ServiceName, StandardPrice, Category FROM Services WHERE ServiceCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", serviceCode)
            };
            DataTable dt = _dbHelper.ExecuteDataTable(query, parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0];
            }
            return null;
        }

        public C__project_Auto_Care.Core.DTOs.ServiceRowDto GetServiceDtoByCode(string serviceCode)
        {
            if (string.IsNullOrWhiteSpace(serviceCode)) return null;
            string query = "SELECT ServiceCode, ServiceName, StandardPrice, Category FROM Services WHERE ServiceCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", serviceCode.Trim())
            };
            return _dbHelper.ExecuteSingle(query, parameters, r => new C__project_Auto_Care.Core.DTOs.ServiceRowDto
            {
                ServiceCode = r["ServiceCode"]?.ToString(),
                ServiceName = r["ServiceName"]?.ToString(),
                StandardPrice = r["StandardPrice"] != DBNull.Value ? Convert.ToDecimal(r["StandardPrice"]).ToString("N2") : "0.00",
                Category = r["Category"]?.ToString()
            });
        }

        public List<C__project_Auto_Care.Core.DTOs.ServiceRowDto> GetAllServicesList(string searchTerm = "")
        {
            string query = "SELECT ServiceCode, ServiceName, StandardPrice, Category FROM Services";
            List<SqlParameter> listParams = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += " WHERE ServiceName LIKE @term OR ServiceCode LIKE @term";
                listParams.Add(new SqlParameter("@term", "%" + searchTerm.Trim() + "%"));
            }
            query += " ORDER BY ServiceName";
            return _dbHelper.ExecuteList(query, listParams.ToArray(), r => new C__project_Auto_Care.Core.DTOs.ServiceRowDto
            {
                ServiceCode = r["ServiceCode"]?.ToString(),
                ServiceName = r["ServiceName"]?.ToString(),
                StandardPrice = r["StandardPrice"] != DBNull.Value ? Convert.ToDecimal(r["StandardPrice"]).ToString("N2") : "0.00",
                Category = r["Category"]?.ToString()
            });
        }

        public List<string> GetDistinctCategories()
        {
            List<string> list = new List<string>();
            string query = "SELECT DISTINCT Category FROM Services WHERE Category IS NOT NULL AND Category <> ''";
            DataTable dt = _dbHelper.ExecuteDataTable(query, null);
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string cat = row["Category"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(cat) && !list.Contains(cat))
                    {
                        list.Add(cat);
                    }
                }
            }
            return list;
        }

        public DataTable GetTopServices(DateTime startDate, DateTime endDate, int topN)
        {
            string query = @"
                SELECT TOP (@topN)
                    s.ServiceCode,
                    s.ServiceName,
                    COUNT(*) AS [Count],
                    SUM(ros.UnitPrice) AS TotalRevenue
                FROM RepairOrderService ros
                INNER JOIN Services s ON ros.ServiceCode = s.ServiceCode
                INNER JOIN RepairOrders ro ON ros.RepairOrderNumber = ro.RepairOrderNumber
                WHERE ro.CompletionDate >= @startDate AND ro.CompletionDate <= @endDate
                GROUP BY s.ServiceCode, s.ServiceName
                ORDER BY COUNT(*) DESC";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@startDate", startDate),
                new SqlParameter("@endDate", endDate),
                new SqlParameter("@topN", topN)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public DataTable GetServicesByCategory(string category)
        {
            // Phase 3 fix: removed LTRIM(RTRIM(Category)) wrapper which prevents index use.
            // Data is cleaned at DB level by Phase3_DataCleanup.sql.
            string query = "SELECT ServiceCode, ServiceName, StandardPrice, Category FROM Services WHERE Category = @category";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@category", category.Trim())
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }
    }
}
