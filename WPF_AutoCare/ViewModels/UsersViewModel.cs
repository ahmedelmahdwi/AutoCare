using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.ViewModels
{
    public class UsersViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;
        private string _searchText = "";
        private string _selectedRoleFilter = "الكل";
        private UserListItemDto _selectedUser;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<UserListItemDto> Users { get; } = new ObservableCollection<UserListItemDto>();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    LoadUsers();
                }
            }
        }

        public string SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                if (_selectedRoleFilter != value)
                {
                    _selectedRoleFilter = value;
                    OnPropertyChanged();
                    LoadUsers();
                }
            }
        }

        public UserListItemDto SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged();
                }
            }
        }

        public System.Windows.Input.ICommand AddUserCommand { get; }
        public System.Windows.Input.ICommand EditUserCommand { get; }
        public System.Windows.Input.ICommand DeleteUserCommand { get; }

        public UsersViewModel(IAuthService authService = null)
        {
            _authService = authService ?? AppServiceLocator.AuthService;

            AddUserCommand = new Commands.RelayCommand(() =>
            {
                Views.DialogCoordinator.ShowAddUser(null, () => LoadUsers());
            });

            EditUserCommand = new Commands.RelayCommand(() =>
            {
                if (SelectedUser == null)
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد مستخدم من الجدول لتعديله.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowEditUser(SelectedUser, null, () => LoadUsers());
            });

            DeleteUserCommand = new Commands.RelayCommand(() =>
            {
                if (SelectedUser == null)
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد مستخدم من الجدول لحذفه.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var confirm = System.Windows.MessageBox.Show($"هل أنت متأكد من رغبتك في حذف حساب المستخدم '{SelectedUser.Username}'؟", "تأكيد الحذف", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (confirm != System.Windows.MessageBoxResult.Yes) return;

                var res = DeleteSelectedUser();
                if (res.Success)
                {
                    System.Windows.MessageBox.Show(res.Message, "نجاح", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    LoadUsers();
                }
                else
                {
                    System.Windows.MessageBox.Show(res.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            });

            LoadUsers();
        }

        public void LoadUsers()
        {
            try
            {
                string roleFilter = "";
                if (_selectedRoleFilter == "مدير النظام") roleFilter = "Admin";
                else if (_selectedRoleFilter == "موظف استقبال") roleFilter = "Receptionist";
                else if (_selectedRoleFilter == "محاسب") roleFilter = "Accountant";

                var list = _authService.GetAllUsers(_searchText, roleFilter);
                Users.Clear();
                foreach (var u in list)
                {
                    Users.Add(u);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("UsersViewModel.LoadUsers failed", ex);
            }
        }

        public OperationResult DeleteSelectedUser()
        {
            if (SelectedUser == null)
                return OperationResult.Fail("الرجاء تحديد مستخدم من الجدول.");

            return _authService.DeleteUser(SelectedUser.Username, UserSession.Username);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
