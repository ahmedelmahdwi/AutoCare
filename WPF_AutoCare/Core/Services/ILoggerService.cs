using System;
using System.IO;
using System.Text;

namespace C__project_Auto_Care.Core.Services
{
    /// <summary>
    /// Production-grade logging abstraction for dependency injection, testability,
    /// and structured daily-rotating diagnostic logging.
    /// </summary>
    public interface ILoggerService
    {
        void Info(string message, string source = "System");
        void Warn(string message, string source = "System");
        void Error(string message, Exception ex = null, string source = "Error");
        void Fatal(string message, Exception ex = null, string source = "Fatal");
        string GetTodayLogFilePath();
        void CleanOldLogs(int retentionDays = 30);
    }

    /// <summary>
    /// File-based logging service with thread-safe file writing,
    /// automatic daily date rotation, and configurable retention cleanup.
    /// </summary>
    public class FileLoggerService : ILoggerService
    {
        private readonly object _lock = new object();
        private readonly string _logsDirectory;

        public FileLoggerService() : this(null)
        {
        }

        public FileLoggerService(string logsDirectory)
        {
            if (string.IsNullOrWhiteSpace(logsDirectory))
            {
                _logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }
            else
            {
                _logsDirectory = logsDirectory;
            }

            try
            {
                if (!Directory.Exists(_logsDirectory))
                {
                    Directory.CreateDirectory(_logsDirectory);
                }
                CleanOldLogs(30);
            }
            catch
            {
                // Fall back to base directory if creation fails
                _logsDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public void Info(string message, string source = "System") =>
            WriteLog("INFO", source, message, null);

        public void Warn(string message, string source = "System") =>
            WriteLog("WARN", source, message, null);

        public void Error(string message, Exception ex = null, string source = "Error") =>
            WriteLog("ERROR", source, message, ex);

        public void Fatal(string message, Exception ex = null, string source = "Fatal") =>
            WriteLog("FATAL", source, message, ex);

        public string GetTodayLogFilePath()
        {
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(_logsDirectory, "autocare_" + dateStr + ".log");
        }

        private void WriteLog(string level, string source, string message, Exception ex)
        {
            try
            {
                lock (_lock)
                {
                    string logFile = GetTodayLogFilePath();
                    var sb = new StringBuilder();
                    sb.AppendFormat("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] [{2}] {3}",
                        DateTime.Now, level.PadRight(5), source ?? "System", message ?? "");
                    sb.AppendLine();

                    if (ex != null)
                    {
                        sb.AppendLine("Exception Details:");
                        sb.AppendLine(ex.ToString());
                    }

                    File.AppendAllText(logFile, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Never let logging failure crash the business application
            }
        }

        public void CleanOldLogs(int retentionDays = 30)
        {
            try
            {
                if (!Directory.Exists(_logsDirectory)) return;

                var files = Directory.GetFiles(_logsDirectory, "autocare_*.log");
                DateTime cutoff = DateTime.Now.AddDays(-retentionDays);

                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTime < cutoff)
                        {
                            info.Delete();
                        }
                    }
                    catch
                    {
                        // Ignore files locked by other processes
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
