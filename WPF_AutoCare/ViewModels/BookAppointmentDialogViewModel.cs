using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoCare.Domain.Entities;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Theme;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class TechnicianLookupItem
    {
        public int? Id { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name;
    }

    public class BookAppointmentDialogViewModel : ViewModelBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ITechnicianService _technicianService;
        private readonly ICustomerService _customerService;

        private string _customerPhone = "";
        private string _customerName = "";
        private string _plateNumber = "";
        private string _carModel = "";
        private DateTime _appointmentDate = DateTime.Today;
        private string _selectedStartTime = "09:00";
        private string _selectedEndTime = "10:00";
        private TechnicianLookupItem _selectedTechnician;
        private string _notes = "";
        private string _conflictMessage = "";
        private bool _hasConflict = false;

        public Action CloseAction { get; set; }
        public bool DialogSuccess { get; private set; }

        public string CustomerPhone
        {
            get => _customerPhone;
            set
            {
                if (SetProperty(ref _customerPhone, value))
                {
                    OnPhoneChanged();
                }
            }
        }

        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        public string PlateNumber
        {
            get => _plateNumber;
            set
            {
                if (SetProperty(ref _plateNumber, value))
                {
                    OnPlateChanged();
                }
            }
        }

        public string CarModel
        {
            get => _carModel;
            set => SetProperty(ref _carModel, value);
        }

        public DateTime AppointmentDate
        {
            get => _appointmentDate;
            set
            {
                if (SetProperty(ref _appointmentDate, value))
                {
                    _ = CheckConflictAsync();
                }
            }
        }

        public string SelectedStartTime
        {
            get => _selectedStartTime;
            set
            {
                if (SetProperty(ref _selectedStartTime, value))
                {
                    AdjustEndTimeIfNeeded();
                    _ = CheckConflictAsync();
                }
            }
        }

        public string SelectedEndTime
        {
            get => _selectedEndTime;
            set
            {
                if (SetProperty(ref _selectedEndTime, value))
                {
                    _ = CheckConflictAsync();
                }
            }
        }

        public TechnicianLookupItem SelectedTechnician
        {
            get => _selectedTechnician;
            set
            {
                if (SetProperty(ref _selectedTechnician, value))
                {
                    _ = CheckConflictAsync();
                }
            }
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string ConflictMessage
        {
            get => _conflictMessage;
            set => SetProperty(ref _conflictMessage, value);
        }

        public bool HasConflict
        {
            get => _hasConflict;
            set => SetProperty(ref _hasConflict, value);
        }

        public ObservableCollection<string> AvailableTimeSlots { get; } = new ObservableCollection<string>();
        public ObservableCollection<TechnicianLookupItem> TechniciansList { get; } = new ObservableCollection<TechnicianLookupItem>();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public BookAppointmentDialogViewModel(
            IAppointmentService appointmentService = null,
            ITechnicianService technicianService = null,
            ICustomerService customerService = null)
        {
            _appointmentService = appointmentService ?? AppServiceLocator.AppointmentService;
            _technicianService = technicianService ?? AppServiceLocator.TechnicianService;
            _customerService = customerService ?? AppServiceLocator.CustomerService;

            InitializeTimeSlots();
            LoadTechnicians();

            SaveCommand = new AsyncRelayCommand(SaveAppointmentAsync);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke());
        }

        private void InitializeTimeSlots()
        {
            AvailableTimeSlots.Clear();
            for (int h = 8; h <= 20; h++)
            {
                AvailableTimeSlots.Add($"{h:D2}:00");
                AvailableTimeSlots.Add($"{h:D2}:30");
            }
        }

        private void LoadTechnicians()
        {
            TechniciansList.Clear();
            TechniciansList.Add(new TechnicianLookupItem { Id = null, Name = "بدون تحديد فني محدد" });

            try
            {
                var dt = _technicianService?.GetActiveTechnicians();
                if (dt != null)
                {
                    int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;
                    foreach (DataRow row in dt.Rows)
                    {
                        if (currentBranch.HasValue && row.Table.Columns.Contains("BranchId"))
                        {
                            if (row["BranchId"] != DBNull.Value && Convert.ToInt32(row["BranchId"]) != currentBranch.Value)
                            {
                                continue;
                            }
                        }

                        int id = Convert.ToInt32(row["TechnicianID"]);
                        string name = row["FullName"]?.ToString() ?? $"فني {id}";
                        string spec = row.Table.Columns.Contains("Specialization") ? row["Specialization"]?.ToString() : "";
                        string display = string.IsNullOrWhiteSpace(spec) ? name : $"{name} ({spec})";
                        TechniciansList.Add(new TechnicianLookupItem { Id = id, Name = display });
                    }
                }
            }
            catch
            {
                // Ignore fallback to empty list
            }

            SelectedTechnician = TechniciansList.FirstOrDefault();
        }

        private void OnPhoneChanged()
        {
            if (string.IsNullOrWhiteSpace(CustomerPhone) || CustomerPhone.Trim().Length < 7)
                return;

            try
            {
                var dt = _customerService?.GetCustomerByPhone(CustomerPhone.Trim());
                if (dt != null && dt.Rows.Count > 0)
                {
                    CustomerName = dt.Rows[0]["FullName"]?.ToString() ?? "";
                }
            }
            catch
            {
                // Silently ignore
            }
        }

        private void OnPlateChanged()
        {
            if (string.IsNullOrWhiteSpace(PlateNumber))
                return;

            try
            {
                var v = _customerService?.GetVehicleWithCustomer(PlateNumber.Trim());
                if (v != null && !string.IsNullOrWhiteSpace(v.PlateNumber))
                {
                    CarModel = $"{v.CarName} {v.CarModel}".Trim();
                    if (!string.IsNullOrWhiteSpace(v.CustomerPhone) && string.IsNullOrWhiteSpace(CustomerPhone))
                    {
                        CustomerPhone = v.CustomerPhone;
                    }
                    if (!string.IsNullOrWhiteSpace(v.CustomerName) && string.IsNullOrWhiteSpace(CustomerName))
                    {
                        CustomerName = v.CustomerName;
                    }
                }
            }
            catch
            {
                // Silently ignore
            }
        }

        private void AdjustEndTimeIfNeeded()
        {
            if (TimeSpan.TryParse(SelectedStartTime, out var start) && TimeSpan.TryParse(SelectedEndTime, out var end))
            {
                if (end <= start)
                {
                    var newEnd = start.Add(TimeSpan.FromHours(1));
                    if (newEnd.TotalHours <= 21)
                    {
                        SelectedEndTime = $"{newEnd.Hours:D2}:{newEnd.Minutes:D2}";
                    }
                }
            }
        }

        public async Task CheckConflictAsync()
        {
            if (SelectedTechnician == null || !SelectedTechnician.Id.HasValue)
            {
                HasConflict = false;
                ConflictMessage = "";
                return;
            }

            if (!TimeSpan.TryParse(SelectedStartTime, out var start) || !TimeSpan.TryParse(SelectedEndTime, out var end))
            {
                HasConflict = false;
                ConflictMessage = "";
                return;
            }

            if (end <= start)
            {
                HasConflict = true;
                ConflictMessage = "⚠️ وقت النهاية يجب أن يكون بعد وقت البداية.";
                return;
            }

            try
            {
                bool conflict = await _appointmentService.CheckConflictAsync(
                    SelectedTechnician.Id.Value,
                    AppointmentDate,
                    start,
                    end);

                if (conflict)
                {
                    HasConflict = true;
                    ConflictMessage = $"⚠️ تنبيه: الفني لديه حجز آخر متعارض في هذا التوقيت ({SelectedStartTime} - {SelectedEndTime})!";
                }
                else
                {
                    HasConflict = false;
                    ConflictMessage = "✓ الموعد متاح بدون أي تعارض.";
                }
            }
            catch
            {
                HasConflict = false;
                ConflictMessage = "";
            }
        }

        private async Task SaveAppointmentAsync()
        {
            // Validations
            if (string.IsNullOrWhiteSpace(CustomerPhone))
            {
                WMessageBox.Show("يرجى إدخال رقم هاتف العميل.", "بيانات ناقصة", WMsgIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                WMessageBox.Show("يرجى إدخال اسم العميل.", "بيانات ناقصة", WMsgIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PlateNumber))
            {
                WMessageBox.Show("يرجى إدخال رقم لوحة المركبة.", "بيانات ناقصة", WMsgIcon.Warning);
                return;
            }

            if (!TimeSpan.TryParse(SelectedStartTime, out var start) || !TimeSpan.TryParse(SelectedEndTime, out var end))
            {
                WMessageBox.Show("يرجى اختيار توقيت سليم للموعد.", "تنبيه", WMsgIcon.Warning);
                return;
            }

            if (end <= start)
            {
                WMessageBox.Show("وقت نهاية الموعد يجب أن يكون بعد وقت البداية.", "تنبيه", WMsgIcon.Warning);
                return;
            }

            if (AppointmentDate.Date < DateTime.Today)
            {
                WMessageBox.Show("لا يمكن حجز موعد في تاريخ سابق.", "تنبيه", WMsgIcon.Warning);
                return;
            }

            // Conflict check
            if (SelectedTechnician != null && SelectedTechnician.Id.HasValue)
            {
                bool conflict = await _appointmentService.CheckConflictAsync(
                    SelectedTechnician.Id.Value,
                    AppointmentDate,
                    start,
                    end);

                if (conflict)
                {
                    WMessageBox.Show(
                        $"الفني المختار لديه حجز متعارض بالفعل في هذا التوقيت ({SelectedStartTime} - {SelectedEndTime}).\nيرجى اختيار موعد آخر أو تغيير الفني.",
                        "تعارض في المواعيد",
                        WMsgIcon.Warning);
                    return;
                }
            }

            try
            {
                IsLoading = true;

                // Ensure Customer exists in DB
                string phone = CustomerPhone.Trim();
                string name = CustomerName.Trim();
                string plate = PlateNumber.Trim().ToUpperInvariant();
                string model = string.IsNullOrWhiteSpace(CarModel) ? "غير محدد" : CarModel.Trim();

                if (_customerService != null)
                {
                    try
                    {
                        var custDt = _customerService.GetCustomerByPhone(phone);
                        if (custDt == null || custDt.Rows.Count == 0)
                        {
                            _customerService.RegisterCustomer(phone, name);
                        }

                        var vDto = _customerService.GetVehicleWithCustomer(plate);
                        if (vDto == null || string.IsNullOrWhiteSpace(vDto.PlateNumber))
                        {
                            _customerService.RegisterVehicle(plate, "", phone, model, DateTime.Today.Year, "");
                        }
                    }
                    catch
                    {
                        // Background customer/vehicle registration is best-effort for appointment
                    }
                }

                int branchId = AppServiceLocator.BranchContext?.CurrentBranchId ?? 1;

                var apt = new Appointment
                {
                    CustomerPhone = phone,
                    CustomerName = name,
                    PlateNumber = plate,
                    CarModel = model,
                    TechnicianId = SelectedTechnician?.Id,
                    AppointmentDate = AppointmentDate.Date,
                    StartTime = start,
                    EndTime = end,
                    Status = AppointmentStatus.Confirmed,
                    Notes = Notes?.Trim() ?? "",
                    BranchId = branchId
                };

                int newId = await _appointmentService.CreateAppointmentAsync(apt);
                if (newId > 0)
                {
                    DialogSuccess = true;
                    WMessageBox.Show(
                        $"تم تأكيد وحجز الموعد بنجاح!\nالعميل: {name}\nالمركبة: {plate}\nالتاريخ: {AppointmentDate:yyyy-MM-dd} ({start:hh\\:mm} - {end:hh\\:mm})",
                        "نجاح الحجز",
                        WMsgIcon.Success);

                    CloseAction?.Invoke();
                }
            }
            catch (Exception ex)
            {
                WMessageBox.Show("فشل حفظ الموعد:\n" + ex.Message, "خطأ", WMsgIcon.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
