using System;
using System.Windows;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.ViewModels;

namespace C__project_Auto_Care.Views
{
    /// <summary>
    /// Central Dialog Coordinator for displaying clean, declarative MVVM dialog windows.
    /// Encapsulates dialog lifecycle, data binding, and callback notifications without code duplication.
    /// </summary>
    public static class DialogCoordinator
    {
        private static Window GetActiveWindow(Window owner)
        {
            if (owner != null) return owner;
            if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
                return Application.Current.MainWindow;
            return null;
        }

        public static void ShowNewRepairOrder(Window owner = null, string plateNumber = null, Action onSaved = null)
        {
            try
            {
                var vm = new NewRepairOrderViewModel(AppServiceLocator.RepairOrderService, AppServiceLocator.CustomerService);
                if (!string.IsNullOrWhiteSpace(plateNumber))
                {
                    vm.LoadVehicleForNewOrder(plateNumber);
                }
                else
                {
                    vm.InitializeBlank();
                }

                var win = ViewLoader.LoadWindow("NewRepairOrderDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowNewRepairOrder failed", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة كرت الصيانة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowEditRepairOrder(string roNumber, Window owner = null, Action onSaved = null)
        {
            if (string.IsNullOrWhiteSpace(roNumber))
            {
                MessageBox.Show("الرجاء تحديد كرت الصيانة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new NewRepairOrderViewModel(AppServiceLocator.RepairOrderService, AppServiceLocator.CustomerService);
                var loadRes = vm.LoadOrderToEdit(roNumber);
                if (!loadRes.Success)
                {
                    MessageBox.Show(loadRes.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var win = ViewLoader.LoadWindow("NewRepairOrderDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowEditRepairOrder failed", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة تعديل أمر الإصلاح: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowVehicleHistory(string plateNumber, Window owner = null)
        {
            if (string.IsNullOrWhiteSpace(plateNumber))
            {
                MessageBox.Show("الرجاء اختيار سيارة من الجدول لعرض سجلها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new VehicleHistoryViewModel(plateNumber, AppServiceLocator.VehicleHistoryService);
                var win = ViewLoader.LoadWindow("VehicleHistoryDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowVehicleHistory failed", ex);
                MessageBox.Show("حدث خطأ أثناء فتح سجل المركبة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowServiceAndParts(string roNumber, Window owner = null, Action onChanged = null)
        {
            if (string.IsNullOrWhiteSpace(roNumber))
            {
                MessageBox.Show("الرجاء تحديد كرت الصيانة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new ServiceAndPartsViewModel(AppServiceLocator.RepairOrderService, AppServiceLocator.InventoryService, AppServiceLocator.TechnicianService);
                vm.LoadOrderData(roNumber);

                var win = ViewLoader.LoadWindow("ServiceAndPartsDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogChanged)
                {
                    onChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowServiceAndParts failed", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة الخدمات وقطع الغيار: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowGenerateInvoice(string roNumber, Window owner = null, Action onInvoiced = null)
        {
            if (string.IsNullOrWhiteSpace(roNumber))
            {
                MessageBox.Show("الرجاء اختيار أمر إصلاح من الجدول لإصدار فاتورته.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new GenerateInvoiceViewModel(AppServiceLocator.InvoiceService, AppServiceLocator.RepairOrderService);
                var loadRes = vm.LoadOrderData(roNumber);
                if (!loadRes.Success)
                {
                    MessageBox.Show(loadRes.Message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var win = ViewLoader.LoadWindow("GenerateInvoiceDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onInvoiced?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowGenerateInvoice failed", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة إصدار الفاتورة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowAddSparePart(Window owner = null, Action onSaved = null)
        {
            try
            {
                var vm = new SparePartDialogViewModel(AppServiceLocator.InventoryService);
                var win = ViewLoader.LoadWindow("AddNewSparePartDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowAddSparePart failed", ex);
                MessageBox.Show("حدث خطأ أثناء إضافة قطعة الغيار: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowEditSparePart(SparePartRowDto part, Window owner = null, Action onSaved = null)
        {
            if (part == null)
            {
                MessageBox.Show("الرجاء اختيار قطعة غيار لتعديلها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new SparePartDialogViewModel(AppServiceLocator.InventoryService);
                vm.LoadPart(part);

                var win = ViewLoader.LoadWindow("AddNewSparePartDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowEditSparePart failed", ex);
                MessageBox.Show("حدث خطأ أثناء تعديل قطعة الغيار: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowAddService(Window owner = null, Action onSaved = null)
        {
            try
            {
                var vm = new ServiceDialogViewModel(AppServiceLocator.ServiceCatalogService);
                var win = ViewLoader.LoadWindow("AddNewServiceDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowAddService failed", ex);
                MessageBox.Show("حدث خطأ أثناء إضافة الخدمة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowEditService(ServiceRowDto svc, Window owner = null, Action onSaved = null)
        {
            if (svc == null)
            {
                MessageBox.Show("الرجاء اختيار خدمة لتعديلها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new ServiceDialogViewModel(AppServiceLocator.ServiceCatalogService);
                vm.LoadService(svc);

                var win = ViewLoader.LoadWindow("AddNewServiceDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowEditService failed", ex);
                MessageBox.Show("حدث خطأ أثناء تعديل الخدمة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowAddUser(Window owner = null, Action onSaved = null)
        {
            try
            {
                var vm = new UserDialogViewModel(AppServiceLocator.AuthService);
                var win = ViewLoader.LoadWindow("AddNewUserDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowAddUser failed", ex);
                MessageBox.Show("حدث خطأ أثناء إضافة المستخدم: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowEditUser(UserListItemDto user, Window owner = null, Action onSaved = null)
        {
            try
            {
                var vm = new UserDialogViewModel(user, AppServiceLocator.AuthService);
                var win = ViewLoader.LoadWindow("AddNewUserDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowEditUser failed", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة تعديل المستخدم: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowAddTechnician(Window owner = null, Action onSaved = null)
        {
            try
            {
                var vm = new TechnicianDialogViewModel(AppServiceLocator.TechnicianService);
                var win = ViewLoader.LoadWindow("NewTechnicianDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowAddTechnician failed", ex);
                MessageBox.Show("حدث خطأ أثناء إضافة الفني: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowEditTechnician(TechnicianRowDto tech, Window owner = null, Action onSaved = null)
        {
            if (tech == null)
            {
                MessageBox.Show("الرجاء تحديد فني من الجدول لتعديله.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new TechnicianDialogViewModel(AppServiceLocator.TechnicianService);
                vm.LoadTechnician(tech);

                var win = ViewLoader.LoadWindow("NewTechnicianDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowEditTechnician failed", ex);
                MessageBox.Show("حدث خطأ أثناء تعديل بيانات الفني: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowAddSupplier(Window owner = null, Action onSaved = null)
        {
            try
            {
                var vm = new SupplierDialogViewModel(AppServiceLocator.SupplierService);
                var win = ViewLoader.LoadWindow("NewSupplierDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowAddSupplier failed", ex);
                MessageBox.Show("حدث خطأ أثناء إضافة المورد: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowEditSupplier(SupplierSummaryDto supplier, Window owner = null, Action onSaved = null)
        {
            if (supplier == null)
            {
                MessageBox.Show("الرجاء اختيار مورد لتعديله.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new SupplierDialogViewModel(AppServiceLocator.SupplierService);
                vm.LoadSupplier(supplier);

                var win = ViewLoader.LoadWindow("NewSupplierDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowEditSupplier failed", ex);
                MessageBox.Show("حدث خطأ أثناء تعديل بيانات المورد: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowNewPurchaseInvoice(Window owner = null, Action onSaved = null)
        {
            try
            {
                var vm = new NewPurchaseInvoiceViewModel(AppServiceLocator.SupplierService, AppServiceLocator.InventoryService);
                var win = ViewLoader.LoadWindow("NewPurchaseInvoiceDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowNewPurchaseInvoice failed", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة تسجيل فاتورة الشراء: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowPurchaseInvoiceDetails(PurchaseInvoiceSummaryDto invoice, Window owner = null)
        {
            if (invoice == null)
            {
                MessageBox.Show("الرجاء تحديد فاتورة شراء لعرض تفاصيلها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new PurchaseInvoiceDetailsViewModel(AppServiceLocator.SupplierService);
                vm.LoadInvoice(invoice);

                var win = ViewLoader.LoadWindow("PurchaseInvoiceDetailsDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowPurchaseInvoiceDetails failed", ex);
                MessageBox.Show("حدث خطأ أثناء عرض تفاصيل الفاتورة: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ShowBookAppointment(Window owner = null, Action onSaved = null)
        {
            try
            {
                var vm = new BookAppointmentDialogViewModel(
                    AppServiceLocator.AppointmentService,
                    AppServiceLocator.TechnicianService,
                    AppServiceLocator.CustomerService);

                var win = ViewLoader.LoadWindow("BookAppointmentDialog.xaml", vm);
                if (win == null) return;

                win.Owner = GetActiveWindow(owner);
                vm.CloseAction = () => win.Close();

                win.ShowDialog();
                if (vm.DialogSuccess)
                {
                    onSaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("DialogCoordinator.ShowBookAppointment failed", ex);
                MessageBox.Show("حدث خطأ أثناء فتح شاشة حجز الموعد: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

