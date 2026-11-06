using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class NewPurchaseInvoiceViewModel : ViewModelBase
    {
        private readonly ISupplierService _supplierService;
        private readonly IInventoryService _inventoryService;

        private string _invoiceNumber = "";
        private DateTime _invoiceDate = DateTime.Today;
        private SupplierSummaryDto _selectedSupplier;
        private SparePartRowDto _selectedPart;
        private int _inputQuantity = 1;
        private decimal _inputUnitCost;
        private string _notes = "";
        private PurchaseItemRowDto _selectedItem;

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => SetProperty(ref _invoiceDate, value);
        }

        public SupplierSummaryDto SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
        }

        public SparePartRowDto SelectedPart
        {
            get => _selectedPart;
            set
            {
                if (SetProperty(ref _selectedPart, value) && value != null)
                {
                    if (decimal.TryParse(value.BuyingPrice, out var bp))
                    {
                        InputUnitCost = bp;
                    }
                }
            }
        }

        public int InputQuantity
        {
            get => _inputQuantity;
            set => SetProperty(ref _inputQuantity, value);
        }

        public decimal InputUnitCost
        {
            get => _inputUnitCost;
            set => SetProperty(ref _inputUnitCost, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public PurchaseItemRowDto SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public ObservableCollection<SupplierSummaryDto> Suppliers { get; } = new ObservableCollection<SupplierSummaryDto>();
        public ObservableCollection<SparePartRowDto> AvailableParts { get; } = new ObservableCollection<SparePartRowDto>();
        public ObservableCollection<PurchaseItemRowDto> Items { get; } = new ObservableCollection<PurchaseItemRowDto>();

        public decimal TotalAmount => Items.Sum(i => i.TotalCost);
        public string TotalAmountDisplay => TotalAmount.ToString("N2") + " د.ل";

        public Action CloseAction { get; set; }
        public bool DialogSuccess { get; private set; }

        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public NewPurchaseInvoiceViewModel(ISupplierService supplierService = null, IInventoryService inventoryService = null)
        {
            _supplierService = supplierService ?? AppServiceLocator.SupplierService;
            _inventoryService = inventoryService ?? AppServiceLocator.InventoryService;

            AddItemCommand = new RelayCommand(ExecuteAddItem);
            RemoveItemCommand = new RelayCommand(ExecuteRemoveItem);
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke());

            LoadInitialData();
        }

        private void LoadInitialData()
        {
            try
            {
                InvoiceNumber = _supplierService.GenerateNewPurchaseInvoiceNumber();

                var supList = _supplierService.GetSuppliersList();
                Suppliers.Clear();
                foreach (var s in supList) Suppliers.Add(s);
                if (Suppliers.Count > 0) SelectedSupplier = Suppliers[0];

                var partsList = _inventoryService.GetSparePartsList();
                AvailableParts.Clear();
                foreach (var p in partsList) AvailableParts.Add(p);
                if (AvailableParts.Count > 0) SelectedPart = AvailableParts[0];
            }
            catch (Exception ex)
            {
                AppLogger.Error("NewPurchaseInvoiceViewModel.LoadInitialData failed", ex);
            }
        }

        private void ExecuteAddItem()
        {
            if (SelectedPart == null)
            {
                MessageBox.Show("الرجاء اختيار قطعة الغيار أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (InputQuantity <= 0)
            {
                MessageBox.Show("يجب أن تكون الكمية المشتراة 1 على الأقل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (InputUnitCost < 0)
            {
                MessageBox.Show("سعر الشراء لا يمكن أن يكون سالباً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = Items.FirstOrDefault(i => i.PartCode == SelectedPart.PartCode);
            if (existing != null)
            {
                existing.Quantity += InputQuantity;
                existing.UnitCost = InputUnitCost;
            }
            else
            {
                Items.Add(new PurchaseItemRowDto
                {
                    PartCode = SelectedPart.PartCode,
                    PartName = SelectedPart.PartName,
                    Quantity = InputQuantity,
                    UnitCost = InputUnitCost
                });
            }

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(TotalAmountDisplay));

            // Reset quantity for next item
            InputQuantity = 1;
        }

        private void ExecuteRemoveItem()
        {
            if (SelectedItem != null)
            {
                Items.Remove(SelectedItem);
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(TotalAmountDisplay));
            }
        }

        private void ExecuteSave()
        {
            if (SelectedSupplier == null)
            {
                MessageBox.Show("الرجاء اختيار المورد.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Items.Count == 0)
            {
                MessageBox.Show("يجب إضافة قطعة واحدة على الأقل في فاتورة الشراء.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(InvoiceNumber))
            {
                InvoiceNumber = _supplierService.GenerateNewPurchaseInvoiceNumber();
            }

            var purchaseItems = Items.Select(i => new PurchaseItemDto
            {
                PartCode = i.PartCode,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList();

            var result = _supplierService.RecordPurchase(
                SelectedSupplier.SupplierID,
                InvoiceNumber.Trim(),
                0m,
                purchaseItems,
                Notes?.Trim()
            );

            if (result.Success)
            {
                DialogSuccess = true;
                MessageBox.Show(result.Message, "نجاح التوريد", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseAction?.Invoke();
            }
            else
            {
                MessageBox.Show(result.Message, "خطأ في تسجيل الفاتورة", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
