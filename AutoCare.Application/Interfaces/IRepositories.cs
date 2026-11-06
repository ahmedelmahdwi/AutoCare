using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCare.Domain.Entities;

namespace AutoCare.Application.Interfaces
{
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<TKey> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    }

    public interface ICustomerRepository
    {
        Task<Customer> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
        Task<Customer> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Customer>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default);
        Task<int> AddAsync(Customer customer, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteAsync(string phoneNumber, CancellationToken cancellationToken = default);
    }

    public interface IVehicleRepository
    {
        Task<Vehicle> GetByPlateAsync(string plateNumber, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Vehicle>> GetByCustomerPhoneAsync(string phone, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Vehicle>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default);
        Task<int> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteAsync(string plateNumber, CancellationToken cancellationToken = default);
    }

    public interface IRepairOrderRepository
    {
        Task<RepairOrder> GetByNumberAsync(string roNumber, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RepairOrder>> GetAllAsync(string searchTerm = "", string statusFilter = "", CancellationToken cancellationToken = default);
        Task<string> GenerateNewOrderNumberAsync(CancellationToken cancellationToken = default);
        Task<int> AddAsync(RepairOrder order, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(string roNumber, string status, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(RepairOrder order, CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteAsync(string roNumber, CancellationToken cancellationToken = default);
        Task<bool> HasActiveOrderForPlateAsync(string plateNumber, CancellationToken cancellationToken = default);
    }

    public interface IInvoiceRepository
    {
        Task<Invoice> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
        Task<Invoice> GetByOrderNumberAsync(string roNumber, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Invoice>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default);
        Task<string> GenerateNewInvoiceNumberAsync(CancellationToken cancellationToken = default);
        Task<int> AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    }

    public interface ISparePartRepository
    {
        Task<SparePartItem> GetByCodeAsync(string partNumber, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SparePartItem>> GetAllAsync(string searchTerm = "", bool lowStockOnly = false, CancellationToken cancellationToken = default);
        Task<int> AddAsync(SparePartItem part, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(SparePartItem part, CancellationToken cancellationToken = default);
        Task<bool> DeductStockAtomicAsync(string partNumber, int quantity, CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteAsync(string partNumber, CancellationToken cancellationToken = default);
    }

    public interface IServiceRepository
    {
        Task<ServiceItem> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceItem>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default);
        Task<int> AddAsync(ServiceItem service, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(ServiceItem service, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface ITechnicianRepository
    {
        Task<Technician> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Technician>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
        Task<int> AddAsync(Technician technician, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Technician technician, CancellationToken cancellationToken = default);
    }

    public interface ISupplierRepository
    {
        Task<Supplier> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Supplier>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default);
        Task<int> AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);
    }

    public interface IAuditRepository
    {
        Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AuditLogEntry>> GetLogsAsync(DateTime? from, DateTime? to, string action, string search, CancellationToken cancellationToken = default);
    }

    public interface INotificationRepository
    {
        Task<NotificationItem> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<NotificationItem>> GetRecentAsync(int top = 25, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
        Task<int> AddAsync(NotificationItem notification, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
        Task GenerateLowStockAlertsAsync(CancellationToken cancellationToken = default);
        Task GeneratePendingOrdersAlertsAsync(CancellationToken cancellationToken = default);
        Task GenerateDeliveryDueAlertsAsync(CancellationToken cancellationToken = default);
        Task GenerateUnpaidInvoicesAlertsAsync(CancellationToken cancellationToken = default);
        Task SyncActiveAlertsOnStartupAsync(CancellationToken cancellationToken = default);
    }

    public interface IDashboardRepository
    {
        Task<IReadOnlyList<AutoCare.Application.DTOs.MonthlyRevenueDto>> GetMonthlyRevenuesAsync(int months = 6, int? branchId = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AutoCare.Application.DTOs.OrderStatusDistributionDto>> GetOrderStatusDistributionAsync(int? branchId = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AutoCare.Application.DTOs.TopServiceDto>> GetTopServicesAsync(int top = 5, int? branchId = null, CancellationToken cancellationToken = default);

        IReadOnlyList<AutoCare.Application.DTOs.MonthlyRevenueDto> GetMonthlyRevenues(int months = 6, int? branchId = null);
        IReadOnlyList<AutoCare.Application.DTOs.OrderStatusDistributionDto> GetOrderStatusDistribution(int? branchId = null);
        IReadOnlyList<AutoCare.Application.DTOs.TopServiceDto> GetTopServices(int top = 5, int? branchId = null);
    }

    public interface IAppointmentRepository
    {
        Task<Appointment> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int? branchId = null, CancellationToken cancellationToken = default);
        Task<bool> CheckTechnicianConflictAsync(int technicianId, DateTime date, TimeSpan start, TimeSpan end, int? excludeAppointmentId = null, CancellationToken cancellationToken = default);
        Task<int> AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(int id, AppointmentStatus status, string repairOrderNumber = null, CancellationToken cancellationToken = default);
        Task<bool> RescheduleAsync(int id, DateTime newDate, TimeSpan newStart, TimeSpan newEnd, CancellationToken cancellationToken = default);
        Task<string> GenerateNextAppointmentNumberAsync(CancellationToken cancellationToken = default);
    }

    public interface IBranchRepository
    {
        Task<Branch> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Branch> GetByCodeAsync(string branchCode, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Branch>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
        Task<int> AddAsync(Branch branch, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Branch branch, CancellationToken cancellationToken = default);
    }

    public interface IUnitOfWork : IDisposable
    {
        ICustomerRepository Customers { get; }
        IVehicleRepository Vehicles { get; }
        IRepairOrderRepository RepairOrders { get; }
        IInvoiceRepository Invoices { get; }
        ISparePartRepository SpareParts { get; }
        IServiceRepository Services { get; }
        ITechnicianRepository Technicians { get; }
        ISupplierRepository Suppliers { get; }
        IAuditRepository Audit { get; }
        INotificationRepository Notifications { get; }
        IDashboardRepository Dashboard { get; }
        IAppointmentRepository Appointments { get; }
        IBranchRepository Branches { get; }

        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }

    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create();
    }
}

