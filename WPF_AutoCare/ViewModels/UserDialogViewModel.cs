using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class UserDialogViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        private string _username = "";
        private string _fullName = "";
        private string _password = "";
        private string _role = "موظف استقبال";
        private bool _isEditMode;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
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
                    OnPropertyChanged(nameof(PasswordLabel));
                    OnPropertyChanged(nameof(IsUsernameEnabled));
                }
            }
        }

        public string DialogTitle => IsEditMode ? "تعديل بيانات المستخدم" : "إضافة مستخدم جديد للنظام";
        public string SaveButtonText => IsEditMode ? "💾  تعديل الحساب" : "💾  حفظ المستخدم";
        public string PasswordLabel => IsEditMode ? "كلمة المرور الجديدة (اتركه فارغاً للإبقاء على الحالية):" : "كلمة المرور:";
        public bool IsUsernameEnabled => !IsEditMode;

        public List<string> Roles { get; } = new List<string>
        {
            "موظف استقبال", "محاسب", "مدير النظام"
        };

        public List<string> AccountStatuses { get; } = new List<string>
        {
            "مفعل", "معطل"
        };

        private string _selectedAccountStatus = "مفعل";
        public string SelectedAccountStatus
        {
            get => _selectedAccountStatus;
            set => SetProperty(ref _selectedAccountStatus, value);
        }

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

        public Action CloseAction { get; set; }
        public bool DialogSuccess { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public UserDialogViewModel(IAuthService authService = null)
        {
            _authService = authService ?? AppServiceLocator.AuthService;
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke());

            LoadBranches();
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

        public UserDialogViewModel(UserListItemDto user, IAuthService authService = null) : this(authService)
        {
            if (user != null)
            {
                IsEditMode = true;
                Username = user.Username ?? "";
                FullName = user.FullName ?? "";
                Role = user.RoleDisplay ?? (user.Role == "Admin" ? "مدير النظام" : (user.Role == "Accountant" ? "محاسب" : "موظف استقبال"));
                SelectedAccountStatus = user.IsActive ? "مفعل" : "معطل";

                foreach (var b in AvailableBranches)
                {
                    if (b.Id == user.BranchId)
                    {
                        SelectedBranch = b;
                        break;
                    }
                }
            }
        }

        private void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("الرجاء إدخال اسم المستخدم.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(FullName))
            {
                MessageBox.Show("الرجاء إدخال الاسم الكامل للموظف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsEditMode && string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("الرجاء إدخال كلمة المرور للمستخدم الجديد.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string systemRole = "Receptionist";
            if (Role == "مدير النظام") systemRole = "Admin";
            else if (Role == "محاسب") systemRole = "Accountant";
            bool isActive = SelectedAccountStatus == "مفعل";
            int branchId = SelectedBranch?.Id ?? 1;

            OperationResult result;
            if (IsEditMode)
            {
                result = _authService.UpdateUser(Username.Trim(), FullName.Trim(), systemRole, isActive, Password, branchId);
            }
            else
            {
                result = _authService.CreateUser(Username.Trim(), FullName.Trim(), systemRole, isActive, Password, branchId);
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
