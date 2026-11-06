using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class RecycleBinViewModel : ViewModelBase
    {
        private readonly IRecycleBinService _recycleBinService;
        private string _searchText = "";
        private string _selectedFilter = "الكل";
        private RecycleBinItemDto _selectedItem;
        private int _totalDeletedCount;
        private int _eligibleForPurgeCount;
        private int _protectedCount;
        private bool _isBusy;

        public ObservableCollection<RecycleBinItemDto> Items { get; } = new ObservableCollection<RecycleBinItemDto>();

        public ICommand RefreshCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand PurgeCommand { get; }
        public ICommand PurgeAllEligibleCommand { get; }

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

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        public RecycleBinItemDto SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public int TotalDeletedCount
        {
            get => _totalDeletedCount;
            set => SetProperty(ref _totalDeletedCount, value);
        }

        public int EligibleForPurgeCount
        {
            get => _eligibleForPurgeCount;
            set => SetProperty(ref _eligibleForPurgeCount, value);
        }

        public int ProtectedCount
        {
            get => _protectedCount;
            set => SetProperty(ref _protectedCount, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public RecycleBinViewModel(IRecycleBinService recycleBinService = null)
        {
            _recycleBinService = recycleBinService ?? AppServiceLocator.RecycleBinService;

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            RestoreCommand = new RelayCommand<RecycleBinItemDto>(ExecuteRestore);
            PurgeCommand = new RelayCommand<RecycleBinItemDto>(ExecutePurge);
            PurgeAllEligibleCommand = new AsyncRelayCommand(ExecutePurgeAllEligibleAsync);

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                string filter = SelectedFilter ?? "الكل";
                string search = SearchText ?? "";

                var list = await _recycleBinService.GetDeletedItemsAsync(filter, search);

                Items.Clear();
                int eligible = 0;
                int protectedCount = 0;

                if (list != null)
                {
                    foreach (var item in list)
                    {
                        Items.Add(item);
                        if (item.CanPurge) eligible++;
                        else protectedCount++;
                    }
                }

                TotalDeletedCount = Items.Count;
                EligibleForPurgeCount = eligible;
                ProtectedCount = protectedCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل سلة المحذوفات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteRestore(RecycleBinItemDto item)
        {
            if (item == null) return;

            string prompt = $"هل أنت متأكد من رغبتك في استعادة {item.ItemType}: \"{item.DisplayName}\" ({item.Key}) إلى النظام النشط؟";
            var res = MessageBox.Show(prompt, "تأكيد الاستعادة", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            var result = _recycleBinService.RestoreItem(item.ItemType, item.Key);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = LoadDataAsync();
            }
            else
            {
                MessageBox.Show(result.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExecutePurge(RecycleBinItemDto item)
        {
            if (item == null) return;

            if (!item.CanPurge)
            {
                MessageBox.Show($"لا يمكن الحذف النهائي:\n{item.StatusReason}\nلحماية الحسابات والتاريخ المالي للورشة.", "إجراء محمي وممنوع", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string prompt = $"تحذير خطير: سيتم حذف {item.ItemType}: \"{item.DisplayName}\" ({item.Key}) نهائياً وبلا رجعة من قاعدة البيانات!\n\nهل تريد المتابعة بالتأكيد؟";
            var res = MessageBox.Show(prompt, "تأكيد الحذف النهائي الصارم", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            var result = _recycleBinService.PurgeItem(item.ItemType, item.Key);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "تم الحذف النهائي", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = LoadDataAsync();
            }
            else
            {
                MessageBox.Show(result.Message, "فشل الحذف", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecutePurgeAllEligibleAsync()
        {
            if (EligibleForPurgeCount == 0)
            {
                MessageBox.Show("لا توجد أي سجلات مؤهلة للحذف النهائي حالياً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string prompt = $"تحذير إداري: سيتم حذف كافة السجلات المؤهلة غير المرتبطة بحسابات ({EligibleForPurgeCount} سجل) نهائياً من قاعدة البيانات!\n\nهل أنت متأكد تماماً من تفريغ المحذوفات؟";
            var res = MessageBox.Show(prompt, "تأكيد تفريغ سلة المحذوفات", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            var result = await Task.Run(() => _recycleBinService.PurgeAllEligible());
            if (result.Success)
            {
                MessageBox.Show(result.Message, "تم التفريغ بنجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show(result.Message, "خطأ أثناء التفريغ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
