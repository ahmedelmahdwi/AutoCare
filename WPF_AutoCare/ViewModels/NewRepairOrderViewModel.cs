using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Entities;
using C__project_Auto_Care.Core.Enums;
using C__project_Auto_Care.Core.Services;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for NewRepairOrderDialog.
    /// Encapsulates dialog state, validation orchestration, and service communication.
    /// Completely frees the View from database queries, entity translation, and business rules.
    /// </summary>
    public class NewRepairOrderViewModel : INotifyPropertyChanged
    {
        private readonly IRepairOrderService _repairOrderService;
        private readonly ICustomerService _customerService;

        private string _customerName = "";
        private string _customerPhone = "";
        private string _plateNumber = "";
        private string _vin = "";
        private string _carName = "";
        private string _carModel = "";
        private string _mileage = "";
        private string _fuelLevel = "غير محدد";
        private string _inspectionNotes = "";
        private string _complaint = "";
        private string _entryDate = "";
        private string _repairOrderNumber = "";
        private string _statusText = RepairOrderStatus.Pending.ToDisplayArabic();
        private bool _isReadOnlyKeys;
        private bool _isEditMode;
        private string _saveButtonTitle = "فتح كرت صيانة";

        public event PropertyChangedEventHandler PropertyChanged;

        public Action CloseAction { get; set; }
        public bool DialogSuccess { get; private set; }
        public System.Windows.Input.ICommand SaveCommand { get; }
        public System.Windows.Input.ICommand CancelCommand { get; }

        public List<string> FuelLevels { get; } = new List<string>
        {
            "غير محدد", "فارغ (E)", "1/4", "1/2", "3/4", "ممتلئ (F)"
        };

        public NewRepairOrderViewModel(IRepairOrderService repairOrderService, ICustomerService customerService)
        {
            _repairOrderService = repairOrderService ?? throw new ArgumentNullException(nameof(repairOrderService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            SaveCommand = new Commands.RelayCommand(ExecuteSaveCommand);
            CancelCommand = new Commands.RelayCommand(() => CloseAction?.Invoke());
        }

        private void ExecuteSaveCommand()
        {
            var res = Save();
            if (res.Success)
            {
                DialogSuccess = true;
                System.Windows.MessageBox.Show(res.Message, "نجاح", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                CloseAction?.Invoke();
            }
            else
            {
                System.Windows.MessageBox.Show(res.Message, "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        #region Properties
        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set { _customerPhone = value; OnPropertyChanged(); }
        }

        public string PlateNumber
        {
            get => _plateNumber;
            set { _plateNumber = value; OnPropertyChanged(); }
        }

        public string Vin
        {
            get => _vin;
            set { _vin = value; OnPropertyChanged(); }
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

        public string Mileage
        {
            get => _mileage;
            set { _mileage = value; OnPropertyChanged(); }
        }

        public string FuelLevel
        {
            get => _fuelLevel;
            set { _fuelLevel = value; OnPropertyChanged(); }
        }

        public string InspectionNotes
        {
            get => _inspectionNotes;
            set { _inspectionNotes = value; OnPropertyChanged(); }
        }

        public string Complaint
        {
            get => _complaint;
            set { _complaint = value; OnPropertyChanged(); }
        }

        public string EntryDate
        {
            get => _entryDate;
            set { _entryDate = value; OnPropertyChanged(); }
        }

        public string RepairOrderNumber
        {
            get => _repairOrderNumber;
            set { _repairOrderNumber = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool IsReadOnlyKeys
        {
            get => _isReadOnlyKeys;
            set { _isReadOnlyKeys = value; OnPropertyChanged(); }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(); }
        }

        public string SaveButtonTitle
        {
            get => _saveButtonTitle;
            set { _saveButtonTitle = value; OnPropertyChanged(); }
        }
        #endregion

        #region Initialization & Modes
        public OperationResult LoadOrderToEdit(string roNumber)
        {
            if (string.IsNullOrWhiteSpace(roNumber))
                return OperationResult.Fail("رقم كرت الصيانة غير صالح.");

            try
            {
                var details = _repairOrderService.GetRepairOrderDetails(roNumber);
                if (details == null || details.Order == null)
                    return OperationResult.Fail("لم يتم العثور على كرت الصيانة المطلوب.");

                IsEditMode = true;
                IsReadOnlyKeys = true;
                SaveButtonTitle = "حفظ التعديلات";

                RepairOrderNumber = roNumber;
                EntryDate = details.Order.EntryDate.ToString("yyyy-MM-dd HH:mm");
                StatusText = ArabicStatus(details.Order.Status);

                if (details.Customer != null)
                {
                    CustomerName = details.Customer.FullName ?? "";
                    CustomerPhone = details.Customer.PhoneNumber ?? "";
                }

                if (details.Vehicle != null)
                {
                    PlateNumber = details.Vehicle.PlateNumber ?? "";
                    Vin = details.Vehicle.Vin ?? "";
                    CarName = details.Vehicle.CarName ?? "";
                    CarModel = details.Vehicle.CarModel ?? "";
                }

                Mileage = details.Order.Mileage.HasValue ? details.Order.Mileage.Value.ToString() : "";
                FuelLevel = !string.IsNullOrEmpty(details.Order.FuelLevel) ? details.Order.FuelLevel : "غير محدد";
                InspectionNotes = details.Order.InspectionNotes ?? "";
                Complaint = details.Order.Complaint ?? "";

                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل تحميل بيانات كرت الصيانة: " + ex.Message);
            }
        }

        public OperationResult LoadVehicleForNewOrder(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber))
                return OperationResult.Fail("رقم اللوحة غير صالح.");

            try
            {
                var veh = _customerService.GetVehicleWithCustomer(plateNumber);
                if (veh == null)
                    return OperationResult.Fail("لم يتم العثور على المركبة المحددة في النظام.");

                IsEditMode = false;
                IsReadOnlyKeys = true;
                SaveButtonTitle = "فتح كرت صيانة";

                RepairOrderNumber = _repairOrderService.GenerateNewRepairOrderNumber();
                EntryDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                StatusText = RepairOrderStatus.Pending.ToDisplayArabic();

                PlateNumber = veh.PlateNumber ?? "";
                Vin = veh.Vin ?? "";
                CarName = veh.CarName ?? "";
                CarModel = veh.CarModel ?? "";
                CustomerPhone = veh.CustomerPhone ?? "";
                CustomerName = veh.CustomerName ?? "";

                Mileage = "";
                FuelLevel = "غير محدد";
                InspectionNotes = "";
                Complaint = "";

                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("فشل استرجاع بيانات المركبة: " + ex.Message);
            }
        }

        public void InitializeBlank()
        {
            IsEditMode = false;
            IsReadOnlyKeys = false;
            SaveButtonTitle = "فتح كرت صيانة";

            RepairOrderNumber = _repairOrderService.GenerateNewRepairOrderNumber();
            EntryDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            StatusText = RepairOrderStatus.Pending.ToDisplayArabic();

            CustomerName = "";
            CustomerPhone = "";
            PlateNumber = "";
            Vin = "";
            CarName = "";
            CarModel = "";
            Mileage = "";
            FuelLevel = "غير محدد";
            InspectionNotes = "";
            Complaint = "";
        }
        #endregion

        #region Save Action
        public OperationResult<string> Save()
        {
            int? mileageVal = null;
            if (!string.IsNullOrWhiteSpace(Mileage) && int.TryParse(Mileage.Trim(), out int m))
                mileageVal = m;

            var customer = new Customer
            {
                FullName = CustomerName?.Trim(),
                PhoneNumber = CustomerPhone?.Trim()
            };

            var vehicle = new Vehicle
            {
                PlateNumber = PlateNumber?.Trim(),
                Vin = Vin?.Trim(),
                CarName = CarName?.Trim(),
                CarModel = CarModel?.Trim(),
                CustomerPhone = CustomerPhone?.Trim()
            };

            var order = new RepairOrder
            {
                RepairOrderNumber = RepairOrderNumber,
                PlateNumber = PlateNumber?.Trim(),
                Complaint = Complaint?.Trim(),
                Mileage = mileageVal,
                FuelLevel = FuelLevel ?? "غير محدد",
                InspectionNotes = InspectionNotes?.Trim() ?? ""
            };

            if (!IsEditMode)
            {
                var result = _repairOrderService.CreateRepairOrder(customer, vehicle, order);
                if (!result.Success)
                    return OperationResult<string>.Fail(result.Message);

                return OperationResult<string>.Ok(result.Data, "تم فتح كرت صيانة جديد بنجاح برقم: " + result.Data);
            }
            else
            {
                var result = _repairOrderService.UpdateRepairOrder(customer, vehicle, order);
                if (!result.Success)
                    return OperationResult<string>.Fail(result.Message);

                return OperationResult<string>.Ok(RepairOrderNumber, "تم حفظ التعديلات على كرت الصيانة بنجاح.");
            }
        }
        #endregion

        private static string ArabicStatus(string status)
        {
            return EnumExtensions.ParseRepairOrderStatus(status).ToDisplayArabic();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
