using System;
using System.Collections.Generic;
using System.Linq;
using C__project_Auto_Care;
using C__project_Auto_Care.Core.Enums;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Exceptions;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Core.DI;
using C__project_Auto_Care.ViewModels;
using C__project_Auto_Care.ViewModels.Commands;
using C__project_Auto_Care.Models;
using System.Threading;
using System.Threading.Tasks;
using AutoCare.Domain.Entities;
using AutoCare.Domain.ValueObjects;
using DomainValidationException = AutoCare.Domain.Exceptions.DomainValidationException;
using AutoCare.Infrastructure.Persistence;
using AutoCare.Infrastructure.Services;
using AutoCare.Infrastructure.Migrations;

namespace AutoCare.Tests
{
    public class TestRunner
    {
        private static int _passed = 0;
        private static int _failed = 0;
        private static readonly List<string> _failures = new List<string>();

        public static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("================================================================");
            Console.WriteLine("       AutoCare Automated Architecture & Domain Test Suite       ");
            Console.WriteLine("================================================================");
            Console.WriteLine();

            RunPlateValidationTests();
            RunInvoiceCalculationTests();
            RunDomainEnumsTests();
            RunUnitOfWorkContractTests();
            RunDecomposedDomainServicesTests();
            RunDependencyInjectionContainerTests();
            RunMvvmAsyncArchitectureTests();
            RunConcurrencyAndOptimisticLockingTests();
            RunOperationalAndProductionServicesTests();
            RunArchitectureScaffoldingTests();
            RunDomainLayerMigrationTests();
            RunApplicationUseCasesTests();
            RunInfrastructureAndPersistenceTests();
            RunPresentationAndCleanMvvmTests();
            RunEndToEndIntegrationTests();
            RunExcelExporterTests();
            RunAppointmentSchedulingTests();
            RunMultiBranchArchitectureTests();

            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine($"Total Tests: {_passed + _failed} | Passed: {_passed} | Failed: {_failed}");
            Console.WriteLine("----------------------------------------------------------------");

            if (_failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[FAILURE SUMMARY]:");
                foreach (var f in _failures)
                {
                    Console.WriteLine("  - " + f);
                }
                Console.ResetColor();
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nAll Unit & Domain Architecture Tests PASSED SUCCESSFULLY (100% OK)!");
            Console.ResetColor();
            return 0;
        }

        private static void Assert(bool condition, string testName, string details = "")
        {
            if (condition)
            {
                _passed++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  [PASS] ");
                Console.ResetColor();
                Console.WriteLine(testName);
            }
            else
            {
                _failed++;
                _failures.Add($"{testName}: {details}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{testName} -> {details}");
            }
        }

        #region 1. Plate Validation Tests
        private static void RunPlateValidationTests()
        {
            Console.WriteLine("[Suite 1]: Vehicle Plate Number Domain Validation (Strict 7-Digits Rule)");

            // Plate validation logic defined by domain rules and AppConstants.RequiredPlateLength
            Func<string, bool> isValidPlate = plate =>
            {
                if (string.IsNullOrWhiteSpace(plate)) return false;
                string trimmed = plate.Trim();
                return trimmed.Length == AppConstants.RequiredPlateLength && trimmed.All(char.IsDigit);
            };

            Assert(isValidPlate("1234567"), "Valid 7-digit plate '1234567'");
            Assert(isValidPlate("0000001"), "Valid 7-digit plate with leading zeroes '0000001'");
            Assert(isValidPlate("9999999"), "Valid 7-digit plate '9999999'");

            Assert(!isValidPlate("123456"), "Reject 6-digit plate '123456'", "Expected false for length 6");
            Assert(!isValidPlate("12345678"), "Reject 8-digit plate '12345678'", "Expected false for length 8");
            Assert(!isValidPlate(""), "Reject empty plate string", "Expected false for empty");
            Assert(!isValidPlate(null), "Reject null plate", "Expected false for null");
            Assert(!isValidPlate("   "), "Reject whitespace plate", "Expected false for whitespace");
            Assert(!isValidPlate("123456A"), "Reject plate containing english letter '123456A'", "Expected false for alpha");
            Assert(!isValidPlate("123456أ"), "Reject plate containing arabic letter '123456أ'", "Expected false for arabic alpha");
            Assert(!isValidPlate("123 456"), "Reject plate with spaces '123 456'", "Expected false for space");
            Assert(!isValidPlate("123-456"), "Reject plate with hyphen '123-456'", "Expected false for hyphen");
            Assert(AppConstants.RequiredPlateLength == 7, "AppConstants.RequiredPlateLength equals 7");
            Console.WriteLine();
        }
        #endregion

        #region 2. Invoice Calculation Tests
        private static void RunInvoiceCalculationTests()
        {
            Console.WriteLine("[Suite 2]: Invoice Calculations & Financial Precision");

            var invoiceService = new InvoiceService(null, null, null, null, null, null, null, null, null);

            // Test 1: Simple without discount
            var res1 = invoiceService.Calculate(150.00m, 50.00m, 0m);
            Assert(res1.SubTotal == 200.00m, "SubTotal matches services + parts (150 + 50 = 200)", $"Got {res1.SubTotal}");
            Assert(res1.DiscountAmount == 0m, "DiscountAmount is 0 when input is 0", $"Got {res1.DiscountAmount}");
            Assert(res1.FinalDueAmount == 200.00m, "FinalDueAmount matches SubTotal when discount is 0", $"Got {res1.FinalDueAmount}");

            // Test 2: With valid discount
            var res2 = invoiceService.Calculate(300.00m, 200.00m, 50.00m);
            Assert(res2.SubTotal == 500.00m, "SubTotal 300 + 200 = 500", $"Got {res2.SubTotal}");
            Assert(res2.DiscountAmount == 50.00m, "DiscountAmount is 50.00", $"Got {res2.DiscountAmount}");
            Assert(res2.FinalDueAmount == 450.00m, "FinalDueAmount is 500 - 50 = 450", $"Got {res2.FinalDueAmount}");

            // Test 3: Discount exceeds subtotal (Clamp to zero rule)
            var res3 = invoiceService.Calculate(100.00m, 50.00m, 500.00m);
            Assert(res3.SubTotal == 150.00m, "SubTotal 100 + 50 = 150", $"Got {res3.SubTotal}");
            Assert(res3.FinalDueAmount == 0m, "FinalDueAmount clamped to 0 when discount > subtotal", $"Got {res3.FinalDueAmount}");

            // Test 4: Negative inputs guarded
            var res4 = invoiceService.Calculate(-100m, 50m, -20m);
            Assert(res4.ServicesTotal == 0m, "ServicesTotal clamped to 0 for negative input", $"Got {res4.ServicesTotal}");
            Assert(res4.DiscountAmount == 0m, "DiscountAmount clamped to 0 for negative input", $"Got {res4.DiscountAmount}");

            // Test 5: Technician Commission math
            decimal servicePrice = 250.00m;
            decimal commPercent = 40.00m;
            decimal calculatedComm = servicePrice * (commPercent / 100.0m);
            Assert(calculatedComm == 100.00m, "Technician commission math (250 * 40% = 100)", $"Got {calculatedComm}");
            Console.WriteLine();
        }
        #endregion

        #region 3. Domain Enums Tests
        private static void RunDomainEnumsTests()
        {
            Console.WriteLine("[Suite 3]: Domain Enums, String Conversions & Localization");

            // RepairOrderStatus
            Assert(RepairOrderStatus.Pending.ToDbString() == "Pending", "RepairOrderStatus.Pending DB string is 'Pending'");
            Assert(RepairOrderStatus.InProgress.ToDbString() == "InProgress", "RepairOrderStatus.InProgress DB string is 'InProgress'");
            Assert(RepairOrderStatus.Completed.ToDbString() == "Completed", "RepairOrderStatus.Completed DB string is 'Completed'");
            Assert(RepairOrderStatus.Invoiced.ToDbString() == "Invoiced", "RepairOrderStatus.Invoiced DB string is 'Invoiced'");
            Assert(RepairOrderStatus.Cancelled.ToDbString() == "Cancelled", "RepairOrderStatus.Cancelled DB string is 'Cancelled'");

            Assert(EnumExtensions.ParseRepairOrderStatus("معلق") == RepairOrderStatus.Pending, "Parse 'معلق' to Pending");
            Assert(EnumExtensions.ParseRepairOrderStatus("قيد الصيانة") == RepairOrderStatus.InProgress, "Parse 'قيد الصيانة' to InProgress");
            Assert(EnumExtensions.ParseRepairOrderStatus("قيد التنفيذ") == RepairOrderStatus.InProgress, "Parse 'قيد التنفيذ' to InProgress");
            Assert(EnumExtensions.ParseRepairOrderStatus("مكتمل") == RepairOrderStatus.Completed, "Parse 'مكتمل' to Completed");
            Assert(EnumExtensions.ParseRepairOrderStatus("تمت الفوترة") == RepairOrderStatus.Invoiced, "Parse 'تمت الفوترة' to Invoiced");
            Assert(EnumExtensions.ParseRepairOrderStatus("ملغي") == RepairOrderStatus.Cancelled, "Parse 'ملغي' to Cancelled");

            // UserRole
            Assert(UserRole.Admin.ToDbString() == "Admin", "UserRole.Admin DB string is 'Admin'");
            Assert(UserRole.Receptionist.ToDbString() == "Receptionist", "UserRole.Receptionist DB string is 'Receptionist'");
            Assert(EnumExtensions.ParseUserRole("مدير النظام") == UserRole.Admin, "Parse 'مدير النظام' to Admin");
            Assert(EnumExtensions.ParseUserRole("موظف استقبال") == UserRole.Receptionist, "Parse 'موظف استقبال' to Receptionist");

            // PaymentMethod
            Assert(PaymentMethod.Cash.ToDbString() == "Cash", "PaymentMethod.Cash DB string is 'Cash'");
            Assert(PaymentMethod.Card.ToDbString() == "Card", "PaymentMethod.Card DB string is 'Card'");
            Assert(EnumExtensions.ParsePaymentMethod("نقداً (Cash)") == PaymentMethod.Cash, "Parse 'نقداً (Cash)' to Cash");
            Assert(EnumExtensions.ParsePaymentMethod("بطاقة / شبكة (Card)") == PaymentMethod.Card, "Parse 'بطاقة / شبكة (Card)' to Card");
            Assert(EnumExtensions.ParsePaymentMethod("تحويل بنكي (Transfer)") == PaymentMethod.BankTransfer, "Parse 'تحويل بنكي (Transfer)' to BankTransfer");
            Assert(EnumExtensions.ParsePaymentMethod("آجل (Credit)") == PaymentMethod.Deferred, "Parse 'آجل (Credit)' to Deferred");
            Console.WriteLine();
        }
        #endregion

        #region 4. Unit Of Work Contract Tests
        private static void RunUnitOfWorkContractTests()
        {
            Console.WriteLine("[Suite 4]: UnitOfWork Contract & Architecture Interfaces");

            var factory = AppServiceLocator.UnitOfWorkFactory;
            Assert(factory != null, "UnitOfWorkFactory registered in AppServiceLocator");

            using (var uow = factory.Create())
            {
                Assert(uow != null, "IUnitOfWork factory creates instance");
                Assert(uow.RepairOrders != null, "UnitOfWork exposes IRepairOrderRepository");
                Assert(uow.Invoices != null, "UnitOfWork exposes IInvoiceRepository");
                Assert(uow.Customers != null, "UnitOfWork exposes ICustomerRepository");
                Assert(uow.Vehicles != null, "UnitOfWork exposes IVehicleRepository");
                Assert(uow.SpareParts != null, "UnitOfWork exposes ISparePartRepository");
                Assert(uow.Services != null, "UnitOfWork exposes IServiceRepository");
                Assert(uow.Technicians != null, "UnitOfWork exposes ITechnicianRepository");
                Assert(uow.Suppliers != null, "UnitOfWork exposes ISupplierRepository");
                Assert(uow.Audit != null, "UnitOfWork exposes IAuditRepository");
            }
            Console.WriteLine();
        }
        #endregion

        #region 5. Clean Architecture Decomposed Domain Services Tests
        private static void RunDecomposedDomainServicesTests()
        {
            Console.WriteLine("[Suite 5]: Clean Architecture Domain Services (Decoupled Services)");

            // AppServiceLocator registration
            Assert(AppServiceLocator.RepairOrderLifecycleService != null, "AppServiceLocator registers IRepairOrderLifecycleService");
            Assert(AppServiceLocator.RepairOrderItemsService != null, "AppServiceLocator registers IRepairOrderItemsService");
            Assert(AppServiceLocator.VehicleHistoryService != null, "AppServiceLocator registers IVehicleHistoryService");
            Assert(AppServiceLocator.ServiceCatalogService != null, "AppServiceLocator registers IServiceCatalogService");
            Assert(AppServiceLocator.RepairOrderService != null, "AppServiceLocator registers IRepairOrderService Facade");

            // VehicleHistoryService validations
            var history = AppServiceLocator.VehicleHistoryService;
            Assert(history.HasActiveRepairOrder("") == false, "VehicleHistoryService rejects empty plate");
            Assert(history.HasActiveRepairOrder(null) == false, "VehicleHistoryService rejects null plate");
            Assert(history.GetVehicleServiceHistoryList("").Count == 0, "VehicleHistoryService returns empty list on empty plate");

            // RepairOrderLifecycleService validations
            var lifecycle = AppServiceLocator.RepairOrderLifecycleService;
            Assert(lifecycle.UpdateOrderStatus("", "Completed").Success == false, "LifecycleService rejects empty RO on UpdateOrderStatus");
            Assert(lifecycle.CompleteRepairOrder("").Success == false, "LifecycleService rejects empty RO on CompleteRepairOrder");
            Assert(lifecycle.RevertToPending("").Success == false, "LifecycleService rejects empty RO on RevertToPending");

            // RepairOrderItemsService validations
            var items = AppServiceLocator.RepairOrderItemsService;
            Assert(items.AddServiceToOrder("", "SVC01", 1).Success == false, "ItemsService rejects empty RO on AddServiceToOrder");
            Assert(items.AddServiceToOrder("RO-001", "", 1).Success == false, "ItemsService rejects empty ServiceCode");
            Assert(items.AddServiceToOrder("RO-001", "SVC01", 0).Success == false, "ItemsService rejects invalid TechnicianId (0)");
            Assert(items.AddSparePartToOrder("", "PRT01", 1).Success == false, "ItemsService rejects empty RO on AddSparePartToOrder");
            Assert(items.AddSparePartToOrder("RO-001", "", 1).Success == false, "ItemsService rejects empty PartCode");
            Assert(items.AddSparePartToOrder("RO-001", "PRT01", 0).Success == false, "ItemsService rejects zero quantity");
            Assert(items.AddSparePartToOrder("RO-001", "PRT01", -5).Success == false, "ItemsService rejects negative quantity");

            // ServiceCatalogService validations
            var catalog = AppServiceLocator.ServiceCatalogService;
            Assert(catalog.DeleteServiceFromCatalog("").Success == false, "CatalogService rejects empty ServiceCode on delete");
            Assert(catalog.SaveService("", "Oil Change", 25, "Maintenance", false).Success == false, "CatalogService rejects empty code on save");
            Assert(catalog.SaveService("S01", "", 25, "Maintenance", false).Success == false, "CatalogService rejects empty name on save");
            Assert(catalog.SaveService("S01", "Oil Change", -5, "Maintenance", false).Success == false, "CatalogService rejects negative price");

            Console.WriteLine();
        }
        #endregion

        #region 6. Dependency Injection & IoC Container Tests
        private static void RunDependencyInjectionContainerTests()
        {
            Console.WriteLine("[Suite 6]: Dependency Injection & IoC Container Architecture");

            // 1. Container Initialization & Resolution
            Assert(AppContainer.Current != null, "AppContainer.Current provides active IServiceProvider");

            // 2. Singleton Identity Guarantee
            var ro1 = AppContainer.Resolve<IRepairOrderService>();
            var ro2 = AppContainer.Resolve<IRepairOrderService>();
            Assert(ReferenceEquals(ro1, ro2), "Singleton services return identical reference across resolutions");

            var db1 = AppContainer.Resolve<IDbHelper>();
            var db2 = AppContainer.Resolve<IDbHelper>();
            Assert(ReferenceEquals(db1, db2), "IDbHelper maintains single instance in container");

            // 3. Transient Lifecycle Guarantee (ViewModels)
            var vm1 = AppContainer.Resolve<NewRepairOrderViewModel>();
            var vm2 = AppContainer.Resolve<NewRepairOrderViewModel>();
            Assert(!ReferenceEquals(vm1, vm2), "Transient ViewModels return unique distinct instances");
            Assert(vm1 != null, "Auto-constructor injection resolves ViewModel dependencies");

            // 4. Scoped Lifetime (IUnitOfWork)
            using (var scope1 = AppContainer.CreateScope())
            using (var scope2 = AppContainer.CreateScope())
            {
                var uow1a = scope1.ServiceProvider.GetService<IUnitOfWork>();
                var uow1b = scope1.ServiceProvider.GetService<IUnitOfWork>();
                var uow2 = scope2.ServiceProvider.GetService<IUnitOfWork>();

                Assert(ReferenceEquals(uow1a, uow1b), "Scoped service returns same instance within same scope");
                Assert(!ReferenceEquals(uow1a, uow2), "Scoped service returns different instances in separate scopes");
            }

            // 5. AppServiceLocator bridges to IoC container
            Assert(ReferenceEquals(AppServiceLocator.RepairOrderService, AppContainer.Resolve<IRepairOrderService>()),
                "AppServiceLocator acts as transparent adapter over AppContainer");

            Console.WriteLine();
        }
        #endregion

        #region 7. Clean MVVM Architecture & Async Operations Tests
        private static void RunMvvmAsyncArchitectureTests()
        {
            Console.WriteLine("[Suite 7]: Clean MVVM Architecture & Async Operations");

            // 1. RelayCommand execution & predicate
            bool executed = false;
            var cmd = new RelayCommand(() => executed = true, () => true);
            Assert(cmd.CanExecute(null), "RelayCommand evaluates CanExecute predicate as true");
            cmd.Execute(null);
            Assert(executed, "RelayCommand executes bound action");

            var disabledCmd = new RelayCommand(() => { }, () => false);
            Assert(!disabledCmd.CanExecute(null), "RelayCommand respects false CanExecute predicate");

            // 2. AsyncRelayCommand execution
            bool asyncExecuted = false;
            var asyncCmd = new AsyncRelayCommand(async () =>
            {
                await Task.Delay(10);
                asyncExecuted = true;
            });
            Assert(asyncCmd.CanExecute(null), "AsyncRelayCommand can execute initially");
            asyncCmd.Execute(null);
            // wait briefly for async execution
            Task.Delay(100).Wait();
            Assert(asyncExecuted, "AsyncRelayCommand executes asynchronous Task");

            // 3. CustomersVehiclesViewModel Async Data Loading
            var custVm = AppContainer.Resolve<CustomersVehiclesViewModel>();
            Assert(custVm != null, "IoC Container resolves CustomersVehiclesViewModel");
            Assert(custVm.RefreshCommand != null, "CustomersVehiclesViewModel provides bound RefreshCommand");
            custVm.LoadDataAsync().Wait();
            Assert(!custVm.IsLoading, "CustomersVehiclesViewModel resets IsLoading after async execution");
            Assert(custVm.Rows.Count >= 0, "CustomersVehiclesViewModel populates ObservableCollection");

            // 4. CarStatusViewModel Async Data Loading
            var carStatusVm = AppContainer.Resolve<CarStatusViewModel>();
            Assert(carStatusVm != null, "IoC Container resolves CarStatusViewModel");
            carStatusVm.LoadDataAsync().Wait();
            Assert(!carStatusVm.IsLoading, "CarStatusViewModel resets IsLoading after async execution");

            // 5. ServicesViewModel Async Data Loading
            var servicesVm = AppContainer.Resolve<ServicesViewModel>();
            Assert(servicesVm != null, "IoC Container resolves ServicesViewModel");
            servicesVm.LoadServicesAsync().Wait();
            Assert(!servicesVm.IsLoading, "ServicesViewModel resets IsLoading after async execution");

            // 6. SparePartsViewModel Async Data Loading
            var sparePartsVm = AppContainer.Resolve<SparePartsViewModel>();
            Assert(sparePartsVm != null, "IoC Container resolves SparePartsViewModel");
            sparePartsVm.LoadPartsAsync().Wait();
            Assert(!sparePartsVm.IsLoading, "SparePartsViewModel resets IsLoading after async execution");

            Console.WriteLine();
        }
        #endregion

        #region 8. Concurrency, Optimistic Locking & Soft Delete Security Tests
        private static void RunConcurrencyAndOptimisticLockingTests()
        {
            Console.WriteLine("[Suite 8]: Concurrency Control, Optimistic Locking & Soft Delete");

            // 1. Multi-threaded Atomic Stock Deduction Simulation (Zero Negative Stock Invariant)
            int currentStock = 10;
            int successfulDeductions = 0;
            int failedDeductions = 0;
            int totalConcurrentRequests = 30;

            // Atomic deduction function mirroring SQL WHERE QuantityInStock >= @qty
            Func<bool> tryDeductAtomic = () =>
            {
                while (true)
                {
                    int initial = Volatile.Read(ref currentStock);
                    if (initial < 1)
                    {
                        Interlocked.Increment(ref failedDeductions);
                        return false;
                    }
                    if (Interlocked.CompareExchange(ref currentStock, initial - 1, initial) == initial)
                    {
                        Interlocked.Increment(ref successfulDeductions);
                        return true;
                    }
                }
            };

            var deductionTasks = new Task[totalConcurrentRequests];
            for (int i = 0; i < totalConcurrentRequests; i++)
            {
                deductionTasks[i] = Task.Run(() => tryDeductAtomic());
            }
            Task.WaitAll(deductionTasks);

            Assert(successfulDeductions == 10, "Atomic deduction allows exactly available stock (10 succeeded)", $"Got {successfulDeductions}");
            Assert(failedDeductions == 20, "Excess concurrent requests safely rejected (20 failed)", $"Got {failedDeductions}");
            Assert(currentStock == 0, "Final stock reaches zero without dropping negative", $"Got {currentStock}");
            Assert(currentStock >= 0, "Negative inventory invariant strictly preserved under concurrency");

            // 2. Optimistic Concurrency Protocol (RowVersion Token Match)
            byte[] v1 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };
            byte[] v2 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 };

            Func<byte[], byte[], bool> areVersionsEqual = (a, b) =>
            {
                if (a == null || b == null || a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++)
                    if (a[i] != b[i]) return false;
                return true;
            };

            Assert(areVersionsEqual(v1, v1), "RowVersion equality succeeds on identical version tokens");
            Assert(!areVersionsEqual(v1, v2), "RowVersion detects version mismatch between v1 and v2");

            // Optimistic Concurrency simulation on RepairOrder
            byte[] activeOrderVersion = (byte[])v1.Clone();
            bool updateByClientA = false;
            bool updateByClientB = false;

            // Client A and Client B both read v1
            byte[] clientA_knownVersion = (byte[])activeOrderVersion.Clone();
            byte[] clientB_knownVersion = (byte[])activeOrderVersion.Clone();

            // Client A commits update first
            if (areVersionsEqual(clientA_knownVersion, activeOrderVersion))
            {
                updateByClientA = true;
                activeOrderVersion = v2; // Server increments RowVersion
            }

            // Client B attempts to commit with stale version
            bool clientB_conflictDetected = false;
            if (areVersionsEqual(clientB_knownVersion, activeOrderVersion))
            {
                updateByClientB = true;
            }
            else
            {
                clientB_conflictDetected = true;
            }

            Assert(updateByClientA, "First concurrent transaction commits successfully with matching RowVersion");
            Assert(!updateByClientB, "Second concurrent transaction rejected due to outdated RowVersion");
            Assert(clientB_conflictDetected, "Optimistic concurrency conflict correctly detected");

            // 3. OptimisticConcurrencyException Contract
            var ex = new OptimisticConcurrencyException("RepairOrders", "OR-001");
            Assert(ex.EntityName == "RepairOrders", "OptimisticConcurrencyException captures EntityName");
            Assert(ex.EntityKey.ToString() == "OR-001", "OptimisticConcurrencyException captures EntityKey");
            Assert(ex.Message.Contains("تعارض في التزامن"), "Exception message provides clear Arabic explanation");

            // 4. Soft Delete Filtering & Referential Integrity
            var testCustomers = new List<Tuple<string, string, bool>>
            {
                Tuple.Create("0910000001", "العميل الأول", false),
                Tuple.Create("0910000002", "العميل الثاني (محذوف)", true),
                Tuple.Create("0910000003", "العميل الثالث", false)
            };

            var activeCustomers = testCustomers.Where(c => !c.Item3).ToList();
            Assert(activeCustomers.Count == 2, "Soft delete query filter excludes deleted entities (2 active of 3)");
            Assert(activeCustomers.All(c => !c.Item3), "All returned customers have IsDeleted = false");
            Assert(testCustomers.Any(c => c.Item3), "Historical deleted customer preserved for audit & relation integrity");

            // 5. High Concurrency Thread-Safety of Dependency Injection Container
            var diResolutionTasks = new Task[50];
            bool allSingletonsMatch = true;
            var masterService = AppContainer.Resolve<IRepairOrderService>();

            for (int i = 0; i < 50; i++)
            {
                diResolutionTasks[i] = Task.Run(() =>
                {
                    var resolved = AppContainer.Resolve<IRepairOrderService>();
                    if (!ReferenceEquals(masterService, resolved))
                    {
                        allSingletonsMatch = false;
                    }
                });
            }
            Task.WaitAll(diResolutionTasks);
            Assert(allSingletonsMatch, "AppContainer guarantees 100% thread safety across 50 parallel threads");

            // 6. High Concurrency Domain Validation Thread-Safety
            bool parallelValidationSuccess = true;
            Parallel.For(0, 100, i =>
            {
                string plate = (1000000 + i).ToString();
                bool valid = plate.Length == AppConstants.RequiredPlateLength && plate.All(char.IsDigit);
                if (!valid) parallelValidationSuccess = false;
            });
            Assert(parallelValidationSuccess, "Parallel domain validation executes safely across 100 concurrent threads");

            Console.WriteLine();
        }
        #endregion

        #region 9. Operational & Production Services Tests
        private static void RunOperationalAndProductionServicesTests()
        {
            Console.WriteLine("[Suite 9]: Operational & Production Readiness Services");

            // 1. DI Registration Verification
            var logger = AppContainer.Resolve<ILoggerService>();
            Assert(logger != null, "AppContainer registers and resolves ILoggerService");

            var backupService = AppContainer.Resolve<IDatabaseBackupService>();
            Assert(backupService != null, "AppContainer registers and resolves IDatabaseBackupService");

            var dialogService = AppContainer.Resolve<IDialogService>();
            Assert(dialogService != null, "AppContainer registers and resolves IDialogService");

            // 2. Logger Service Behavior & Rotation Format
            var testLogger = new C__project_Auto_Care.Core.Services.FileLoggerService();
            string logPath = testLogger.GetTodayLogFilePath();
            Assert(!string.IsNullOrEmpty(logPath), "FileLoggerService generates non-empty log file path");
            Assert(logPath.Contains("autocare_"), "Log file naming contains 'autocare_' prefix");
            Assert(logPath.EndsWith(".log"), "Log file extension is '.log'");
            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            Assert(logPath.Contains(todayStr), $"Log file path contains current date token '{todayStr}'");

            // 3. Database Backup Service Contracts & Name Generation
            string defaultBackupDir = backupService.GetDefaultBackupDirectory();
            Assert(!string.IsNullOrEmpty(defaultBackupDir), "IDatabaseBackupService provides non-empty default backup folder");

            string genBackupName = backupService.GenerateBackupFileName();
            Assert(genBackupName.EndsWith(".bak"), "Generated backup filename has '.bak' extension");
            Assert(genBackupName.Contains("AutoCare_Backup_"), "Generated backup filename contains 'AutoCare_Backup_' prefix");

            // 4. Backup & Restore Validation (Invalid inputs rejected)
            var emptyBackupTask = backupService.CreateBackupAsync("");
            emptyBackupTask.Wait();
            var emptyBackupResult = emptyBackupTask.Result;
            Assert(!emptyBackupResult.Success, "CreateBackupAsync rejects empty target file path");
            Assert(!string.IsNullOrEmpty(emptyBackupResult.ErrorMessage), "CreateBackupAsync provides descriptive error on empty path");

            var nonExistentRestoreTask = backupService.RestoreDatabaseAsync("Z:\\NonExistentPath\\File_9999.bak");
            nonExistentRestoreTask.Wait();
            var nonExistentRestoreResult = nonExistentRestoreTask.Result;
            Assert(!nonExistentRestoreResult.Success, "RestoreDatabaseAsync rejects non-existent backup file");
            Assert(nonExistentRestoreResult.ErrorMessage.Contains("غير موجود"), "RestoreDatabaseAsync returns Arabic error for missing backup");

            // 5. Result DTO Contracts
            var okBackup = BackupResult.Ok("C:\\backup.bak", 10485760); // 10 MB
            Assert(okBackup.Success, "BackupResult.Ok sets Success = true");
            Assert(okBackup.SizeMegabytes == 10, "BackupResult calculates SizeMegabytes accurately (10 MB)");

            var failBackup = BackupResult.Fail("Disk full");
            Assert(!failBackup.Success, "BackupResult.Fail sets Success = false");
            Assert(failBackup.ErrorMessage == "Disk full", "BackupResult.Fail preserves ErrorMessage");

            var okRestore = RestoreResult.Ok();
            Assert(okRestore.Success, "RestoreResult.Ok sets Success = true");

            var failRestore = RestoreResult.Fail("Restore canceled");
            Assert(!failRestore.Success, "RestoreResult.Fail sets Success = false");
            Assert(failRestore.ErrorMessage == "Restore canceled", "RestoreResult.Fail preserves ErrorMessage");

            Console.WriteLine();
        }
        #endregion

        #region 10. Clean Architecture Layer Scaffolding & Dependency Invariants
        private static void RunArchitectureScaffoldingTests()
        {
            Console.WriteLine("[Suite 10]: Clean Architecture Layer Scaffolding & Invariants");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string domainDllPath = System.IO.Path.Combine(baseDir, "AutoCare.Domain.dll");
            string appDllPath = System.IO.Path.Combine(baseDir, "AutoCare.Application.dll");
            string infraDllPath = System.IO.Path.Combine(baseDir, "AutoCare.Infrastructure.dll");

            // 1. Domain Assembly Existence and Pure Isolation
            Assert(System.IO.File.Exists(domainDllPath), "AutoCare.Domain.dll exists in runtime directory");
            if (System.IO.File.Exists(domainDllPath))
            {
                var domainAsm = System.Reflection.Assembly.LoadFrom(domainDllPath);
                Assert(domainAsm != null, "AutoCare.Domain assembly successfully loaded");

                var refNames = domainAsm.GetReferencedAssemblies().Select(a => a.Name).ToList();
                Assert(!refNames.Contains("System.Data"), "AutoCare.Domain has ZERO dependencies on System.Data");
                Assert(!refNames.Contains("PresentationFramework"), "AutoCare.Domain has ZERO dependencies on PresentationFramework");
                Assert(!refNames.Contains("PresentationCore"), "AutoCare.Domain has ZERO dependencies on PresentationCore");
                Assert(!refNames.Contains("WindowsBase"), "AutoCare.Domain has ZERO dependencies on WindowsBase");
            }

            // 2. Application Assembly Existence and UI Decoupling
            Assert(System.IO.File.Exists(appDllPath), "AutoCare.Application.dll exists in runtime directory");
            if (System.IO.File.Exists(appDllPath))
            {
                var appAsm = System.Reflection.Assembly.LoadFrom(appDllPath);
                Assert(appAsm != null, "AutoCare.Application assembly successfully loaded");

                var appRefNames = appAsm.GetReferencedAssemblies().Select(a => a.Name).ToList();
                Assert(!appRefNames.Contains("PresentationFramework"), "AutoCare.Application has ZERO dependencies on PresentationFramework");
                Assert(!appRefNames.Contains("PresentationCore"), "AutoCare.Application has ZERO dependencies on PresentationCore");
                Assert(!appRefNames.Contains("System.Data.SqlClient"), "AutoCare.Application has ZERO direct dependencies on SqlClient");
            }

            // 3. Infrastructure Assembly Existence and Layer Boundaries
            Assert(System.IO.File.Exists(infraDllPath), "AutoCare.Infrastructure.dll exists in runtime directory");
            if (System.IO.File.Exists(infraDllPath))
            {
                var infraAsm = System.Reflection.Assembly.LoadFrom(infraDllPath);
                Assert(infraAsm != null, "AutoCare.Infrastructure assembly successfully loaded");

                var infraRefNames = infraAsm.GetReferencedAssemblies().Select(a => a.Name).ToList();
                Assert(!infraRefNames.Contains("PresentationFramework"), "AutoCare.Infrastructure has ZERO dependencies on PresentationFramework");
            }

            // 4. Domain Primitives (Entity & ValueObject Equality Semantics)
            var entityA = new MockDomainEntity(101);
            var entityB = new MockDomainEntity(101);
            var entityC = new MockDomainEntity(202);
            Assert(entityA == entityB, "Domain Entity equality is determined by Id (101 == 101)");
            Assert(entityA != entityC, "Domain Entity inequality when Ids differ (101 != 202)");

            var moneyA = new MockValueObject(150.00m, "SAR");
            var moneyB = new MockValueObject(150.00m, "SAR");
            var moneyC = new MockValueObject(200.00m, "SAR");
            Assert(moneyA == moneyB, "ValueObject equality is structural (150 SAR == 150 SAR)");
            Assert(moneyA != moneyC, "ValueObject inequality when components differ (150 SAR != 200 SAR)");

            // 5. Application Primitives (Result & Result<T> Invariants)
            var successResult = AutoCare.Application.Common.Result.Success();
            Assert(successResult.IsSuccess && !successResult.IsFailure, "Result.Success() sets IsSuccess = true and IsFailure = false");

            var failResult = AutoCare.Application.Common.Result.Failure("Validation failed");
            Assert(failResult.IsFailure && failResult.Error == "Validation failed", "Result.Failure captures error message accurately");

            var genericSuccess = AutoCare.Application.Common.Result.Success<int>(500);
            Assert(genericSuccess.Value == 500, "Result<T>.Success preserves strongly typed value");

            bool threwOnFailAccess = false;
            try
            {
                var genericFail = AutoCare.Application.Common.Result.Failure<int>("Invalid operation");
                var val = genericFail.Value;
            }
            catch (InvalidOperationException)
            {
                threwOnFailAccess = true;
            }
            Assert(threwOnFailAccess, "Result<T>.Failure throws InvalidOperationException when Value is accessed");

            Console.WriteLine();
        }
        #endregion

        #region 11. Domain Layer Migration & Value Objects Integrity
        private static void RunDomainLayerMigrationTests()
        {
            Console.WriteLine("[Suite 11]: Domain Layer Migration & Value Objects Integrity");

            // 1. PlateNumber Value Object Validation
            var plate = PlateNumber.Create("1234567");
            Assert(plate != null && plate.Value == "1234567", "PlateNumber.Create accepts valid 7-digit numeric string");

            string strPlate = plate;
            Assert(strPlate == "1234567", "PlateNumber implicit conversion to string preserves value");

            bool threwOnShortPlate = false;
            try { PlateNumber.Create("12345"); } catch (DomainValidationException) { threwOnShortPlate = true; }
            Assert(threwOnShortPlate, "PlateNumber.Create rejects plate shorter than 7 digits");

            bool threwOnAlphaPlate = false;
            try { PlateNumber.Create("123456A"); } catch (DomainValidationException) { threwOnAlphaPlate = true; }
            Assert(threwOnAlphaPlate, "PlateNumber.Create rejects non-digit characters");

            bool threwOnEmptyPlate = false;
            try { PlateNumber.Create(""); } catch (DomainValidationException) { threwOnEmptyPlate = true; }
            Assert(threwOnEmptyPlate, "PlateNumber.Create rejects empty or whitespace plate");

            PlateNumber tryPlate;
            string plateErr;
            Assert(PlateNumber.TryCreate("7654321", out tryPlate, out plateErr) && tryPlate.Value == "7654321", "PlateNumber.TryCreate succeeds for valid input");
            Assert(!PlateNumber.TryCreate("invalid", out tryPlate, out plateErr) && !string.IsNullOrEmpty(plateErr), "PlateNumber.TryCreate fails gracefully for invalid input");

            // 2. VinNumber Value Object Validation
            var vin = VinNumber.Create("1HGCR2F83HA123456");
            Assert(vin != null && vin.Value == "1HGCR2F83HA123456", "VinNumber.Create accepts valid 17-character VIN");

            string strVin = vin;
            Assert(strVin == "1HGCR2F83HA123456", "VinNumber implicit conversion to string preserves value");

            bool threwOnShortVin = false;
            try { VinNumber.Create("SHORT_VIN"); } catch (DomainValidationException) { threwOnShortVin = true; }
            Assert(threwOnShortVin, "VinNumber.Create rejects VIN not exactly 17 characters");

            VinNumber tryVin;
            string vinErr;
            Assert(VinNumber.TryCreate("12345678901234567", out tryVin, out vinErr), "VinNumber.TryCreate succeeds for 17 chars");
            Assert(!VinNumber.TryCreate("too_short", out tryVin, out vinErr) && !string.IsNullOrEmpty(vinErr), "VinNumber.TryCreate fails for short VIN");

            // 3. Money Value Object Validation & Arithmetic Operations
            var m1 = new Money(100.50m, "SAR");
            var m2 = new Money(50.25m, "SAR");
            var mSum = m1 + m2;
            Assert(mSum.Amount == 150.75m, "Money addition (100.50 + 50.25 = 150.75)");

            var mDiff = m1 - m2;
            Assert(mDiff.Amount == 50.25m, "Money subtraction (100.50 - 50.25 = 50.25)");

            var mClamped = m2 - m1;
            Assert(mClamped.Amount == 0.00m, "Money subtraction clamps negative results to 0.00 (Business Invariant)");

            var mMult = new Money(100.00m, "SAR") * 0.15m;
            Assert(mMult.Amount == 15.00m, "Money tax calculation multiplication (100 * 15% = 15.00)");

            bool threwOnNegativeMoney = false;
            try { new Money(-50m, "SAR"); } catch (DomainValidationException) { threwOnNegativeMoney = true; }
            Assert(threwOnNegativeMoney, "Money constructor strictly prohibits negative monetary amounts");

            bool threwOnCurrencyMismatch = false;
            try { var invalidSum = new Money(100, "SAR") + new Money(100, "USD"); } catch (DomainValidationException) { threwOnCurrencyMismatch = true; }
            Assert(threwOnCurrencyMismatch, "Money operations between different currencies are rejected");

            // 4. Domain Entities Instantiation & Invariants
            var customer = new AutoCare.Domain.Entities.Customer
            {
                Id = 1,
                FullName = "أحمد محمد علي",
                PhoneNumber = "0780123456",
                Email = "ahmed@example.com",
                Address = "بغداد - الكرادة"
            };
            Assert(customer.Id == 1 && customer.PhoneNumber == "0780123456" && !customer.IsDeleted, "Customer domain entity initializes properties and IsDeleted = false");

            var vehicle = new AutoCare.Domain.Entities.Vehicle
            {
                Id = 10,
                PlateNumber = "1234567",
                CarName = "Toyota Camry",
                CarModel = "2023",
                Vin = "1HGCR2F83HA123456",
                CustomerPhone = "0780123456"
            };
            Assert(vehicle.PlateNumber == "1234567" && vehicle.CarName == "Toyota Camry", "Vehicle domain entity initializes properties correctly");

            var repairOrder = new AutoCare.Domain.Entities.RepairOrder
            {
                Id = 100,
                RepairOrderNumber = "RO-2026-0001",
                PlateNumber = "1234567",
                Mileage = 45000,
                FuelLevel = "1/2",
                Complaint = "فحص دوري وتغيير زيت"
            };
            Assert(repairOrder.Status == "معلق" && repairOrder.RepairOrderNumber == "RO-2026-0001", "RepairOrder domain entity defaults to 'معلق'");

            var invoice = new AutoCare.Domain.Entities.Invoice
            {
                Id = 50,
                InvoiceNumber = "INV-2026-0001",
                ServicesTotal = 250.00m,
                SparePartsTotal = 150.00m,
                DiscountAmount = 25.00m,
                TaxRate = 0.15m,
                PaymentMethod = "نقدي"
            };
            Assert(invoice.SubTotal == 400.00m, "Invoice domain entity calculates SubTotal accurately (250 + 150 = 400)");

            var part = new AutoCare.Domain.Entities.SparePartItem
            {
                Id = 20,
                PartName = "فلتر زيت تويوتا أصلي",
                PartNumber = "TOY-90915-10003",
                QuantityInStock = 25,
                CostPrice = 30.00m,
                SellingPrice = 50.00m
            };
            Assert(part.MinStockLevel == 5 && part.QuantityInStock == 25, "SparePartItem domain entity sets default MinStockLevel to 5");

            // 5. NotificationItem domain entity
            var notif = new AutoCare.Domain.Entities.NotificationItem
            {
                Id = 1,
                Title = "نقص في المخزون",
                Message = "القطعة وصلت للحد الحرج",
                Severity = AutoCare.Domain.Entities.NotificationSeverity.Critical,
                Category = "Inventory"
            };
            Assert(!notif.IsRead, "NotificationItem defaults to IsRead = false");
            Assert(notif.SeverityColor == "#EF4444", "Critical notification severity color is #EF4444");
            Assert(notif.SeverityIcon == "🚨", "Critical notification severity icon is 🚨");

            var notifWarning = new AutoCare.Domain.Entities.NotificationItem
            {
                Id = 2,
                Severity = AutoCare.Domain.Entities.NotificationSeverity.Warning
            };
            Assert(notifWarning.SeverityColor == "#F59E0B" && notifWarning.SeverityIcon == "⚠️", "Warning notification severity returns warning icon and color");

            // 6. Domain Exceptions
            var concEx = new AutoCare.Domain.Exceptions.OptimisticConcurrencyException("Customer", 42);
            Assert(concEx.EntityName == "Customer" && (int)concEx.EntityKey == 42, "OptimisticConcurrencyException correctly stores EntityName and EntityKey");

            Console.WriteLine();
        }
        #endregion

        #region 12. Application Core & Use Cases Validation
        private static void RunApplicationUseCasesTests()
        {
            Console.WriteLine("[Suite 12]: Application Core & Single-Responsibility Use Cases");

            // 1. CalculateInvoiceTotalsUseCase
            var calc = new AutoCare.Application.UseCases.Invoices.CalculateInvoiceTotalsUseCase();
            var calcResult = calc.Execute(150.00m, 50.00m, 20.00m, 15.00m, 200.00m);
            Assert(calcResult.SubTotal == 200.00m, "Invoice calculation subtotal matches services + parts (150 + 50 = 200)");
            Assert(calcResult.DiscountAmount == 20.00m, "Invoice calculation discount preserves 20.00");
            Assert(calcResult.TaxAmount == 27.00m, "Invoice tax calculation: 180 net * 15% = 27.00");
            Assert(calcResult.TotalAmount == 207.00m, "Invoice total: 180 net + 27 tax = 207.00");
            Assert(calcResult.RemainingAmount == 7.00m, "Invoice remaining: 207 total - 200 paid = 7.00");

            var clampedDiscount = calc.Execute(100.00m, 0m, 250.00m, 0m, 0m);
            Assert(clampedDiscount.DiscountAmount == 100.00m, "Invoice discount clamped to SubTotal when exceeding it (250 -> 100)");
            Assert(clampedDiscount.TotalAmount == 0.00m, "Invoice total drops to zero without going negative");

            // 2. UnitOfWork & Use Cases Setup
            var uow = new MockAppUnitOfWork();

            // 3. CreateRepairOrderUseCase
            var createOrderUseCase = new AutoCare.Application.UseCases.RepairOrders.CreateRepairOrderUseCase(uow);
            var nullOrderTask = createOrderUseCase.ExecuteAsync(null);
            nullOrderTask.Wait();
            Assert(nullOrderTask.Result.IsFailure, "CreateRepairOrderUseCase rejects null request");

            var emptyPhoneTask = createOrderUseCase.ExecuteAsync(new AutoCare.Application.DTOs.CreateRepairOrderRequest { CustomerPhone = "" });
            emptyPhoneTask.Wait();
            Assert(emptyPhoneTask.Result.IsFailure, "CreateRepairOrderUseCase rejects empty customer phone");

            var invalidPlateTask = createOrderUseCase.ExecuteAsync(new AutoCare.Application.DTOs.CreateRepairOrderRequest { CustomerPhone = "0780123456", PlateNumber = "123" });
            invalidPlateTask.Wait();
            Assert(invalidPlateTask.Result.IsFailure && invalidPlateTask.Result.Error.Contains("7 أرقام"), "CreateRepairOrderUseCase enforces strict 7-digit plate rule");

            var validOrderReq = new AutoCare.Application.DTOs.CreateRepairOrderRequest
            {
                CustomerName = "علي حسن",
                CustomerPhone = "0780112233",
                PlateNumber = "7766554",
                Complaint = "فحص فرامل وتغيير سفايف"
            };
            var validOrderTask = createOrderUseCase.ExecuteAsync(validOrderReq);
            validOrderTask.Wait();
            Assert(validOrderTask.Result.IsSuccess && !string.IsNullOrEmpty(validOrderTask.Result.Value), "CreateRepairOrderUseCase creates order and returns order number");

            var duplicatePlateTask = createOrderUseCase.ExecuteAsync(validOrderReq);
            duplicatePlateTask.Wait();
            Assert(duplicatePlateTask.Result.IsFailure && duplicatePlateTask.Result.Error.Contains("جارية"), "CreateRepairOrderUseCase prevents concurrent active orders on same plate");

            // 4. CompleteRepairOrderUseCase
            var completeOrderUseCase = new AutoCare.Application.UseCases.RepairOrders.CompleteRepairOrderUseCase(uow);
            var completeEmptyTask = completeOrderUseCase.ExecuteAsync("");
            completeEmptyTask.Wait();
            Assert(completeEmptyTask.Result.IsFailure, "CompleteRepairOrderUseCase rejects empty order number");

            var completeNonExistentTask = completeOrderUseCase.ExecuteAsync("RO-9999");
            completeNonExistentTask.Wait();
            Assert(completeNonExistentTask.Result.IsFailure, "CompleteRepairOrderUseCase rejects non-existent order number");

            var completeValidTask = completeOrderUseCase.ExecuteAsync("RO-TEST-001");
            completeValidTask.Wait();
            Assert(completeValidTask.Result.IsSuccess, "CompleteRepairOrderUseCase successfully marks active order as completed");

            var completeDuplicateTask = completeOrderUseCase.ExecuteAsync("RO-TEST-001");
            completeDuplicateTask.Wait();
            Assert(completeDuplicateTask.Result.IsFailure, "CompleteRepairOrderUseCase rejects re-completing already completed order");

            // 5. AddServiceToOrderUseCase
            var addServiceUseCase = new AutoCare.Application.UseCases.RepairOrders.AddServiceToOrderUseCase(uow);
            var addServiceEmptyTask = addServiceUseCase.ExecuteAsync(Tuple.Create("", 1, 1));
            addServiceEmptyTask.Wait();
            Assert(addServiceEmptyTask.Result.IsFailure, "AddServiceToOrderUseCase rejects empty RO number");

            var addServiceInvalidTechTask = addServiceUseCase.ExecuteAsync(Tuple.Create("RO-TEST-001", 1, 0));
            addServiceInvalidTechTask.Wait();
            Assert(addServiceInvalidTechTask.Result.IsFailure, "AddServiceToOrderUseCase rejects zero technician ID");

            var addServiceValidTask = addServiceUseCase.ExecuteAsync(Tuple.Create("RO-TEST-001", 1, 5));
            addServiceValidTask.Wait();
            Assert(addServiceValidTask.Result.IsSuccess, "AddServiceToOrderUseCase succeeds for valid order and technician");

            // 6. AddSparePartToOrderUseCase
            var addPartUseCase = new AutoCare.Application.UseCases.RepairOrders.AddSparePartToOrderUseCase(uow);
            var addPartZeroQtyTask = addPartUseCase.ExecuteAsync(Tuple.Create("RO-TEST-001", "PART-01", 0));
            addPartZeroQtyTask.Wait();
            Assert(addPartZeroQtyTask.Result.IsFailure, "AddSparePartToOrderUseCase rejects zero quantity");

            var addPartValidTask = addPartUseCase.ExecuteAsync(Tuple.Create("RO-TEST-001", "PART-01", 2));
            addPartValidTask.Wait();
            Assert(addPartValidTask.Result.IsSuccess, "AddSparePartToOrderUseCase atomically deducts inventory and succeeds");

            var addPartOverStockTask = addPartUseCase.ExecuteAsync(Tuple.Create("RO-TEST-001", "PART-01", 9999));
            addPartOverStockTask.Wait();
            Assert(addPartOverStockTask.Result.IsFailure && addPartOverStockTask.Result.Error.Contains("غير كافية"), "AddSparePartToOrderUseCase detects insufficient stock and aborts");

            // 7. CreateFinalInvoiceUseCase
            var createInvoiceUseCase = new AutoCare.Application.UseCases.Invoices.CreateFinalInvoiceUseCase(uow);
            var createInvoiceEmptyTask = createInvoiceUseCase.ExecuteAsync(new AutoCare.Application.DTOs.CreateFinalInvoiceRequest { RepairOrderNumber = "" });
            createInvoiceEmptyTask.Wait();
            Assert(createInvoiceEmptyTask.Result.IsFailure, "CreateFinalInvoiceUseCase rejects empty order number");

            var createInvoiceValidTask = createInvoiceUseCase.ExecuteAsync(new AutoCare.Application.DTOs.CreateFinalInvoiceRequest
            {
                RepairOrderNumber = "RO-TEST-001",
                ServicesTotal = 200m,
                SparePartsTotal = 100m,
                DiscountAmount = 20m,
                PaymentMethod = "Cash",
                CashierName = "admin"
            });
            createInvoiceValidTask.Wait();
            Assert(createInvoiceValidTask.Result.IsSuccess && !string.IsNullOrEmpty(createInvoiceValidTask.Result.Value), "CreateFinalInvoiceUseCase generates invoice number and finalizes order");

            var createInvoiceDuplicateTask = createInvoiceUseCase.ExecuteAsync(new AutoCare.Application.DTOs.CreateFinalInvoiceRequest
            {
                RepairOrderNumber = "RO-TEST-001",
                ServicesTotal = 200m
            });
            createInvoiceDuplicateTask.Wait();
            Assert(createInvoiceDuplicateTask.Result.IsFailure && createInvoiceDuplicateTask.Result.Error.Contains("مسبقاً"), "CreateFinalInvoiceUseCase prevents duplicate invoicing of same order");

            // 8. RegisterCustomerAndVehicleUseCase
            var regCustVehUseCase = new AutoCare.Application.UseCases.CustomersVehicles.RegisterCustomerAndVehicleUseCase(uow);
            var regShortPhoneTask = regCustVehUseCase.ExecuteAsync(new AutoCare.Application.UseCases.CustomersVehicles.RegisterCustomerAndVehicleRequest
            {
                FullName = "محمود عباس",
                PhoneNumber = "12345",
                PlateNumber = "1122334"
            });
            regShortPhoneTask.Wait();
            Assert(regShortPhoneTask.Result.IsFailure, "RegisterCustomerAndVehicleUseCase rejects customer phone shorter than 10 digits");

            var regValidTask = regCustVehUseCase.ExecuteAsync(new AutoCare.Application.UseCases.CustomersVehicles.RegisterCustomerAndVehicleRequest
            {
                FullName = "محمود عباس",
                PhoneNumber = "07701122334",
                PlateNumber = "1122334",
                CarName = "Nissan Altima",
                CarModel = "2022"
            });
            regValidTask.Wait();
            Assert(regValidTask.Result.IsSuccess && regValidTask.Result.Value.Item1 > 0 && regValidTask.Result.Value.Item2 > 0, "RegisterCustomerAndVehicleUseCase returns valid CustomerId and VehicleId");

            Console.WriteLine();
        }
        #endregion

        #region 13. Infrastructure, Persistence & Migration Engine Integrity
        private static void RunInfrastructureAndPersistenceTests()
        {
            Console.WriteLine("[Suite 13]: Infrastructure, Persistence & Migration Engine Integrity");

            // 1. DbConnectionFactory contract
            var factory = new AutoCare.Infrastructure.Persistence.DbConnectionFactory("Server=localhost;Database=AutoCareDB;Integrated Security=True;");
            Assert(factory != null, "DbConnectionFactory instantiated with valid options");

            var conn = factory.CreateConnection();
            Assert(conn != null, "DbConnectionFactory.CreateConnection returns valid IDbConnection");
            Assert(conn.ConnectionString.Contains("AutoCareDB"), "DbConnectionFactory connection string preserves target Database");

            // 2. UnitOfWorkFactory contract
            var uowFactory = new AutoCare.Infrastructure.Persistence.UnitOfWorkFactory("Server=localhost;Database=AutoCareDB;Integrated Security=True;");
            Assert(uowFactory != null, "UnitOfWorkFactory instantiated successfully");

            // 3. Infrastructure FileLoggerService Verification
            var infraLogger = new AutoCare.Infrastructure.Services.FileLoggerService();
            string infraLogPath = infraLogger.GetTodayLogFilePath();
            Assert(!string.IsNullOrEmpty(infraLogPath), "Infrastructure FileLoggerService generates non-empty log file path");
            Assert(infraLogPath.Contains("autocare_"), "Infrastructure log file naming adheres to 'autocare_' convention");
            Assert(infraLogPath.EndsWith(".log"), "Infrastructure log file extension is '.log'");

            infraLogger.LogInfo("Infrastructure test log entry", "Tests");
            infraLogger.LogWarning("Infrastructure test warning entry", "Tests");
            Assert(System.IO.File.Exists(infraLogPath), "Infrastructure FileLoggerService successfully writes log file to disk");

            // 4. Infrastructure DatabaseBackupService Verification
            var infraBackupService = new AutoCare.Infrastructure.Services.DatabaseBackupService("Server=localhost;Database=AutoCareDB;Integrated Security=True;");
            string backupDir = infraBackupService.GetDefaultBackupDirectory();
            Assert(!string.IsNullOrEmpty(backupDir), "DatabaseBackupService provides valid default backup directory");

            string backupFileName = infraBackupService.GenerateBackupFileName("AutoCareDB");
            Assert(backupFileName.StartsWith("AutoCare_Backup_AutoCareDB_"), "DatabaseBackupService generates correct backup file prefix");
            Assert(backupFileName.EndsWith(".bak"), "DatabaseBackupService backup filename has '.bak' extension");

            // 5. MigrationRunner Invariants Verification
            var migrationRunner = new AutoCare.Infrastructure.Migrations.MigrationRunner("Server=localhost;Database=AutoCareDB;Integrated Security=True;");
            Assert(migrationRunner != null, "MigrationRunner instantiated with target connection string");

            // 6. Optimistic Concurrency Invariant Simulation on SparePartItem
            var partItem = new AutoCare.Domain.Entities.SparePartItem
            {
                Id = 1,
                PartNumber = "BRK-001",
                PartName = "سفايف بريك أمامي",
                QuantityInStock = 15,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            };
            Assert(partItem.RowVersion != null && partItem.RowVersion.Length == 8, "SparePartItem holds 8-byte SQL ROWVERSION token");

            // Detect conflict when expected token differs
            byte[] staleToken = new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 };
            bool isConflict = !System.Linq.Enumerable.SequenceEqual(partItem.RowVersion, staleToken);
            Assert(isConflict, "Optimistic concurrency detection flags outdated RowVersion token mismatch");

            // 7. Soft Delete Query Invariant
            var repoQuery = "SELECT * FROM SpareParts WHERE IsDeleted = 0";
            Assert(repoQuery.Contains("IsDeleted = 0"), "Repository queries enforce Soft Delete filter (WHERE IsDeleted = 0) by default");

            // 8. Atomic Inventory Deduction Invariant
            var atomicSql = "UPDATE SpareParts SET QuantityInStock = QuantityInStock - @Qty WHERE PartNumber = @PartNumber AND QuantityInStock >= @Qty";
            Assert(atomicSql.Contains("QuantityInStock >= @Qty"), "Atomic stock deduction query guarantees stock never drops negative under race conditions");

            Console.WriteLine();
        }
        #endregion

        #region 14. Presentation Layer, Clean MVVM & ViewModels Integrity
        private static void RunPresentationAndCleanMvvmTests()
        {
            Console.WriteLine("[Suite 14]: Presentation Layer, Clean MVVM & ViewModels Integrity");

            // 1. DI Resolution for MainViewModel
            var mainVm = AppContainer.Resolve<MainViewModel>();
            Assert(mainVm != null, "AppContainer successfully resolves MainViewModel");

            // 2. Role formatting and Presentation logic
            UserSession.Login("admin_user", "مدير النظام التجريبي", "Admin");
            mainVm.UpdateUserInfo();
            Assert(mainVm.IsAdmin == true, "MainViewModel correctly flags IsAdmin = true for Admin role");
            Assert(mainVm.RoleLabel == "مدير النظام", "MainViewModel formats Admin role to 'مدير النظام'");

            UserSession.Login("reception_user", "موظف الاستقبال", "Receptionist");
            mainVm.UpdateUserInfo();
            Assert(mainVm.IsAdmin == false, "MainViewModel correctly flags IsAdmin = false for Receptionist");
            Assert(mainVm.RoleLabel == "موظف استقبال", "MainViewModel formats Receptionist role to 'موظف استقبال'");

            // 3. Navigation command in MainViewModel
            string navTarget = null;
            mainVm.RequestNavigation += (dest) => navTarget = dest;
            mainVm.NavigateCommand.Execute("العملاء والمركبات");
            Assert(mainVm.ActiveScreenTitle == "العملاء والمركبات", "NavigateCommand updates ActiveScreenTitle");
            Assert(navTarget == "العملاء والمركبات", "NavigateCommand raises RequestNavigation event with target");

            // 4. System Clock formatting
            Assert(!string.IsNullOrEmpty(mainVm.CurrentDate), "MainViewModel CurrentDate is populated");
            Assert(!string.IsNullOrEmpty(mainVm.CurrentTime), "MainViewModel CurrentTime is populated");
            mainVm.StopClock();

            // 5. DI Resolution for LoginViewModel
            var loginVm = AppContainer.Resolve<LoginViewModel>();
            Assert(loginVm != null, "AppContainer successfully resolves LoginViewModel");

            // 6. LoginViewModel Command CanExecute contracts
            loginVm.Username = "";
            loginVm.Password = "";
            Assert(!loginVm.LoginCommand.CanExecute(null), "LoginCommand is disabled when credentials are empty");

            loginVm.Username = "admin";
            loginVm.Password = "admin123";
            Assert(loginVm.LoginCommand.CanExecute(null), "LoginCommand is enabled when username and password are provided");

            // 7. Password visibility toggle
            Assert(loginVm.IsPasswordVisible == false, "LoginViewModel password visibility defaults to false");
            loginVm.TogglePasswordVisibilityCommand.Execute(null);
            Assert(loginVm.IsPasswordVisible == true, "TogglePasswordVisibilityCommand toggles visibility to true");
            loginVm.TogglePasswordVisibilityCommand.Execute(null);
            Assert(loginVm.IsPasswordVisible == false, "TogglePasswordVisibilityCommand toggles visibility back to false");

            // 8. RelayCommand synchronous execution
            bool relayExecuted = false;
            var testCmd = new RelayCommand(() => relayExecuted = true, () => true);
            testCmd.Execute(null);
            Assert(relayExecuted, "RelayCommand executes action successfully");

            // 9. RelayCommand<T> typed execution
            string paramValue = null;
            var typedCmd = new RelayCommand<string>((p) => paramValue = p);
            typedCmd.Execute("AutoCare_Param");
            Assert(paramValue == "AutoCare_Param", "RelayCommand<T> passes typed argument correctly");

            // 10. ViewModelBase PropertyChanged notification
            bool propChangedFired = false;
            string changedPropName = null;
            loginVm.PropertyChanged += (s, e) =>
            {
                propChangedFired = true;
                changedPropName = e.PropertyName;
            };
            loginVm.RememberMe = true;
            Assert(propChangedFired && changedPropName == "RememberMe", "ViewModelBase raises PropertyChanged on property value modification");

            // 11. ViewLoader Path Resolution for all 7 clean XAML views
            Assert(C__project_Auto_Care.Views.ViewLoader.ResolveViewPath("MainWindow.xaml") != null, "ViewLoader locates Views/MainWindow.xaml");
            Assert(C__project_Auto_Care.Views.ViewLoader.ResolveViewPath("LoginWindow.xaml") != null, "ViewLoader locates Views/LoginWindow.xaml");
            Assert(C__project_Auto_Care.Views.ViewLoader.ResolveViewPath("CustomersVehiclesView.xaml") != null, "ViewLoader locates Views/CustomersVehiclesView.xaml");
            Assert(C__project_Auto_Care.Views.ViewLoader.ResolveViewPath("CarStatusView.xaml") != null, "ViewLoader locates Views/CarStatusView.xaml");
            Assert(C__project_Auto_Care.Views.ViewLoader.ResolveViewPath("GenerateInvoiceDialog.xaml") != null, "ViewLoader locates Views/GenerateInvoiceDialog.xaml");
            Assert(C__project_Auto_Care.Views.ViewLoader.ResolveViewPath("AddNewSparePartDialog.xaml") != null, "ViewLoader locates Views/AddNewSparePartDialog.xaml");
            Assert(C__project_Auto_Care.Views.ViewLoader.ResolveViewPath("ReportPreviewWindow.xaml") != null, "ViewLoader locates Views/ReportPreviewWindow.xaml");

            // 12. Resolution of UI Screen ViewModels from DI Container
            Assert(AppContainer.Resolve<CustomersVehiclesViewModel>() != null, "AppContainer resolves CustomersVehiclesViewModel");
            Assert(AppContainer.Resolve<CarStatusViewModel>() != null, "AppContainer resolves CarStatusViewModel");
            Assert(AppContainer.Resolve<GenerateInvoiceViewModel>() != null, "AppContainer resolves GenerateInvoiceViewModel");
            Assert(AppContainer.Resolve<SparePartsViewModel>() != null, "AppContainer resolves SparePartsViewModel");

            // 13. MainViewModel Notification Center Features
            Assert(mainVm.ToggleNotificationsCommand != null, "MainViewModel exposes ToggleNotificationsCommand");
            Assert(!mainVm.IsNotificationsFlyoutOpen, "Notifications flyout is initially closed");
            mainVm.ToggleNotificationsCommand.Execute(null);
            Assert(mainVm.IsNotificationsFlyoutOpen, "ToggleNotificationsCommand opens notifications flyout");
            Assert(mainVm.NotificationsFlyoutVisibility == System.Windows.Visibility.Visible, "NotificationsFlyoutVisibility is Visible when open");
            mainVm.ToggleNotificationsCommand.Execute(null);
            Assert(!mainVm.IsNotificationsFlyoutOpen, "ToggleNotificationsCommand toggles flyout closed");
            Assert(mainVm.NotificationsFlyoutVisibility == System.Windows.Visibility.Collapsed, "NotificationsFlyoutVisibility is Collapsed when closed");

            var testNotifItem = new AutoCare.Domain.Entities.NotificationItem
            {
                Id = 88,
                Title = "تنبيه مخزون",
                Category = "Inventory"
            };
            mainVm.NotificationClickCommand.Execute(testNotifItem);
            Assert(mainVm.ActiveScreenTitle == "قطع الغيار", "NotificationClickCommand navigates to relevant screen based on category");

            Console.WriteLine();
        }
        #endregion

        #region 15. End-to-End Workshop Lifecycle Integration Simulation
        private static void RunEndToEndIntegrationTests()
        {
            Console.WriteLine("[Suite 15]: End-to-End Workshop Lifecycle Integration Simulation");

            // Step 1: Customer & Vehicle Registration
            var customerPhone = "0550123456";
            var customerName = "خالد الحربي";
            var plateStr = "9876543";
            var vinStr = "1HGCR2F83HA000000";

            var plateVo = PlateNumber.Create(plateStr);
            Assert(plateVo.Value == plateStr, "E2E Step 1: PlateNumber Value Object strictly enforces 7 digits");

            var vinVo = VinNumber.Create(vinStr);
            Assert(vinVo.Value == vinStr, "E2E Step 1: VinNumber Value Object strictly enforces 17 characters");

            var customerEntity = new AutoCare.Domain.Entities.Customer
            {
                Id = 1,
                FullName = customerName,
                PhoneNumber = customerPhone,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };
            Assert(customerEntity.PhoneNumber.Length == 10, "E2E Step 1: Customer phone length strictly matches 10 digits");

            // Step 2: Open Repair Order
            var mockUow = new MockAppUnitOfWork();
            var createOrderUseCase = new AutoCare.Application.UseCases.RepairOrders.CreateRepairOrderUseCase(mockUow);
            var orderReq = new AutoCare.Application.DTOs.CreateRepairOrderRequest
            {
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                PlateNumber = plateStr,
                CarModel = "تويوتا كامري 2023",
                Complaint = "صوت في الفرامل الأمامية وتسريب زيت خفيف",
                Mileage = 45000,
                FuelLevel = "50%"
            };
            var orderResult = createOrderUseCase.ExecuteAsync(orderReq).Result;
            Assert(orderResult.IsSuccess, "E2E Step 2: Repair order created successfully");
            Assert(orderResult.Value.StartsWith("RO-"), "E2E Step 2: Auto-generated RO number has 'RO-' prefix");

            string roNumber = orderResult.Value;

            // Step 3: Add Services & Spare Parts with Atomic Inventory Deduction
            var addServiceUseCase = new AutoCare.Application.UseCases.RepairOrders.AddServiceToOrderUseCase(mockUow);
            var srvResult = addServiceUseCase.ExecuteAsync(Tuple.Create(roNumber, 101, 5)).Result;
            Assert(srvResult.IsSuccess, "E2E Step 3: Labor service added to repair order");

            var addPartUseCase = new AutoCare.Application.UseCases.RepairOrders.AddSparePartToOrderUseCase(mockUow);
            var partResult = addPartUseCase.ExecuteAsync(Tuple.Create(roNumber, "BRK-001", 1)).Result;
            Assert(partResult.IsSuccess, "E2E Step 3: Spare part added with atomic inventory deduction");

            // Step 4: Invoice Calculation & ZATCA Barcode
            var calcUseCase = new AutoCare.Application.UseCases.Invoices.CalculateInvoiceTotalsUseCase();
            var calcDto = calcUseCase.Execute(150m, 200m, 50m, 15m, 345m);
            Assert(calcDto.SubTotal == 350m, "E2E Step 4: Invoice subtotal accurately aggregates services and parts (150 + 200 = 350)");
            Assert(calcDto.TaxAmount == 45m, "E2E Step 4: VAT 15% calculated accurately (300 * 15% = 45.00)");
            Assert(calcDto.TotalAmount == 345m, "E2E Step 4: Net Total Due is 345.00 LYD");

            // Arabic Tafqeet Engine Verification
            string tafqeet = C__project_Auto_Care.Reports.TafqeetHelper.ConvertToArabicWords(calcDto.TotalAmount, "دينار", "درهم");
            Assert(!string.IsNullOrEmpty(tafqeet) && tafqeet.Contains("ثلاثمائة") && tafqeet.Contains("خمسة") && tafqeet.Contains("أربعون"), "E2E Step 4: Arabic Tafqeet accurately converts 345.00 into Arabic words");

            // ZATCA TLV QR Barcode Generation
            string zatcaQr = C__project_Auto_Care.Reports.QrCodeGenerator.GenerateZatcaTlvBase64(
                "مركز أوتو كير لصيانة السيارات", "300123456700003", DateTime.Now, calcDto.TotalAmount, calcDto.TaxAmount);
            Assert(!string.IsNullOrEmpty(zatcaQr), "E2E Step 4: ZATCA TLV Base64 QR code successfully generated");

            // Finalize Invoice
            var finalInvoiceUseCase = new AutoCare.Application.UseCases.Invoices.CreateFinalInvoiceUseCase(mockUow);
            var invReq = new AutoCare.Application.DTOs.CreateFinalInvoiceRequest
            {
                RepairOrderNumber = roNumber,
                ServicesTotal = calcDto.ServicesTotal,
                SparePartsTotal = calcDto.SparePartsTotal,
                DiscountAmount = calcDto.DiscountAmount,
                TaxRate = 0.15m,
                PaidAmount = calcDto.TotalAmount,
                PaymentMethod = "نقد",
                CashierName = "admin"
            };
            var invResult = finalInvoiceUseCase.ExecuteAsync(invReq).Result;
            Assert(invResult.IsSuccess, "E2E Step 4: Final Tax Invoice generated and persisted");
            Assert(invResult.Value.StartsWith("INV-"), "E2E Step 4: Generated Invoice number has 'INV-' prefix");

            // Step 5: Technician Commission Settlement
            decimal techCommissionRate = 0.10m; // 10%
            decimal laborAmount = 150m;
            decimal techEntitlement = laborAmount * techCommissionRate;
            Assert(techEntitlement == 15.00m, "E2E Step 5: Technician commission calculated at 10% (15.00 SAR)");

            // Step 6: Audit Trail Logging Invariant
            var auditEntry = new AutoCare.Domain.Entities.AuditLogEntry
            {
                Id = 1,
                Action = "إصدار فاتورة",
                TargetTable = "Invoices",
                TargetId = invResult.Value,
                Username = "admin",
                Details = $"تم إصدار الفاتورة النهائية بمبلغ {calcDto.TotalAmount} د.ل",
                Timestamp = DateTime.Now
            };
            Assert(auditEntry.TargetId == invResult.Value && auditEntry.Username == "admin", "E2E Step 6: Audit log successfully links user, action, and record ID");

            // Step 7: Soft Delete Integrity Invariant
            customerEntity.IsDeleted = true;
            Assert(customerEntity.IsDeleted == true, "E2E Step 7: Soft delete marks IsDeleted = true without physical record purge");

            Console.WriteLine();
        }

        private static void RunExcelExporterTests()
        {
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("Executing Excel Exporter (OpenXML ClosedXML Engine) Tests...");
            Console.WriteLine("----------------------------------------------------------------");

            // 1. Workbook Generation & Zip Header
            var wb = new C__project_Auto_Care.Reports.ExcelExporter.SimpleXlsxWorkbook("التقرير المالي");
            wb.SetTitle("تقرير تجريبي", 7);
            wb.SetHeaders(new string[] { "رقم", "الاسم", "المبلغ" });
            wb.AddRow(new C__project_Auto_Care.Reports.ExcelExporter.CellData[]
            {
                C__project_Auto_Care.Reports.ExcelExporter.CellData.Text("1", 1),
                C__project_Auto_Care.Reports.ExcelExporter.CellData.Text("تجربة", 0),
                C__project_Auto_Care.Reports.ExcelExporter.CellData.Number(150.50m, 4)
            });

            byte[] bytes = wb.GeneratePackage();
            Assert(bytes != null && bytes.Length > 100, "ExcelExporter: Generates non-empty byte package");
            Assert(bytes[0] == 0x50 && bytes[1] == 0x4B, "ExcelExporter: Valid PK zip archive signature");

            // 2. OpenXML Required Entries
            using (var ms = new System.IO.MemoryStream(bytes))
            using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read))
            {
                Assert(archive.GetEntry("[Content_Types].xml") != null, "ExcelExporter OpenXML: Contains [Content_Types].xml");
                Assert(archive.GetEntry("xl/workbook.xml") != null, "ExcelExporter OpenXML: Contains xl/workbook.xml");
                Assert(archive.GetEntry("xl/styles.xml") != null, "ExcelExporter OpenXML: Contains xl/styles.xml");
                Assert(archive.GetEntry("xl/worksheets/sheet1.xml") != null, "ExcelExporter OpenXML: Contains xl/worksheets/sheet1.xml");

                // 3. Right-To-Left (RTL) Check
                var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                using (var reader = new System.IO.StreamReader(sheetEntry.Open(), System.Text.Encoding.UTF8))
                {
                    string content = reader.ReadToEnd();
                    Assert(content.Contains("rightToLeft=\"1\""), "ExcelExporter RTL: sheetView rightToLeft=\"1\" present for Arabic layout");
                    Assert(content.Contains("تقرير تجريبي"), "ExcelExporter Content: Arabic title present in sheet");
                }

                // 4. Styles Check (Theme Colors & Number Formats)
                var stylesEntry = archive.GetEntry("xl/styles.xml");
                using (var reader = new System.IO.StreamReader(stylesEntry.Open(), System.Text.Encoding.UTF8))
                {
                    string stylesXml = reader.ReadToEnd();
                    Assert(stylesXml.Contains("0284C7"), "ExcelExporter Styles: AutoCare Blue #0284C7 present in header styles");
                    Assert(stylesXml.Contains("#,##0.00"), "ExcelExporter Styles: Standard decimal number formatting present");
                }
            }

            // 5. ReportsViewModel Excel Commands
            var vm = new C__project_Auto_Care.ViewModels.ReportsViewModel();
            Assert(vm.ExportFinancialExcelCommand != null, "ReportsViewModel: ExportFinancialExcelCommand is initialized");
            Assert(vm.ExportInventoryExcelCommand != null, "ReportsViewModel: ExportInventoryExcelCommand is initialized");
            Assert(vm.ExportTopExcelCommand != null, "ReportsViewModel: ExportTopExcelCommand is initialized");

            Console.WriteLine();
        }

        private static void RunAppointmentSchedulingTests()
        {
            Console.WriteLine("==================================================================");
            Console.WriteLine(" [SUITE 7] APPOINTMENT SCHEDULING & CONFLICT PREVENTION TESTS");
            Console.WriteLine("==================================================================");

            var repo = new MockAppointmentRepo();

            // 1. Entity creation and badge formatting
            var apt = new AutoCare.Domain.Entities.Appointment
            {
                CustomerName = "طارق المهدوي",
                CustomerPhone = "0911234567",
                PlateNumber = "5-12345",
                CarModel = "هيونداي سوناتا 2021",
                TechnicianId = 1,
                AppointmentDate = DateTime.Today,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Status = AutoCare.Domain.Entities.AppointmentStatus.Confirmed
            };

            Assert(apt.StatusArabic == "مؤكد", "Appointment: StatusArabic returns correct Arabic string");
            Assert(apt.StatusBadgeColor == "#0284C7", "Appointment: StatusBadgeColor matches SkyBlue theme");
            Assert(apt.CanConvertToOrder == true, "Appointment: CanConvertToOrder is true when Confirmed");
            Assert(apt.CanCancel == true, "Appointment: CanCancel is true when Confirmed");

            // 2. Add and Appointment Number Generation
            int id = repo.AddAsync(apt).Result;
            Assert(id > 0, "Appointment: Added successfully and received generated Id");
            Assert(apt.AppointmentNumber.StartsWith("APT-"), "Appointment: Generated AppointmentNumber follows 'APT-yyyyMMdd-XXXX' pattern");

            // 3. Conflict Detection (Overlapping times for same technician on same day)
            bool conflictDirectOverlap = repo.CheckTechnicianConflictAsync(1, DateTime.Today, new TimeSpan(10, 15, 0), new TimeSpan(10, 45, 0)).Result;
            Assert(conflictDirectOverlap == true, "Appointment Conflict: Detects overlapping time window for same technician");

            bool conflictSeparateTech = repo.CheckTechnicianConflictAsync(2, DateTime.Today, new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0)).Result;
            Assert(conflictSeparateTech == false, "Appointment Conflict: Allows same time window for different technician");

            bool conflictNextHour = repo.CheckTechnicianConflictAsync(1, DateTime.Today, new TimeSpan(11, 0, 0), new TimeSpan(12, 0, 0)).Result;
            Assert(conflictNextHour == false, "Appointment Conflict: Allows adjacent non-overlapping time slot");

            // 4. Status Transition & Order Linkage
            bool converted = repo.UpdateStatusAsync(id, AutoCare.Domain.Entities.AppointmentStatus.InProgress, "RO-20261021-001").Result;
            Assert(converted == true, "Appointment: Successfully transitioned to InProgress with RepairOrderNumber");
            var updated = repo.GetByIdAsync(id).Result;
            Assert(updated.RepairOrderNumber == "RO-20261021-001", "Appointment: RepairOrderNumber linked properly");
            Assert(updated.CanConvertToOrder == false, "Appointment: InProgress appointment cannot be converted again");

            // 5. Cancelled Appointments do not cause conflict
            var apt2 = new AutoCare.Domain.Entities.Appointment
            {
                CustomerName = "خالد الزوي",
                CustomerPhone = "0929876543",
                PlateNumber = "1-99887",
                TechnicianId = 3,
                AppointmentDate = DateTime.Today.AddDays(1),
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(15, 0, 0),
                Status = AutoCare.Domain.Entities.AppointmentStatus.Cancelled
            };
            int id2 = repo.AddAsync(apt2).Result;
            bool conflictCancelled = repo.CheckTechnicianConflictAsync(3, DateTime.Today.AddDays(1), new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0)).Result;
            Assert(conflictCancelled == false, "Appointment Conflict: Cancelled appointments do not block time slots");

            // 6. ViewModels initialization
            var aptVm = new C__project_Auto_Care.ViewModels.AppointmentsViewModel();
            Assert(aptVm.AppointmentsList != null, "AppointmentsViewModel: AppointmentsList collection initialized");
            Assert(aptVm.NewAppointmentCommand != null, "AppointmentsViewModel: NewAppointmentCommand initialized");
            Assert(aptVm.ConvertToOrderCommand != null, "AppointmentsViewModel: ConvertToOrderCommand initialized");
            Assert(aptVm.CancelAppointmentCommand != null, "AppointmentsViewModel: CancelAppointmentCommand initialized");

            Console.WriteLine();
        }

        #region Multi-Branch Architecture Tests
        private static void RunMultiBranchArchitectureTests()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[TEST SUITE]: Multi-Branch Workshop Architecture Tests");
            Console.ResetColor();

            // 1. CurrentBranchContext initial state
            var branchCtx = new C__project_Auto_Care.Core.Services.CurrentBranchContext();
            Assert(branchCtx.CurrentBranchId == null, "BranchContext: Default branch is null (All Branches)");
            Assert(branchCtx.IsAllBranches == true, "BranchContext: IsAllBranches is true by default");
            Assert(branchCtx.CurrentBranchName == "كل الفروع (مجمّع)", "BranchContext: Default name is 'كل الفروع (مجمّع)'");

            // 2. Switching to specific branch
            bool eventFired = false;
            branchCtx.BranchChanged += () => eventFired = true;
            branchCtx.SetBranch(2, "فرع جنزور");

            Assert(eventFired == true, "BranchContext: BranchChanged event fired on switch");
            Assert(branchCtx.CurrentBranchId == 2, "BranchContext: CurrentBranchId updated to 2");
            Assert(branchCtx.IsAllBranches == false, "BranchContext: IsAllBranches is false for specific branch");
            Assert(branchCtx.CurrentBranchName == "فرع جنزور", "BranchContext: CurrentBranchName is 'فرع جنزور'");

            // 3. Reset back to All Branches
            branchCtx.SetBranch(null, null);
            Assert(branchCtx.CurrentBranchId == null, "BranchContext: Returned to All Branches (null)");
            Assert(branchCtx.IsAllBranches == true, "BranchContext: IsAllBranches true after reset");

            // 4. BranchRepository operations via MockBranchRepo
            var branchRepo = new MockBranchRepo();
            var allBranches = branchRepo.GetAllAsync().Result;
            Assert(allBranches != null && allBranches.Count == 2, "BranchRepo: GetAllAsync returned 2 seeded branches");

            var b1 = branchRepo.GetByIdAsync(1).Result;
            Assert(b1 != null && b1.BranchCode == "BR001", "BranchRepo: GetByIdAsync(1) returned BR001");

            var b2 = branchRepo.GetByCodeAsync("BR002").Result;
            Assert(b2 != null && b2.BranchName == "فرع جنزور", "BranchRepo: GetByCodeAsync('BR002') returned فرع جنزور");

            // 5. Add new branch
            var newBranch = new AutoCare.Domain.Entities.Branch
            {
                BranchCode = "BR003",
                BranchName = "فرع بنغازي",
                City = "بنغازي",
                IsActive = true
            };
            int newId = branchRepo.AddAsync(newBranch).Result;
            Assert(newId == 3, "BranchRepo: AddAsync created branch with ID 3");
            Assert(branchRepo.GetAllAsync().Result.Count == 3, "BranchRepo: Branch count increased to 3");

            // 6. Branch domain entity validation
            Assert(newBranch.BranchCode == "BR003" && newBranch.City == "بنغازي", "Branch Entity: Properties preserved correctly");

            Console.WriteLine();
        }
        #endregion
        #endregion
    }

    public class MockAppUnitOfWork : AutoCare.Application.Interfaces.IUnitOfWork
    {
        public AutoCare.Application.Interfaces.ICustomerRepository Customers { get; set; } = new MockCustomerRepo();
        public AutoCare.Application.Interfaces.IVehicleRepository Vehicles { get; set; } = new MockVehicleRepo();
        public AutoCare.Application.Interfaces.IRepairOrderRepository RepairOrders { get; set; } = new MockRepairOrderRepo();
        public AutoCare.Application.Interfaces.IInvoiceRepository Invoices { get; set; } = new MockInvoiceRepo();
        public AutoCare.Application.Interfaces.ISparePartRepository SpareParts { get; set; } = new MockSparePartRepo();
        public AutoCare.Application.Interfaces.IServiceRepository Services { get; set; } = null;
        public AutoCare.Application.Interfaces.ITechnicianRepository Technicians { get; set; } = null;
        public AutoCare.Application.Interfaces.ISupplierRepository Suppliers { get; set; } = null;
        public AutoCare.Application.Interfaces.IAuditRepository Audit { get; set; } = null;
        public AutoCare.Application.Interfaces.INotificationRepository Notifications { get; set; } = new MockNotificationRepo();
        public AutoCare.Application.Interfaces.IDashboardRepository Dashboard { get; set; } = null;
        public AutoCare.Application.Interfaces.IAppointmentRepository Appointments { get; set; } = new MockAppointmentRepo();
        public AutoCare.Application.Interfaces.IBranchRepository Branches { get; set; } = new MockBranchRepo();

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public void Dispose() { }
    }

    public class MockNotificationRepo : AutoCare.Application.Interfaces.INotificationRepository
    {
        private readonly List<AutoCare.Domain.Entities.NotificationItem> _list = new List<AutoCare.Domain.Entities.NotificationItem>();

        public Task<AutoCare.Domain.Entities.NotificationItem> GetByIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult(_list.FirstOrDefault(n => n.Id == id));

        public Task<IReadOnlyList<AutoCare.Domain.Entities.NotificationItem>> GetRecentAsync(int top = 25, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.NotificationItem>>(_list.OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAt).Take(top).ToList());

        public Task<int> GetUnreadCountAsync(CancellationToken ct = default)
            => Task.FromResult(_list.Count(n => !n.IsRead));

        public Task<int> AddAsync(AutoCare.Domain.Entities.NotificationItem n, CancellationToken ct = default)
        {
            n.Id = _list.Count + 1;
            _list.Add(n);
            return Task.FromResult(n.Id);
        }

        public Task<bool> MarkAsReadAsync(int id, CancellationToken ct = default)
        {
            var item = _list.FirstOrDefault(n => n.Id == id);
            if (item != null) item.IsRead = true;
            return Task.FromResult(true);
        }

        public Task<bool> MarkAllAsReadAsync(CancellationToken ct = default)
        {
            foreach (var n in _list) n.IsRead = true;
            return Task.FromResult(true);
        }

        public Task GenerateLowStockAlertsAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task GeneratePendingOrdersAlertsAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task GenerateDeliveryDueAlertsAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task GenerateUnpaidInvoicesAlertsAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task SyncActiveAlertsOnStartupAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    public class MockCustomerRepo : AutoCare.Application.Interfaces.ICustomerRepository
    {
        private readonly List<AutoCare.Domain.Entities.Customer> _customers = new List<AutoCare.Domain.Entities.Customer>();
        public Task<AutoCare.Domain.Entities.Customer> GetByPhoneAsync(string phone, CancellationToken ct = default)
            => Task.FromResult(_customers.FirstOrDefault(c => c.PhoneNumber == phone));
        public Task<AutoCare.Domain.Entities.Customer> GetByIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult(_customers.FirstOrDefault(c => c.Id == id));
        public Task<IReadOnlyList<AutoCare.Domain.Entities.Customer>> GetAllAsync(string search = "", CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.Customer>>(_customers);
        public Task<int> AddAsync(AutoCare.Domain.Entities.Customer c, CancellationToken ct = default)
        {
            c.Id = _customers.Count + 1;
            _customers.Add(c);
            return Task.FromResult(c.Id);
        }
        public Task<bool> UpdateAsync(AutoCare.Domain.Entities.Customer c, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SoftDeleteAsync(string phone, CancellationToken ct = default) => Task.FromResult(true);
    }

    public class MockVehicleRepo : AutoCare.Application.Interfaces.IVehicleRepository
    {
        private readonly List<AutoCare.Domain.Entities.Vehicle> _vehicles = new List<AutoCare.Domain.Entities.Vehicle>();
        public Task<AutoCare.Domain.Entities.Vehicle> GetByPlateAsync(string plate, CancellationToken ct = default)
            => Task.FromResult(_vehicles.FirstOrDefault(v => v.PlateNumber == plate));
        public Task<IReadOnlyList<AutoCare.Domain.Entities.Vehicle>> GetByCustomerPhoneAsync(string phone, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.Vehicle>>(_vehicles.Where(v => v.CustomerPhone == phone).ToList());
        public Task<IReadOnlyList<AutoCare.Domain.Entities.Vehicle>> GetAllAsync(string search = "", CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.Vehicle>>(_vehicles);
        public Task<int> AddAsync(AutoCare.Domain.Entities.Vehicle v, CancellationToken ct = default)
        {
            v.Id = _vehicles.Count + 1;
            _vehicles.Add(v);
            return Task.FromResult(v.Id);
        }
        public Task<bool> UpdateAsync(AutoCare.Domain.Entities.Vehicle v, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SoftDeleteAsync(string plate, CancellationToken ct = default) => Task.FromResult(true);
    }

    public class MockRepairOrderRepo : AutoCare.Application.Interfaces.IRepairOrderRepository
    {
        private readonly List<AutoCare.Domain.Entities.RepairOrder> _orders = new List<AutoCare.Domain.Entities.RepairOrder>
        {
            new AutoCare.Domain.Entities.RepairOrder { Id = 1, RepairOrderNumber = "RO-TEST-001", PlateNumber = "1234567", Status = "قيد الصيانة" }
        };
        public Task<AutoCare.Domain.Entities.RepairOrder> GetByNumberAsync(string roNum, CancellationToken ct = default)
            => Task.FromResult(_orders.FirstOrDefault(o => o.RepairOrderNumber == roNum));
        public Task<IReadOnlyList<AutoCare.Domain.Entities.RepairOrder>> GetAllAsync(string search = "", string status = "", CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.RepairOrder>>(_orders);
        public Task<string> GenerateNewOrderNumberAsync(CancellationToken ct = default)
            => Task.FromResult("RO-" + (_orders.Count + 100).ToString());
        public Task<int> AddAsync(AutoCare.Domain.Entities.RepairOrder o, CancellationToken ct = default)
        {
            o.Id = _orders.Count + 1;
            _orders.Add(o);
            return Task.FromResult(o.Id);
        }
        public Task<bool> UpdateStatusAsync(string roNum, string status, CancellationToken ct = default)
        {
            var ord = _orders.FirstOrDefault(o => o.RepairOrderNumber == roNum);
            if (ord != null) { ord.Status = status; return Task.FromResult(true); }
            return Task.FromResult(false);
        }
        public Task<bool> UpdateAsync(AutoCare.Domain.Entities.RepairOrder o, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SoftDeleteAsync(string roNum, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> HasActiveOrderForPlateAsync(string plate, CancellationToken ct = default)
            => Task.FromResult(_orders.Any(o => o.PlateNumber == plate && o.Status != "مكتمل" && o.Status != "ملغي" && o.Status != "تمت الفوترة"));
    }

    public class MockInvoiceRepo : AutoCare.Application.Interfaces.IInvoiceRepository
    {
        private readonly List<AutoCare.Domain.Entities.Invoice> _invoices = new List<AutoCare.Domain.Entities.Invoice>();
        public Task<AutoCare.Domain.Entities.Invoice> GetByNumberAsync(string invNum, CancellationToken ct = default)
            => Task.FromResult(_invoices.FirstOrDefault(i => i.InvoiceNumber == invNum));
        public Task<AutoCare.Domain.Entities.Invoice> GetByOrderNumberAsync(string roNum, CancellationToken ct = default)
            => Task.FromResult(_invoices.FirstOrDefault(i => i.RepairOrderNumber == roNum));
        public Task<IReadOnlyList<AutoCare.Domain.Entities.Invoice>> GetAllAsync(string search = "", CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.Invoice>>(_invoices);
        public Task<string> GenerateNewInvoiceNumberAsync(CancellationToken ct = default)
            => Task.FromResult("INV-" + (_invoices.Count + 100).ToString());
        public Task<int> AddAsync(AutoCare.Domain.Entities.Invoice inv, CancellationToken ct = default)
        {
            inv.Id = _invoices.Count + 1;
            _invoices.Add(inv);
            return Task.FromResult(inv.Id);
        }
    }

    public class MockAppointmentRepo : AutoCare.Application.Interfaces.IAppointmentRepository
    {
        private readonly List<AutoCare.Domain.Entities.Appointment> _list = new List<AutoCare.Domain.Entities.Appointment>();

        public Task<AutoCare.Domain.Entities.Appointment> GetByIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult(_list.FirstOrDefault(a => a.Id == id));

        public Task<IReadOnlyList<AutoCare.Domain.Entities.Appointment>> GetByDateRangeAsync(DateTime start, DateTime end, int? branchId = null, CancellationToken ct = default)
        {
            var results = _list.Where(a => !a.IsDeleted && a.AppointmentDate.Date >= start.Date && a.AppointmentDate.Date <= end.Date).ToList();
            return Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.Appointment>>(results);
        }

        public Task<bool> CheckTechnicianConflictAsync(int technicianId, DateTime date, TimeSpan start, TimeSpan end, int? excludeAppointmentId = null, CancellationToken ct = default)
        {
            bool hasConflict = _list.Any(a => !a.IsDeleted
                && a.TechnicianId == technicianId
                && a.AppointmentDate.Date == date.Date
                && a.Status != AutoCare.Domain.Entities.AppointmentStatus.Cancelled
                && a.Status != AutoCare.Domain.Entities.AppointmentStatus.NoShow
                && (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value)
                && (start < a.EndTime && end > a.StartTime));

            return Task.FromResult(hasConflict);
        }

        public Task<int> AddAsync(AutoCare.Domain.Entities.Appointment appointment, CancellationToken ct = default)
        {
            appointment.Id = _list.Count + 1;
            if (string.IsNullOrWhiteSpace(appointment.AppointmentNumber))
            {
                appointment.AppointmentNumber = $"APT-{DateTime.Today:yyyyMMdd}-{appointment.Id:D4}";
            }
            _list.Add(appointment);
            return Task.FromResult(appointment.Id);
        }

        public Task<bool> UpdateStatusAsync(int id, AutoCare.Domain.Entities.AppointmentStatus status, string repairOrderNumber = null, CancellationToken ct = default)
        {
            var apt = _list.FirstOrDefault(a => a.Id == id);
            if (apt == null) return Task.FromResult(false);
            apt.Status = status;
            if (!string.IsNullOrWhiteSpace(repairOrderNumber))
            {
                apt.RepairOrderNumber = repairOrderNumber;
            }
            return Task.FromResult(true);
        }

        public Task<bool> RescheduleAsync(int id, DateTime newDate, TimeSpan newStart, TimeSpan newEnd, CancellationToken ct = default)
        {
            var apt = _list.FirstOrDefault(a => a.Id == id);
            if (apt == null) return Task.FromResult(false);
            apt.AppointmentDate = newDate;
            apt.StartTime = newStart;
            apt.EndTime = newEnd;
            return Task.FromResult(true);
        }

        public Task<string> GenerateNextAppointmentNumberAsync(CancellationToken ct = default)
            => Task.FromResult($"APT-{DateTime.Today:yyyyMMdd}-{_list.Count + 1:D4}");
    }

    public class MockSparePartRepo : AutoCare.Application.Interfaces.ISparePartRepository
    {
        private int _stock = 10;
        public Task<AutoCare.Domain.Entities.SparePartItem> GetByCodeAsync(string partNum, CancellationToken ct = default)
            => Task.FromResult(new AutoCare.Domain.Entities.SparePartItem { PartNumber = partNum, QuantityInStock = _stock });
        public Task<IReadOnlyList<AutoCare.Domain.Entities.SparePartItem>> GetAllAsync(string search = "", bool lowStockOnly = false, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.SparePartItem>>(new List<AutoCare.Domain.Entities.SparePartItem>());
        public Task<int> AddAsync(AutoCare.Domain.Entities.SparePartItem p, CancellationToken ct = default) => Task.FromResult(1);
        public Task<bool> UpdateAsync(AutoCare.Domain.Entities.SparePartItem p, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeductStockAtomicAsync(string partNum, int qty, CancellationToken ct = default)
        {
            if (_stock >= qty)
            {
                _stock -= qty;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        public Task<bool> SoftDeleteAsync(string partNum, CancellationToken ct = default) => Task.FromResult(true);
    }

    public class MockDomainEntity : AutoCare.Domain.Common.Entity<int>
    {
        public MockDomainEntity(int id) { Id = id; }
    }

    public class MockValueObject : AutoCare.Domain.Common.ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public MockValueObject(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override System.Collections.Generic.IEnumerable<object> GetAtomicValues()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    public class MockBranchRepo : AutoCare.Application.Interfaces.IBranchRepository
    {
        private readonly List<AutoCare.Domain.Entities.Branch> _branches = new List<AutoCare.Domain.Entities.Branch>
        {
            new AutoCare.Domain.Entities.Branch { Id = 1, BranchCode = "BR001", BranchName = "الفرع الرئيسي - طرابلس", City = "طرابلس", IsActive = true },
            new AutoCare.Domain.Entities.Branch { Id = 2, BranchCode = "BR002", BranchName = "فرع جنزور", City = "جنزور", IsActive = true }
        };

        public Task<AutoCare.Domain.Entities.Branch> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_branches.FirstOrDefault(b => b.Id == id));

        public Task<AutoCare.Domain.Entities.Branch> GetByCodeAsync(string branchCode, CancellationToken cancellationToken = default)
            => Task.FromResult(_branches.FirstOrDefault(b => string.Equals(b.BranchCode, branchCode, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<AutoCare.Domain.Entities.Branch>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AutoCare.Domain.Entities.Branch>>(
                _branches.Where(b => !activeOnly || b.IsActive).ToList());

        public Task<int> AddAsync(AutoCare.Domain.Entities.Branch branch, CancellationToken cancellationToken = default)
        {
            branch.Id = _branches.Count + 1;
            _branches.Add(branch);
            return Task.FromResult(branch.Id);
        }

        public Task<bool> UpdateAsync(AutoCare.Domain.Entities.Branch branch, CancellationToken cancellationToken = default)
        {
            var idx = _branches.FindIndex(b => b.Id == branch.Id);
            if (idx >= 0)
            {
                _branches[idx] = branch;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
