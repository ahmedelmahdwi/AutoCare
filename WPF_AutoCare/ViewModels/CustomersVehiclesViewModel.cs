using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for CustomersVehiclesScreen.
    /// Manages vehicles, customer visits, repair order statuses, and operations.
    /// Clean Architecture & MVVM.
    /// </summary>
    public class CustomersVehiclesViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;
        private readonly IRepairOrderService _repairOrderService;

        private string _searchText = "";
        private string _statusFilter = "الكل";
        private CustomerVehicleRowDto _selectedRow;

        public ObservableCollection<CustomerVehicleRowDto> Rows { get; } =
            new ObservableCollection<CustomerVehicleRowDto>();

        public ICommand RefreshCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand OpenNewOrderCommand { get; }
        public ICommand EditSelectedOrderCommand { get; }
        public ICommand ViewVehicleHistoryCommand { get; }

        public CustomersVehiclesViewModel(ICustomerService customerService, IRepairOrderService repairOrderService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _repairOrderService = repairOrderService ?? throw new ArgumentNullException(nameof(repairOrderService));

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            DeleteSelectedCommand = new RelayCommand(() => DeleteSelected(), () => SelectedRow != null);

            OpenNewOrderCommand = new RelayCommand(() =>
            {
                Views.DialogCoordinator.ShowNewRepairOrder(null, null, () => _ = LoadDataAsync());
            });

            EditSelectedOrderCommand = new RelayCommand(() =>
            {
                if (SelectedRow == null || string.IsNullOrWhiteSpace(SelectedRow.RoNumber))
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد أمر إصلاح من الجدول لتعديله.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowEditRepairOrder(SelectedRow.RoNumber, null, () => _ = LoadDataAsync());
            });

            ViewVehicleHistoryCommand = new RelayCommand(() =>
            {
                if (SelectedRow == null || string.IsNullOrWhiteSpace(SelectedRow.PlateNumber))
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد سيارة من الجدول لعرض تاريخها.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowVehicleHistory(SelectedRow.PlateNumber);
            });

            LoadData();
        }

        #region Properties
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        public CustomerVehicleRowDto SelectedRow
        {
            get => _selectedRow;
            set => SetProperty(ref _selectedRow, value);
        }
        #endregion

        /// <summary>
        /// Loads vehicle and customer data asynchronously without freezing the UI thread.
        /// </summary>
        public async Task LoadDataAsync()
        {
            string search = _searchText?.Trim() ?? "";
            string filter = _statusFilter ?? "الكل";

            await RunAsync(async () =>
            {
                var list = await Task.Run(() => _customerService.GetVehiclesWithCustomersList(search, filter));
                RunOnUIThread(() =>
                {
                    Rows.Clear();
                    if (list != null)
                    {
                        foreach (var r in list)
                        {
                            Rows.Add(r);
                        }
                    }
                });
            }, "جاري تحميل بيانات المركبات والعملاء...");
        }

        /// <summary>
        /// Synchronous load method for backward-compatibility.
        /// </summary>
        public void LoadData()
        {
            IsLoading = true;
            try
            {
                var list = _customerService.GetVehiclesWithCustomersList(_searchText, _statusFilter);
                Rows.Clear();
                if (list != null)
                {
                    foreach (var r in list)
                    {
                        Rows.Add(r);
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public OperationResult DeleteSelected()
        {
            if (SelectedRow == null)
            {
                System.Windows.MessageBox.Show("الرجاء تحديد صف من الجدول أولاً.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return OperationResult.Fail("الرجاء تحديد صف من الجدول أولاً.");
            }

            if (!string.IsNullOrEmpty(SelectedRow.RoNumber))
            {
                var confirm = System.Windows.MessageBox.Show(
                    $"هل أنت متأكد من إلغاء أمر الإصلاح ({SelectedRow.RoNumber}) للسيارة ({SelectedRow.CarName} - {SelectedRow.PlateNumber})؟\n\nسيتم نقله إلى حالة 'ملغى' وإخراجه من أوامر الإصلاح النشطة.",
                    "تأكيد إلغاء أمر الإصلاح",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (confirm != System.Windows.MessageBoxResult.Yes)
                    return OperationResult.Fail("تم التراجع عن الإلغاء.");

                var res = _repairOrderService.CancelRepairOrder(SelectedRow.RoNumber, "إلغاء من شاشة العملاء والمركبات");
                if (res.Success)
                {
                    System.Windows.MessageBox.Show(res.Message, "تم الإلغاء", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    _ = LoadDataAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "تعذر الإلغاء", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                return res;
            }
            else
            {
                if (string.IsNullOrEmpty(SelectedRow.PlateNumber))
                {
                    System.Windows.MessageBox.Show("لم يتم العثور على رقم اللوحة الخاص بالمركبة المحددة.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return OperationResult.Fail("لم يتم العثور على رقم اللوحة الخاص بالمركبة المحددة.");
                }

                var confirm = System.Windows.MessageBox.Show(
                    $"هل أنت متأكد من نقل المركبة ({SelectedRow.CarName} - {SelectedRow.PlateNumber}) إلى سلة المحذوفات؟\n\nيمكنك استعادتها أو حذفها نهائياً من شاشة سلة المحذوفات.",
                    "تأكيد نقل المركبة لسلة المحذوفات",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (confirm != System.Windows.MessageBoxResult.Yes)
                    return OperationResult.Fail("تم التراجع عن الحذف.");

                var res = _customerService.DeleteVehicle(SelectedRow.PlateNumber);
                if (res.Success)
                {
                    System.Windows.MessageBox.Show(res.Message, "سلة المحذوفات", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    _ = LoadDataAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "تعذر الحذف", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                return res;
            }
        }
    }
}
