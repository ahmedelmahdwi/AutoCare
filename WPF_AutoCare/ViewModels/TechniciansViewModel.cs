using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Data;

namespace C__project_Auto_Care.ViewModels
{
    public class TechniciansViewModel : INotifyPropertyChanged
    {
        private readonly ITechnicianService _techService;

        private string _searchText = "";
        private TechnicianSummaryDto _selectedTechFilter;
        private string _selectedSpecFilter = "الكل";
        private TechnicianRowDto _selectedTechnician;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TechnicianRowDto> Technicians { get; } = new ObservableCollection<TechnicianRowDto>();
        public ObservableCollection<TechnicianSummaryDto> ActiveTechsFilter { get; } = new ObservableCollection<TechnicianSummaryDto>();
        public ObservableCollection<string> SpecializationsFilter { get; } = new ObservableCollection<string>();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    LoadData();
                }
            }
        }

        public TechnicianSummaryDto SelectedTechFilter
        {
            get => _selectedTechFilter;
            set
            {
                if (_selectedTechFilter != value)
                {
                    _selectedTechFilter = value;
                    OnPropertyChanged();
                    LoadData();
                }
            }
        }

        public string SelectedSpecFilter
        {
            get => _selectedSpecFilter;
            set
            {
                if (_selectedSpecFilter != value)
                {
                    _selectedSpecFilter = value;
                    OnPropertyChanged();
                    LoadData();
                }
            }
        }

        public TechnicianRowDto SelectedTechnician
        {
            get => _selectedTechnician;
            set
            {
                if (_selectedTechnician != value)
                {
                    _selectedTechnician = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanManageTechnicians => !string.Equals(Models.UserSession.Role, "Accountant", StringComparison.OrdinalIgnoreCase);
        public System.Windows.Visibility ManagementVisibility => CanManageTechnicians ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public System.Windows.Input.ICommand AddTechnicianCommand { get; }
        public System.Windows.Input.ICommand EditTechnicianCommand { get; }
        public System.Windows.Input.ICommand DeleteTechnicianCommand { get; }

        public TechniciansViewModel(ITechnicianService techService = null)
        {
            _techService = techService ?? AppServiceLocator.TechnicianService;

            AddTechnicianCommand = new Commands.RelayCommand(() =>
            {
                if (!CanManageTechnicians)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بإضافة فنيين (اطلاع فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowAddTechnician(null, () =>
                {
                    LoadFilters();
                    LoadData();
                });
            });

            EditTechnicianCommand = new Commands.RelayCommand(() =>
            {
                if (!CanManageTechnicians)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بتعديل بيانات الفنيين (اطلاع فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                if (SelectedTechnician == null)
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد فني من الجدول لتعديله.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowEditTechnician(SelectedTechnician, null, () =>
                {
                    LoadFilters();
                    LoadData();
                });
            });

            DeleteTechnicianCommand = new Commands.RelayCommand(() =>
            {
                if (!CanManageTechnicians)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بحذف الفنيين.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                if (SelectedTechnician == null)
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد فني من الجدول لحذفه.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var confirm = System.Windows.MessageBox.Show($"هل أنت متأكد من رغبتك في حذف الفني '{SelectedTechnician.TechName}'؟", "تأكيد الحذف", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (confirm != System.Windows.MessageBoxResult.Yes) return;

                var res = DeleteSelectedTechnician();
                if (res.Success)
                {
                    System.Windows.MessageBox.Show(res.Message, "نجاح", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    LoadFilters();
                    LoadData();
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            });

            LoadFilters();
            LoadData();
        }

        public void LoadFilters()
        {
            try
            {
                ActiveTechsFilter.Clear();
                ActiveTechsFilter.Add(new TechnicianSummaryDto { TechnicianId = 0, FullName = "الكل" });
                var activeList = _techService.GetActiveTechniciansList();
                foreach (var t in activeList)
                {
                    ActiveTechsFilter.Add(t);
                }

                SpecializationsFilter.Clear();
                SpecializationsFilter.Add("الكل");
                var specs = _techService.GetSpecializations();
                foreach (var s in specs)
                {
                    SpecializationsFilter.Add(s);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("TechniciansViewModel.LoadFilters failed", ex);
            }
        }

        public void LoadData()
        {
            try
            {
                string spec = (_selectedSpecFilter == "الكل" || string.IsNullOrEmpty(_selectedSpecFilter)) ? "" : _selectedSpecFilter;
                int? selectedTechId = (_selectedTechFilter != null && _selectedTechFilter.TechnicianId > 0) ? (int?)_selectedTechFilter.TechnicianId : null;

                var list = _techService.GetTechniciansList(_searchText, spec);
                Technicians.Clear();
                foreach (var item in list)
                {
                    if (selectedTechId.HasValue && item.TechId != selectedTechId.Value)
                        continue;

                    Technicians.Add(item);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("TechniciansViewModel.LoadData failed", ex);
            }
        }

        public OperationResult DeleteSelectedTechnician()
        {
            if (SelectedTechnician == null)
                return OperationResult.Fail("الرجاء تحديد فني من الجدول.");

            return _techService.DeleteTechnician(SelectedTechnician.TechId);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
