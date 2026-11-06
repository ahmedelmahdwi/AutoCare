using System;
using C__project_Auto_Care.Core.DI;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care
{
    /// <summary>
    /// Service Locator adapter bridging legacy access to the central IoC Container (<see cref="AppContainer"/>).
    /// Adheres to Clean Architecture and Inversion of Control.
    /// </summary>
    public static class AppServiceLocator
    {
        private static IDbHelper _customDbHelper;
        private static IAuditRepository _customAuditRepo;
        private static IUserRepository _customUserRepo;
        private static ICustomerRepository _customCustomerRepo;
        private static IVehicleRepository _customVehicleRepo;
        private static IRepairOrderRepository _customRoRepo;
        private static IRepairOrderServiceRepository _customRosRepo;
        private static IRepairOrderSparePartRepository _customRospRepo;
        private static IServiceRepository _customServiceRepo;
        private static ISparePartRepository _customSparePartRepo;
        private static IInvoiceRepository _customInvoiceRepo;
        private static ITechnicianRepository _customTechnicianRepo;
        private static ITechnicianSettlementRepository _customTechnicianSettlementRepo;
        private static ISupplierRepository _customSupplierRepo;

        private static IAuthService _customAuthService;
        private static IRepairOrderLifecycleService _customRepairOrderLifecycleService;
        private static IRepairOrderItemsService _customRepairOrderItemsService;
        private static IVehicleHistoryService _customVehicleHistoryService;
        private static IServiceCatalogService _customServiceCatalogService;
        private static IRepairOrderService _customRepairOrderService;
        private static IInvoiceService _customInvoiceService;
        private static IInventoryService _customInventoryService;
        private static ISupplierService _customSupplierService;
        private static ICustomerService _customCustomerService;
        private static ITechnicianService _customTechnicianService;
        private static IAuditService _customAuditService;
        private static IUnitOfWorkFactory _customUowFactory;
        private static IDialogService _customDialogService;
        private static ILoggerService _customLoggerService;
        private static IDatabaseBackupService _customDatabaseBackupService;
        private static INotificationService _customNotificationService;
        private static IAppointmentService _customAppointmentService;
        private static AutoCare.Application.Interfaces.ICurrentBranchContext _customBranchContext;
        private static IRecycleBinService _customRecycleBinService;

        public static IDbHelper DbHelper
        {
            get => _customDbHelper ?? AppContainer.Resolve<IDbHelper>();
            set
            {
                _customDbHelper = value;
                if (value != null)
                {
                    // Reinitialize container with the provided DbHelper
                    var services = new ServiceCollection();
                    services.AddSingleton(value);
                    services.AddAutoCareCoreServices();
                    AppContainer.Current = services.BuildServiceProvider();
                }
                else
                {
                    AppContainer.Reset();
                }
            }
        }

        public static void Reset()
        {
            _customDbHelper = null;
            _customAuditRepo = null;
            _customUserRepo = null;
            _customCustomerRepo = null;
            _customVehicleRepo = null;
            _customRoRepo = null;
            _customRosRepo = null;
            _customRospRepo = null;
            _customServiceRepo = null;
            _customSparePartRepo = null;
            _customInvoiceRepo = null;
            _customTechnicianRepo = null;
            _customTechnicianSettlementRepo = null;
            _customSupplierRepo = null;

            _customAuthService = null;
            _customRepairOrderLifecycleService = null;
            _customRepairOrderItemsService = null;
            _customVehicleHistoryService = null;
            _customServiceCatalogService = null;
            _customRepairOrderService = null;
            _customInvoiceService = null;
            _customInventoryService = null;
            _customSupplierService = null;
            _customCustomerService = null;
            _customTechnicianService = null;
            _customAuditService = null;
            _customUowFactory = null;
            _customDialogService = null;
            _customRecycleBinService = null;

            AppContainer.Reset();
        }

        // ── Repositories (Resolved via IoC Container) ─────────────────
        public static IAuditRepository AuditRepo
        {
            get => _customAuditRepo ?? AppContainer.Resolve<IAuditRepository>();
            set => _customAuditRepo = value;
        }

        public static IUserRepository UserRepo
        {
            get => _customUserRepo ?? AppContainer.Resolve<IUserRepository>();
            set => _customUserRepo = value;
        }

        public static ICustomerRepository CustomerRepo
        {
            get => _customCustomerRepo ?? AppContainer.Resolve<ICustomerRepository>();
            set => _customCustomerRepo = value;
        }

        public static IVehicleRepository VehicleRepo
        {
            get => _customVehicleRepo ?? AppContainer.Resolve<IVehicleRepository>();
            set => _customVehicleRepo = value;
        }

        public static IRepairOrderRepository RepairOrderRepo
        {
            get => _customRoRepo ?? AppContainer.Resolve<IRepairOrderRepository>();
            set => _customRoRepo = value;
        }

        public static IRepairOrderServiceRepository RepairOrderServiceRepo
        {
            get => _customRosRepo ?? AppContainer.Resolve<IRepairOrderServiceRepository>();
            set => _customRosRepo = value;
        }

        public static IRepairOrderSparePartRepository RepairOrderSparePartRepo
        {
            get => _customRospRepo ?? AppContainer.Resolve<IRepairOrderSparePartRepository>();
            set => _customRospRepo = value;
        }

        public static IServiceRepository ServiceRepo
        {
            get => _customServiceRepo ?? AppContainer.Resolve<IServiceRepository>();
            set => _customServiceRepo = value;
        }

        public static ISparePartRepository SparePartRepo
        {
            get => _customSparePartRepo ?? AppContainer.Resolve<ISparePartRepository>();
            set => _customSparePartRepo = value;
        }

        public static IInvoiceRepository InvoiceRepo
        {
            get => _customInvoiceRepo ?? AppContainer.Resolve<IInvoiceRepository>();
            set => _customInvoiceRepo = value;
        }

        public static ITechnicianRepository TechnicianRepo
        {
            get => _customTechnicianRepo ?? AppContainer.Resolve<ITechnicianRepository>();
            set => _customTechnicianRepo = value;
        }

        public static ITechnicianSettlementRepository TechnicianSettlementRepo
        {
            get => _customTechnicianSettlementRepo ?? AppContainer.Resolve<ITechnicianSettlementRepository>();
            set => _customTechnicianSettlementRepo = value;
        }

        public static ISupplierRepository SupplierRepo
        {
            get => _customSupplierRepo ?? AppContainer.Resolve<ISupplierRepository>();
            set => _customSupplierRepo = value;
        }

        // ── Clean Architecture Application Services ─────────────────
        public static IAuthService AuthService
        {
            get => _customAuthService ?? AppContainer.Resolve<IAuthService>();
            set => _customAuthService = value;
        }

        public static IRepairOrderLifecycleService RepairOrderLifecycleService
        {
            get => _customRepairOrderLifecycleService ?? AppContainer.Resolve<IRepairOrderLifecycleService>();
            set => _customRepairOrderLifecycleService = value;
        }

        public static IRepairOrderItemsService RepairOrderItemsService
        {
            get => _customRepairOrderItemsService ?? AppContainer.Resolve<IRepairOrderItemsService>();
            set => _customRepairOrderItemsService = value;
        }

        public static IVehicleHistoryService VehicleHistoryService
        {
            get => _customVehicleHistoryService ?? AppContainer.Resolve<IVehicleHistoryService>();
            set => _customVehicleHistoryService = value;
        }

        public static IServiceCatalogService ServiceCatalogService
        {
            get => _customServiceCatalogService ?? AppContainer.Resolve<IServiceCatalogService>();
            set => _customServiceCatalogService = value;
        }

        public static IRepairOrderService RepairOrderService
        {
            get => _customRepairOrderService ?? AppContainer.Resolve<IRepairOrderService>();
            set => _customRepairOrderService = value;
        }

        public static IInvoiceService InvoiceService
        {
            get => _customInvoiceService ?? AppContainer.Resolve<IInvoiceService>();
            set => _customInvoiceService = value;
        }

        public static IInventoryService InventoryService
        {
            get => _customInventoryService ?? AppContainer.Resolve<IInventoryService>();
            set => _customInventoryService = value;
        }

        public static ISupplierService SupplierService
        {
            get => _customSupplierService ?? AppContainer.Resolve<ISupplierService>();
            set => _customSupplierService = value;
        }

        public static ICustomerService CustomerService
        {
            get => _customCustomerService ?? AppContainer.Resolve<ICustomerService>();
            set => _customCustomerService = value;
        }

        public static ITechnicianService TechnicianService
        {
            get => _customTechnicianService ?? AppContainer.Resolve<ITechnicianService>();
            set => _customTechnicianService = value;
        }

        public static IAuditService AuditService
        {
            get => _customAuditService ?? AppContainer.Resolve<IAuditService>();
            set => _customAuditService = value;
        }

        // ── Unit of Work & Transaction Management ────────────────────
        public static IUnitOfWorkFactory UnitOfWorkFactory
        {
            get => _customUowFactory ?? AppContainer.Resolve<IUnitOfWorkFactory>();
            set => _customUowFactory = value;
        }

        public static IUnitOfWork CreateUnitOfWork()
        {
            return UnitOfWorkFactory.Create();
        }

        public static IUnitOfWork UnitOfWork => CreateUnitOfWork();

        public static AutoCare.Application.Interfaces.IUnitOfWork AppUnitOfWork
        {
            get
            {
                var factory = AppContainer.Resolve<AutoCare.Application.Interfaces.IUnitOfWorkFactory>();
                return factory?.Create();
            }
        }

        // ── Dialog & Notification Service ────────────────────────────
        public static IDialogService DialogService
        {
            get => _customDialogService ?? AppContainer.Resolve<IDialogService>();
            set => _customDialogService = value;
        }

        // ── Operational & Infrastructure Services ────────────────────
        public static ILoggerService LoggerService
        {
            get => _customLoggerService ?? AppContainer.Resolve<ILoggerService>();
            set => _customLoggerService = value;
        }

        public static IDatabaseBackupService DatabaseBackupService
        {
            get => _customDatabaseBackupService ?? AppContainer.Resolve<IDatabaseBackupService>();
            set => _customDatabaseBackupService = value;
        }

        public static INotificationService NotificationService
        {
            get => _customNotificationService ?? AppContainer.Resolve<INotificationService>();
            set => _customNotificationService = value;
        }

        public static IAppointmentService AppointmentService
        {
            get => _customAppointmentService ?? AppContainer.Resolve<IAppointmentService>();
            set => _customAppointmentService = value;
        }

        public static AutoCare.Application.Interfaces.ICurrentBranchContext BranchContext
        {
            get => _customBranchContext ?? AppContainer.Resolve<AutoCare.Application.Interfaces.ICurrentBranchContext>();
            set => _customBranchContext = value;
        }

        public static IRecycleBinService RecycleBinService
        {
            get => _customRecycleBinService ?? AppContainer.Resolve<IRecycleBinService>();
            set => _customRecycleBinService = value;
        }
    }
}
