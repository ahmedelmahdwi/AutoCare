using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.ViewModels
{
    /// <summary>
    /// ViewModel for SuppliersScreen.
    /// Manages supplier records, purchase invoices, and search queries.
    /// Clean Architecture & MVVM.
    /// </summary>
    public class SuppliersViewModel : INotifyPropertyChanged
    {
        private readonly ISupplierService _supplierService;

        private string _supplierSearchText = "";
        private SupplierSummaryDto _selectedSupplier;
        private string _invoiceSearchText = "";
        private PurchaseInvoiceSummaryDto _selectedInvoice;
        private bool _isLoading;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SupplierSummaryDto> Suppliers { get; } =
            new ObservableCollection<SupplierSummaryDto>();

        public ObservableCollection<PurchaseInvoiceSummaryDto> Invoices { get; } =
            new ObservableCollection<PurchaseInvoiceSummaryDto>();

        public System.Windows.Input.ICommand AddSupplierCommand { get; }
        public System.Windows.Input.ICommand EditSupplierCommand { get; }
        public System.Windows.Input.ICommand DeleteSupplierCommand { get; }
        public System.Windows.Input.ICommand NewPurchaseInvoiceCommand { get; }
        public System.Windows.Input.ICommand ViewInvoiceDetailsCommand { get; }
        public System.Windows.Input.ICommand RefreshInvoicesCommand { get; }
        public System.Windows.Input.ICommand SelectSuppliersTabCommand { get; }
        public System.Windows.Input.ICommand SelectPurchasesTabCommand { get; }

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSuppliersTabActive));
                    OnPropertyChanged(nameof(IsPurchasesTabActive));
                }
            }
        }

        public bool IsSuppliersTabActive => SelectedTabIndex == 0;
        public bool IsPurchasesTabActive => SelectedTabIndex == 1;

        public bool CanManageSuppliers => !string.Equals(UserSession.Role, "Accountant", StringComparison.OrdinalIgnoreCase);
        public Visibility ManagementVisibility => CanManageSuppliers ? Visibility.Visible : Visibility.Collapsed;

        public SuppliersViewModel(ISupplierService supplierService)
        {
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));

            SelectSuppliersTabCommand = new Commands.RelayCommand(() => SelectedTabIndex = 0);
            SelectPurchasesTabCommand = new Commands.RelayCommand(() => SelectedTabIndex = 1);

            AddSupplierCommand = new Commands.RelayCommand(() =>
            {
                if (!CanManageSuppliers)
                {
                    MessageBox.Show("غير مصرح للمحاسب بإضافة موردين (اطلاع فقط).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowAddSupplier(null, () => LoadSuppliers());
            });

            EditSupplierCommand = new Commands.RelayCommand(() =>
            {
                if (!CanManageSuppliers)
                {
                    MessageBox.Show("غير مصرح للمحاسب بتعديل بيانات الموردين (اطلاع فقط).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (SelectedSupplier == null)
                {
                    MessageBox.Show("الرجاء تحديد مورد من الجدول لتعديله.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowEditSupplier(SelectedSupplier, null, () => LoadSuppliers());
            });

            DeleteSupplierCommand = new Commands.RelayCommand(() =>
            {
                if (!CanManageSuppliers)
                {
                    MessageBox.Show("غير مصرح للمحاسب بحذف الموردين.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (SelectedSupplier == null)
                {
                    MessageBox.Show("الرجاء تحديد مورد من الجدول لحذفه.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"هل أنت متأكد من رغبتك في حذف المورد '{SelectedSupplier.SupplierName}'؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                var res = DeleteSelectedSupplier();
                if (res.Success)
                {
                    MessageBox.Show(res.Message, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadSuppliers();
                }
                else
                {
                    MessageBox.Show(res.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });

            NewPurchaseInvoiceCommand = new Commands.RelayCommand(() =>
            {
                if (!CanManageSuppliers)
                {
                    MessageBox.Show("غير مصرح للمحاسب بتسجيل فواتير شراء جديدة (اطلاع فقط).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowNewPurchaseInvoice(null, () =>
                {
                    LoadInvoices();
                    LoadSuppliers();
                });
            });

            ViewInvoiceDetailsCommand = new Commands.RelayCommand(() =>
            {
                if (SelectedInvoice == null)
                {
                    System.Windows.MessageBox.Show("الرجاء تحديد فاتورة شراء لعرض تفاصيلها.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                Views.DialogCoordinator.ShowPurchaseInvoiceDetails(SelectedInvoice);
            });

            RefreshInvoicesCommand = new Commands.RelayCommand(() =>
            {
                LoadInvoices();
            });

            LoadSuppliers();
            LoadInvoices();
        }

        #region Properties
        public string SupplierSearchText
        {
            get => _supplierSearchText;
            set
            {
                _supplierSearchText = value;
                OnPropertyChanged();
                LoadSuppliers();
            }
        }

        public SupplierSummaryDto SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                OnPropertyChanged();
            }
        }

        public string InvoiceSearchText
        {
            get => _invoiceSearchText;
            set
            {
                _invoiceSearchText = value;
                OnPropertyChanged();
                LoadInvoices();
            }
        }

        public PurchaseInvoiceSummaryDto SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public void LoadSuppliers()
        {
            IsLoading = true;
            try
            {
                var list = _supplierService.GetSuppliersList(_supplierSearchText);
                Suppliers.Clear();
                foreach (var s in list)
                {
                    Suppliers.Add(s);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LoadInvoices()
        {
            IsLoading = true;
            try
            {
                var list = _supplierService.GetPurchaseInvoicesList(_invoiceSearchText);
                Invoices.Clear();
                foreach (var inv in list)
                {
                    Invoices.Add(inv);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public OperationResult DeleteSelectedSupplier()
        {
            if (SelectedSupplier == null || SelectedSupplier.SupplierID <= 0)
                return OperationResult.Fail("الرجاء تحديد مورد أولاً.");

            var res = _supplierService.DeleteSupplier(SelectedSupplier.SupplierID, SelectedSupplier.SupplierName);
            if (res.Success)
            {
                LoadSuppliers();
            }
            return res;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
