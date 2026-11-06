using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using AutoCare.Application.Interfaces;
using AutoCare.Infrastructure.Repositories;

namespace AutoCare.Infrastructure.Persistence
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IDbConnection CreateConnection()
        {
            var conn = new SqlConnection(_connectionString);
            return conn;
        }

        public SqlConnection CreateSqlConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly SqlConnection _connection;
        private SqlTransaction _transaction;
        private bool _disposed;

        public ICustomerRepository Customers { get; }
        public IVehicleRepository Vehicles { get; }
        public IRepairOrderRepository RepairOrders { get; }
        public IInvoiceRepository Invoices { get; }
        public ISparePartRepository SpareParts { get; }
        public IServiceRepository Services { get; }
        public ITechnicianRepository Technicians { get; }
        public ISupplierRepository Suppliers { get; }
        public IAuditRepository Audit { get; }
        public INotificationRepository Notifications { get; }
        public IDashboardRepository Dashboard { get; }
        public IAppointmentRepository Appointments { get; }
        public IBranchRepository Branches { get; }

        public UnitOfWork(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();

            Customers = new CustomerRepository(_connection, () => _transaction);
            Vehicles = new VehicleRepository(_connection, () => _transaction);
            RepairOrders = new RepairOrderRepository(_connection, () => _transaction);
            Invoices = new InvoiceRepository(_connection, () => _transaction);
            SpareParts = new SparePartRepository(_connection, () => _transaction);
            Services = new ServiceRepository(_connection, () => _transaction);
            Technicians = new TechnicianRepository(_connection, () => _transaction);
            Suppliers = new SupplierRepository(_connection, () => _transaction);
            Audit = new AuditRepository(_connection, () => _transaction);
            Notifications = new NotificationRepository(_connection, () => _transaction);
            Dashboard = new DashboardRepository(_connection, () => _transaction);
            Appointments = new AppointmentRepository(_connection, () => _transaction);
            Branches = new BranchRepository(_connection, () => _transaction);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                _transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
            }
            return Task.FromResult(0);
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
            return Task.FromResult(0);
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }

    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly string _connectionString;

        public UnitOfWorkFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IUnitOfWork Create()
        {
            return new UnitOfWork(_connectionString);
        }
    }
}
