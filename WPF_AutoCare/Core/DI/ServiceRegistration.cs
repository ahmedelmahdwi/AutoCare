using System;
using C__project_Auto_Care.Core.Services;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.ViewModels;

namespace C__project_Auto_Care.Core.DI
{
    /// <summary>
    /// Central IoC Bootstrapper registering all Clean Architecture layers into the service collection.
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddAutoCareCoreServices(this IServiceCollection services, string connectionString = "")
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // 1. Data Infrastructure & DbHelper
            services.AddSingleton<IDbHelper>(sp => new DbHelper(connectionString ?? ""));

            // 2. Repositories
            services.AddSingleton<IAuditRepository, AuditRepository>();
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<ICustomerRepository, CustomerRepository>();
            services.AddSingleton<IVehicleRepository, VehicleRepository>();
            services.AddSingleton<IRepairOrderRepository, RepairOrderRepository>();
            services.AddSingleton<IRepairOrderServiceRepository, RepairOrderServiceRepository>();
            services.AddSingleton<IRepairOrderSparePartRepository, RepairOrderSparePartRepository>();
            services.AddSingleton<IServiceRepository, ServiceRepository>();
            services.AddSingleton<ISparePartRepository, SparePartRepository>();
            services.AddSingleton<IInvoiceRepository, InvoiceRepository>();
            services.AddSingleton<ITechnicianRepository, TechnicianRepository>();
            services.AddSingleton<ITechnicianSettlementRepository, TechnicianSettlementRepository>();
            services.AddSingleton<ISupplierRepository, SupplierRepository>();

            // 3. Unit of Work
            services.AddSingleton<C__project_Auto_Care.Interfaces.IUnitOfWorkFactory, UnitOfWorkFactory>();
            services.AddScoped<C__project_Auto_Care.Interfaces.IUnitOfWork, UnitOfWork>();
            services.AddSingleton<AutoCare.Application.Interfaces.IUnitOfWorkFactory>(sp =>
            {
                var dbHelper = sp.GetService<IDbHelper>();
                string connStr = dbHelper?.GetConnectionString() ?? connectionString ?? "";
                return new AutoCare.Infrastructure.Persistence.UnitOfWorkFactory(connStr);
            });

            // 4. Infrastructure & Operational Services
            services.AddSingleton<ILoggerService, FileLoggerService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();

            // 5. Domain Application Services
            services.AddSingleton<IAuditService, AuditService>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IVehicleHistoryService, VehicleHistoryService>();
            services.AddSingleton<IServiceCatalogService, ServiceCatalogService>();
            services.AddSingleton<IRepairOrderLifecycleService, RepairOrderLifecycleService>();
            services.AddSingleton<IRepairOrderItemsService, RepairOrderItemsService>();
            services.AddSingleton<IRepairOrderService, RepairOrderService>();
            services.AddSingleton<IInvoiceService, InvoiceService>();
            services.AddSingleton<IInventoryService, InventoryService>();
            services.AddSingleton<ISupplierService, SupplierService>();
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton<ITechnicianService, TechnicianService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IAppointmentService, AppointmentService>();
            services.AddSingleton<AutoCare.Application.Interfaces.ICurrentBranchContext, CurrentBranchContext>();
            services.AddSingleton<IRecycleBinService, RecycleBinService>();

            // 6. ViewModels (Transient - fresh instance for each window / tab)
            services.AddTransient<MainViewModel>();
            services.AddTransient<AppointmentsViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<DatabaseBackupViewModel>();
            services.AddTransient<AuditLogViewModel>();
            services.AddTransient<RecycleBinViewModel>();
            services.AddTransient<CarStatusViewModel>();
            services.AddTransient<CustomersVehiclesViewModel>();
            services.AddTransient<FinancialReportViewModel>();
            services.AddTransient<GenerateInvoiceViewModel>();
            services.AddTransient<InventoryReportViewModel>();
            services.AddTransient<InvoiceScreenViewModel>();
            services.AddTransient<NewRepairOrderViewModel>();
            services.AddTransient<ServiceAndPartsViewModel>();
            services.AddTransient<ServicesViewModel>();
            services.AddTransient<SettleCommissionsViewModel>();
            services.AddTransient<SparePartsViewModel>();
            services.AddTransient<SuppliersViewModel>();
            services.AddTransient<TechniciansViewModel>();
            services.AddTransient<TopServicesPartsViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<SparePartDialogViewModel>();
            services.AddTransient<ServiceDialogViewModel>();
            services.AddTransient<UserDialogViewModel>();
            services.AddTransient<TechnicianDialogViewModel>();
            services.AddTransient<SupplierDialogViewModel>();

            return services;
        }
    }
}
