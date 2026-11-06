using System;
using System.IO;
using System.Windows;
using C__project_Auto_Care.Data;

namespace C__project_Auto_Care
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogCrash("AppDomain", e.ExceptionObject as Exception);

            var app = new Application { ShutdownMode = ShutdownMode.OnLastWindowClose };
            C__project_Auto_Care.Theme.AppTheme.InitializeTheme(app);

            app.DispatcherUnhandledException += (s, e) =>
            {
                LogCrash("Dispatcher", e.Exception);
                e.Handled = true;
            };

            try
            {
                C__project_Auto_Care.Core.DI.AppContainer.Initialize();
                AppServiceLocator.DbHelper = new DbHelper();

                var cmd = Environment.GetCommandLineArgs();
                bool testMain = false;
                string testReport = null;

                for (int i = 0; i < cmd.Length; i++)
                {
                    if (string.Equals(cmd[i], "--test-main", StringComparison.OrdinalIgnoreCase))
                        testMain = true;
                    else if (string.Equals(cmd[i], "--test-report", StringComparison.OrdinalIgnoreCase) && i + 1 < cmd.Length)
                        testReport = cmd[i + 1];
                }

                if (testReport != null)
                {
                    Models.UserSession.Login("tester", "مستخدم الاختبار", "Admin");
                    TestReport(testReport);
                    return;
                }

                if (testMain)
                {
                    Models.UserSession.Login("tester", "مستخدم الاختبار", "Admin");
                    var mainVm = Core.DI.AppContainer.Resolve<ViewModels.MainViewModel>();
                    var main = Views.ViewLoader.LoadWindow("MainWindow.xaml", mainVm);
                    if (main != null)
                    {
                        Action exitApp = () =>
                        {
                            try { main?.Close(); } catch { }
                            try { System.Windows.Application.Current?.Shutdown(); } catch { }
                            try { Environment.Exit(0); } catch { }
                        };
                        mainVm.RequestLogout += exitApp;
                        mainVm.RequestExit += exitApp;
                        main.Closed += (s, e) => { try { System.Windows.Application.Current?.Shutdown(); } catch { } };
                        app.Run(main);
                    }
                    return;
                }

                var loginVm = Core.DI.AppContainer.Resolve<ViewModels.LoginViewModel>();
                var cleanLogin = Views.ViewLoader.LoadWindow("LoginWindow.xaml", loginVm);

                if (cleanLogin == null)
                {
                    throw new InvalidOperationException("Failed to load Views/LoginWindow.xaml");
                }

                loginVm.LoginSuccessful += (user) =>
                {
                    var mainVm = Core.DI.AppContainer.Resolve<ViewModels.MainViewModel>();
                    var main = Views.ViewLoader.LoadWindow("MainWindow.xaml", mainVm);
                    if (main != null)
                    {
                        cleanLogin.Hide();

                        Action closeAllAndExit = () =>
                        {
                            try { main?.Close(); } catch { }
                            try { cleanLogin?.Close(); } catch { }
                            try { System.Windows.Application.Current?.Shutdown(); } catch { }
                            try { Environment.Exit(0); } catch { }
                        };

                        mainVm.RequestLogout += closeAllAndExit;
                        mainVm.RequestExit += closeAllAndExit;

                        main.Closed += (s, e) =>
                        {
                            try
                            {
                                cleanLogin?.Close();
                                System.Windows.Application.Current?.Shutdown();
                            }
                            catch { }
                        };

                        main.Show();
                    }
                };

                loginVm.ServerSettingsRequested += () =>
                {
                    var diag = new Views.ServerConnectionDialog(AppServiceLocator.DbHelper?.CurrentServer);
                    diag.Owner = cleanLogin;
                    if (diag.ShowDialog() == true)
                    {
                        loginVm.CheckDatabaseConnection();
                    }
                };

                app.Run(cleanLogin);
            }
            catch (Exception ex)
            {
                LogCrash("Main", ex);
            }
        }

        static void TestReport(string name)
        {
            string outDir = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(outDir);
            DateTime start = new DateTime(2020, 1, 1);
            DateTime end = new DateTime(2027, 12, 31);

            Reports.ReportDocument doc = null;
            try
            {
                switch ((name ?? "").ToLowerInvariant())
                {
                    case "financial":
                        doc = Reports.ReportsBuilders.BuildFinancial(start, end);
                        break;
                    case "inventory":
                        var sp = new SparePartRepository(AppServiceLocator.DbHelper);
                        doc = Reports.ReportsBuilders.BuildInventory(sp.GetAllSpareParts(""));
                        break;
                    case "tops":
                        var sr = new ServiceRepository(AppServiceLocator.DbHelper);
                        doc = Reports.ReportsBuilders.BuildTopServices(start, end, 25, sr.GetTopServices(start, end, 25));
                        break;
                    case "toparts":
                        var pr = new SparePartRepository(AppServiceLocator.DbHelper);
                        doc = Reports.ReportsBuilders.BuildTopParts(start, end, 25, pr.GetTopSpareParts(start, end, 25));
                        break;
                    case "invoice":
                        var inv = AppServiceLocator.DbHelper.ExecuteDataTable(
                            "SELECT TOP 1 RepairOrderNumber FROM RepairOrders WHERE CompletionDate IS NOT NULL", null);
                        if (inv != null && inv.Rows.Count > 0)
                        {
                            doc = Reports.FinalReportViewer.BuildDocument("", inv.Rows[0][0].ToString());
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogCrash("TestReport-build", ex);
                return;
            }

            if (doc == null)
                return;

            try
            {
                System.Drawing.Bitmap[] pages = Reports.ReportRenderer.Render(doc);
                for (int i = 0; i < pages.Length; i++)
                {
                    pages[i].Save(Path.Combine(outDir, "hook_" + name + "_p" + (i + 1) + ".png"),
                        System.Drawing.Imaging.ImageFormat.Png);
                }
                foreach (System.Drawing.Bitmap b in pages)
                    b.Dispose();
            }
            catch (Exception ex)
            {
                LogCrash("TestReport-render", ex);
            }

            Reports.ReportPreviewWindow.Show(doc, null);
        }

        static void LogCrash(string source, Exception ex)
        {
            try
            {
                string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.AppendAllText(log, "[" + source + " @ " + DateTime.Now.ToString("HH:mm:ss") + "] " +
                    (ex != null ? ex.ToString() : "null ex") + Environment.NewLine);
            }
            catch { }
        }
    }
}
