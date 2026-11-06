using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Theme;

namespace C__project_Auto_Care.Views
{
    /// <summary>
    /// Dialog for configuring SQL Server connection.
    /// Pure XAML view backed with presentation logic, completely decoupled from procedural UI generation.
    /// </summary>
    public class ServerConnectionDialog : Window
    {
        private TextBox _serverTxt;
        private RadioButton _winAuthRadio;
        private RadioButton _sqlAuthRadio;
        private StackPanel _sqlAuthPanel;
        private TextBox _userTxt;
        private PasswordBox _passwordTxt;
        private TextBlock _statusTxt;
        private Button _testBtn;
        private Button _saveBtn;
        private Button _cancelBtn;

        public string SelectedConnectionString { get; private set; }

        public ServerConnectionDialog(string initialServer = null)
        {
            var loadedWin = ViewLoader.LoadView("ServerConnectionDialog.xaml") as Window;
            if (loadedWin == null)
            {
                throw new InvalidOperationException("Failed to load Views/ServerConnectionDialog.xaml");
            }

            Title = loadedWin.Title;
            Width = loadedWin.Width;
            Height = loadedWin.Height;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            FlowDirection = FlowDirection.RightToLeft;
            Background = loadedWin.Background ?? new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));

            // 1. Transfer NameScope so FindName works directly on this Window instance
            var nameScope = NameScope.GetNameScope(loadedWin);
            if (nameScope != null)
            {
                NameScope.SetNameScope(this, nameScope);
            }

            // 2. Safely detach and transfer visual content
            var content = loadedWin.Content;
            loadedWin.Content = null;
            Content = content;

            // 3. Resolve controls and bind event handlers
            InitializeControls(initialServer);

            // 4. Handle Escape key to cancel
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }

        private void InitializeControls(string initialServer)
        {
            _serverTxt = FindName("TxtServer") as TextBox ?? (Content as FrameworkElement)?.FindName("TxtServer") as TextBox;
            _winAuthRadio = FindName("RadioWinAuth") as RadioButton ?? (Content as FrameworkElement)?.FindName("RadioWinAuth") as RadioButton;
            _sqlAuthRadio = FindName("RadioSqlAuth") as RadioButton ?? (Content as FrameworkElement)?.FindName("RadioSqlAuth") as RadioButton;
            _sqlAuthPanel = FindName("PanelSqlAuth") as StackPanel ?? (Content as FrameworkElement)?.FindName("PanelSqlAuth") as StackPanel;
            _userTxt = FindName("TxtUser") as TextBox ?? (Content as FrameworkElement)?.FindName("TxtUser") as TextBox;
            _passwordTxt = FindName("TxtPassword") as PasswordBox ?? (Content as FrameworkElement)?.FindName("TxtPassword") as PasswordBox;
            _statusTxt = FindName("TxtStatus") as TextBlock ?? (Content as FrameworkElement)?.FindName("TxtStatus") as TextBlock;
            _testBtn = FindName("BtnTestConn") as Button ?? (Content as FrameworkElement)?.FindName("BtnTestConn") as Button;
            _saveBtn = FindName("BtnSave") as Button ?? (Content as FrameworkElement)?.FindName("BtnSave") as Button;
            _cancelBtn = FindName("BtnCancel") as Button ?? (Content as FrameworkElement)?.FindName("BtnCancel") as Button;

            if (_serverTxt != null && !string.IsNullOrEmpty(initialServer))
            {
                _serverTxt.Text = initialServer;
            }

            if (_winAuthRadio != null && _sqlAuthRadio != null && _sqlAuthPanel != null)
            {
                _winAuthRadio.Checked += (s, e) => _sqlAuthPanel.Visibility = Visibility.Collapsed;
                _sqlAuthRadio.Checked += (s, e) => _sqlAuthPanel.Visibility = Visibility.Visible;
            }

            if (_testBtn != null)
            {
                _testBtn.Click += OnTestAndInitializeClick;
            }

            if (_saveBtn != null)
            {
                _saveBtn.Click += OnSaveClick;
            }

            if (_cancelBtn != null)
            {
                _cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
            }
        }

        private async void OnTestAndInitializeClick(object sender, RoutedEventArgs e)
        {
            string server = _serverTxt?.Text?.Trim();
            if (string.IsNullOrEmpty(server))
            {
                SetStatus("يرجى إدخال أو اختيار اسم الخادم أولاً.", false);
                return;
            }

            bool isWinAuth = _winAuthRadio?.IsChecked == true;
            string user = _userTxt?.Text?.Trim();
            string pass = _passwordTxt?.Password;

            if (_testBtn != null) _testBtn.IsEnabled = false;
            if (_saveBtn != null) _saveBtn.IsEnabled = false;
            SetStatus("جارٍ فحص الاتصال بالخادم وتهيئة قاعدة البيانات...", null);

            try
            {
                string err = null;
                bool ready = await System.Threading.Tasks.Task.Run(() =>
                {
                    return DatabaseInitializer.EnsureDatabaseReady(server, out err, msg =>
                    {
                        Dispatcher.Invoke(() => SetStatus(msg, null));
                    });
                });

                if (ready)
                {
                    SelectedConnectionString = DatabaseInitializer.BuildConnectionString(server, DatabaseInitializer.DatabaseName, isWinAuth, user, pass);
                    SetStatus("✅ تم الاتصال بنجاح وقاعدة البيانات AutoCare جاهزة تماماً للعمل!", true);
                    if (_saveBtn != null) _saveBtn.IsEnabled = true;
                }
                else
                {
                    SetStatus($"❌ فشل الاتصال: {err}", false);
                    if (_saveBtn != null) _saveBtn.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"❌ خطأ غير متوقع: {ex.Message}", false);
                if (_saveBtn != null) _saveBtn.IsEnabled = false;
            }
            finally
            {
                if (_testBtn != null) _testBtn.IsEnabled = true;
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedConnectionString))
            {
                return;
            }

            try
            {
                AppServiceLocator.DbHelper = new DbHelper(SelectedConnectionString);
            }
            catch
            {
            }

            DialogResult = true;
            Close();
        }

        private void SetStatus(string message, bool? success)
        {
            if (_statusTxt == null) return;
            _statusTxt.Text = message;
            if (success == true)
                _statusTxt.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
            else if (success == false)
                _statusTxt.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
            else
                _statusTxt.Foreground = new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7));
        }
    }
}
