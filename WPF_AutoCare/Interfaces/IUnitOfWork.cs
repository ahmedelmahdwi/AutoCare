using System;
using System.Data;
using System.Data.SqlClient;

namespace C__project_Auto_Care.Interfaces
{
    /// <summary>
    /// Unit of Work pattern contract for managing atomic transactions and repository lifecycles.
    /// Strictly guarantees ACID consistency across related multi-repository operations.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        SqlConnection Connection { get; }
        SqlTransaction Transaction { get; }
        bool IsInTransaction { get; }

        ICustomerRepository Customers { get; }
        IVehicleRepository Vehicles { get; }
        IRepairOrderRepository RepairOrders { get; }
        IServiceRepository Services { get; }
        ISparePartRepository SpareParts { get; }
        IInvoiceRepository Invoices { get; }
        ITechnicianRepository Technicians { get; }
        ITechnicianSettlementRepository TechnicianSettlements { get; }
        IRepairOrderServiceRepository RepairOrderServices { get; }
        IRepairOrderSparePartRepository RepairOrderSpareParts { get; }
        ISupplierRepository Suppliers { get; }
        IAuditRepository Audit { get; }

        void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        void Commit();
        void Rollback();
        void ExecuteInTransaction(Action<IUnitOfWork> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        T ExecuteInTransaction<T>(Func<IUnitOfWork, T> func, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    }

    /// <summary>
    /// Factory for creating isolated Unit of Work instances.
    /// </summary>
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create();
    }
}
