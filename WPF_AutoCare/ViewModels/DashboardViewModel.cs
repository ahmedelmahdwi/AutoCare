using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Input;
using AutoCare.Application.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.ViewModels.Commands;
using DashRepo = AutoCare.Infrastructure.Repositories.DashboardRepository;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for DashboardScreen.
    /// Fetches live operational statistics, financial KPIs, and interactive vector chart data.
    /// Clean MVVM & Decoupled Architecture.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private int _lowStockCount;
        private int _pendingCount;
        private int _completedCount;
        private int _inProgressCount;
        private decimal _todaySales;
        private decimal _todaySpareCost;
        private decimal _netProfit;
        private decimal _maxMonthlyRevenue;
        private int _totalOrdersInDistribution;
        private string _lastUpdate = "";
        private bool _isBusy;

        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }

        public int CompletedCount
        {
            get => _completedCount;
            set => SetProperty(ref _completedCount, value);
        }

        public int InProgressCount
        {
            get => _inProgressCount;
            set => SetProperty(ref _inProgressCount, value);
        }

        public string TodaySalesFormatted => $"{_todaySales:N2} د.ل";
        public string NetProfitFormatted => $"{_netProfit:N2} د.ل";

        public decimal MaxMonthlyRevenue
        {
            get => _maxMonthlyRevenue;
            set
            {
                if (SetProperty(ref _maxMonthlyRevenue, value))
                {
                    OnPropertyChanged(nameof(MaxMonthlyRevenueFormatted));
                }
            }
        }

        public string MaxMonthlyRevenueFormatted => $"{_maxMonthlyRevenue:N2} د.ل";

        public int TotalOrdersInDistribution
        {
            get => _totalOrdersInDistribution;
            set => SetProperty(ref _totalOrdersInDistribution, value);
        }

        public string LastUpdate
        {
            get => _lastUpdate;
            set => SetProperty(ref _lastUpdate, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // Interactive Vector Chart Collections
        public ObservableCollection<MonthlyRevenueDto> MonthlyRevenues { get; } = new ObservableCollection<MonthlyRevenueDto>();
        public ObservableCollection<OrderStatusDistributionDto> StatusDistributions { get; } = new ObservableCollection<OrderStatusDistributionDto>();
        public ObservableCollection<TopServiceDto> TopServices { get; } = new ObservableCollection<TopServiceDto>();

        public ICommand RefreshCommand { get; }
        public ICommand NavigateCommand { get; }
        public event Action<string> RequestNavigate;

        public DashboardViewModel()
        {
            RefreshCommand = new RelayCommand(LoadData);
            NavigateCommand = new RelayCommand<string>(dest =>
            {
                if (!string.IsNullOrWhiteSpace(dest))
                {
                    RequestNavigate?.Invoke(dest);
                }
            });

            if (AppServiceLocator.BranchContext != null)
            {
                AppServiceLocator.BranchContext.BranchChanged += () => LoadData();
            }

            LoadData();
        }

        public void LoadData()
        {
            try
            {
                IsBusy = true;
                var db = AppServiceLocator.DbHelper;
                if (db == null) return;

                int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;

                var roRepo = new RepairOrderRepository(db);
                var spareRepo = new SparePartRepository(db);
                var invoiceRepo = new InvoiceRepository(db);

                PendingCount = roRepo.GetOrdersCountByStatus("Pending");
                InProgressCount = roRepo.GetOrdersCountByStatus("InProgress");
                CompletedCount = roRepo.GetOrdersCountByStatus("Completed");
                LowStockCount = spareRepo.GetLowStockCount();

                _todaySales = invoiceRepo.GetTodaySales();
                _todaySpareCost = invoiceRepo.GetTodaySparePartsCost();
                _netProfit = _todaySales - _todaySpareCost;

                OnPropertyChanged(nameof(TodaySalesFormatted));
                OnPropertyChanged(nameof(NetProfitFormatted));

                // Load Interactive Vector Charts via Clean Repository
                string connStr = db.GetConnectionString();
                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    using (var conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        var dashRepo = new DashRepo(conn, () => null);

                        var revenues = dashRepo.GetMonthlyRevenues(6, currentBranch);
                        var statuses = dashRepo.GetOrderStatusDistribution(currentBranch);
                        var services = dashRepo.GetTopServices(5, currentBranch);

                        MonthlyRevenues.Clear();
                        decimal maxRev = 0m;
                        foreach (var r in revenues)
                        {
                            MonthlyRevenues.Add(r);
                            if (r.TotalRevenue > maxRev) maxRev = r.TotalRevenue;
                        }
                        MaxMonthlyRevenue = maxRev;

                        StatusDistributions.Clear();
                        int totalOrders = 0;
                        foreach (var s in statuses)
                        {
                            StatusDistributions.Add(s);
                            totalOrders += s.Count;
                        }
                        TotalOrdersInDistribution = totalOrders;

                        TopServices.Clear();
                        foreach (var srv in services)
                        {
                            TopServices.Add(srv);
                        }
                    }
                }

                LastUpdate = "آخر تحديث: " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                LastUpdate = "خطأ في التحديث: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
