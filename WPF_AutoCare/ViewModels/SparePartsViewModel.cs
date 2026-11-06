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
    /// ViewModel for SparePartsScreen.
    /// Manages spare parts inventory querying and operations.
    /// Clean Architecture & MVVM.
    /// </summary>
    public class SparePartsViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventoryService;
        private string _searchText = "";
        private SparePartRowDto _selectedPart;

        public ObservableCollection<SparePartRowDto> Parts { get; } =
            new ObservableCollection<SparePartRowDto>();

        public bool CanManageSpareParts => !string.Equals(Models.UserSession.Role, "Accountant", StringComparison.OrdinalIgnoreCase);
        public System.Windows.Visibility ManagementVisibility => CanManageSpareParts ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public ICommand RefreshCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand AddPartCommand { get; }
        public ICommand EditPartCommand { get; }

        public SparePartsViewModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            RefreshCommand = new AsyncRelayCommand(LoadPartsAsync);
            DeleteSelectedCommand = new RelayCommand(() => DeleteSelectedPart(), () => SelectedPart != null && CanManageSpareParts);

            AddPartCommand = new RelayCommand(() =>
            {
                if (!CanManageSpareParts)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بإضافة أو تعديل قطع الغيار (اطلاع فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowAddSparePart(null, () => _ = LoadPartsAsync());
            });

            EditPartCommand = new RelayCommand(() =>
            {
                if (!CanManageSpareParts)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بإضافة أو تعديل قطع الغيار (اطلاع فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                if (SelectedPart == null)
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد قطعة غيار من الجدول لتعديلها.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowEditSparePart(SelectedPart, null, () => _ = LoadPartsAsync());
            });

            LoadParts();
        }

        private bool _isLowStockOnly;

        #region Properties
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadPartsAsync();
                }
            }
        }

        public bool IsLowStockOnly
        {
            get => _isLowStockOnly;
            set
            {
                if (SetProperty(ref _isLowStockOnly, value))
                {
                    _ = LoadPartsAsync();
                }
            }
        }

        public SparePartRowDto SelectedPart
        {
            get => _selectedPart;
            set => SetProperty(ref _selectedPart, value);
        }
        #endregion

        public async Task LoadPartsAsync()
        {
            string search = _searchText?.Trim() ?? "";

            await RunAsync(async () =>
            {
                var list = await Task.Run(() => _inventoryService.GetSparePartsList(search));
                if (_isLowStockOnly && list != null)
                {
                    list = list.FindAll(p =>
                    {
                        int q = 0, m = 0;
                        int.TryParse(p.Qty, out q);
                        int.TryParse(p.MinStock, out m);
                        return q <= m;
                    });
                }

                RunOnUIThread(() =>
                {
                    Parts.Clear();
                    if (list != null)
                    {
                        foreach (var p in list)
                        {
                            Parts.Add(p);
                        }
                    }
                });
            }, "جاري تحميل قطع الغيار...");
        }

        public void LoadParts()
        {
            IsLoading = true;
            try
            {
                var list = _inventoryService.GetSparePartsList(_searchText);
                if (_isLowStockOnly && list != null)
                {
                    list = list.FindAll(p =>
                    {
                        int q = 0, m = 0;
                        int.TryParse(p.Qty, out q);
                        int.TryParse(p.MinStock, out m);
                        return q <= m;
                    });
                }

                Parts.Clear();
                if (list != null)
                {
                    foreach (var p in list)
                    {
                        Parts.Add(p);
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public OperationResult DeleteSelectedPart()
        {
            if (!CanManageSpareParts)
                return OperationResult.Fail("غير مصرح للمحاسب بحذف قطع الغيار (اطلاع فقط).");

            if (SelectedPart == null || string.IsNullOrWhiteSpace(SelectedPart.PartCode))
                return OperationResult.Fail("الرجاء اختيار قطعة غيار لحذفها.");

            var res = _inventoryService.DeleteSparePart(SelectedPart.PartCode);
            if (res.Success)
            {
                _ = LoadPartsAsync();
            }
            return res;
        }
    }
}
