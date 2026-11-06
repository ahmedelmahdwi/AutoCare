using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class TechnicianDialogViewModel : ViewModelBase
    {
        private readonly ITechnicianService _techService;

        private int _technicianId;
        private string _techName = "";
        private string _phone = "";
        private string _specialization = "ميكانيك عام";
        private string _commissionRate = "10.0";
        private bool _isEditMode;
        private string _selectedAccountStatus = "نشط";

        public int TechnicianId
        {
            get => _technicianId;
            set
            {
                if (SetProperty(ref _technicianId, value))
                {
                    OnPropertyChanged(nameof(TechnicianIdDisplay));
                }
            }
        }

        public string TechnicianIdDisplay => TechnicianId > 0 ? TechnicianId.ToString() : "(يحدد تلقائياً)";

        public string TechName
        {
            get => _techName;
            set => SetProperty(ref _techName, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string Specialization
        {
            get => _specialization;
            set => SetProperty(ref _specialization, value);
        }

        public string CommissionRate
        {
            get => _commissionRate;
            set => SetProperty(ref _commissionRate, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(DialogTitle));
                    OnPropertyChanged(nameof(SaveButtonText));
                }
            }
        }

        public string DialogTitle => IsEditMode ? "تعديل بيانات فني الصيانة" : "إضافة فني صيانة جديد";
        public string SaveButtonText => IsEditMode ? "💾  تعديل بيانات الفني" : "💾  حفظ بيانات الفني";

        public ObservableCollection<string> AccountStatuses { get; } = new ObservableCollection<string> { "نشط", "معطل" };

        public class BranchOptionItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        public ObservableCollection<BranchOptionItem> AvailableBranches { get; } = new ObservableCollection<BranchOptionItem>();

        private BranchOptionItem _selectedBranch;
        public BranchOptionItem SelectedBranch
        {
            get => _selectedBranch;
            set => SetProperty(ref _selectedBranch, value);
        }

        public string SelectedAccountStatus
        {
            get => _selectedAccountStatus;
            set => SetProperty(ref _selectedAccountStatus, value);
        }

        public bool IsActive => SelectedAccountStatus == "نشط";

        public Action CloseAction { get; set; }
        public bool DialogSuccess { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public TechnicianDialogViewModel(ITechnicianService techService = null)
        {
            _techService = techService ?? AppServiceLocator.TechnicianService;
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke());

            LoadBranches();
            InitNew();
        }

        private async void LoadBranches()
        {
            try
            {
                AvailableBranches.Clear();
                var uow = AppServiceLocator.AppUnitOfWork;
                if (uow?.Branches != null)
                {
                    var branches = await uow.Branches.GetAllAsync();
                    foreach (var b in branches)
                    {
                        AvailableBranches.Add(new BranchOptionItem { Id = b.Id, Name = $"🏢 {b.BranchName}" });
                    }
                }

                if (AvailableBranches.Count == 0)
                {
                    AvailableBranches.Add(new BranchOptionItem { Id = 1, Name = "🏢 الفرع الرئيسي - طرابلس" });
                    AvailableBranches.Add(new BranchOptionItem { Id = 2, Name = "🏢 فرع جنزور" });
                }

                if (SelectedBranch == null && AvailableBranches.Count > 0)
                {
                    SelectedBranch = AvailableBranches[0];
                }
            }
            catch
            {
                if (AvailableBranches.Count == 0)
                {
                    AvailableBranches.Add(new BranchOptionItem { Id = 1, Name = "🏢 الفرع الرئيسي" });
                    SelectedBranch = AvailableBranches[0];
                }
            }
        }

        public void InitNew()
        {
            IsEditMode = false;
            try
            {
                TechnicianId = _techService.GetNextTechnicianId();
            }
            catch
            {
                TechnicianId = 1;
            }
            SelectedAccountStatus = "نشط";
            if (AvailableBranches.Count > 0)
            {
                SelectedBranch = AvailableBranches[0];
            }
        }

        public void LoadTechnician(TechnicianRowDto dto)
        {
            if (dto == null) return;

            IsEditMode = true;
            TechnicianId = dto.TechId;
            TechName = dto.TechName ?? "";
            Specialization = string.IsNullOrWhiteSpace(dto.Specialization) ? "ميكانيك عام" : dto.Specialization;
            CommissionRate = dto.CommissionPercent.ToString("0.##");
            SelectedAccountStatus = dto.IsActive ? "نشط" : "معطل";

            foreach (var b in AvailableBranches)
            {
                if (b.Id == dto.BranchId)
                {
                    SelectedBranch = b;
                    break;
                }
            }
        }

        private void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(TechName))
            {
                MessageBox.Show("الرجاء إدخال اسم الفني الكامل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(CommissionRate?.Trim(), out decimal comm) || comm < 0 || comm > 100)
            {
                MessageBox.Show("الرجاء إدخال نسبة عمولة صالحة بين 0 و 100.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int branchId = SelectedBranch?.Id ?? 1;

            OperationResult result;
            if (IsEditMode)
            {
                result = _techService.UpdateTechnician(
                    TechnicianId,
                    TechName.Trim(),
                    Specialization?.Trim() ?? "ميكانيك عام",
                    comm,
                    IsActive,
                    branchId
                );
            }
            else
            {
                if (TechnicianId <= 0)
                {
                    TechnicianId = _techService.GetNextTechnicianId();
                }

                result = _techService.AddTechnician(
                    TechnicianId,
                    TechName.Trim(),
                    Specialization?.Trim() ?? "ميكانيك عام",
                    comm,
                    IsActive,
                    branchId
                );
            }

            if (result.Success)
            {
                DialogSuccess = true;
                MessageBox.Show(result.Message, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseAction?.Invoke();
            }
            else
            {
                MessageBox.Show(result.Message, "خطأ في الحفظ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
