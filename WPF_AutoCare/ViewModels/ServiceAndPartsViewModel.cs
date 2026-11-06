using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class ServiceRow
    {
        public string ServiceName { get; set; }
        public string UnitPrice { get; set; }
        public string TechnicianName { get; set; }
        public string Commission { get; set; }
        public string ServiceCode { get; set; }
        public string TechId { get; set; }
    }

    public class PartRow
    {
        public string PartName { get; set; }
        public string Quantity { get; set; }
        public string UnitPrice { get; set; }
        public string PartCode { get; set; }
    }

    /// <summary>
    /// ViewModel for ServiceAndPartsDialog adhering to MVVM / Clean Architecture.
    /// Manages UI presentation state, catalog loading, totals calculation,
    /// and delegates business commands to IRepairOrderService, IInventoryService, and ITechnicianService.
    /// 100% decoupled from Data Repositories and DataTable.
    /// </summary>
    public class ServiceAndPartsViewModel : INotifyPropertyChanged
    {
        private readonly IRepairOrderService _roService;
        private readonly IInventoryService _invService;
        private readonly ITechnicianService _techService;

        private string _repairOrderNumber = "";
        private decimal _totalServices;
        private decimal _totalParts;
        private string _currentStatus = "Pending";
        private string _currentStatusArabic = "معلق";

        private string _selectedServiceCategory = "";
        private ServiceCatalogItemDto _selectedServiceItem;
        private TechnicianSummaryDto _selectedTechnician;

        private string _selectedPartCategory = "";
        private SparePartCatalogItemDto _selectedPartItem;
        private string _enteredPartQuantity = "1";

        private ServiceRow _selectedServiceRow;
        private PartRow _selectedPartRow;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ServiceRow> ServiceRows { get; } = new ObservableCollection<ServiceRow>();
        public ObservableCollection<PartRow> PartRows { get; } = new ObservableCollection<PartRow>();

        public ObservableCollection<string> ServiceCategories { get; } = new ObservableCollection<string>();
        public ObservableCollection<ServiceCatalogItemDto> AvailableServices { get; } = new ObservableCollection<ServiceCatalogItemDto>();
        public ObservableCollection<TechnicianSummaryDto> AvailableTechnicians { get; } = new ObservableCollection<TechnicianSummaryDto>();

        public ObservableCollection<string> PartCategories { get; } = new ObservableCollection<string>();
        public ObservableCollection<SparePartCatalogItemDto> AvailableParts { get; } = new ObservableCollection<SparePartCatalogItemDto>();

        public Action CloseAction { get; set; }
        public bool DialogChanged { get; private set; }

        public ICommand AddServiceCommand { get; }
        public ICommand DeleteServiceCommand { get; }
        public ICommand AddPartCommand { get; }
        public ICommand DeletePartCommand { get; }
        public ICommand SaveAndFinishCommand { get; }
        public ICommand RevertToPendingCommand { get; }
        public ICommand CloseCommand { get; }

        public string RepairOrderNumber
        {
            get => _repairOrderNumber;
            set { _repairOrderNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(DialogTitle)); }
        }

        public string DialogTitle => $"إدارة الخدمات وقطع الغيار لأمر الصيانة ({RepairOrderNumber})";

        public decimal TotalServices
        {
            get => _totalServices;
            private set { _totalServices = value; OnPropertyChanged(); OnPropertyChanged(nameof(GrandTotal)); }
        }

        public decimal TotalParts
        {
            get => _totalParts;
            private set { _totalParts = value; OnPropertyChanged(); OnPropertyChanged(nameof(GrandTotal)); }
        }

        public decimal GrandTotal => TotalServices + TotalParts;

        public string CurrentStatus
        {
            get => _currentStatus;
            private set
            {
                _currentStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRevertToPending));
            }
        }

        public string CurrentStatusArabic
        {
            get => _currentStatusArabic;
            private set { _currentStatusArabic = value; OnPropertyChanged(); }
        }

        public bool CanRevertToPending => CurrentStatus == "InProgress";

        public string SelectedServiceCategory
        {
            get => _selectedServiceCategory;
            set
            {
                _selectedServiceCategory = value;
                OnPropertyChanged();
                LoadServicesForCategory(value);
            }
        }

        public ServiceCatalogItemDto SelectedServiceItem
        {
            get => _selectedServiceItem;
            set { _selectedServiceItem = value; OnPropertyChanged(); }
        }

        public TechnicianSummaryDto SelectedTechnician
        {
            get => _selectedTechnician;
            set { _selectedTechnician = value; OnPropertyChanged(); }
        }

        public ServiceRow SelectedServiceRow
        {
            get => _selectedServiceRow;
            set { _selectedServiceRow = value; OnPropertyChanged(); }
        }

        public string SelectedPartCategory
        {
            get => _selectedPartCategory;
            set
            {
                _selectedPartCategory = value;
                OnPropertyChanged();
                LoadPartsForCategory(value);
            }
        }

        public SparePartCatalogItemDto SelectedPartItem
        {
            get => _selectedPartItem;
            set { _selectedPartItem = value; OnPropertyChanged(); }
        }

        public string EnteredPartQuantity
        {
            get => _enteredPartQuantity;
            set { _enteredPartQuantity = value; OnPropertyChanged(); }
        }

        public PartRow SelectedPartRow
        {
            get => _selectedPartRow;
            set { _selectedPartRow = value; OnPropertyChanged(); }
        }

        private readonly string[] _defaultCategories = { "صيانة عامة", "صيانة دورية", "كهرباء", "ميكانيك", "سمكرة", "دوكو", "إطارات", "تكييف", "أخرى" };

        public ServiceAndPartsViewModel(IRepairOrderService roService = null, IInventoryService invService = null, ITechnicianService techService = null)
        {
            _roService = roService ?? AppServiceLocator.RepairOrderService;
            _invService = invService ?? AppServiceLocator.InventoryService;
            _techService = techService ?? AppServiceLocator.TechnicianService;

            AddServiceCommand = new RelayCommand(ExecuteAddService);
            DeleteServiceCommand = new RelayCommand(ExecuteDeleteService, () => SelectedServiceRow != null);
            AddPartCommand = new RelayCommand(ExecuteAddPart);
            DeletePartCommand = new RelayCommand(ExecuteDeletePart, () => SelectedPartRow != null);
            SaveAndFinishCommand = new RelayCommand(ExecuteSaveAndFinish);
            RevertToPendingCommand = new RelayCommand(ExecuteRevertToPending);
            CloseCommand = new RelayCommand(() => CloseAction?.Invoke());

            InitializeCategories();
            InitializeTechnicians();
        }

        public void InitializeCategories()
        {
            ServiceCategories.Clear();
            var svcCats = _roService.GetServiceCategories();
            var finalSvc = (svcCats != null && svcCats.Count > 0) ? svcCats : new List<string>(_defaultCategories);
            foreach (var c in finalSvc) ServiceCategories.Add(c);
            if (ServiceCategories.Count > 0) SelectedServiceCategory = ServiceCategories[0];

            PartCategories.Clear();
            var partCats = _invService.GetDistinctCategories();
            var finalParts = (partCats != null && partCats.Count > 0) ? partCats : new List<string>(_defaultCategories);
            foreach (var c in finalParts) PartCategories.Add(c);
            if (PartCategories.Count > 0) SelectedPartCategory = PartCategories[0];
        }

        public void InitializeTechnicians()
        {
            AvailableTechnicians.Clear();
            var techs = _techService.GetActiveTechniciansList();
            if (techs != null)
            {
                foreach (var t in techs) AvailableTechnicians.Add(t);
                if (AvailableTechnicians.Count > 0) SelectedTechnician = AvailableTechnicians[0];
            }
        }

        private void LoadServicesForCategory(string category)
        {
            AvailableServices.Clear();
            if (string.IsNullOrWhiteSpace(category)) return;
            var list = _roService.GetServicesByCategory(category.Trim());
            if (list != null)
            {
                foreach (var s in list) AvailableServices.Add(s);
                if (AvailableServices.Count > 0) SelectedServiceItem = AvailableServices[0];
            }
        }

        private void LoadPartsForCategory(string category)
        {
            AvailableParts.Clear();
            if (string.IsNullOrWhiteSpace(category)) return;
            var list = _invService.GetSparePartsByCategory(category.Trim());
            if (list != null)
            {
                foreach (var p in list) AvailableParts.Add(p);
                if (AvailableParts.Count > 0) SelectedPartItem = AvailableParts[0];
            }
        }

        public void LoadOrderData(string repairOrderNumber)
        {
            RepairOrderNumber = repairOrderNumber;
            if (string.IsNullOrEmpty(repairOrderNumber))
            {
                ServiceRows.Clear();
                PartRows.Clear();
                CalculateTotals();
                return;
            }

            LoadServicesGrid(repairOrderNumber);
            LoadPartsGrid(repairOrderNumber);
            CalculateTotals();

            string status = _roService.GetRepairOrderStatus(repairOrderNumber);
            if (!string.IsNullOrEmpty(status))
            {
                CurrentStatus = status;
                CurrentStatusArabic = MapArabicStatus(CurrentStatus);
            }
        }

        public void LoadServicesGrid(string repairOrderNumber)
        {
            ServiceRows.Clear();
            if (string.IsNullOrEmpty(repairOrderNumber)) return;

            try
            {
                var services = _roService.GetOrderServices(repairOrderNumber);
                if (services != null)
                {
                    foreach (var s in services)
                    {
                        ServiceRows.Add(new ServiceRow
                        {
                            ServiceName = s.ServiceName,
                            UnitPrice = s.UnitPrice.ToString("0.00"),
                            TechnicianName = s.TechnicianName ?? "",
                            Commission = s.CommissionPercent.ToString("0.00"),
                            ServiceCode = s.ServiceCode ?? "",
                            TechId = s.TechnicianId > 0 ? s.TechnicianId.ToString() : ""
                        });
                    }
                }
            }
            catch
            {
                ServiceRows.Clear();
            }
        }

        public void LoadPartsGrid(string repairOrderNumber)
        {
            PartRows.Clear();
            if (string.IsNullOrEmpty(repairOrderNumber)) return;

            try
            {
                var parts = _roService.GetOrderSpareParts(repairOrderNumber);
                if (parts != null)
                {
                    foreach (var p in parts)
                    {
                        PartRows.Add(new PartRow
                        {
                            PartName = p.PartName,
                            Quantity = p.Quantity.ToString(),
                            UnitPrice = p.UnitPrice.ToString("0.00"),
                            PartCode = p.PartCode ?? ""
                        });
                    }
                }
            }
            catch
            {
                PartRows.Clear();
            }
        }

        public void CalculateTotals()
        {
            decimal totalServices = 0;
            foreach (var row in ServiceRows)
            {
                if (decimal.TryParse(row.UnitPrice, out decimal price))
                    totalServices += price;
            }

            decimal totalParts = 0;
            foreach (var row in PartRows)
            {
                if (int.TryParse(row.Quantity, out int qty) && decimal.TryParse(row.UnitPrice, out decimal price))
                    totalParts += qty * price;
            }

            TotalServices = totalServices;
            TotalParts = totalParts;
        }

        private void ExecuteAddService()
        {
            if (SelectedServiceItem == null)
            {
                MessageBox.Show("الرجاء اختيار الخدمة المراد إضافتها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedTechnician == null || SelectedTechnician.TechnicianId <= 0)
            {
                MessageBox.Show("الرجاء اختيار الفني المسؤول عن الخدمة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = AddService(RepairOrderNumber, SelectedServiceItem.ServiceCode, SelectedTechnician.TechnicianId);
            if (res.Success)
            {
                DialogChanged = true;
            }
            else
            {
                MessageBox.Show(res.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecuteDeleteService()
        {
            if (SelectedServiceRow == null) return;
            var res = DeleteService(RepairOrderNumber, SelectedServiceRow.ServiceCode, SelectedServiceRow.TechId);
            if (res.Success)
            {
                DialogChanged = true;
            }
            else
            {
                MessageBox.Show(res.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecuteAddPart()
        {
            if (SelectedPartItem == null)
            {
                MessageBox.Show("الرجاء اختيار قطعة الغيار المراد إضافتها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(EnteredPartQuantity?.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("الرجاء إدخال كمية صحيحة أكبر من الصفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = AddPart(RepairOrderNumber, SelectedPartItem.PartCode, qty);
            if (res.Success)
            {
                DialogChanged = true;
            }
            else
            {
                MessageBox.Show(res.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecuteDeletePart()
        {
            if (SelectedPartRow == null) return;
            int.TryParse(SelectedPartRow.Quantity, out int qty);
            var res = DeletePart(RepairOrderNumber, SelectedPartRow.PartCode, qty);
            if (res.Success)
            {
                DialogChanged = true;
            }
            else
            {
                MessageBox.Show(res.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecuteSaveAndFinish()
        {
            var res = SaveAndFinish(RepairOrderNumber);
            if (res.Success)
            {
                DialogChanged = true;
                MessageBox.Show("تم تأكيد خدمات كرت الصيانة وبدء العمل بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseAction?.Invoke();
            }
            else
            {
                MessageBox.Show(res.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecuteRevertToPending()
        {
            var res = RevertToPending(RepairOrderNumber);
            if (res.Success)
            {
                DialogChanged = true;
                MessageBox.Show("تمت إعادة حالة كرت الصيانة إلى (معلق) بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(res.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public OperationResult AddService(string repairOrderNumber, string serviceCode, int technicianId)
        {
            if (string.IsNullOrEmpty(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");
            if (string.IsNullOrEmpty(serviceCode))
                return OperationResult.Fail("الرجاء اختيار الخدمة أولاً.");
            if (technicianId <= 0)
                return OperationResult.Fail("الرجاء اختيار الفني المسؤول عن الخدمة.");

            foreach (var row in ServiceRows)
            {
                if (row.ServiceCode == serviceCode && row.TechId == technicianId.ToString())
                {
                    return OperationResult.Fail("هذه الخدمة مضافة ومسندة إلى هذا الفني مسبقاً في هذا الأمر.");
                }
            }

            var res = _roService.AddServiceToOrder(repairOrderNumber, serviceCode, technicianId);
            if (res.Success)
            {
                LoadServicesGrid(repairOrderNumber);
                CalculateTotals();
            }
            return res;
        }

        public OperationResult AddPart(string repairOrderNumber, string partCode, int quantity)
        {
            if (string.IsNullOrEmpty(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");
            if (string.IsNullOrEmpty(partCode))
                return OperationResult.Fail("الرجاء اختيار قطعة الغيار أولاً.");
            if (quantity <= 0)
                return OperationResult.Fail("الرجاء إدخال كمية صالحة أكبر من الصفر.");

            var res = _roService.AddSparePartToOrder(repairOrderNumber, partCode, quantity);
            if (res.Success)
            {
                LoadPartsGrid(repairOrderNumber);
                CalculateTotals();
            }
            return res;
        }

        public OperationResult DeleteService(string repairOrderNumber, string serviceCode, string techId)
        {
            if (string.IsNullOrEmpty(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");
            if (string.IsNullOrEmpty(serviceCode))
                return OperationResult.Fail("لم يتم تحديد الخدمة.");

            int.TryParse(techId, out int parsedTechId);
            var res = _roService.RemoveServiceFromOrder(repairOrderNumber, serviceCode, parsedTechId);
            if (res.Success)
            {
                LoadServicesGrid(repairOrderNumber);
                CalculateTotals();
            }
            return res;
        }

        public OperationResult DeletePart(string repairOrderNumber, string partCode, int quantity)
        {
            if (string.IsNullOrEmpty(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");
            if (string.IsNullOrEmpty(partCode))
                return OperationResult.Fail("لم يتم تحديد قطعة الغيار.");

            var res = _roService.RemoveSparePartFromOrder(repairOrderNumber, partCode, quantity);
            if (res.Success)
            {
                LoadPartsGrid(repairOrderNumber);
                CalculateTotals();
            }
            return res;
        }

        public OperationResult ConfirmServices(string repairOrderNumber)
        {
            if (string.IsNullOrEmpty(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");

            if (ServiceRows.Count == 0)
                return OperationResult.Fail("الرجاء اختيار خدمة واحدة على الأقل قبل التأكيد.");

            var res = _roService.ConfirmOrderServices(repairOrderNumber);
            if (res.Success)
            {
                CurrentStatus = "InProgress";
                CurrentStatusArabic = "قيد التنفيذ";
            }
            return res;
        }

        public OperationResult SaveAndFinish(string repairOrderNumber)
        {
            if (string.IsNullOrEmpty(repairOrderNumber))
                return OperationResult.Ok();

            if (ServiceRows.Count == 0)
                return OperationResult.Fail("لا يمكن تغيير الحالة، يجب إضافة خدمة واحدة على الأقل.");

            var res = _roService.StartOrderProgress(repairOrderNumber);
            if (res.Success)
            {
                CurrentStatus = "InProgress";
                CurrentStatusArabic = "قيد التنفيذ";
            }
            return res;
        }

        public OperationResult RevertToPending(string repairOrderNumber)
        {
            if (string.IsNullOrEmpty(repairOrderNumber))
                return OperationResult.Fail("رمز أمر الإصلاح غير متوفر.");

            var res = _roService.RevertToPending(repairOrderNumber);
            if (res.Success)
            {
                CurrentStatus = "Pending";
                CurrentStatusArabic = "معلق";
            }
            return res;
        }

        private static string MapArabicStatus(string status)
        {
            if (status == "InProgress") return "قيد التنفيذ";
            if (status == "Completed") return "مكتمل";
            if (status == "Canceled") return "ملغى";
            return "معلق";
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
