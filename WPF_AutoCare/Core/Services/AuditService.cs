using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Core.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepo;

        public AuditService(IAuditRepository auditRepo)
        {
            _auditRepo = auditRepo ?? throw new ArgumentNullException(nameof(auditRepo));
        }

        public AuditQueryResultDto GetAuditLogs(DateTime? from, DateTime? to, string action, string search)
        {
            var result = new AuditQueryResultDto();
            try
            {
                DataTable dt = _auditRepo.GetLogs(from, to, action, search);
                int todayCount = 0;
                DateTime today = DateTime.Today;

                if (dt != null && dt.Rows.Count > 0)
                {
                    result.TotalCount = dt.Rows.Count;

                    DataRow firstRow = dt.Rows[0];
                    if (firstRow["Timestamp"] != DBNull.Value)
                    {
                        DateTime lastT = Convert.ToDateTime(firstRow["Timestamp"]);
                        result.LastLogTime = lastT.ToString("HH:mm:ss");
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        DateTime ts = row["Timestamp"] != DBNull.Value ? Convert.ToDateTime(row["Timestamp"]) : DateTime.MinValue;
                        if (ts.Date == today) todayCount++;

                        result.Logs.Add(new AuditLogRowDto
                        {
                            Timestamp = ts != DateTime.MinValue ? ts.ToString("yyyy-MM-dd HH:mm:ss") : "-",
                            Username = row["Username"]?.ToString() ?? "-",
                            Action = row["Action"]?.ToString() ?? "-",
                            TargetTable = row["TargetTable"]?.ToString() ?? "-",
                            RecordKey = row["RecordKey"]?.ToString() ?? "-",
                            Details = row["Details"]?.ToString() ?? "-"
                        });
                    }
                }

                result.TodayCount = todayCount;
            }
            catch (Exception ex)
            {
                AppLogger.Error("AuditService.GetAuditLogs failed", ex);
            }
            return result;
        }

        public void Log(string action, string targetTable, string recordKey, string details, string username = null)
        {
            try
            {
                _auditRepo.Log(action, targetTable, recordKey, details, username);
            }
            catch (Exception ex)
            {
                AppLogger.Error("AuditService.Log failed", ex);
            }
        }
    }
}
