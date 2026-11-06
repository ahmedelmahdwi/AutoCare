using System;
using System.IO;
using System.Threading.Tasks;
using AutoCare.Application.Common;

namespace AutoCare.Infrastructure.Services
{
    public class FileLoggerService : ILoggerService
    {
        private readonly string _logDirectory;
        private static readonly object _lock = new object();

        public FileLoggerService(string logDirectory = null)
        {
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public string GetTodayLogFilePath()
        {
            string dateToken = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(_logDirectory, $"autocare_{dateToken}.log");
        }

        public void LogInfo(string message, string context = null)
        {
            WriteLog("INFO", message, context);
        }

        public void LogWarning(string message, string context = null)
        {
            WriteLog("WARN", message, context);
        }

        public void LogError(string message, Exception ex = null, string context = null)
        {
            string fullMsg = message;
            if (ex != null)
            {
                fullMsg += $"\n[Exception]: {ex.GetType().FullName}: {ex.Message}\n[StackTrace]: {ex.StackTrace}";
            }
            WriteLog("ERROR", fullMsg, context);
        }

        private void WriteLog(string level, string message, string context)
        {
            try
            {
                string logFile = GetTodayLogFilePath();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string ctxStr = string.IsNullOrWhiteSpace(context) ? "" : $" [{context}]";
                string line = $"[{timestamp}] [{level}]{ctxStr} {message}";

                lock (_lock)
                {
                    File.AppendAllText(logFile, line + Environment.NewLine);
                }
            }
            catch
            {
                // File logging should never crash the application
            }
        }
    }

    public class DatabaseBackupService
    {
        private readonly string _connectionString;
        private readonly string _defaultBackupDirectory;

        public DatabaseBackupService(string connectionString, string defaultBackupDirectory = null)
        {
            _connectionString = connectionString;
            _defaultBackupDirectory = defaultBackupDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(_defaultBackupDirectory))
            {
                Directory.CreateDirectory(_defaultBackupDirectory);
            }
        }

        public string GetDefaultBackupDirectory() => _defaultBackupDirectory;

        public string GenerateBackupFileName(string dbName = "AutoCareDB")
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"AutoCare_Backup_{dbName}_{timestamp}.bak";
        }
    }
}
