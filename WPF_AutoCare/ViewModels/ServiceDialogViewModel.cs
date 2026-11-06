using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class ServiceDialogViewModel : ViewModelBase
    {
        private readonly IServiceCatalogService _catalogService;

        private string _serviceCode = "";
        private string _serviceName = "";
        private string _standardPrice = "0.00";
        private string _category = "صيانة عامة";
        private bool _isEditMode;

        public string ServiceCode
        {
            get => _serviceCode;
            set => SetProperty(ref _serviceCode, value);
        }

        public string ServiceName
        {
            get => _serviceName;
            set => SetProperty(ref _serviceName, value);
        }

        public string StandardPrice
        {
            get => _standardPrice;
            set => SetProperty(ref _standardPrice, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public List<string> Categories { get; } = new List<string>
        {
            "صيانة عامة", "ميكانيك", "كهرباء", "تكييف", "سمكرة ودهان", "تغيير زيوت وفلاتر", "فحص كمبيوتر", "إطارات وميزان"
        };

        public Action CloseAction { get; set; }
        public bool DialogSuccess { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ServiceDialogViewModel(IServiceCatalogService catalogService = null)
        {
            _catalogService = catalogService ?? AppServiceLocator.ServiceCatalogService;
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        public void LoadService(ServiceRowDto dto)
        {
            if (dto == null) return;
            IsEditMode = true;
            ServiceCode = dto.ServiceCode ?? "";
            ServiceName = dto.ServiceName ?? "";
            StandardPrice = dto.StandardPrice ?? "0.00";
            Category = dto.Category ?? "صيانة عامة";
        }

        private void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                MessageBox.Show("الرجاء إدخال اسم الخدمة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(StandardPrice?.Trim(), out decimal price) || price < 0)
            {
                MessageBox.Show("الرجاء إدخال سعر خدمة صحيح أكبر من أو يساوي الصفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string code = string.IsNullOrWhiteSpace(ServiceCode) ? "SVC" + DateTime.Now.ToString("yyMMddHHmm") : ServiceCode.Trim();

            var result = _catalogService.SaveService(code, ServiceName.Trim(), price, Category?.Trim() ?? "صيانة عامة", IsEditMode);
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
