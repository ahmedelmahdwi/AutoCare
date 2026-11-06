using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Core.Services
{
    public class BackupResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public long SizeBytes { get; set; }
        public long SizeMegabytes => SizeBytes / (1024 * 1024);
        public string ErrorMessage { get; set; }

        public static BackupResult Ok(string filePath, long sizeBytes) =>
            new BackupResult { Success = true, FilePath = filePath, SizeBytes = sizeBytes };

        public static BackupResult Fail(string error) =>
            new BackupResult { Success = false, ErrorMessage = error };
    }

    public class RestoreResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public static RestoreResult Ok() =>
            new RestoreResult { Success = true };

        public static RestoreResult Fail(string error) =>
            new RestoreResult { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Service contract for automated and manual database backups and restores.
    /// Completely abstracts SQL Server database engine commands away from UI screens.
    /// </summary>
    public interface IDatabaseBackupService
    {
        string GetDefaultBackupDirectory();
        string GenerateBackupFileName(string destinationFolder = null);
        Task<BackupResult> CreateBackupAsync(string targetFilePath, Action<int, string> progressCallback = null);
        Task<RestoreResult> RestoreDatabaseAsync(string backupFilePath, Action<int, string> progressCallback = null);
    }

    /// <summary>
    /// Production implementation of SQL Server Backup and Restore.
    /// Manages master database switching, single-user mode rollback, progress capture, and error translation.
    /// </summary>
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILoggerService _logger;

        public DatabaseBackupService(IDbHelper dbHelper, ILoggerService logger = null)
        {
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _logger = logger;
        }

        public string GetDefaultBackupDirectory()
        {
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            string folder = Path.Combine(systemDrive, "AutoCareBackups");

            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                return folder;
            }
            catch
            {
                try
                {
                    folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "AutoCareBackups");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    return folder;
                }
                catch
                {
                    return AppDomain.CurrentDomain.BaseDirectory;
                }
            }
        }

        public string GenerateBackupFileName(string destinationFolder = null)
        {
            string folder = string.IsNullOrWhiteSpace(destinationFolder) ? GetDefaultBackupDirectory() : destinationFolder;
            string baseFileName = "AutoCare_Backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
            string file = Path.Combine(folder, baseFileName + ".bak");

            int counter = 2;
            while (File.Exists(file))
            {
                file = Path.Combine(folder, baseFileName + "_" + counter + ".bak");
                counter++;
            }

            return file;
        }

        public async Task<BackupResult> CreateBackupAsync(string targetFilePath, Action<int, string> progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(targetFilePath))
                return BackupResult.Fail("مسار ملف النسخة الاحتياطية مطلوب.");

            string targetDirectory = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                try
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                catch (Exception ex)
                {
                    return BackupResult.Fail("لا يمكن إنشاء مجلد النسخة الاحتياطية: " + ex.Message);
                }
            }

            string dbName = _dbHelper.GetDatabaseName();
            string safeStagingDir = GetDefaultBackupDirectory();

            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new SqlConnection(_dbHelper.GetConnectionString()))
                    {
                        conn.Open();

                        if (IsUserIsolatedPath(targetFilePath))
                        {
                            // Target path is in user-isolated folder (e.g. MyDocuments, Desktop)
                            // Back up to safe staging directory first, then copy via C# process privileges
                            if (!Directory.Exists(safeStagingDir))
                                Directory.CreateDirectory(safeStagingDir);

                            string tempStagingFile = Path.Combine(safeStagingDir, "_staging_" + Guid.NewGuid().ToString("N") + ".bak");
                            try
                            {
                                ExecuteBackupSql(conn, dbName, tempStagingFile, progressCallback);
                                File.Copy(tempStagingFile, targetFilePath, true);
                            }
                            finally
                            {
                                if (File.Exists(tempStagingFile))
                                {
                                    try { File.Delete(tempStagingFile); } catch { }
                                }
                            }
                        }
                        else
                        {
                            // Target path is directly accessible by SQL Server service
                            try
                            {
                                ExecuteBackupSql(conn, dbName, targetFilePath, progressCallback);
                            }
                            catch (SqlException ex) when (IsAccessDeniedError(ex))
                            {
                                // Defensive fallback if unexpected access denied occurs
                                if (!Directory.Exists(safeStagingDir))
                                    Directory.CreateDirectory(safeStagingDir);

                                string tempStagingFile = Path.Combine(safeStagingDir, "_staging_" + Guid.NewGuid().ToString("N") + ".bak");
                                try
                                {
                                    ExecuteBackupSql(conn, dbName, tempStagingFile, progressCallback);
                                    File.Copy(tempStagingFile, targetFilePath, true);
                                }
                                finally
                                {
                                    if (File.Exists(tempStagingFile))
                                    {
                                        try { File.Delete(tempStagingFile); } catch { }
                                    }
                                }
                            }
                        }
                    }
                });

                long bytes = 0;
                if (File.Exists(targetFilePath))
                {
                    try { bytes = new FileInfo(targetFilePath).Length; } catch { }
                }
                _logger?.Info($"Database backup created successfully at: {targetFilePath} ({bytes / (1024 * 1024)} MB)", "Backup");

                // Auto-cleanup rolling retention (keep last 14 days)
                TryCleanOldBackups(targetDirectory, 14);
                if (!string.Equals(targetDirectory, safeStagingDir, StringComparison.OrdinalIgnoreCase))
                {
                    TryCleanOldBackups(safeStagingDir, 14);
                }

                return BackupResult.Ok(targetFilePath, bytes);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Database backup failed for path: {targetFilePath}", ex, "Backup");
                return BackupResult.Fail(TranslateBackupError(ex, targetFilePath));
            }
        }

        public async Task<RestoreResult> RestoreDatabaseAsync(string backupFilePath, Action<int, string> progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
                return RestoreResult.Fail("ملف النسخة الاحتياطية المحدد غير موجود.");

            string dbName = _dbHelper.GetDatabaseName();
            string masterConnStr = _dbHelper.GetMasterConnectionString();
            string safeStagingDir = GetDefaultBackupDirectory();

            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new SqlConnection(masterConnStr))
                    {
                        conn.Open();

                        string actualRestoreFile = backupFilePath;
                        string tempStagingFile = null;

                        try
                        {
                            // If file is in user-isolated folder, stage it to public folder so SQL Server service can read it
                            if (IsUserIsolatedPath(backupFilePath))
                            {
                                if (!Directory.Exists(safeStagingDir))
                                    Directory.CreateDirectory(safeStagingDir);

                                tempStagingFile = Path.Combine(safeStagingDir, "_restore_" + Guid.NewGuid().ToString("N") + ".bak");
                                File.Copy(backupFilePath, tempStagingFile, true);
                                actualRestoreFile = tempStagingFile;
                            }

                            ExecuteRestoreSql(conn, dbName, actualRestoreFile, progressCallback);
                        }
                        catch (SqlException ex) when (tempStagingFile == null && IsAccessDeniedError(ex))
                        {
                            // Dynamic fallback to staging if direct restore threw Access Denied
                            if (!Directory.Exists(safeStagingDir))
                                Directory.CreateDirectory(safeStagingDir);

                            tempStagingFile = Path.Combine(safeStagingDir, "_restore_" + Guid.NewGuid().ToString("N") + ".bak");
                            File.Copy(backupFilePath, tempStagingFile, true);
                            ExecuteRestoreSql(conn, dbName, tempStagingFile, progressCallback);
                        }
                        finally
                        {
                            if (tempStagingFile != null && File.Exists(tempStagingFile))
                            {
                                try { File.Delete(tempStagingFile); } catch { }
                            }
                        }
                    }
                });

                _logger?.Info($"Database restored successfully from: {backupFilePath}", "Restore");
                return RestoreResult.Ok();
            }
            catch (Exception ex)
            {
                _logger?.Error($"Database restore failed from: {backupFilePath}", ex, "Restore");

                // Ensure database is reset to MULTI_USER if locked
                try
                {
                    using (var conn = new SqlConnection(masterConnStr))
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand($"ALTER DATABASE [{dbName}] SET MULTI_USER", conn))
                        {
                            cmd.CommandTimeout = 30;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch { }

                return RestoreResult.Fail("فشلت عملية استعادة النسخة الاحتياطية: " + ex.Message);
            }
        }

        private static void ExecuteBackupSql(SqlConnection conn, string dbName, string filePath, Action<int, string> progressCallback)
        {
            if (progressCallback != null)
            {
                conn.FireInfoMessageEventOnUserErrors = false;
                conn.InfoMessage += (s, ev) =>
                {
                    int percent = ParsePercent(ev.Message);
                    if (percent >= 0)
                        progressCallback(percent, ev.Message);
                };
            }

            using (var cmd = new SqlCommand(
                $"BACKUP DATABASE [{dbName}] TO DISK = @path WITH INIT, STATS = 10", conn))
            {
                cmd.CommandTimeout = 0;
                cmd.Parameters.AddWithValue("@path", filePath);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ExecuteRestoreSql(SqlConnection conn, string dbName, string filePath, Action<int, string> progressCallback)
        {
            if (progressCallback != null)
            {
                conn.FireInfoMessageEventOnUserErrors = false;
                conn.InfoMessage += (s, ev) =>
                {
                    int percent = ParsePercent(ev.Message);
                    if (percent >= 0)
                        progressCallback(percent, ev.Message);
                };
            }

            // 1. Kick existing connections
            string killQuery = $@"
                ALTER DATABASE [{dbName}] 
                SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
            using (var cmd = new SqlCommand(killQuery, conn))
            {
                cmd.CommandTimeout = 60;
                cmd.ExecuteNonQuery();
            }

            // 2. Restore database
            string restoreQuery = $@"
                RESTORE DATABASE [{dbName}] 
                FROM DISK = @path 
                WITH REPLACE, STATS = 10";
            using (var cmd = new SqlCommand(restoreQuery, conn))
            {
                cmd.CommandTimeout = 0;
                cmd.Parameters.AddWithValue("@path", filePath);
                cmd.ExecuteNonQuery();
            }

            // 3. Return to multi-user mode
            string multiQuery = $@"
                ALTER DATABASE [{dbName}] 
                SET MULTI_USER";
            using (var cmd = new SqlCommand(multiQuery, conn))
            {
                cmd.CommandTimeout = 60;
                cmd.ExecuteNonQuery();
            }
        }

        private static bool IsAccessDeniedError(SqlException ex)
        {
            if (ex == null) return false;
            foreach (SqlError err in ex.Errors)
            {
                if (err.Number == 3201 || err.Number == 5) return true;
                string msg = err.Message ?? "";
                if (msg.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (msg.IndexOf("Operating system error 5", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static bool IsUserIsolatedPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            try
            {
                string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(userDir) && filePath.StartsWith(userDir, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }
            return false;
        }

        private static void TryCleanOldBackups(string directory, int retentionDays)
        {
            try
            {
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
                var dirInfo = new DirectoryInfo(directory);
                DateTime cutoff = DateTime.Now.AddDays(-retentionDays);
                foreach (var file in dirInfo.GetFiles("AutoCare_Backup_*.bak"))
                {
                    if (file.CreationTime < cutoff && file.LastWriteTime < cutoff)
                    {
                        try { file.Delete(); } catch { }
                    }
                }
            }
            catch { }
        }

        private static int ParsePercent(string message)
        {
            if (string.IsNullOrEmpty(message)) return -1;
            var match = Regex.Match(message, @"(\d+)\s+percent\s+processed", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int pct))
                return pct;
            return -1;
        }

        private static string TranslateBackupError(Exception ex, string file)
        {
            if (ex is SqlException sqlEx)
            {
                foreach (SqlError err in sqlEx.Errors)
                {
                    if (err.Number == 3201 || err.Number == 5 || err.Number == 1203 || err.Number == 1205)
                    {
                        return "لا يمكن كتابة ملف النسخة الاحتياطية على المجلد المحدد.\n\n" + file +
                               "\n\nتأكد من أن حساب خدمة SQL Server لديه صلاحية الكتابة (Write) على هذا المجلد." +
                               "\n(تفاصيل: " + sqlEx.Message + ")";
                    }
                    if (err.Number == 9002 || err.Number == 1105)
                        return "مساحة القرص غير كافية لحفظ النسخة الاحتياطية.\n\n" + sqlEx.Message;
                }
            }
            return "فشلت عملية النسخ الاحتياطي: " + ex.Message;
        }
    }
}
