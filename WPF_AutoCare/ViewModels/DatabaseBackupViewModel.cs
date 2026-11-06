using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for DatabaseBackupView.
    /// Manages full database backup and restore operations with live progress reporting.
    /// Clean MVVM.
    /// </summary>
    public class DatabaseBackupViewModel : ViewModelBase
    {
        private readonly IDatabaseBackupService _backupService;
        private string _backupPath = "";
        private string _restorePath = "";
        private int _backupProgress = 0;
        private int _restoreProgress = 0;
        private string _backupStatus = "";
        private string _restoreStatus = "";
        private string _databaseName = "AutoCare";

        public string BackupPath
        {
            get => _backupPath;
            set => SetProperty(ref _backupPath, value);
        }

        public string RestorePath
        {
            get => _restorePath;
            set => SetProperty(ref _restorePath, value);
        }

        public int BackupProgress
        {
            get => _backupProgress;
            set => SetProperty(ref _backupProgress, value);
        }

        public int RestoreProgress
        {
            get => _restoreProgress;
            set => SetProperty(ref _restoreProgress, value);
        }

        public string BackupStatus
        {
            get => _backupStatus;
            set => SetProperty(ref _backupStatus, value);
        }

        public string RestoreStatus
        {
            get => _restoreStatus;
            set => SetProperty(ref _restoreStatus, value);
        }

        public string DatabaseName
        {
            get => _databaseName;
            set => SetProperty(ref _databaseName, value);
        }

        public ICommand StartBackupCommand { get; }
        public ICommand StartRestoreCommand { get; }

        public DatabaseBackupViewModel(IDatabaseBackupService backupService = null)
        {
            _backupService = backupService ?? Core.DI.AppContainer.Resolve<IDatabaseBackupService>();

            if (_backupService != null)
            {
                BackupPath = _backupService.GetDefaultBackupDirectory();
            }

            StartBackupCommand = new AsyncRelayCommand(ExecuteBackupAsync);
            StartRestoreCommand = new AsyncRelayCommand(ExecuteRestoreAsync);
        }

        private async Task ExecuteBackupAsync()
        {
            if (string.IsNullOrWhiteSpace(BackupPath))
            {
                BackupStatus = "يرجى تحديد مسار مجلد الحفظ.";
                return;
            }

            BackupProgress = 10;
            BackupStatus = "جارٍ إنشاء النسخة الاحتياطية...";

            try
            {
                string file = _backupService.GenerateBackupFileName(BackupPath);
                var res = await _backupService.CreateBackupAsync(file, (pct, msg) =>
                {
                    BackupProgress = pct;
                });

                if (res != null && res.Success)
                {
                    BackupProgress = 100;
                    BackupStatus = "تم إنشاء النسخة الاحتياطية بنجاح: " + Path.GetFileName(file);
                }
                else
                {
                    BackupStatus = "فشل النسخ: " + (res?.ErrorMessage ?? "خطأ غير معروف");
                }
            }
            catch (Exception ex)
            {
                BackupStatus = "خطأ: " + ex.Message;
            }
        }

        private async Task ExecuteRestoreAsync()
        {
            if (string.IsNullOrWhiteSpace(RestorePath) || !File.Exists(RestorePath))
            {
                RestoreStatus = "يرجى تحديد ملف نسخة احتياطية صالح بصيغة (.bak).";
                return;
            }

            RestoreProgress = 10;
            RestoreStatus = "جارٍ استعادة قاعدة البيانات...";

            try
            {
                var res = await _backupService.RestoreDatabaseAsync(RestorePath, (pct, msg) =>
                {
                    RestoreProgress = pct;
                });

                if (res != null && res.Success)
                {
                    RestoreProgress = 100;
                    RestoreStatus = "تمت استعادة قاعدة البيانات بنجاح!";
                }
                else
                {
                    RestoreStatus = "فشل الاستعادة: " + (res?.ErrorMessage ?? "خطأ غير معروف");
                }
            }
            catch (Exception ex)
            {
                RestoreStatus = "خطأ: " + ex.Message;
            }
        }
    }
}
