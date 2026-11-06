using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for CarStatusScreen.
    /// Manages active repair orders in workshop with asynchronous loading.
    /// Clean MVVM & Interface Segregation.
    /// </summary>
    public class CarStatusViewModel : ViewModelBase
    {
        private readonly IRepairOrderLifecycleService _lifecycleService;
        private string _searchText = "";
        private string _selectedFilter = "الكل";
        private CarStatusRowDto _selectedOrder;

        public ObservableCollection<CarStatusRowDto> Orders { get; } = new ObservableCollection<CarStatusRowDto>();

        public ICommand RefreshCommand { get; }
        public ICommand CompleteOrderCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand DeleteOrderCommand { get; }
        public ICommand AddServicesCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand OrderDetailsCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    LoadData();
                }
            }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    LoadData();
                }
            }
        }

        public CarStatusRowDto SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        /// <summary>
        /// Preferred Clean Architecture constructor injecting specific lifecycle service.
        /// </summary>
        public CarStatusViewModel(IRepairOrderLifecycleService lifecycleService)
        {
            _lifecycleService = lifecycleService ?? AppServiceLocator.RepairOrderLifecycleService;
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            CompleteOrderCommand = new RelayCommand(() => CompleteSelectedOrder(), () => SelectedOrder != null);
            CancelOrderCommand = new RelayCommand(() => CancelSelectedOrder(), () => SelectedOrder != null);
            DeleteOrderCommand = new RelayCommand(() => DeleteSelectedOrder(), () => SelectedOrder != null);

            AddServicesCommand = new RelayCommand(() =>
            {
                if (SelectedOrder == null || string.IsNullOrWhiteSpace(SelectedOrder.RoNumber))
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد سيارة من الجدول لإضافة الخدمات إليها.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowServiceAndParts(SelectedOrder.RoNumber, null, () => _ = LoadDataAsync());
            });

            EditOrderCommand = new RelayCommand(() =>
            {
                if (SelectedOrder == null || string.IsNullOrWhiteSpace(SelectedOrder.RoNumber))
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد سيارة من الجدول لتعديل أمر الإصلاح.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowEditRepairOrder(SelectedOrder.RoNumber, null, () => _ = LoadDataAsync());
            });

            OrderDetailsCommand = new RelayCommand(() =>
            {
                if (SelectedOrder == null || string.IsNullOrWhiteSpace(SelectedOrder.RoNumber))
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد سيارة من الجدول لعرض تفاصيلها.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowServiceAndParts(SelectedOrder.RoNumber, null, () => _ = LoadDataAsync());
            });

            if (AppServiceLocator.BranchContext != null)
            {
                AppServiceLocator.BranchContext.BranchChanged += () => LoadData();
            }

            LoadData();
        }

        /// <summary>
        /// Backward-compatible constructor for existing callers passing IRepairOrderService facade.
        /// </summary>
        public CarStatusViewModel(IRepairOrderService roService)
            : this((IRepairOrderLifecycleService)roService)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CarStatusViewModel()
            : this((IRepairOrderLifecycleService)null)
        {
        }

        public async Task LoadDataAsync()
        {
            string search = _searchText?.Trim() ?? "";
            string filter = _selectedFilter ?? "الكل";

            await RunAsync(async () =>
            {
                var list = await Task.Run(() => _lifecycleService.GetCarStatusList(search, filter));
                RunOnUIThread(() =>
                {
                    Orders.Clear();
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            Orders.Add(item);
                        }
                    }
                });
            }, "جاري تحديث حالات السيارات بالورشة...");
        }

        public void LoadData()
        {
            try
            {
                var list = _lifecycleService.GetCarStatusList(_searchText, _selectedFilter);
                Orders.Clear();
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        Orders.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("CarStatusViewModel.LoadData failed", ex);
            }
        }

        public string GetOrderStatus(string roNumber)
        {
            return _lifecycleService.GetRepairOrderStatus(roNumber);
        }

        public OperationResult CompleteSelectedOrder()
        {
            if (SelectedOrder == null)
                return OperationResult.Fail("الرجاء تحديد أمر إصلاح من الجدول لإكماله.");

            var result = _lifecycleService.CompleteRepairOrder(SelectedOrder.RoNumber);
            if (result.Success)
            {
                _ = LoadDataAsync();
            }
            return result;
        }

        public OperationResult CancelSelectedOrder()
        {
            if (SelectedOrder == null || string.IsNullOrWhiteSpace(SelectedOrder.RoNumber))
            {
                System.Windows.MessageBox.Show("الرجاء تحديد سيارة من الجدول لإلغاء أمر إصلاحها.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return OperationResult.Fail("لم يتم تحديد أمر إصلاح.");
            }

            var confirm = System.Windows.MessageBox.Show(
                $"هل أنت متأكد من إلغاء أمر الإصلاح رقم ({SelectedOrder.RoNumber}) للسيارة ({SelectedOrder.CarName} - {SelectedOrder.PlateNumber})؟\n\nسيتم نقل حالة الأمر إلى 'ملغى' وإخراجه من قائمة الورشة النشطة.",
                "تأكيد إلغاء أمر الإصلاح",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes)
                return OperationResult.Fail("تم التراجع عن الإلغاء.");

            var result = _lifecycleService.CancelRepairOrder(SelectedOrder.RoNumber, "إلغاء من شاشة متابعة حالة السيارات");
            if (result.Success)
            {
                System.Windows.MessageBox.Show(result.Message, "تم الإلغاء", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _ = LoadDataAsync();
            }
            else
            {
                System.Windows.MessageBox.Show(result.Message, "تعذر الإلغاء", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
            return result;
        }

        public OperationResult DeleteSelectedOrder()
        {
            if (!string.Equals(Models.UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.MessageBox.Show("عذراً، صلاحية الحذف التام مخصصة للمدير (Admin) فقط للأوامر التي فُتحت بالخطأ البحت.", "صلاحية غير كافية", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Stop);
                return OperationResult.Fail("صلاحية غير كافية.");
            }

            if (SelectedOrder == null || string.IsNullOrWhiteSpace(SelectedOrder.RoNumber))
            {
                System.Windows.MessageBox.Show("الرجاء تحديد أمر إصلاح من الجدول لحذفه.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return OperationResult.Fail("لم يتم تحديد أمر إصلاح.");
            }

            var confirm = System.Windows.MessageBox.Show(
                $"⚠️ تنبيه أمني هام:\nالحذف التام مخصص فقط للأوامر التي فُتحت بالخطأ البحت (مثل فتح كرت مكرر).\n\nهل تريد بالتأكيد حذف أمر الإصلاح ({SelectedOrder.RoNumber}) نهائياً من قاعدة البيانات؟",
                "تأكيد الحذف النهائي (للأدمن فقط)",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes)
                return OperationResult.Fail("تم التراجع عن الحذف.");

            var roService = AppServiceLocator.RepairOrderService;
            var result = roService.DeleteRepairOrder(SelectedOrder.RoNumber);
            if (result.Success)
            {
                System.Windows.MessageBox.Show(result.Message, "تم الحذف النهائي", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _ = LoadDataAsync();
            }
            else
            {
                System.Windows.MessageBox.Show(result.Message, "تعذر الحذف", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return result;
        }
    }
}
