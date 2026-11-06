using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for FinancialReportView.
    /// Manages revenue calculations, date filtering, and invoice breakdown.
    /// Clean MVVM.
    /// </summary>
    public class FinancialReportViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;
        private int _invoiceCount;
        private decimal _totalServices;
        private decimal _totalParts;
        private decimal _grandTotal;

        public ObservableCollection<FinancialReportItemDto> Invoices { get; } = new ObservableCollection<FinancialReportItemDto>();

        public int InvoiceCount
        {
            get => _invoiceCount;
            set => SetProperty(ref _invoiceCount, value);
        }

        public decimal TotalServices
        {
            get => _totalServices;
            set => SetProperty(ref _totalServices, value);
        }

        public decimal TotalParts
        {
            get => _totalParts;
            set => SetProperty(ref _totalParts, value);
        }

        public decimal GrandTotal
        {
            get => _grandTotal;
            set => SetProperty(ref _grandTotal, value);
        }

        public FinancialReportViewModel(IInvoiceService invoiceService = null)
        {
            _invoiceService = invoiceService ?? AppServiceLocator.InvoiceService;
        }

        public async Task LoadReportAsync(DateTime startDate, DateTime endDate)
        {
            await RunAsync(async () =>
            {
                var summary = await Task.Run(() => _invoiceService.GetFinancialReport(startDate, endDate));
                RunOnUIThread(() =>
                {
                    Invoices.Clear();
                    if (summary != null && summary.Invoices != null)
                    {
                        foreach (var inv in summary.Invoices)
                        {
                            Invoices.Add(inv);
                        }
                        InvoiceCount = summary.InvoiceCount;
                        TotalServices = summary.TotalServices;
                        TotalParts = summary.TotalParts;
                        GrandTotal = summary.GrandTotal;
                    }
                    else
                    {
                        InvoiceCount = 0;
                        TotalServices = 0;
                        TotalParts = 0;
                        GrandTotal = 0;
                    }
                });
            }, "جاري إنشاء التقرير المالي...");
        }

        public void LoadReport(DateTime startDate, DateTime endDate)
        {
            Invoices.Clear();
            var summary = _invoiceService.GetFinancialReport(startDate, endDate);
            if (summary != null && summary.Invoices != null)
            {
                foreach (var inv in summary.Invoices)
                {
                    Invoices.Add(inv);
                }
                InvoiceCount = summary.InvoiceCount;
                TotalServices = summary.TotalServices;
                TotalParts = summary.TotalParts;
                GrandTotal = summary.GrandTotal;
            }
            else
            {
                InvoiceCount = 0;
                TotalServices = 0;
                TotalParts = 0;
                GrandTotal = 0;
            }
        }
    }
}
