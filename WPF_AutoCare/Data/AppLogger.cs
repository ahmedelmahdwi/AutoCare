using System;
using System.IO;
using System.Text;

namespace C__project_Auto_Care.Data
{
    /// <summary>
    /// Production-grade diagnostic logging system with automatic daily log rotation
    /// and 30-day retention cleanup.
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _lock = new object();
        private static string _logsDirectory;

        static AppLogger()
        {
            try
            {
                _logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(_logsDirectory))
                {
                    Directory.CreateDirectory(_logsDirectory);
                }

                CleanOldLogs(30);
            }
            catch
            {
                _logsDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public static void Info(string message, string source = "System")
        {
            WriteLog("INFO", source, message, null);
        }

        public static void Warn(string message, string source = "System")
        {
            WriteLog("WARN", source, message, null);
        }

        public static void Error(string message, Exception ex = null, string source = "Error")
        {
            WriteLog("ERROR", source, message, ex);
        }

        public static void Fatal(string message, Exception ex = null, string source = "Fatal")
        {
            WriteLog("FATAL", source, message, ex);
        }

        private static void WriteLog(string level, string source, string message, Exception ex)
        {
            try
            {
                lock (_lock)
                {
                    string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                    string logFile = Path.Combine(_logsDirectory, "autocare_" + dateStr + ".log");

                    var sb = new StringBuilder();
                    sb.AppendFormat("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] [{2}] {3}",
                        DateTime.Now, level.PadRight(5), source, message ?? "");
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

        private static void CleanOldLogs(int retentionDays)
        {
            try
            {
                if (!Directory.Exists(_logsDirectory)) return;

                var files = Directory.GetFiles(_logsDirectory, "autocare_*.log");
                DateTime cutoff = DateTime.Now.AddDays(-retentionDays);

                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (fi.LastWriteTime < cutoff)
                    {
                        try { fi.Delete(); } catch { }
                    }
                }
            }
            catch { }
        }
    }
}
