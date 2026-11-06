using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for AuditLogView.
    /// Manages system activity logs with asynchronous fetching and filtering.
    /// Clean MVVM.
    /// </summary>
    public class AuditLogViewModel : ViewModelBase
    {
        private readonly IAuditService _auditService;

        private DateTime? _startDate;
        private DateTime? _endDate;
        private string _selectedActionFilter = "";
        private string _searchText = "";
        private int _totalLogsCount;
        private int _todayLogsCount;
        private string _lastLogTime = "-";

        public ObservableCollection<AuditLogRowDto> Logs { get; } = new ObservableCollection<AuditLogRowDto>();

        public ICommand RefreshCommand { get; }

        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        public string SelectedActionFilter
        {
            get => _selectedActionFilter;
            set
            {
                if (SetProperty(ref _selectedActionFilter, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        public int TotalLogsCount
        {
            get => _totalLogsCount;
            set => SetProperty(ref _totalLogsCount, value);
        }

        public int TodayLogsCount
        {
            get => _todayLogsCount;
            set => SetProperty(ref _todayLogsCount, value);
        }

        public string LastLogTime
        {
            get => _lastLogTime;
            set => SetProperty(ref _lastLogTime, value);
        }

        public AuditLogViewModel(IAuditService auditService = null)
        {
            _auditService = auditService ?? AppServiceLocator.AuditService;
            _startDate = DateTime.Today.AddDays(-30);
            _endDate = DateTime.Today;
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            DateTime? start = _startDate;
            DateTime? end = _endDate;
            string action = (_selectedActionFilter == "الكل") ? "" : _selectedActionFilter;
            string search = _searchText?.Trim() ?? "";

            await RunAsync(async () =>
            {
                var result = await Task.Run(() => _auditService.GetAuditLogs(start, end, action, search));
                RunOnUIThread(() =>
                {
                    Logs.Clear();
                    if (result?.Logs != null)
                    {
                        foreach (var log in result.Logs)
                        {
                            Logs.Add(log);
                        }
                    }

                    if (result != null)
                    {
                        TotalLogsCount = result.TotalCount;
                        TodayLogsCount = result.TodayCount;
                        LastLogTime = result.LastLogTime;
                    }
                });
            }, "جاري جلب سجلات التدقيق...");
        }

        public void LoadData()
        {
            try
            {
                string action = (_selectedActionFilter == "الكل") ? "" : _selectedActionFilter;
                var result = _auditService.GetAuditLogs(_startDate, _endDate, action, _searchText);

                Logs.Clear();
                if (result?.Logs != null)
                {
                    foreach (var log in result.Logs)
                    {
                        Logs.Add(log);
                    }
                }

                if (result != null)
                {
                    TotalLogsCount = result.TotalCount;
                    TodayLogsCount = result.TodayCount;
                    LastLogTime = result.LastLogTime;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("AuditLogViewModel.LoadData failed", ex);
            }
        }
    }
}
