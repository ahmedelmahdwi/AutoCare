using System;
using System.Threading.Tasks;
using System.Windows;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Entities;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Models;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// Clean MVVM ViewModel for LoginWindow.
    /// Handles authentication logic, input validation, and decoupled window commands.
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IDialogService _dialogService;
        private readonly ILoggerService _loggerService;

        private string _username = "";
        private string _password = "";
        private bool _isPasswordVisible = false;
        private bool _rememberMe = false;
        private bool _hasError = false;


        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ClearError();
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ClearError();
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }



        public RelayCommand LoginCommand { get; }
        public RelayCommand TogglePasswordVisibilityCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand MinimizeCommand { get; }
        public RelayCommand OpenServerSettingsCommand { get; }

        public event Action<User> LoginSuccessful;
        public event Action CloseRequested;
        public event Action MinimizeRequested;
        public event Action ServerSettingsRequested;

        public LoginViewModel(
            IAuthService authService = null,
            IDialogService dialogService = null,
            ILoggerService loggerService = null)
        {
            _authService = authService ?? (AppServiceLocator.AuthService);
            _dialogService = dialogService;
            _loggerService = loggerService;

            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            TogglePasswordVisibilityCommand = new RelayCommand(ExecuteTogglePasswordVisibility);
            CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());
            MinimizeCommand = new RelayCommand(() => MinimizeRequested?.Invoke());
            OpenServerSettingsCommand = new RelayCommand(() => ServerSettingsRequested?.Invoke());

            TryLoadSavedCredentials();
            CheckDatabaseConnection();
        }

        public void CheckDatabaseConnection()
        {
            try
            {
                if (AppServiceLocator.DbHelper == null || !AppServiceLocator.DbHelper.TestConnection(out string err))
                {
                    SetError("تعذر الاتصال بقاعدة البيانات. يمكنك الضغط على 'إعدادات خادم قاعدة البيانات' بالأسفل لتحديد السيرفر وتهيئته.");
                }
            }
            catch { }
        }

        private bool CanExecuteLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !IsLoading;
        }

        private void TryLoadSavedCredentials()
        {
            try
            {
                if (_authService != null && _authService.TryLoadSavedCredentials(out string savedUser, out string savedPass))
                {
                    Username = savedUser;
                    Password = savedPass;
                    RememberMe = true;
                }
            }
            catch (Exception ex)
            {
                _loggerService?.Warn($"Failed to load saved credentials: {ex.Message}");
            }
        }

        private void ExecuteTogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private void ExecuteLogin()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                SetError("يرجى إدخال اسم المستخدم");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                SetError("يرجى إدخال كلمة المرور");
                return;
            }

            ClearError();
            IsLoading = true;
            BusyMessage = "جاري التحقق من بيانات الدخول...";

            try
            {
                // Authenticate synchronously or on UI thread for prompt responsive navigation
                var auth = _authService ?? AppServiceLocator.AuthService;
                OperationResult<User> result;
                if (auth != null)
                {
                    result = auth.Authenticate(Username.Trim(), Password, RememberMe);
                }
                else
                {
                    result = OperationResult<User>.Fail("تعذر الوصول لخدمة تسجيل الدخول");
                }

                if (result != null && result.Success && result.Data != null)
                {
                    _loggerService?.Info($"User logged in successfully: {Username}");
                    UserSession.Login(result.Data.Username, result.Data.FullName, result.Data.Role);
                    LoginSuccessful?.Invoke(result.Data);
                }
                else
                {
                    string msg = result?.Message ?? "اسم المستخدم أو كلمة المرور غير صحيحة، أو الحساب معطل";
                    SetError(msg);
                }
            }
            catch (Exception ex)
            {
                SetError("حدث خطأ أثناء الاتصال بقاعدة البيانات لتوثيق المستخدم: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
                BusyMessage = "";
            }
        }

        public void SetError(string message)
        {
            ErrorMessage = message;
            HasError = !string.IsNullOrEmpty(message);
        }

        public void ClearError()
        {
            ErrorMessage = "";
            HasError = false;
        }

        public void RequestClose()
        {
            CloseRequested?.Invoke();
        }
    }
}
