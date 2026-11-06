using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for InventoryReportView.
    /// Manages stock value evaluations, low-stock alerts, and item queries.
    /// Clean MVVM.
    /// </summary>
    public class InventoryReportViewModel : ViewModelBase
    {
        private readonly IInventoryService _invService;
        private int _totalTypes;
        private int _totalQuantity;
        private int _lowStockCount;
        private decimal _totalInventoryValue;

        public ObservableCollection<InventoryReportItemDto> Items { get; } = new ObservableCollection<InventoryReportItemDto>();

        public int TotalTypes
        {
            get => _totalTypes;
            set => SetProperty(ref _totalTypes, value);
        }

        public int TotalQuantity
        {
            get => _totalQuantity;
            set => SetProperty(ref _totalQuantity, value);
        }

        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        public decimal TotalInventoryValue
        {
            get => _totalInventoryValue;
            set => SetProperty(ref _totalInventoryValue, value);
        }

        public InventoryReportViewModel(IInventoryService invService = null)
        {
            _invService = invService ?? AppServiceLocator.InventoryService;
        }

        public async Task LoadReportAsync(string search = "", bool lowStockOnly = false)
        {
            await RunAsync(async () =>
            {
                var report = await Task.Run(() => _invService.GetInventoryReport(search, lowStockOnly));
                RunOnUIThread(() =>
                {
                    Items.Clear();
                    if (report != null && report.Items != null)
                    {
                        foreach (var item in report.Items)
                        {
                            Items.Add(item);
                        }
                        TotalTypes = report.TotalTypes;
                        TotalQuantity = report.TotalQuantity;
                        LowStockCount = report.LowStockCount;
                        TotalInventoryValue = report.TotalInventoryValue;
                    }
                    else
                    {
                        TotalTypes = 0;
                        TotalQuantity = 0;
                        LowStockCount = 0;
                        TotalInventoryValue = 0;
                    }
                });
            }, "جاري إعداد تقرير المخزون...");
        }

        public void LoadReport(string search = "", bool lowStockOnly = false)
        {
            Items.Clear();
            var report = _invService.GetInventoryReport(search, lowStockOnly);
            if (report != null && report.Items != null)
            {
                foreach (var item in report.Items)
                {
                    Items.Add(item);
                }
                TotalTypes = report.TotalTypes;
                TotalQuantity = report.TotalQuantity;
                LowStockCount = report.LowStockCount;
                TotalInventoryValue = report.TotalInventoryValue;
            }
            else
            {
                TotalTypes = 0;
                TotalQuantity = 0;
                LowStockCount = 0;
                TotalInventoryValue = 0;
            }
        }
    }
}
