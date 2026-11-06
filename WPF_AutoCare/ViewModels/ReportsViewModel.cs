using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Reports;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// Master ViewModel driving the comprehensive 3-tab ReportsView (Financial, Inventory, Top Services/Parts).
    /// Pure Clean MVVM, backed by live SQL queries and direct printing/export integration.
    /// </summary>
    public class ReportsViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IInventoryService _inventoryService;
        private readonly IRepairOrderService _repairOrderService;

        #region Tab 1: Financial Report Fields
        private DateTime _financialStartDate;
        private DateTime _financialEndDate;
        private int _financialInvoiceCount;
        private decimal _financialTotalServices;
        private decimal _financialTotalParts;
        private decimal _financialGrandTotal;
        #endregion

        #region Tab 2: Inventory Report Fields
        private string _inventorySearchText = "";
        private bool _inventoryLowStockOnly = false;
        private int _inventoryTotalTypes;
        private int _inventoryTotalQuantity;
        private int _inventoryLowStockCount;
        private decimal _inventoryTotalValue;
        #endregion

        #region Tab 3: Top Services & Parts Fields
        private DateTime _topStartDate;
        private DateTime _topEndDate;
        private int _topN = 10;
        private string _topCategory = "الكل";
        private int _topTotalServicesCount;
        private decimal _topTotalServicesRevenue;
        private string _mostRequestedServiceName = "لا يوجد";
        private string _mostRequestedPartName = "لا يوجد";
        #endregion

        #region Collections
        public ObservableCollection<FinancialReportItemDto> FinancialInvoices { get; } =
            new ObservableCollection<FinancialReportItemDto>();

        public ObservableCollection<InventoryReportItemDto> InventoryItems { get; } =
            new ObservableCollection<InventoryReportItemDto>();

        public ObservableCollection<TopServiceItemDto> TopServices { get; } =
            new ObservableCollection<TopServiceItemDto>();

        public ObservableCollection<TopPartItemDto> TopSpareParts { get; } =
            new ObservableCollection<TopPartItemDto>();
        #endregion

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        #region Commands
        public ICommand GenerateFinancialReportCommand { get; }
        public ICommand PrintFinancialReportCommand { get; }
        public ICommand ExportFinancialPdfCommand { get; }
        public ICommand ExportFinancialExcelCommand { get; }
        public ICommand GenerateInventoryReportCommand { get; }
        public ICommand PrintInventoryReportCommand { get; }
        public ICommand ExportInventoryPdfCommand { get; }
        public ICommand ExportInventoryExcelCommand { get; }
        public ICommand GenerateTopReportCommand { get; }
        public ICommand PrintTopReportCommand { get; }
        public ICommand ExportTopPdfCommand { get; }
        public ICommand ExportTopExcelCommand { get; }
        #endregion

        public ReportsViewModel(
            IInvoiceService invoiceService = null,
            IInventoryService inventoryService = null,
            IRepairOrderService repairOrderService = null)
        {
            _invoiceService = invoiceService ?? AppServiceLocator.InvoiceService;
            _inventoryService = inventoryService ?? AppServiceLocator.InventoryService;
            _repairOrderService = repairOrderService ?? AppServiceLocator.RepairOrderService;

            DateTime now = DateTime.Today;
            _financialStartDate = new DateTime(now.Year, now.Month, 1);
            _financialEndDate = now;

            _topStartDate = now.AddDays(-30);
            _topEndDate = now;

            GenerateFinancialReportCommand = new AsyncRelayCommand(LoadFinancialReportAsync);
            PrintFinancialReportCommand = new RelayCommand(PrintFinancialReport);
            ExportFinancialPdfCommand = new RelayCommand(ExportFinancialPdf);
            ExportFinancialExcelCommand = new RelayCommand(ExportFinancialExcel);
            GenerateInventoryReportCommand = new AsyncRelayCommand(LoadInventoryReportAsync);
            PrintInventoryReportCommand = new RelayCommand(PrintInventoryReport);
            ExportInventoryPdfCommand = new RelayCommand(ExportInventoryPdf);
            ExportInventoryExcelCommand = new RelayCommand(ExportInventoryExcel);
            GenerateTopReportCommand = new AsyncRelayCommand(LoadTopReportAsync);
            PrintTopReportCommand = new RelayCommand(PrintTopReport);
            ExportTopPdfCommand = new RelayCommand(ExportTopPdf);
            ExportTopExcelCommand = new RelayCommand(ExportTopExcel);

            // Initial auto-load
            LoadFinancialReport();
            LoadInventoryReport();
            LoadTopReport();
        }

        #region Tab 1 Properties
        public DateTime FinancialStartDate
        {
            get => _financialStartDate;
            set => SetProperty(ref _financialStartDate, value);
        }

        public DateTime FinancialEndDate
        {
            get => _financialEndDate;
            set => SetProperty(ref _financialEndDate, value);
        }

        public int FinancialInvoiceCount
        {
            get => _financialInvoiceCount;
            set => SetProperty(ref _financialInvoiceCount, value);
        }

        public decimal FinancialTotalServices
        {
            get => _financialTotalServices;
            set => SetProperty(ref _financialTotalServices, value);
        }

        public decimal FinancialTotalParts
        {
            get => _financialTotalParts;
            set => SetProperty(ref _financialTotalParts, value);
        }

        public decimal FinancialGrandTotal
        {
            get => _financialGrandTotal;
            set => SetProperty(ref _financialGrandTotal, value);
        }
        #endregion

        #region Tab 2 Properties
        public string InventorySearchText
        {
            get => _inventorySearchText;
            set => SetProperty(ref _inventorySearchText, value);
        }

        public bool InventoryLowStockOnly
        {
            get => _inventoryLowStockOnly;
            set
            {
                if (SetProperty(ref _inventoryLowStockOnly, value))
                {
                    _ = LoadInventoryReportAsync();
                }
            }
        }

        public int InventoryTotalTypes
        {
            get => _inventoryTotalTypes;
            set => SetProperty(ref _inventoryTotalTypes, value);
        }

        public int InventoryTotalQuantity
        {
            get => _inventoryTotalQuantity;
            set => SetProperty(ref _inventoryTotalQuantity, value);
        }

        public int InventoryLowStockCount
        {
            get => _inventoryLowStockCount;
            set => SetProperty(ref _inventoryLowStockCount, value);
        }

        public decimal InventoryTotalValue
        {
            get => _inventoryTotalValue;
            set => SetProperty(ref _inventoryTotalValue, value);
        }
        #endregion

        #region Tab 3 Properties
        public DateTime TopStartDate
        {
            get => _topStartDate;
            set => SetProperty(ref _topStartDate, value);
        }

        public DateTime TopEndDate
        {
            get => _topEndDate;
            set => SetProperty(ref _topEndDate, value);
        }

        public int TopN
        {
            get => _topN;
            set => SetProperty(ref _topN, value);
        }

        public string TopCategory
        {
            get => _topCategory;
            set
            {
                if (SetProperty(ref _topCategory, value))
                {
                    _ = LoadTopReportAsync();
                }
            }
        }

        public int TopTotalServicesCount
        {
            get => _topTotalServicesCount;
            set => SetProperty(ref _topTotalServicesCount, value);
        }

        public decimal TopTotalServicesRevenue
        {
            get => _topTotalServicesRevenue;
            set => SetProperty(ref _topTotalServicesRevenue, value);
        }

        public string MostRequestedServiceName
        {
            get => _mostRequestedServiceName;
            set => SetProperty(ref _mostRequestedServiceName, value);
        }

        public string MostRequestedPartName
        {
            get => _mostRequestedPartName;
            set => SetProperty(ref _mostRequestedPartName, value);
        }
        #endregion

        #region Loading Logic
        public void LoadFinancialReport()
        {
            try
            {
                var summary = _invoiceService.GetFinancialReport(_financialStartDate, _financialEndDate);
                FinancialInvoices.Clear();
                if (summary != null && summary.Invoices != null)
                {
                    foreach (var inv in summary.Invoices)
                    {
                        FinancialInvoices.Add(inv);
                    }
                    FinancialInvoiceCount = summary.InvoiceCount;
                    FinancialTotalServices = summary.TotalServices;
                    FinancialTotalParts = summary.TotalParts;
                    FinancialGrandTotal = summary.GrandTotal;
                }
                else
                {
                    FinancialInvoiceCount = 0;
                    FinancialTotalServices = 0;
                    FinancialTotalParts = 0;
                    FinancialGrandTotal = 0;
                }
            }
            catch { }
        }

        public async Task LoadFinancialReportAsync()
        {
            await RunAsync(async () =>
            {
                var summary = await Task.Run(() => _invoiceService.GetFinancialReport(_financialStartDate, _financialEndDate));
                RunOnUIThread(() =>
                {
                    FinancialInvoices.Clear();
                    if (summary != null && summary.Invoices != null)
                    {
                        foreach (var inv in summary.Invoices)
                        {
                            FinancialInvoices.Add(inv);
                        }
                        FinancialInvoiceCount = summary.InvoiceCount;
                        FinancialTotalServices = summary.TotalServices;
                        FinancialTotalParts = summary.TotalParts;
                        FinancialGrandTotal = summary.GrandTotal;
                    }
                    else
                    {
                        FinancialInvoiceCount = 0;
                        FinancialTotalServices = 0;
                        FinancialTotalParts = 0;
                        FinancialGrandTotal = 0;
                    }
                });
            }, "جاري إنشاء التقرير المالي...");
        }

        public void LoadInventoryReport()
        {
            try
            {
                var report = _inventoryService.GetInventoryReport(_inventorySearchText, _inventoryLowStockOnly);
                InventoryItems.Clear();
                if (report != null && report.Items != null)
                {
                    foreach (var item in report.Items)
                    {
                        InventoryItems.Add(item);
                    }
                    InventoryTotalTypes = report.TotalTypes;
                    InventoryTotalQuantity = report.TotalQuantity;
                    InventoryLowStockCount = report.LowStockCount;
                    InventoryTotalValue = report.TotalInventoryValue;
                }
                else
                {
                    InventoryTotalTypes = 0;
                    InventoryTotalQuantity = 0;
                    InventoryLowStockCount = 0;
                    InventoryTotalValue = 0;
                }
            }
            catch { }
        }

        public async Task LoadInventoryReportAsync()
        {
            await RunAsync(async () =>
            {
                var report = await Task.Run(() => _inventoryService.GetInventoryReport(_inventorySearchText, _inventoryLowStockOnly));
                RunOnUIThread(() =>
                {
                    InventoryItems.Clear();
                    if (report != null && report.Items != null)
                    {
                        foreach (var item in report.Items)
                        {
                            InventoryItems.Add(item);
                        }
                        InventoryTotalTypes = report.TotalTypes;
                        InventoryTotalQuantity = report.TotalQuantity;
                        InventoryLowStockCount = report.LowStockCount;
                        InventoryTotalValue = report.TotalInventoryValue;
                    }
                    else
                    {
                        InventoryTotalTypes = 0;
                        InventoryTotalQuantity = 0;
                        InventoryLowStockCount = 0;
                        InventoryTotalValue = 0;
                    }
                });
            }, "جاري إعداد تقرير المخزون...");
        }

        public void LoadTopReport()
        {
            try
            {
                TopServices.Clear();
                TopSpareParts.Clear();

                int totalCount = 0;
                decimal totalRev = 0;
                string topSvcName = "";
                string topPrtName = "";

                if (_topCategory == "الكل" || _topCategory == "الخدمات")
                {
                    var services = _repairOrderService.GetTopServices(_topStartDate, _topEndDate, _topN);
                    if (services != null && services.Count > 0)
                    {
                        topSvcName = services[0].ServiceName;
                        foreach (var s in services)
                        {
                            TopServices.Add(s);
                            totalCount += s.Count;
                            totalRev += s.TotalRevenue;
                        }
                    }
                }

                if (_topCategory == "الكل" || _topCategory == "قطع الغيار")
                {
                    var parts = _inventoryService.GetTopSpareParts(_topStartDate, _topEndDate, _topN);
                    if (parts != null && parts.Count > 0)
                    {
                        topPrtName = parts[0].PartName;
                        foreach (var p in parts)
                        {
                            TopSpareParts.Add(p);
                        }
                    }
                }

                TopTotalServicesCount = totalCount;
                TopTotalServicesRevenue = totalRev;
                MostRequestedServiceName = string.IsNullOrEmpty(topSvcName) ? "لا يوجد" : topSvcName;
                MostRequestedPartName = string.IsNullOrEmpty(topPrtName) ? "لا يوجد" : topPrtName;
            }
            catch { }
        }

        public async Task LoadTopReportAsync()
        {
            await RunAsync(async () =>
            {
                var services = (_topCategory == "الكل" || _topCategory == "الخدمات")
                    ? await Task.Run(() => _repairOrderService.GetTopServices(_topStartDate, _topEndDate, _topN))
                    : null;

                var parts = (_topCategory == "الكل" || _topCategory == "قطع الغيار")
                    ? await Task.Run(() => _inventoryService.GetTopSpareParts(_topStartDate, _topEndDate, _topN))
                    : null;

                RunOnUIThread(() =>
                {
                    TopServices.Clear();
                    TopSpareParts.Clear();

                    int totalCount = 0;
                    decimal totalRev = 0;
                    string topSvcName = "";
                    string topPrtName = "";

                    if (services != null && services.Count > 0)
                    {
                        topSvcName = services[0].ServiceName;
                        foreach (var s in services)
                        {
                            TopServices.Add(s);
                            totalCount += s.Count;
                            totalRev += s.TotalRevenue;
                        }
                    }

                    if (parts != null && parts.Count > 0)
                    {
                        topPrtName = parts[0].PartName;
                        foreach (var p in parts)
                        {
                            TopSpareParts.Add(p);
                        }
                    }

                    TopTotalServicesCount = totalCount;
                    TopTotalServicesRevenue = totalRev;
                    MostRequestedServiceName = string.IsNullOrEmpty(topSvcName) ? "لا يوجد" : topSvcName;
                    MostRequestedPartName = string.IsNullOrEmpty(topPrtName) ? "لا يوجد" : topPrtName;
                });
            }, "جاري تحليل الخدمات وقطع الغيار الأكثر طلباً...");
        }

        private void PrintFinancialReport()
        {
            ReportsBuilders.ShowFinancial(_financialStartDate, _financialEndDate, System.Windows.Application.Current?.MainWindow);
        }

        private void ExportFinancialPdf()
        {
            ReportsBuilders.ExportFinancialToPdf(_financialStartDate, _financialEndDate, System.Windows.Application.Current?.MainWindow);
        }

        private void ExportFinancialExcel()
        {
            ReportsBuilders.ExportFinancialToExcel(_financialStartDate, _financialEndDate, System.Windows.Application.Current?.MainWindow);
        }

        private void PrintInventoryReport()
        {
            var dt = _inventoryService.GetAllSpareParts("");
            ReportsBuilders.ShowInventory(dt, System.Windows.Application.Current?.MainWindow);
        }

        private void ExportInventoryPdf()
        {
            var dt = _inventoryService.GetAllSpareParts("");
            ReportsBuilders.ExportInventoryToPdf(dt, System.Windows.Application.Current?.MainWindow);
        }

        private void ExportInventoryExcel()
        {
            var dt = _inventoryService.GetAllSpareParts("");
            ReportsBuilders.ExportInventoryToExcel(dt, System.Windows.Application.Current?.MainWindow);
        }

        private void PrintTopReport()
        {
            if (_topCategory == "قطع الغيار")
            {
                ReportsBuilders.ShowTopParts(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
            else if (_topCategory == "الخدمات")
            {
                ReportsBuilders.ShowTopServices(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
            else
            {
                ReportsBuilders.ShowTopCombined(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
        }

        private void ExportTopPdf()
        {
            if (_topCategory == "قطع الغيار")
            {
                ReportsBuilders.ExportTopPartsToPdf(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
            else if (_topCategory == "الخدمات")
            {
                ReportsBuilders.ExportTopServicesToPdf(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
            else
            {
                ReportsBuilders.ExportTopCombinedToPdf(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
        }

        private void ExportTopExcel()
        {
            if (_topCategory == "قطع الغيار")
            {
                ReportsBuilders.ExportTopPartsToExcel(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
            else if (_topCategory == "الخدمات")
            {
                ReportsBuilders.ExportTopServicesToExcel(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
            else
            {
                ReportsBuilders.ExportTopCombinedToExcel(_topStartDate, _topEndDate, _topN, System.Windows.Application.Current?.MainWindow);
            }
        }
        #endregion
    }
}
