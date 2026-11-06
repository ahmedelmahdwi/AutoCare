using System;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class SupplierDialogViewModel : ViewModelBase
    {
        private readonly ISupplierService _supplierService;

        private int _supplierId;
        private string _supplierName = "";
        private string _contactPerson = "";
        private string _phoneNumber = "";
        private string _address = "";
        private bool _isEditMode;

        public int SupplierId
        {
            get => _supplierId;
            set => SetProperty(ref _supplierId, value);
        }

        public string SupplierName
        {
            get => _supplierName;
            set => SetProperty(ref _supplierName, value);
        }

        public string ContactPerson
        {
            get => _contactPerson;
            set => SetProperty(ref _contactPerson, value);
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
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

        public SupplierDialogViewModel(ISupplierService supplierService = null)
        {
            _supplierService = supplierService ?? AppServiceLocator.SupplierService;
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        public void LoadSupplier(SupplierSummaryDto dto)
        {
            if (dto == null) return;
            IsEditMode = true;
            SupplierId = dto.SupplierID;
            SupplierName = dto.SupplierName ?? "";
            ContactPerson = dto.ContactPerson ?? "";
            PhoneNumber = dto.PhoneNumber ?? "";
            Address = dto.Address ?? "";
        }

        private void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(SupplierName))
            {
                MessageBox.Show("الرجاء إدخال اسم المورد أو الشركة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OperationResult result;
            if (IsEditMode)
            {
                result = _supplierService.UpdateSupplier(SupplierId, SupplierName.Trim(), ContactPerson?.Trim(), PhoneNumber?.Trim(), Address?.Trim());
            }
            else
            {
                result = _supplierService.AddSupplier(SupplierName.Trim(), ContactPerson?.Trim(), PhoneNumber?.Trim(), Address?.Trim());
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
