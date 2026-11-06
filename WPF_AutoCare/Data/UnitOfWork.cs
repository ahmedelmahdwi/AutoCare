using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    /// <summary>
    /// Thread-safe and scoped Unit of Work implementation.
    /// Encapsulates SqlConnection and SqlTransaction, injecting scoped IDbHelper into all domain repositories.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbHelper _rootDbHelper;
        private readonly IDbHelper _scopedDbHelper;
        private SqlConnection _connection;
        private SqlTransaction _transaction;
        private bool _disposed;

        public SqlConnection Connection => _connection;
        public SqlTransaction Transaction => _transaction;
        public bool IsInTransaction => _transaction != null;

        public ICustomerRepository Customers { get; }
        public IVehicleRepository Vehicles { get; }
        public IRepairOrderRepository RepairOrders { get; }
        public IServiceRepository Services { get; }
        public ISparePartRepository SpareParts { get; }
        public IInvoiceRepository Invoices { get; }
        public ITechnicianRepository Technicians { get; }
        public ITechnicianSettlementRepository TechnicianSettlements { get; }
        public IRepairOrderServiceRepository RepairOrderServices { get; }
        public IRepairOrderSparePartRepository RepairOrderSpareParts { get; }
        public ISupplierRepository Suppliers { get; }
        public IAuditRepository Audit { get; }

        public UnitOfWork(IDbHelper rootDbHelper)
        {
            _rootDbHelper = rootDbHelper ?? throw new ArgumentNullException(nameof(rootDbHelper));
            _scopedDbHelper = new ScopedUnitOfWorkDbHelper(this, _rootDbHelper);

            Customers = new CustomerRepository(_scopedDbHelper);
            Vehicles = new VehicleRepository(_scopedDbHelper);
            RepairOrders = new RepairOrderRepository(_scopedDbHelper);
            Services = new ServiceRepository(_scopedDbHelper);
            SpareParts = new SparePartRepository(_scopedDbHelper);
            Invoices = new InvoiceRepository(_scopedDbHelper);
            Technicians = new TechnicianRepository(_scopedDbHelper);
            TechnicianSettlements = new TechnicianSettlementRepository(_scopedDbHelper);
            RepairOrderServices = new RepairOrderServiceRepository(_scopedDbHelper);
            RepairOrderSpareParts = new RepairOrderSparePartRepository(_scopedDbHelper);
            Suppliers = new SupplierRepository(_scopedDbHelper);
            Audit = new AuditRepository(_scopedDbHelper);
        }

        public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            EnsureNotDisposed();

            if (IsInTransaction)
            {
                throw new InvalidOperationException("توجد معاملة نشطة بالفعل في وحدة العمل هذه.");
            }

            if (_connection == null)
            {
                _connection = new SqlConnection(_rootDbHelper.GetConnectionString());
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            _transaction = _connection.BeginTransaction(isolationLevel);
        }

        public void Commit()
        {
            EnsureNotDisposed();

            if (!IsInTransaction)
            {
                throw new InvalidOperationException("لا توجد معاملة نشطة لإتمامها (Commit).");
            }

            try
            {
                _transaction.Commit();
            }
            finally
            {
                CleanupTransaction();
            }
        }

        public void Rollback()
        {
            if (_disposed || !IsInTransaction) return;

            try
            {
                _transaction.Rollback();
            }
            catch (Exception ex)
            {
                AppLogger.Error("UnitOfWork.Rollback failed", ex);
            }
            finally
            {
                CleanupTransaction();
            }
        }

        public void ExecuteInTransaction(Action<IUnitOfWork> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            BeginTransaction(isolationLevel);
            try
            {
                action(this);
                Commit();
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        public T ExecuteInTransaction<T>(Func<IUnitOfWork, T> func, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            BeginTransaction(isolationLevel);
            try
            {
                T result = func(this);
                Commit();
                return result;
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        private void CleanupTransaction()
        {
            if (_transaction != null)
            {
                try { _transaction.Dispose(); } catch { }
                _transaction = null;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWork), "تم التخلص من وحدة العمل (Unit of Work) مسبقاً.");
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (IsInTransaction)
                {
                    Rollback();
                }
            }
            catch { }

            CleanupTransaction();

            if (_connection != null)
            {
                try
                {
                    if (_connection.State == ConnectionState.Open)
                        _connection.Close();
                    _connection.Dispose();
                }
                catch { }
                _connection = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Internal scoped DbHelper wrapper that redirects queries to UnitOfWork's active connection and transaction.
        /// </summary>
        private class ScopedUnitOfWorkDbHelper : IDbHelper
        {
            private readonly UnitOfWork _uow;
            private readonly IDbHelper _root;

            public ScopedUnitOfWorkDbHelper(UnitOfWork uow, IDbHelper root)
            {
                _uow = uow;
                _root = root;
            }

            public string CurrentServer => _root.CurrentServer;
            public string GetConnectionString() => _root.GetConnectionString();
            public string GetDatabaseName() => _root.GetDatabaseName();
            public string GetMasterConnectionString() => _root.GetMasterConnectionString();
            public bool TestConnection(out string errorMessage) => _root.TestConnection(out errorMessage);

            public int ExecuteNonQuery(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null)
            {
                if (conn != null)
                    return _root.ExecuteNonQuery(query, parameters, conn, tran);

                if (_uow.IsInTransaction && _uow.Connection != null)
                    return _root.ExecuteNonQuery(query, parameters, _uow.Connection, _uow.Transaction);

                return _root.ExecuteNonQuery(query, parameters, null, null);
            }

            public DataTable ExecuteDataTable(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null)
            {
                if (conn != null)
                    return _root.ExecuteDataTable(query, parameters, conn, tran);

                if (_uow.IsInTransaction && _uow.Connection != null)
                    return _root.ExecuteDataTable(query, parameters, _uow.Connection, _uow.Transaction);

                return _root.ExecuteDataTable(query, parameters, null, null);
            }

            public T ExecuteScalar<T>(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null)
            {
                if (conn != null)
                    return _root.ExecuteScalar<T>(query, parameters, conn, tran);

                if (_uow.IsInTransaction && _uow.Connection != null)
                    return _root.ExecuteScalar<T>(query, parameters, _uow.Connection, _uow.Transaction);

                return _root.ExecuteScalar<T>(query, parameters, null, null);
            }

            public List<T> ExecuteList<T>(string query, SqlParameter[] parameters, Func<SqlDataReader, T> mapper, SqlConnection conn = null, SqlTransaction tran = null)
            {
                if (conn != null)
                    return _root.ExecuteList<T>(query, parameters, mapper, conn, tran);

                if (_uow.IsInTransaction && _uow.Connection != null)
                    return _root.ExecuteList<T>(query, parameters, mapper, _uow.Connection, _uow.Transaction);

                return _root.ExecuteList<T>(query, parameters, mapper, null, null);
            }

            public T ExecuteSingle<T>(string query, SqlParameter[] parameters, Func<SqlDataReader, T> mapper, SqlConnection conn = null, SqlTransaction tran = null)
            {
                if (conn != null)
                    return _root.ExecuteSingle<T>(query, parameters, mapper, conn, tran);

                if (_uow.IsInTransaction && _uow.Connection != null)
                    return _root.ExecuteSingle<T>(query, parameters, mapper, _uow.Connection, _uow.Transaction);

                return _root.ExecuteSingle<T>(query, parameters, mapper, null, null);
            }

            public void ExecuteTransaction(Action<SqlConnection, SqlTransaction> action)
            {
                if (_uow.IsInTransaction && _uow.Connection != null)
                {
                    action(_uow.Connection, _uow.Transaction);
                }
                else
                {
                    _root.ExecuteTransaction(action);
                }
            }

            public T ExecuteTransaction<T>(Func<SqlConnection, SqlTransaction, T> func)
            {
                if (_uow.IsInTransaction && _uow.Connection != null)
                {
                    return func(_uow.Connection, _uow.Transaction);
                }
                else
                {
                    return _root.ExecuteTransaction(func);
                }
            }
        }
    }

    /// <summary>
    /// Default factory for creating Unit of Work instances.
    /// </summary>
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IDbHelper _rootDbHelper;

        public UnitOfWorkFactory(IDbHelper rootDbHelper)
        {
            _rootDbHelper = rootDbHelper ?? throw new ArgumentNullException(nameof(rootDbHelper));
        }

        public IUnitOfWork Create()
        {
            return new UnitOfWork(_rootDbHelper);
        }
    }
}
