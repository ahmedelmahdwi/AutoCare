using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using AutoCare.Application.Interfaces;
using AutoCare.Domain.Entities;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Models;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    public class BranchLookupItem
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string DisplayText => Name;
        public override string ToString() => Name;
    }

    /// <summary>
    /// ViewModel for MainWindow driving navigation, user role presentation, system clock, and application session.
    /// Fully decoupled from UI code-behind and adhering to Clean MVVM.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly ILoggerService _loggerService;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly ICurrentBranchContext _branchContext;
        private readonly DispatcherTimer _clockTimer;

        private string _currentDate = "";
        private string _currentTime = "";
        private string _activeUsername = "";
        private string _activeFullName = "";
        private string _roleLabel = "";
        private bool _isAdmin = false;
        private string _activeScreenTitle = "لوحة التحكم";
        private object _currentView;
        private bool _isNotificationsFlyoutOpen;
        private BranchLookupItem _selectedBranch;

        public ObservableCollection<BranchLookupItem> AvailableBranches { get; } = new ObservableCollection<BranchLookupItem>();

        public BranchLookupItem SelectedBranch
        {
            get => _selectedBranch;
            set
            {
                if (SetProperty(ref _selectedBranch, value))
                {
                    OnBranchSelectionChanged(value);
                }
            }
        }

        public Visibility BranchSelectorVisibility => (IsAdmin || IsAccountant) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ReceptionistBranchBadgeVisibility => (!IsAdmin && !IsAccountant) ? Visibility.Visible : Visibility.Collapsed;
        public string CurrentBranchName => _branchContext?.CurrentBranchName ?? (UserSession.BranchName ?? "الفرع الرئيسي");

        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string ActiveUsername
        {
            get => _activeUsername;
            set => SetProperty(ref _activeUsername, value);
        }

        public string ActiveFullName
        {
            get => _activeFullName;
            set => SetProperty(ref _activeFullName, value);
        }

        public string RoleLabel
        {
            get => _roleLabel;
            set => SetProperty(ref _roleLabel, value);
        }

        private bool _isAccountant;

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (SetProperty(ref _isAdmin, value))
                {
                    OnPropertyChanged(nameof(UsersNavVisibility));
                    OnPropertyChanged(nameof(BackupNavVisibility));
                    OnPropertyChanged(nameof(AuditNavVisibility));
                    OnPropertyChanged(nameof(BranchSelectorVisibility));
                    OnPropertyChanged(nameof(ReceptionistBranchBadgeVisibility));
                }
            }
        }

        public bool IsAccountant
        {
            get => _isAccountant;
            set
            {
                if (SetProperty(ref _isAccountant, value))
                {
                    OnPropertyChanged(nameof(CustomersNavVisibility));
                    OnPropertyChanged(nameof(AppointmentsNavVisibility));
                    OnPropertyChanged(nameof(CarStatusNavVisibility));
                    OnPropertyChanged(nameof(SparePartsNavVisibility));
                    OnPropertyChanged(nameof(ServicesNavVisibility));
                    OnPropertyChanged(nameof(NotificationsButtonVisibility));
                    OnPropertyChanged(nameof(BranchSelectorVisibility));
                    OnPropertyChanged(nameof(ReceptionistBranchBadgeVisibility));
                }
            }
        }

        public Visibility UsersNavVisibility => IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BackupNavVisibility => IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AuditNavVisibility => IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RecycleBinNavVisibility => IsAdmin ? Visibility.Visible : Visibility.Collapsed;

        public Visibility AppointmentsNavVisibility => !IsAccountant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CustomersNavVisibility => !IsAccountant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CarStatusNavVisibility => !IsAccountant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SparePartsNavVisibility => Visibility.Visible;
        public Visibility ServicesNavVisibility => Visibility.Visible;
        public Visibility TechniciansNavVisibility => Visibility.Visible;
        public Visibility NotificationsButtonVisibility => !IsAccountant ? Visibility.Visible : Visibility.Collapsed;

        public string ActiveScreenTitle
        {
            get => _activeScreenTitle;
            set => SetProperty(ref _activeScreenTitle, value);
        }

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        #region Notifications MVVM State
        public ObservableCollection<NotificationItem> Notifications => _notificationService?.Notifications;
        public int UnreadCount => _notificationService?.UnreadCount ?? 0;
        public bool HasUnreadNotifications => _notificationService?.HasUnreadNotifications ?? false;
        public Visibility UnreadBadgeVisibility => HasUnreadNotifications ? Visibility.Visible : Visibility.Collapsed;
        public bool HasNoNotifications => Notifications == null || Notifications.Count == 0;
        public Visibility EmptyStateVisibility => HasNoNotifications ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NotificationsListVisibility => (!HasNoNotifications) ? Visibility.Visible : Visibility.Collapsed;

        public bool IsNotificationsFlyoutOpen
        {
            get => _isNotificationsFlyoutOpen;
            set
            {
                if (SetProperty(ref _isNotificationsFlyoutOpen, value))
                {
                    OnPropertyChanged(nameof(NotificationsFlyoutVisibility));
                }
            }
        }

        public Visibility NotificationsFlyoutVisibility => _isNotificationsFlyoutOpen ? Visibility.Visible : Visibility.Collapsed;
        #endregion

        #region Navigation Active State Indicators
        public bool IsDashboardActive => ActiveScreenTitle == "لوحة التحكم";
        public bool IsAppointmentsActive => ActiveScreenTitle == "المواعيد" || ActiveScreenTitle == "المواعيد والحجوزات";
        public bool IsCarStatusActive => ActiveScreenTitle == "كروت الصيانة" || ActiveScreenTitle == "حالة السيارات";
        public bool IsCustomersActive => ActiveScreenTitle == "العملاء والمركبات";
        public bool IsInvoicesActive => ActiveScreenTitle == "الفواتير" || ActiveScreenTitle == "إصدار الفواتير";
        public bool IsSparePartsActive => ActiveScreenTitle == "قطع الغيار";
        public bool IsServicesActive => ActiveScreenTitle == "الخدمات";
        public bool IsTechniciansActive => ActiveScreenTitle == "الفنيين" || ActiveScreenTitle == "فنيو الصيانة";
        public bool IsCommissionsActive => ActiveScreenTitle == "تسوية العمولات";
        public bool IsSuppliersActive => ActiveScreenTitle == "الموردين" || ActiveScreenTitle == "الموردين والمشتريات";
        public bool IsUsersActive => ActiveScreenTitle == "المستخدمين" || ActiveScreenTitle == "إدارة المستخدمين";
        public bool IsReportsActive => ActiveScreenTitle == "التقارير" || ActiveScreenTitle == "التقارير والإحصائيات";
        public bool IsBackupActive => ActiveScreenTitle == "النسخ الاحتياطي" || ActiveScreenTitle == "النسخ الاحتياطي والاستعادة";
        public bool IsAuditActive => ActiveScreenTitle == "سجل العمليات" || ActiveScreenTitle == "سجل العمليات والتدقيق";
        public bool IsRecycleBinActive => ActiveScreenTitle == "سلة المحذوفات" || ActiveScreenTitle == "الأرشيف والمحذوفات" || ActiveScreenTitle == "سلة المحذوفات والأرشيف";
        #endregion

        public RelayCommand<string> NavigateCommand { get; }
        public RelayCommand LogoutCommand { get; }
        public RelayCommand ExitCommand { get; }

        public RelayCommand ToggleNotificationsCommand { get; }
        public RelayCommand CloseNotificationsCommand { get; }
        public RelayCommand MarkAllReadCommand { get; }
        public RelayCommand ClearNotificationsCommand { get; }
        public RelayCommand RefreshNotificationsCommand { get; }
        public RelayCommand<NotificationItem> NotificationClickCommand { get; }

        public event Action<string> RequestNavigation;
        public event Action RequestLogout;
        public event Action RequestExit;

        public MainViewModel(
            IDialogService dialogService = null,
            ILoggerService loggerService = null,
            IAuditService auditService = null,
            INotificationService notificationService = null,
            ICurrentBranchContext branchContext = null)
        {
            _dialogService = dialogService;
            _loggerService = loggerService;
            _auditService = auditService;
            _notificationService = notificationService ?? AppServiceLocator.NotificationService;
            _branchContext = branchContext ?? AppServiceLocator.BranchContext;

            if (_notificationService != null)
            {
                _notificationService.NotificationsChanged += OnNotificationsChanged;
            }

            if (_branchContext != null)
            {
                _branchContext.BranchChanged += () =>
                {
                    OnPropertyChanged(nameof(CurrentBranchName));
                };
            }

            NavigateCommand = new RelayCommand<string>(ExecuteNavigate);
            LogoutCommand = new RelayCommand(ExecuteLogout);
            ExitCommand = new RelayCommand(ExecuteExit);

            ToggleNotificationsCommand = new RelayCommand(() => IsNotificationsFlyoutOpen = !IsNotificationsFlyoutOpen);
            CloseNotificationsCommand = new RelayCommand(() => IsNotificationsFlyoutOpen = false);

            MarkAllReadCommand = new RelayCommand(async () =>
            {
                if (_notificationService != null)
                {
                    await _notificationService.MarkAllAsReadAsync();
                }
            });

            ClearNotificationsCommand = new RelayCommand(async () =>
            {
                if (_notificationService != null)
                {
                    await _notificationService.MarkAllAsReadAsync();
                }
            });

            RefreshNotificationsCommand = new RelayCommand(async () =>
            {
                if (_notificationService != null)
                {
                    await _notificationService.RefreshNotificationsAsync();
                }
            });

            NotificationClickCommand = new RelayCommand<NotificationItem>(item =>
            {
                if (item == null) return;
                if (_notificationService != null && !item.IsRead)
                {
                    _ = _notificationService.MarkAsReadAsync(item.Id);
                }

                IsNotificationsFlyoutOpen = false;

                string refId = item.TargetReferenceId?.Trim();

                // Smart navigation based on notification category (supports Arabic & English)
                if (string.Equals(item.Category, "Inventory", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Category, "قطع الغيار", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Category, "المخزون", StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteNavigate(string.IsNullOrEmpty(refId) ? "قطع الغيار" : $"قطع الغيار:{refId}");
                }
                else if (string.Equals(item.Category, "RepairOrder", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.Category, "كروت الصيانة", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.Category, "أوامر الشغل", StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteNavigate(string.IsNullOrEmpty(refId) ? "كروت الصيانة" : $"كروت الصيانة:{refId}");
                }
                else if (string.Equals(item.Category, "Invoice", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.Category, "الفواتير", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.Category, "الحسابات", StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteNavigate(string.IsNullOrEmpty(refId) ? "الفواتير" : $"الفواتير:{refId}");
                }
                else if (string.Equals(item.Category, "Customer", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(item.Category, "العملاء", StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteNavigate(string.IsNullOrEmpty(refId) ? "كروت الصيانة" : $"كروت الصيانة:{refId}");
                }
                else
                {
                    ExecuteNavigate("لوحة التحكم");
                }
            });

            UpdateUserInfo();
            UpdateClock();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();

            _notificationService?.StartMonitoring(60);

            ExecuteNavigate("لوحة التحكم");
        }

        private void OnNotificationsChanged()
        {
            OnPropertyChanged(nameof(Notifications));
            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasUnreadNotifications));
            OnPropertyChanged(nameof(UnreadBadgeVisibility));
            OnPropertyChanged(nameof(HasNoNotifications));
            OnPropertyChanged(nameof(EmptyStateVisibility));
            OnPropertyChanged(nameof(NotificationsListVisibility));
        }

        public void UpdateUserInfo()
        {
            ActiveUsername = UserSession.Username ?? "";
            ActiveFullName = string.IsNullOrWhiteSpace(UserSession.FullName) ? ActiveUsername : UserSession.FullName;
            IsAdmin = string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            IsAccountant = string.Equals(UserSession.Role, "Accountant", StringComparison.OrdinalIgnoreCase);
            RoleLabel = FormatRole(UserSession.Role);
            OnPropertyChanged(nameof(UsersNavVisibility));
            OnPropertyChanged(nameof(BackupNavVisibility));
            OnPropertyChanged(nameof(AuditNavVisibility));
            OnPropertyChanged(nameof(CustomersNavVisibility));
            OnPropertyChanged(nameof(CarStatusNavVisibility));
            OnPropertyChanged(nameof(SparePartsNavVisibility));
            OnPropertyChanged(nameof(ServicesNavVisibility));
            OnPropertyChanged(nameof(TechniciansNavVisibility));
            OnPropertyChanged(nameof(NotificationsButtonVisibility));
            OnPropertyChanged(nameof(BranchSelectorVisibility));
            OnPropertyChanged(nameof(ReceptionistBranchBadgeVisibility));
            OnPropertyChanged(nameof(CurrentBranchName));

            LoadBranchOptions();
        }

        private async void LoadBranchOptions()
        {
            try
            {
                AvailableBranches.Clear();

                if (IsAdmin || IsAccountant)
                {
                    var allItem = new BranchLookupItem { Id = null, Name = "🌐 كل الفروع (مجمّع)" };
                    AvailableBranches.Add(allItem);

                    var uow = AppServiceLocator.AppUnitOfWork;
                    if (uow?.Branches != null)
                    {
                        var branches = await uow.Branches.GetAllAsync();
                        foreach (var b in branches)
                        {
                            AvailableBranches.Add(new BranchLookupItem { Id = b.Id, Name = $"🏢 {b.BranchName}" });
                        }
                    }

                    // Select matching or default to All Branches
                    int? currentId = _branchContext?.CurrentBranchId;
                    BranchLookupItem match = null;
                    if (currentId.HasValue)
                    {
                        foreach (var item in AvailableBranches)
                        {
                            if (item.Id == currentId.Value) { match = item; break; }
                        }
                    }
                    _selectedBranch = match ?? allItem;
                    OnPropertyChanged(nameof(SelectedBranch));
                }
                else
                {
                    // Receptionist: locked to user branch
                    int branchId = UserSession.BranchId > 0 ? UserSession.BranchId : 1;
                    string branchName = string.IsNullOrWhiteSpace(UserSession.BranchName) ? "الفرع الرئيسي" : UserSession.BranchName;
                    _branchContext?.SetBranch(branchId, branchName);
                    OnPropertyChanged(nameof(CurrentBranchName));
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("Failed to load branch options in MainViewModel", ex);
            }
        }

        private void OnBranchSelectionChanged(BranchLookupItem item)
        {
            if (_branchContext == null) return;
            string cleanName = item?.Name?.Replace("🏢 ", "")?.Replace("🌐 ", "");
            _branchContext.SetBranch(item?.Id, cleanName);
            OnPropertyChanged(nameof(CurrentBranchName));

            // Refresh current screen so it immediately presents data filtered for the selected branch
            if (!string.IsNullOrWhiteSpace(ActiveScreenTitle))
            {
                ExecuteNavigate(ActiveScreenTitle);
            }

            _notificationService?.RefreshNotificationsAsync();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            var culture = new System.Globalization.CultureInfo("ar-LY");
            culture.DateTimeFormat.Calendar = new System.Globalization.GregorianCalendar();
            CurrentDate = now.ToString("dddd، d MMMM yyyy", culture);
            CurrentTime = now.ToString("HH:mm:ss");
        }

        private static string FormatRole(string role)
        {
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return "مدير النظام";
            if (string.Equals(role, "Receptionist", StringComparison.OrdinalIgnoreCase)) return "موظف استقبال";
            if (string.Equals(role, "Accountant", StringComparison.OrdinalIgnoreCase)) return "محاسب";
            return string.IsNullOrEmpty(role) ? "المسمى الوظيفي" : role;
        }

        private void ExecuteNavigate(string destination)
        {
            if (string.IsNullOrWhiteSpace(destination)) return;

            string filterParam = null;
            string mainDest = destination;
            if (destination.Contains(":"))
            {
                var parts = destination.Split(new char[] { ':' }, 2);
                mainDest = parts[0];
                filterParam = parts[1];
            }

            // Security Authorization Guard: Admin-only sections
            if ((mainDest == "المستخدمين" || mainDest == "إدارة المستخدمين" ||
                 mainDest == "النسخ الاحتياطي" || mainDest == "النسخ الاحتياطي والاستعادة" ||
                 mainDest == "سجل العمليات" || mainDest == "سجل العمليات والتدقيق" ||
                 mainDest == "سلة المحذوفات" || mainDest == "الأرشيف والمحذوفات" || mainDest == "سلة المحذوفات والأرشيف") && !IsAdmin)
            {
                MessageBox.Show("عذراً، هذه الشاشة مخصصة لمدير النظام فقط.", "صلاحية غير كافية", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Security Authorization Guard: Sections restricted for Accountant (Financial Auditor)
            if ((mainDest == "العملاء والمركبات" || mainDest == "العملاء" ||
                 mainDest == "المواعيد" || mainDest == "المواعيد والحجوزات" ||
                 mainDest == "كروت الصيانة" || mainDest == "حالة السيارات") && IsAccountant)
            {
                MessageBox.Show("عذراً، هذه الشاشة غير مصرح بها للمحاسب.", "صلاحية غير كافية", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ActiveScreenTitle = mainDest;
            RequestNavigation?.Invoke(mainDest);

            OnPropertyChanged(nameof(IsDashboardActive));
            OnPropertyChanged(nameof(IsAppointmentsActive));
            OnPropertyChanged(nameof(IsCarStatusActive));
            OnPropertyChanged(nameof(IsCustomersActive));
            OnPropertyChanged(nameof(IsInvoicesActive));
            OnPropertyChanged(nameof(IsSparePartsActive));
            OnPropertyChanged(nameof(IsServicesActive));
            OnPropertyChanged(nameof(IsTechniciansActive));
            OnPropertyChanged(nameof(IsCommissionsActive));
            OnPropertyChanged(nameof(IsSuppliersActive));
            OnPropertyChanged(nameof(IsUsersActive));
            OnPropertyChanged(nameof(IsReportsActive));
            OnPropertyChanged(nameof(IsBackupActive));
            OnPropertyChanged(nameof(IsAuditActive));
            OnPropertyChanged(nameof(IsRecycleBinActive));

            try
            {
                switch (mainDest)
                {
                    case "لوحة التحكم":
                        var dashVm = C__project_Auto_Care.Core.DI.AppContainer.Resolve<DashboardViewModel>();
                        dashVm.RequestNavigate += (d) => ExecuteNavigate(d);
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("DashboardView.xaml", dashVm);
                        break;
                    case "المواعيد":
                    case "المواعيد والحجوزات":
                        var aptVm = C__project_Auto_Care.Core.DI.AppContainer.Resolve<AppointmentsViewModel>();
                        aptVm.RequestOpenRepairOrder += (roNum) => ExecuteNavigate("كروت الصيانة:" + roNum);
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("AppointmentsView.xaml", aptVm);
                        break;
                    case "كروت الصيانة":
                    case "حالة السيارات":
                        var carVm = C__project_Auto_Care.Core.DI.AppContainer.Resolve<CarStatusViewModel>();
                        if (!string.IsNullOrEmpty(filterParam))
                        {
                            if (filterParam == "معلق" || filterParam == "قيد التنفيذ" || filterParam == "مكتمل" || filterParam == "ملغى" || filterParam == "الكل")
                            {
                                carVm.SelectedFilter = filterParam;
                                carVm.SearchText = "";
                            }
                            else
                            {
                                carVm.SearchText = filterParam;
                            }
                        }
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("CarStatusView.xaml", carVm);
                        break;
                    case "العملاء والمركبات":
                        var custVm = C__project_Auto_Care.Core.DI.AppContainer.Resolve<CustomersVehiclesViewModel>();
                        if (!string.IsNullOrEmpty(filterParam))
                        {
                            custVm.SearchText = filterParam;
                        }
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("CustomersVehiclesView.xaml", custVm);
                        break;
                    case "الفواتير":
                    case "إصدار الفواتير":
                        var invVm = C__project_Auto_Care.Core.DI.AppContainer.Resolve<InvoiceScreenViewModel>();
                        if (!string.IsNullOrEmpty(filterParam))
                        {
                            if (filterParam == "today" || filterParam == "اليوم")
                            {
                                invVm.ActiveTabIndex = 1; // Archive tab
                                invVm.SearchText = DateTime.Today.ToString("yyyy-MM-dd");
                            }
                            else if (int.TryParse(filterParam, out int tabIdx))
                            {
                                invVm.ActiveTabIndex = tabIdx;
                            }
                            else
                            {
                                invVm.ActiveTabIndex = 1; // Archive tab
                                invVm.SearchText = filterParam;
                            }
                        }
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("InvoiceView.xaml", invVm);
                        break;
                    case "قطع الغيار":
                        var spVm = C__project_Auto_Care.Core.DI.AppContainer.Resolve<SparePartsViewModel>();
                        if (!string.IsNullOrEmpty(filterParam))
                        {
                            if (filterParam == "نواقص" || filterParam.Equals("lowstock", StringComparison.OrdinalIgnoreCase))
                            {
                                spVm.IsLowStockOnly = true;
                                spVm.SearchText = "";
                            }
                            else
                            {
                                spVm.SearchText = filterParam;
                            }
                        }
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("SparePartsView.xaml", spVm);
                        break;
                    case "الخدمات":
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("ServicesView.xaml", C__project_Auto_Care.Core.DI.AppContainer.Resolve<ServicesViewModel>());
                        break;
                    case "الموردين":
                    case "الموردين والمشتريات":
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("SuppliersView.xaml", C__project_Auto_Care.Core.DI.AppContainer.Resolve<SuppliersViewModel>());
                        break;
                    case "الفنيين":
                    case "فنيو الصيانة":
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("TechniciansView.xaml", C__project_Auto_Care.Core.DI.AppContainer.Resolve<TechniciansViewModel>());
                        break;
                    case "المستخدمين":
                    case "إدارة المستخدمين":
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("UsersView.xaml", C__project_Auto_Care.Core.DI.AppContainer.Resolve<UsersViewModel>());
                        break;
                    case "تسوية العمولات":
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("SettleCommissionsView.xaml", C__project_Auto_Care.Core.DI.AppContainer.Resolve<SettleCommissionsViewModel>());
                        break;
                    case "التقارير":
                    case "التقارير والإحصائيات":
                        var repVm = C__project_Auto_Care.Core.DI.AppContainer.Resolve<ReportsViewModel>();
                        if (!string.IsNullOrEmpty(filterParam))
                        {
                            if (filterParam == "today" || filterParam == "اليوم")
                            {
                                repVm.SelectedTabIndex = 0; // Financial report tab
                                repVm.FinancialStartDate = DateTime.Today;
                                repVm.FinancialEndDate = DateTime.Today;
                                repVm.LoadFinancialReport();
                            }
                            else if (int.TryParse(filterParam, out int repTabIdx))
                            {
                                repVm.SelectedTabIndex = repTabIdx;
                            }
                        }
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("ReportsView.xaml", repVm);
                        break;
                    case "النسخ الاحتياطي":
                    case "النسخ الاحتياطي والاستعادة":
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("DatabaseBackupView.xaml", C__project_Auto_Care.Core.DI.AppContainer.Resolve<DatabaseBackupViewModel>());
                        break;
                    case "سجل العمليات":
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("AuditLogView.xaml", C__project_Auto_Care.Core.DI.AppContainer.Resolve<AuditLogViewModel>());
                        break;
                    case "سلة المحذوفات":
                    case "سلة المحذوفات والأرشيف":
                    case "الأرشيف والمحذوفات":
                        CurrentView = C__project_Auto_Care.Views.ViewLoader.LoadView("RecycleBinView.xaml", C__project_Auto_Care.Core.DI.AppContainer.Resolve<RecycleBinViewModel>());
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                // Fallback gracefully without crash
            }
        }

        private void ExecuteLogout()
        {
            if (_dialogService != null)
            {
                if (!_dialogService.Confirm("هل أنت متأكد من رغبتك في تسجيل الخروج وإغلاق البرنامج؟", "تأكيد الخروج"))
                    return;
            }

            try
            {
                _auditService?.Log("تسجيل خروج", "Users", UserSession.Username, $"تسجيل خروج المستخدم: {UserSession.FullName ?? UserSession.Username}", UserSession.Username);
            }
            catch { }

            UserSession.Logout();

            if (RequestLogout != null)
            {
                RequestLogout.Invoke();
            }

            if (RequestExit != null)
            {
                RequestExit.Invoke();
            }

            // Guaranteed full shutdown fallback: close all windows and terminate process
            try
            {
                if (System.Windows.Application.Current != null)
                {
                    foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
                    {
                        try { w.Close(); } catch { }
                    }
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch { }

            try
            {
                System.Environment.Exit(0);
            }
            catch { }
        }

        private void ExecuteExit()
        {
            if (_dialogService != null)
            {
                if (!_dialogService.Confirm("هل أنت متأكد من إغلاق النظام؟", "تأكيد الخروج"))
                    return;
            }

            if (RequestExit != null)
            {
                RequestExit.Invoke();
            }

            try
            {
                if (System.Windows.Application.Current != null)
                {
                    foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
                    {
                        try { w.Close(); } catch { }
                    }
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch { }

            try
            {
                System.Environment.Exit(0);
            }
            catch { }
        }

        public void StopClock()
        {
            _clockTimer?.Stop();
        }
    }
}
