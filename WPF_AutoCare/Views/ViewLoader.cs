using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Xml;
using C__project_Auto_Care.Data;

namespace C__project_Auto_Care.Views
{
    /// <summary>
    /// Utility for dynamically loading clean XAML views and wiring them to ViewModels.
    /// Eliminates ugly procedural C# UI code while maintaining 100% design fidelity.
    /// </summary>
    public static class ViewLoader
    {
        public static string ResolveViewPath(string xamlFileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidatePaths = {
                Path.Combine(baseDir, "Views", xamlFileName),
                Path.Combine(baseDir, xamlFileName),
                Path.Combine(baseDir, "..", "..", "Views", xamlFileName),
                Path.Combine(baseDir, "..", "..", "WPF_AutoCare", "Views", xamlFileName)
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                    return Path.GetFullPath(path);
            }

            return null;
        }

        public static FrameworkElement LoadView(string xamlFileName, object dataContext = null)
        {
            string path = ResolveViewPath(xamlFileName);
            if (path == null)
            {
                AppLogger.Warn($"XAML view file not found: {xamlFileName}", "ViewLoader");
                return null;
            }

            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var parserContext = new ParserContext();
                    parserContext.BaseUri = new Uri(path, UriKind.Absolute);

                    var element = (FrameworkElement)XamlReader.Load(fs, parserContext);
                    if (dataContext != null && element != null)
                    {
                        element.DataContext = dataContext;
                    }
                    return element;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load XAML view '{xamlFileName}': {ex.Message}", ex);
                return null;
            }
        }

        public static Window LoadWindow(string xamlFileName, object dataContext = null)
        {
            var win = LoadView(xamlFileName, dataContext) as Window;
            if (win != null)
            {
                if (xamlFileName.Equals("LoginWindow.xaml", StringComparison.OrdinalIgnoreCase) && dataContext is ViewModels.LoginViewModel loginVm)
                {
                    WireLoginWindow(win, loginVm);
                }
                else if (xamlFileName.Equals("MainWindow.xaml", StringComparison.OrdinalIgnoreCase) && dataContext is ViewModels.MainViewModel mainVm)
                {
                    WireMainWindow(win, mainVm);
                }
            }
            return win;
        }

        private static void WireMainWindow(Window win, ViewModels.MainViewModel vm)
        {
            vm.RequestLogout += () =>
            {
                try { win.Close(); } catch { }
                try { System.Windows.Application.Current?.Shutdown(); } catch { }
                try { System.Environment.Exit(0); } catch { }
            };

            vm.RequestExit += () =>
            {
                try { win.Close(); } catch { }
                try { System.Windows.Application.Current?.Shutdown(); } catch { }
                try { System.Environment.Exit(0); } catch { }
            };
        }

        private static void WireLoginWindow(Window win, ViewModels.LoginViewModel vm)
        {
            // Close / Minimize commands
            vm.CloseRequested += () =>
            {
                try { win.Close(); } catch { }
                try { System.Windows.Application.Current?.Shutdown(); } catch { }
                try { System.Environment.Exit(0); } catch { }
            };

            vm.MinimizeRequested += () =>
            {
                try { win.WindowState = WindowState.Minimized; } catch { }
            };

            // 0. Ensure Logo Image is loaded from disk if pack URI is unavailable
            var logoImg = win.FindName("logoImg") as System.Windows.Controls.Image;
            if (logoImg != null)
            {
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] candidates = {
                        Path.Combine(baseDir, "app_logo.png"),
                        Path.Combine(baseDir, "Resources", "app_logo.png"),
                        Path.Combine(baseDir, "..", "..", "app_logo.png"),
                        Path.Combine(baseDir, "..", "..", "WPF_AutoCare", "app_logo.png")
                    };
                    foreach (var p in candidates)
                    {
                        if (File.Exists(p))
                        {
                            var bmp = new System.Windows.Media.Imaging.BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                            bmp.UriSource = new Uri(Path.GetFullPath(p), UriKind.Absolute);
                            bmp.EndInit();
                            bmp.Freeze();
                            logoImg.Source = bmp;
                            break;
                        }
                    }
                }
                catch { }
            }

            // 1. Drag move anywhere on window
            win.MouseDown += (s, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    try { win.DragMove(); } catch { }
                }
            };

            // 2. Password Box Sync
            var pwdBox = win.FindName("pwdBox") as System.Windows.Controls.PasswordBox;
            var visiblePwdBox = win.FindName("visiblePwdBox") as System.Windows.Controls.TextBox;
            var eyeBtn = win.FindName("eyeBtn") as System.Windows.Controls.Button;
            var showPwdChk = win.FindName("showPwdChk") as System.Windows.Controls.CheckBox;
            var usernameTxt = win.FindName("usernameTxt") as System.Windows.Controls.TextBox;

            bool isSyncing = false;

            if (pwdBox != null && visiblePwdBox != null)
            {
                if (!string.IsNullOrEmpty(vm.Password))
                {
                    pwdBox.Password = vm.Password;
                    visiblePwdBox.Text = vm.Password;
                }

                pwdBox.PasswordChanged += (s, e) =>
                {
                    if (isSyncing) return;
                    isSyncing = true;
                    visiblePwdBox.Text = pwdBox.Password;
                    vm.Password = pwdBox.Password;
                    isSyncing = false;
                };

                visiblePwdBox.TextChanged += (s, e) =>
                {
                    if (isSyncing) return;
                    isSyncing = true;
                    pwdBox.Password = visiblePwdBox.Text;
                    vm.Password = visiblePwdBox.Text;
                    isSyncing = false;
                };

                pwdBox.KeyDown += (s, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Enter && vm.LoginCommand.CanExecute(null))
                        vm.LoginCommand.Execute(null);
                };

                visiblePwdBox.KeyDown += (s, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Enter && vm.LoginCommand.CanExecute(null))
                        vm.LoginCommand.Execute(null);
                };
            }

            if (usernameTxt != null)
            {
                usernameTxt.KeyDown += (s, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Enter && vm.LoginCommand.CanExecute(null))
                        vm.LoginCommand.Execute(null);
                };
            }

            if (eyeBtn != null && showPwdChk != null)
            {
                eyeBtn.Click += (s, e) =>
                {
                    showPwdChk.IsChecked = !(showPwdChk.IsChecked == true);
                };
            }

            if (showPwdChk != null && pwdBox != null && visiblePwdBox != null && eyeBtn != null)
            {
                showPwdChk.Checked += (s, e) =>
                {
                    pwdBox.Visibility = Visibility.Collapsed;
                    visiblePwdBox.Visibility = Visibility.Visible;
                    eyeBtn.Content = "🙈";
                    visiblePwdBox.Focus();
                    visiblePwdBox.CaretIndex = visiblePwdBox.Text.Length;
                };

                showPwdChk.Unchecked += (s, e) =>
                {
                    visiblePwdBox.Visibility = Visibility.Collapsed;
                    pwdBox.Visibility = Visibility.Visible;
                    eyeBtn.Content = "👁";
                    pwdBox.Focus();
                };
            }

            win.Loaded += (s, e) => usernameTxt?.Focus();
        }
    }
}
