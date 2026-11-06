using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.ViewModels.Commands;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for InvoiceScreen.
    /// Manages search filtering, completed orders ready for invoicing,
    /// issued invoices archive, and status reversals.
    /// Clean Architecture & MVVM.
    /// </summary>
    public class InvoiceScreenViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;

        private string _searchText = "";
        private int _activeTabIndex = 0; // 0: Awaiting Invoicing, 1: Issued Invoices Archive
        private CompletedOrderWithoutInvoiceDto _selectedOrder;
        private IssuedInvoiceSummaryDto _selectedInvoice;

        public ObservableCollection<CompletedOrderWithoutInvoiceDto> Orders { get; } =
            new ObservableCollection<CompletedOrderWithoutInvoiceDto>();

        public ObservableCollection<IssuedInvoiceSummaryDto> Invoices { get; } =
            new ObservableCollection<IssuedInvoiceSummaryDto>();

        public bool CanCreateInvoice => !string.Equals(Models.UserSession.Role, "Accountant", StringComparison.OrdinalIgnoreCase);
        public System.Windows.Visibility CreateInvoiceVisibility => CanCreateInvoice ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public ICommand RefreshCommand { get; }
        public ICommand RevertSelectedCommand { get; }
        public ICommand CreateInvoiceCommand { get; }
        public ICommand PrintInvoiceCommand { get; }
        public ICommand ExportInvoicePdfCommand { get; }
        public ICommand FilterTodayInvoicesCommand { get; }

        public InvoiceScreenViewModel(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            RefreshCommand = new AsyncRelayCommand(LoadActiveDataAsync);
            RevertSelectedCommand = new RelayCommand(() =>
            {
                if (!CanCreateInvoice)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بتعديل حالة أوامر الشغل (اطلاع وتدقيق مالي فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                RevertSelectedToInProgress();
            }, () => SelectedOrder != null);

            FilterTodayInvoicesCommand = new RelayCommand(() =>
            {
                ActiveTabIndex = 1;
                string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
                if (SearchText == todayStr)
                {
                    SearchText = ""; // Toggle off
                }
                else
                {
                    SearchText = todayStr;
                }
            });

            CreateInvoiceCommand = new RelayCommand(() =>
            {
                if (!CanCreateInvoice)
                {
                    System.Windows.MessageBox.Show("غير مصرح للمحاسب بإنشاء فواتير (اطلاع وتدقيق مالي فقط).", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                if (SelectedOrder == null || string.IsNullOrWhiteSpace(SelectedOrder.RoNumber))
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد أمر إصلاح من الجدول لإصدار الفاتورة له.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowGenerateInvoice(SelectedOrder.RoNumber, null, () => _ = LoadOrdersAsync());
            });

            PrintInvoiceCommand = new RelayCommand(PrintSelectedInvoice);
            ExportInvoicePdfCommand = new RelayCommand(ExportSelectedInvoicePdf);

            if (!CanCreateInvoice)
            {
                _activeTabIndex = 1; // Default to Issued Invoices Archive for Financial Auditor
            }

            if (AppServiceLocator.BranchContext != null)
            {
                AppServiceLocator.BranchContext.BranchChanged += () => LoadAll();
            }

            LoadAll();
        }

        #region Properties
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadActiveDataAsync();
                }
            }
        }

        public int ActiveTabIndex
        {
            get => _activeTabIndex;
            set
            {
                if (SetProperty(ref _activeTabIndex, value))
                {
                    _ = LoadActiveDataAsync();
                }
            }
        }

        public CompletedOrderWithoutInvoiceDto SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        public IssuedInvoiceSummaryDto SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }
        #endregion

        public async Task LoadActiveDataAsync()
        {
            if (ActiveTabIndex == 0)
            {
                await LoadOrdersAsync();
            }
            else
            {
                await LoadInvoicesAsync();
            }
        }

        public void LoadActiveData()
        {
            if (ActiveTabIndex == 0)
            {
                LoadOrders();
            }
            else
            {
                LoadInvoices();
            }
        }

        public void LoadAll()
        {
            LoadOrders();
            LoadInvoices();
        }

        public async Task LoadOrdersAsync()
        {
            string search = _searchText?.Trim() ?? "";

            await RunAsync(async () =>
            {
                var list = await Task.Run(() => _invoiceService.GetCompletedOrdersWithoutInvoice(search));
                RunOnUIThread(() =>
                {
                    Orders.Clear();
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            Orders.Add(item);
                        }
                    }
                });
            }, "جاري جلب الأوامر الجاهزة للفوترة...");
        }

        public void LoadOrders()
        {
            IsLoading = true;
            try
            {
                var list = _invoiceService.GetCompletedOrdersWithoutInvoice(_searchText?.Trim() ?? "");
                Orders.Clear();
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        Orders.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Data.AppLogger.Error($"LoadOrders error: {ex.Message}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadInvoicesAsync()
        {
            string search = _searchText?.Trim() ?? "";

            await RunAsync(async () =>
            {
                var list = await Task.Run(() => _invoiceService.GetIssuedInvoices(search));
                RunOnUIThread(() =>
                {
                    Invoices.Clear();
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            Invoices.Add(item);
                        }
                    }
                });
            }, "جاري جلب أرشيف الفواتير الصادرة...");
        }

        public void LoadInvoices()
        {
            IsLoading = true;
            try
            {
                var list = _invoiceService.GetIssuedInvoices(_searchText?.Trim() ?? "");
                Invoices.Clear();
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        Invoices.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Data.AppLogger.Error($"LoadInvoices error: {ex.Message}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public bool DoesOrderExist(string roNumber)
        {
            if (string.IsNullOrWhiteSpace(roNumber)) return false;
            return _invoiceService.DoesRepairOrderExist(roNumber);
        }

        public OperationResult RevertSelectedToInProgress()
        {
            if (SelectedOrder == null || string.IsNullOrWhiteSpace(SelectedOrder.RoNumber))
                return OperationResult.Fail("الرجاء اختيار أمر إصلاح من القائمة.");

            var result = _invoiceService.RevertToInProgress(SelectedOrder.RoNumber);
            if (result.Success)
            {
                _ = LoadOrdersAsync();
            }
            return result;
        }

        public void PrintSelectedInvoice()
        {
            if (ActiveTabIndex == 1 && SelectedInvoice != null)
            {
                Reports.FinalReportViewer.Show(SelectedInvoice.InvoiceNumber, SelectedInvoice.RepairOrderNumber, null);
            }
            else if (SelectedOrder != null)
            {
                Reports.FinalReportViewer.Show("", SelectedOrder.RoNumber, null);
            }
            else
            {
                System.Windows.MessageBox.Show("الرجاء تحديد فاتورة أو أمر إصلاح من الجدول للمعاينة والطباعة.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        public void ExportSelectedInvoicePdf()
        {
            if (ActiveTabIndex == 1 && SelectedInvoice != null)
            {
                Reports.FinalReportViewer.ExportToPdf(SelectedInvoice.InvoiceNumber, SelectedInvoice.RepairOrderNumber, null);
            }
            else if (SelectedOrder != null)
            {
                Reports.FinalReportViewer.ExportToPdf("", SelectedOrder.RoNumber, null);
            }
            else
            {
                System.Windows.MessageBox.Show("الرجاء تحديد فاتورة أو أمر إصلاح من الجدول لتصديره كـ PDF.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
    }
}
