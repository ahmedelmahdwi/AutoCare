using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoCare.Domain.Entities;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Theme;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class AppointmentsViewModel : ViewModelBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ITechnicianService _technicianService;
        private readonly ICustomerService _customerService;

        private DateTime _selectedDate = DateTime.Today;
        private string _searchText = string.Empty;
        private string _selectedStatusFilter = "الكل";
        private Appointment _selectedAppointment;

        private int _totalTodayCount;
        private int _confirmedCount;
        private int _inProgressCount;
        private int _completedCount;

        public event Action<string> RequestOpenRepairOrder;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    OnPropertyChanged(nameof(SelectedDateFormatted));
                    _ = LoadAppointmentsAsync();
                }
            }
        }

        public string SelectedDateFormatted => _selectedDate.ToString("yyyy-MM-dd (dddd)", new System.Globalization.CultureInfo("ar-LY"));

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public Appointment SelectedAppointment
        {
            get => _selectedAppointment;
            set => SetProperty(ref _selectedAppointment, value);
        }


        public int TotalTodayCount { get => _totalTodayCount; private set => SetProperty(ref _totalTodayCount, value); }
        public int ConfirmedCount { get => _confirmedCount; private set => SetProperty(ref _confirmedCount, value); }
        public int InProgressCount { get => _inProgressCount; private set => SetProperty(ref _inProgressCount, value); }
        public int CompletedCount { get => _completedCount; private set => SetProperty(ref _completedCount, value); }

        public ObservableCollection<Appointment> AppointmentsList { get; } = new ObservableCollection<Appointment>();
        public ObservableCollection<Appointment> FilteredAppointments { get; } = new ObservableCollection<Appointment>();

        public ICommand RefreshCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand NewAppointmentCommand { get; }
        public ICommand ConvertToOrderCommand { get; }
        public ICommand CancelAppointmentCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public AppointmentsViewModel(
            IAppointmentService appointmentService = null,
            ITechnicianService technicianService = null,
            ICustomerService customerService = null)
        {
            _appointmentService = appointmentService ?? AppServiceLocator.AppointmentService;
            _technicianService = technicianService ?? AppServiceLocator.TechnicianService;
            _customerService = customerService ?? AppServiceLocator.CustomerService;

            RefreshCommand = new AsyncRelayCommand(LoadAppointmentsAsync);
            PreviousDayCommand = new RelayCommand(() => SelectedDate = SelectedDate.AddDays(-1));
            NextDayCommand = new RelayCommand(() => SelectedDate = SelectedDate.AddDays(1));
            TodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);

            NewAppointmentCommand = new RelayCommand(OpenNewAppointmentDialog);
            ConvertToOrderCommand = new RelayCommand<Appointment>(async apt => await ConvertToOrderAsync(apt));
            CancelAppointmentCommand = new RelayCommand<Appointment>(async apt => await CancelAppointmentAsync(apt));
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);

            if (AppServiceLocator.BranchContext != null)
            {
                AppServiceLocator.BranchContext.BranchChanged += () => _ = LoadAppointmentsAsync();
            }

            _ = LoadAppointmentsAsync();
        }

        public async Task LoadAppointmentsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;
                var items = await _appointmentService.GetAppointmentsByDateAsync(SelectedDate, currentBranch);

                AppointmentsList.Clear();
                int total = 0, confirmed = 0, inProg = 0, completed = 0;

                foreach (var item in items)
                {
                    AppointmentsList.Add(item);
                    total++;
                    if (item.Status == AppointmentStatus.Confirmed || item.Status == AppointmentStatus.Pending) confirmed++;
                    else if (item.Status == AppointmentStatus.InProgress) inProg++;
                    else if (item.Status == AppointmentStatus.Completed) completed++;
                }

                TotalTodayCount = total;
                ConfirmedCount = confirmed;
                InProgressCount = inProg;
                CompletedCount = completed;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                WMessageBox.Show("تعذر تحميل المواعيد:\n" + ex.Message, "خطأ", WMsgIcon.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            FilteredAppointments.Clear();
            string query = (SearchText ?? "").Trim().ToLowerInvariant();

            foreach (var item in AppointmentsList)
            {
                // Status Filter
                if (SelectedStatusFilter != "الكل" && item.StatusArabic != SelectedStatusFilter)
                {
                    continue;
                }

                // Text search
                if (!string.IsNullOrEmpty(query))
                {
                    bool match = (item.CustomerName ?? "").ToLowerInvariant().Contains(query) ||
                                 (item.CustomerPhone ?? "").Contains(query) ||
                                 (item.PlateNumber ?? "").ToLowerInvariant().Contains(query) ||
                                 (item.TechnicianName ?? "").ToLowerInvariant().Contains(query) ||
                                 (item.AppointmentNumber ?? "").ToLowerInvariant().Contains(query);

                    if (!match) continue;
                }

                FilteredAppointments.Add(item);
            }
        }

        private async Task ConvertToOrderAsync(Appointment apt)
        {
            if (apt == null) return;

            if (!apt.CanConvertToOrder)
            {
                WMessageBox.Show($"لا يمكن تحويل هذا الموعد؛ حالته الحالية: {apt.StatusArabic}", "تنبيه", WMsgIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"هل ترغب في تحويل موعد العميل ({apt.CustomerName}) للسيارة ذات اللوحة ({apt.PlateNumber}) إلى كرت صيانة جديد الآن؟",
                "تأكيد تحويل الموعد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                string username = Models.UserSession.Username ?? "admin";
                string roNumber = await _appointmentService.ConvertAppointmentToRepairOrderAsync(apt.Id, username);

                WMessageBox.Show(
                    $"تم بنجاح تحويل الموعد إلى كرت صيانة جديد برقم:\n{roNumber}\n\nيمكنك الآن استكمال إضافة الفحوصات والخدمات للسيارة.",
                    "نجاح التحويل",
                    WMsgIcon.Success);

                await LoadAppointmentsAsync();
                RequestOpenRepairOrder?.Invoke(roNumber);
            }
            catch (Exception ex)
            {
                WMessageBox.Show("فشل تحويل الموعد إلى أمر صيانة:\n" + ex.Message, "خطأ في التحويل", WMsgIcon.Error);
            }
        }

        private async Task CancelAppointmentAsync(Appointment apt)
        {
            if (apt == null) return;

            if (!apt.CanCancel)
            {
                WMessageBox.Show($"لا يمكن إلغاء هذا الموعد؛ حالته الحالية: {apt.StatusArabic}", "تنبيه", WMsgIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"هل أنت متأكد من إلغاء موعد العميل ({apt.CustomerName}) رقم ({apt.AppointmentNumber})؟",
                "تأكيد الإلغاء",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool ok = await _appointmentService.CancelAppointmentAsync(apt.Id);
                if (ok)
                {
                    WMessageBox.Show("تم إلغاء الموعد بنجاح.", "تم الإلغاء", WMsgIcon.Info);
                    await LoadAppointmentsAsync();
                }
            }
            catch (Exception ex)
            {
                WMessageBox.Show("حدث خطأ أثناء إلغاء الموعد:\n" + ex.Message, "خطأ", WMsgIcon.Error);
            }
        }

        private void OpenNewAppointmentDialog()
        {
            try
            {
                Views.DialogCoordinator.ShowBookAppointment(null, () => _ = LoadAppointmentsAsync());
            }
            catch (Exception ex)
            {
                WMessageBox.Show("تعذر فتح نافذة حجز موعد جديد:\n" + ex.Message, "خطأ", WMsgIcon.Error);
            }
        }
    }
}
