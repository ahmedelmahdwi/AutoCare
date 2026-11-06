using System;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class SparePartDialogViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventoryService;

        private string _partCode = "";
        private string _partName = "";
        private string _category = "قطع عامة";
        private string _quantity = "0";
        private string _minStockLevel = "5";
        private string _costPrice = "0.00";
        private string _sellingPrice = "0.00";
        private bool _isEditMode;

        public string PartCode
        {
            get => _partCode;
            set => SetProperty(ref _partCode, value);
        }

        public string PartName
        {
            get => _partName;
            set => SetProperty(ref _partName, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public string Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public string MinStockLevel
        {
            get => _minStockLevel;
            set => SetProperty(ref _minStockLevel, value);
        }

        public string CostPrice
        {
            get => _costPrice;
            set => SetProperty(ref _costPrice, value);
        }

        public string SellingPrice
        {
            get => _sellingPrice;
            set => SetProperty(ref _sellingPrice, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public Action CloseAction { get; set; }
        public bool DialogSuccess { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public SparePartDialogViewModel(IInventoryService inventoryService = null)
        {
            _inventoryService = inventoryService ?? AppServiceLocator.InventoryService;
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        public void LoadPart(SparePartRowDto dto)
        {
            if (dto == null) return;
            IsEditMode = true;
            PartCode = dto.PartCode ?? "";
            PartName = dto.PartName ?? "";
            Category = dto.Category ?? "قطع عامة";
            Quantity = dto.Qty ?? "0";
            MinStockLevel = dto.MinStock ?? "5";
            CostPrice = dto.BuyingPrice ?? "0.00";
            SellingPrice = dto.SellingPrice ?? "0.00";
        }

        private void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(PartCode))
            {
                MessageBox.Show("الرجاء إدخال رمز القطعة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PartName))
            {
                MessageBox.Show("الرجاء إدخال اسم القطعة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(Quantity?.Trim(), out int qty) || qty < 0)
            {
                MessageBox.Show("الرجاء إدخال كمية صالحة (صفر أو أكثر).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MinStockLevel?.Trim(), out int minStock) || minStock < 0)
            {
                MessageBox.Show("الرجاء إدخال حد أدنى صالح للمخزون.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(CostPrice?.Trim(), out decimal cost) || cost < 0)
            {
                MessageBox.Show("الرجاء إدخال سعر شراء صحيح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(SellingPrice?.Trim(), out decimal sell) || sell < 0)
            {
                MessageBox.Show("الرجاء إدخال سعر بيع صحيح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OperationResult result;
            if (IsEditMode)
            {
                result = _inventoryService.UpdateSparePart(PartCode.Trim(), PartName.Trim(), qty, minStock, cost, sell, "الرف الرئيسي", Category?.Trim() ?? "قطع عامة");
            }
            else
            {
                result = _inventoryService.AddSparePart(PartCode.Trim(), PartName.Trim(), qty, minStock, cost, sell, "الرف الرئيسي", Category?.Trim() ?? "قطع عامة");
            }

            if (result.Success)
            {
                DialogSuccess = true;
                MessageBox.Show(result.Message, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseAction?.Invoke();
            }
            else
            {
                MessageBox.Show(result.Message, "خطأ في الحفظ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
