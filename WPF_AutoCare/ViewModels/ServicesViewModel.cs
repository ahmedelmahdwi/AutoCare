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
    /// ViewModel for ServicesScreen.
    /// Manages service catalog operations and search filtering.
    /// Clean Architecture & MVVM.
    /// </summary>
    public class ServicesViewModel : ViewModelBase
    {
        private readonly IServiceCatalogService _catalogService;
        private string _searchText = "";
        private ServiceRowDto _selectedService;

        public ObservableCollection<ServiceRowDto> Services { get; } =
            new ObservableCollection<ServiceRowDto>();

        public bool CanManageServices => !string.Equals(Models.UserSession.Role, "Accountant", StringComparison.OrdinalIgnoreCase);
        public System.Windows.Visibility ManagementVisibility => CanManageServices ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public ICommand RefreshCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand AddServiceCommand { get; }
        public ICommand EditServiceCommand { get; }

        public ServicesViewModel(IServiceCatalogService catalogService)
        {
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
            RefreshCommand = new AsyncRelayCommand(LoadServicesAsync);
            DeleteSelectedCommand = new RelayCommand(() => DeleteSelectedService(), () => SelectedService != null && CanManageServices);

            AddServiceCommand = new RelayCommand(() =>
            {
                if (!CanManageServices)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بإضافة أو تعديل الخدمات (اطلاع فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowAddService(null, () => _ = LoadServicesAsync());
            });

            EditServiceCommand = new RelayCommand(() =>
            {
                if (!CanManageServices)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بإضافة أو تعديل الخدمات (اطلاع فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                if (SelectedService == null)
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد خدمة من الجدول لتعديلها.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowEditService(SelectedService, null, () => _ = LoadServicesAsync());
            });

            LoadServices();
        }

        public ServicesViewModel(IRepairOrderService repairOrderService)
            : this((IServiceCatalogService)repairOrderService)
        {
        }

        #region Properties
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadServicesAsync();
                }
            }
        }

        public ServiceRowDto SelectedService
        {
            get => _selectedService;
            set => SetProperty(ref _selectedService, value);
        }
        #endregion

        public async Task LoadServicesAsync()
        {
            string search = _searchText?.Trim() ?? "";

            await RunAsync(async () =>
            {
                var list = await Task.Run(() => _catalogService.GetServicesCatalog(search));
                RunOnUIThread(() =>
                {
                    Services.Clear();
                    if (list != null)
                    {
                        foreach (var s in list)
                        {
                            Services.Add(s);
                        }
                    }
                });
            }, "جاري تحميل قائمة الخدمات...");
        }

        public void LoadServices()
        {
            IsLoading = true;
            try
            {
                var list = _catalogService.GetServicesCatalog(_searchText);
                Services.Clear();
                if (list != null)
                {
                    foreach (var s in list)
                    {
                        Services.Add(s);
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public OperationResult DeleteSelectedService()
        {
            if (!CanManageServices)
                return OperationResult.Fail("غير مصرح للمحاسب بحذف الخدمات (اطلاع فقط).");

            if (SelectedService == null || string.IsNullOrWhiteSpace(SelectedService.ServiceCode))
                return OperationResult.Fail("الرجاء اختيار خدمة لحذفها.");

            var res = _catalogService.DeleteServiceFromCatalog(SelectedService.ServiceCode);
            if (res.Success)
            {
                _ = LoadServicesAsync();
            }
            return res;
        }
    }
}
