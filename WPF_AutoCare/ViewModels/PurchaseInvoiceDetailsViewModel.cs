using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class PurchaseInvoiceDetailsViewModel : ViewModelBase
    {
        private readonly ISupplierService _supplierService;

        private string _invoiceNumber = "";
        private string _supplierName = "";
        private string _invoiceDate = "";
        private string _totalAmount = "";
        private string _notes = "";
        private string _createdBy = "";

        public string InvoiceNumber { get => _invoiceNumber; set => SetProperty(ref _invoiceNumber, value); }
        public string SupplierName { get => _supplierName; set => SetProperty(ref _supplierName, value); }
        public string InvoiceDate { get => _invoiceDate; set => SetProperty(ref _invoiceDate, value); }
        public string TotalAmount { get => _totalAmount; set => SetProperty(ref _totalAmount, value); }
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
        public string CreatedBy { get => _createdBy; set => SetProperty(ref _createdBy, value); }

        public ObservableCollection<PurchaseInvoiceDetailDto> Items { get; } = new ObservableCollection<PurchaseInvoiceDetailDto>();

        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        public PurchaseInvoiceDetailsViewModel(ISupplierService supplierService = null)
        {
            _supplierService = supplierService ?? AppServiceLocator.SupplierService;
            CloseCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        public void LoadInvoice(PurchaseInvoiceSummaryDto invoice)
        {
            if (invoice == null) return;

            InvoiceNumber = invoice.PurchaseInvoiceNumber;
            SupplierName = invoice.SupplierName;
            InvoiceDate = invoice.InvoiceDate;
            TotalAmount = invoice.TotalAmount;
            Notes = invoice.Notes;
            CreatedBy = invoice.CreatedBy;

            Items.Clear();
            var details = _supplierService.GetPurchaseInvoiceDetails(invoice.PurchaseInvoiceNumber);
            foreach (var d in details)
            {
                Items.Add(d);
            }
        }
    }
}
