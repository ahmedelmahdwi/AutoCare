using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Enums;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Models;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class InvoiceLineItem
    {
        public string Name { get; set; }
        public string Detail { get; set; }
        public string UnitPrice { get; set; }
        public string Total { get; set; }
    }

    public class InvoiceItemRow
    {
        public string ItemType { get; set; }
        public string Description { get; set; }
        public string Quantity { get; set; }
        public string UnitPriceFormatted { get; set; }
        public string TotalPriceFormatted { get; set; }
    }

    /// <summary>
    /// ViewModel for GenerateTheFinalInvoiceDialog.
    /// Manages invoice calculations, line item aggregation, and creation transactions.
    /// Clean Architecture & MVVM.
    /// </summary>
    public class GenerateInvoiceViewModel : INotifyPropertyChanged
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IRepairOrderService _repairOrderService;

        private string _repairOrderNumber = "";
        private string _customerName = "";
        private string _phone = "";
        private string _plateNumber = "";
        private string _carName = "";
        private string _carModel = "";
        private decimal _servicesTotal = 0;
        private decimal _partsTotal = 0;
        private decimal _subTotal = 0;
        private decimal _discountAmount = 0;
        private decimal _netTotal = 0;
        private string _discountInput = "0.00";
        private string _paymentMethod = "نقد (Cash)";
        private bool _isBusy;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InvoiceLineItem> Lines { get; } = new ObservableCollection<InvoiceLineItem>();
        public ObservableCollection<InvoiceItemRow> InvoiceItems { get; } = new ObservableCollection<InvoiceItemRow>();

        public List<string> PaymentMethods { get; } = new List<string>
        {
            "نقد (Cash)",
            "شبكة / مدى (POS)",
            "تحويل بنكي (Bank Transfer)",
            "آجل / ذمم (Credit)"
        };

        public Action CloseAction { get; set; }
        public bool DialogSuccess { get; private set; }
        public ICommand GenerateInvoiceCommand { get; }
        public ICommand CancelCommand { get; }

        public GenerateInvoiceViewModel(IInvoiceService invoiceService, IRepairOrderService repairOrderService)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _repairOrderService = repairOrderService ?? throw new ArgumentNullException(nameof(repairOrderService));

            GenerateInvoiceCommand = new AsyncRelayCommand(ExecuteGenerateInvoiceAsync);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        #region Properties
        public string RepairOrderNumber
        {
            get => _repairOrderNumber;
            set { _repairOrderNumber = value; OnPropertyChanged(); }
        }

        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); OnPropertyChanged(nameof(CustomerPhone)); }
        }

        public string CustomerPhone => Phone;

        public string PlateNumber
        {
            get => _plateNumber;
            set { _plateNumber = value; OnPropertyChanged(); }
        }

        public string CarName
        {
            get => _carName;
            set { _carName = value; OnPropertyChanged(); }
        }

        public string CarModel
        {
            get => _carModel;
            set { _carModel = value; OnPropertyChanged(); }
        }

        public decimal SubTotal
        {
            get => _subTotal;
            set
            {
                _subTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SubTotalDisplay));
                OnPropertyChanged(nameof(SubTotalFormatted));
            }
        }

        public string SubTotalDisplay => $"{_subTotal:N2} د.ل";
        public string SubTotalFormatted => $"{_subTotal:N2} د.ل";

        public decimal DiscountAmount
        {
            get => _discountAmount;
            set { _discountAmount = value; OnPropertyChanged(); }
        }

        public string DiscountInput
        {
            get => _discountInput;
            set
            {
                _discountInput = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Discount));
                RecalculateTotals();
            }
        }

        public string Discount
        {
            get => _discountInput;
            set => DiscountInput = value;
        }

        public string TaxAmountFormatted => "0.00 د.ل";

        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                _paymentMethod = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedPaymentMethod));
            }
        }

        public object SelectedPaymentMethod
        {
            get => PaymentMethod;
            set
            {
                if (value is System.Windows.Controls.ComboBoxItem item && item.Content != null)
                    PaymentMethod = item.Content.ToString();
                else if (value != null)
                    PaymentMethod = value.ToString();
            }
        }

        public decimal NetTotal
        {
            get => _netTotal;
            set
            {
                _netTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NetTotalDisplay));
                OnPropertyChanged(nameof(NetTotalFormatted));
            }
        }

        public string NetTotalDisplay => $"{_netTotal:N2} د.ل";
        public string NetTotalFormatted => $"{_netTotal:N2} د.ل";

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }
        #endregion

        public OperationResult LoadOrderData(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رقم أمر الإصلاح غير صالح.");

            try
            {
                RepairOrderNumber = repairOrderNumber;
                RepairOrderDetailsDto details = _repairOrderService.GetRepairOrderDetails(repairOrderNumber);
                if (details == null || details.Order == null)
                    return OperationResult.Fail("لم يتم العثور على أمر الإصلاح المطلوب.");

                if (details.Customer != null)
                {
                    CustomerName = details.Customer.FullName ?? "";
                    Phone = details.Customer.PhoneNumber ?? "";
                }

                if (details.Vehicle != null)
                {
                    PlateNumber = details.Vehicle.PlateNumber ?? "";
                    CarName = details.Vehicle.CarName ?? "";
                    CarModel = details.Vehicle.CarModel ?? "";
                }

                Lines.Clear();
                InvoiceItems.Clear();
                _servicesTotal = 0;
                _partsTotal = 0;

                foreach (var svc in details.Services)
                {
                    Lines.Add(new InvoiceLineItem
                    {
                        Name = svc.ServiceName,
                        Detail = svc.TechnicianName,
                        UnitPrice = svc.Price.ToString("F2"),
                        Total = svc.Price.ToString("F2")
                    });
                    InvoiceItems.Add(new InvoiceItemRow
                    {
                        ItemType = "أجر خدمة",
                        Description = $"{svc.ServiceName} (الفني: {svc.TechnicianName})",
                        Quantity = "1",
                        UnitPriceFormatted = svc.Price.ToString("N2"),
                        TotalPriceFormatted = svc.Price.ToString("N2")
                    });
                    _servicesTotal += svc.Price;
                }

                foreach (var part in details.SpareParts)
                {
                    Lines.Add(new InvoiceLineItem
                    {
                        Name = part.PartName,
                        Detail = part.Quantity.ToString(),
                        UnitPrice = part.UnitPrice.ToString("F2"),
                        Total = part.TotalPrice.ToString("F2")
                    });
                    InvoiceItems.Add(new InvoiceItemRow
                    {
                        ItemType = "قطعة غيار",
                        Description = part.PartName,
                        Quantity = part.Quantity.ToString(),
                        UnitPriceFormatted = part.UnitPrice.ToString("N2"),
                        TotalPriceFormatted = part.TotalPrice.ToString("N2")
                    });
                    _partsTotal += part.TotalPrice;
                }

                RecalculateTotals();
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل استرجاع بيانات أمر الإصلاح: " + ex.Message);
            }
        }

        public void RecalculateTotals()
        {
            decimal discInput = 0;
            if (decimal.TryParse(_discountInput?.Trim(), out decimal d) && d >= 0)
                discInput = d;

            var calc = _invoiceService.Calculate(_servicesTotal, _partsTotal, discInput);
            SubTotal = calc.SubTotal;
            DiscountAmount = calc.DiscountAmount;
            NetTotal = calc.FinalDueAmount;
        }

        private async Task ExecuteGenerateInvoiceAsync()
        {
            if (string.IsNullOrWhiteSpace(RepairOrderNumber))
            {
                MessageBox.Show("رقم أمر الإصلاح غير متوفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string user = UserSession.Username ?? "admin";
            var result = await CreateInvoiceAsync(RepairOrderNumber, user);
            if (result.Success)
            {
                DialogSuccess = true;
                MessageBox.Show($"تم إصدار وحفظ الفاتورة النهائية بنجاح برقم: {result.Data}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseAction?.Invoke();
            }
            else
            {
                MessageBox.Show(result.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public async Task<OperationResult<string>> CreateInvoiceAsync(string repairOrderNumber, string username)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult<string>.Fail("رقم أمر الإصلاح غير موجود.");

            IsBusy = true;
            try
            {
                decimal capturedNet = NetTotal;
                decimal capturedDiscount = DiscountAmount;
                string capturedMethod = PaymentMethod ?? "نقد (Cash)";
                string capturedUser = !string.IsNullOrWhiteSpace(username) ? username : "admin";

                return await Task.Run(() =>
                    _invoiceService.CreateFinalInvoice(
                        repairOrderNumber,
                        capturedNet,
                        capturedDiscount,
                        capturedMethod,
                        capturedUser
                    )
                );
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail("حدث خطأ أثناء إنشاء الفاتورة: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
