using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Data
{
    public class AuditRepository : IAuditRepository
    {
        private readonly IDbHelper _dbHelper;
        private static bool _tableInitialized = false;
        private static readonly object _initLock = new object();

        public AuditRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            EnsureTableExists();
        }

        private void EnsureTableExists()
        {
            if (_tableInitialized) return;
            lock (_initLock)
            {
                if (_tableInitialized) return;
                try
                {
                    string script = @"
                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs')
                        BEGIN
                            CREATE TABLE AuditLogs
                            (
                                LogID       BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                Timestamp   DATETIME NOT NULL CONSTRAINT DF_AuditLogs_Timestamp DEFAULT GETDATE(),
                                Username    NVARCHAR(50) NULL,
                                Action      NVARCHAR(40) NOT NULL,
                                TargetTable NVARCHAR(50) NULL,
                                RecordKey   NVARCHAR(50) NULL,
                                Details     NVARCHAR(MAX) NULL
                            );
                            CREATE NONCLUSTERED INDEX IX_AuditLogs_Timestamp ON AuditLogs (Timestamp DESC);
                            CREATE NONCLUSTERED INDEX IX_AuditLogs_Action ON AuditLogs (Action ASC);
                            CREATE NONCLUSTERED INDEX IX_AuditLogs_Username ON AuditLogs (Username ASC);
                        END";
                    _dbHelper.ExecuteNonQuery(script, null);
                    _tableInitialized = true;
                }
                catch
                {
                    // If DB is offline or restricted, will retry on next call
                }
            }
        }

        public void Log(string action, string targetTable, string recordKey, string details, string username = null)
        {
            try
            {
                EnsureTableExists();
                string user = !string.IsNullOrEmpty(username) ? username : (UserSession.Username ?? "System");
                string query = @"
                    INSERT INTO AuditLogs (Timestamp, Username, Action, TargetTable, RecordKey, Details)
                    VALUES (GETDATE(), @user, @action, @target, @key, @details)";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@user", user ?? (object)DBNull.Value),
                    new SqlParameter("@action", action ?? "ACTION"),
                    new SqlParameter("@target", string.IsNullOrEmpty(targetTable) ? (object)DBNull.Value : targetTable),
                    new SqlParameter("@key", string.IsNullOrEmpty(recordKey) ? (object)DBNull.Value : recordKey),
                    new SqlParameter("@details", string.IsNullOrEmpty(details) ? (object)DBNull.Value : details)
                };

                _dbHelper.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Audit Log Failed: " + ex.Message);
            }
        }

        public DataTable GetLogs(DateTime? fromDate = null, DateTime? toDate = null, string actionFilter = "", string search = "")
        {
            EnsureTableExists();
            string query = @"
                SELECT LogID, Timestamp, Username, Action, TargetTable, RecordKey, Details
                FROM AuditLogs
                WHERE 1 = 1";

            List<SqlParameter> listParams = new List<SqlParameter>();

            if (fromDate.HasValue)
            {
                query += " AND Timestamp >= @from";
                listParams.Add(new SqlParameter("@from", fromDate.Value.Date));
            }

            if (toDate.HasValue)
            {
                query += " AND Timestamp < @to";
                listParams.Add(new SqlParameter("@to", toDate.Value.Date.AddDays(1)));
            }

            if (!string.IsNullOrEmpty(actionFilter) && actionFilter != "الكل")
            {
                query += " AND Action = @action";
                listParams.Add(new SqlParameter("@action", actionFilter));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query += " AND (Username LIKE @search OR RecordKey LIKE @search OR Details LIKE @search OR TargetTable LIKE @search)";
                listParams.Add(new SqlParameter("@search", "%" + search.Trim() + "%"));
            }

            query += " ORDER BY Timestamp DESC";

            return _dbHelper.ExecuteDataTable(query, listParams.ToArray());
        }
    }
}
