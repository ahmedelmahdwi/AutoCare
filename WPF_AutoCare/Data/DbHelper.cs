using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class DbHelper : IDbHelper
    {
        private string ConnectionString;

        public DbHelper(string customConnectionString)
        {
            if (!string.IsNullOrWhiteSpace(customConnectionString))
            {
                ConnectionString = customConnectionString;
            }
            else
            {
                InitConnection();
            }
        }

        public DbHelper()
        {
            InitConnection();
        }

        private void InitConnection()
        {
            string preferredServer = null;
            try
            {
                var connSetting = System.Configuration.ConfigurationManager.ConnectionStrings["C__project_Auto_Care.Properties.Settings.AutoCareConnectionString"]
                               ?? System.Configuration.ConfigurationManager.ConnectionStrings["AutoCareConnectionString"];
                if (connSetting != null && !string.IsNullOrWhiteSpace(connSetting.ConnectionString))
                {
                    var csb = new SqlConnectionStringBuilder(connSetting.ConnectionString);
                    preferredServer = csb.DataSource;

                    // Test if the configured connection string works immediately
                    using (SqlConnection conn = new SqlConnection(connSetting.ConnectionString))
                    {
                        conn.Open();
                        ConnectionString = connSetting.ConnectionString;
                        EnsureSchemaUpgrades();
                        return;
                    }
                }
            }
            catch
            {
            }

            // If direct configured connection fails or database does not exist, auto-discover & initialize
            if (DatabaseInitializer.TryFindAndInitializeServer(preferredServer, out string workingServer, out _))
            {
                ConnectionString = DatabaseInitializer.BuildConnectionString(workingServer, DatabaseInitializer.DatabaseName);
                EnsureSchemaUpgrades();
                return;
            }

            // Fallback default
            ConnectionString = DatabaseInitializer.BuildConnectionString(preferredServer ?? ".", DatabaseInitializer.DatabaseName);
            EnsureSchemaUpgrades();
        }

        public void EnsureSchemaUpgrades()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConnectionString)) return;
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string script = @"
                        IF COL_LENGTH('RepairOrders', 'Mileage') IS NULL
                            ALTER TABLE RepairOrders ADD Mileage INT NULL;

                        IF COL_LENGTH('RepairOrders', 'FuelLevel') IS NULL
                            ALTER TABLE RepairOrders ADD FuelLevel NVARCHAR(30) NULL;

                        IF COL_LENGTH('RepairOrders', 'InspectionNotes') IS NULL
                            ALTER TABLE RepairOrders ADD InspectionNotes NVARCHAR(500) NULL;

                        IF COL_LENGTH('Invoices', 'DiscountAmount') IS NULL
                            ALTER TABLE Invoices ADD DiscountAmount DECIMAL(10,2) NOT NULL CONSTRAINT DF_Invoices_Discount DEFAULT 0;

                        IF COL_LENGTH('Invoices', 'TaxRate') IS NULL
                            ALTER TABLE Invoices ADD TaxRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_Invoices_TaxRate DEFAULT 0;

                        IF COL_LENGTH('Invoices', 'TaxAmount') IS NULL
                            ALTER TABLE Invoices ADD TaxAmount DECIMAL(10,2) NOT NULL CONSTRAINT DF_Invoices_TaxAmount DEFAULT 0;

                        IF COL_LENGTH('Services', 'WarrantyDays') IS NULL
                            ALTER TABLE Services ADD WarrantyDays INT NOT NULL CONSTRAINT DF_Services_WarrantyDays DEFAULT 0;

                        IF COL_LENGTH('SpareParts', 'WarrantyDays') IS NULL
                            ALTER TABLE SpareParts ADD WarrantyDays INT NOT NULL CONSTRAINT DF_SpareParts_WarrantyDays DEFAULT 0;

                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Suppliers')
                        BEGIN
                            CREATE TABLE dbo.Suppliers
                            (
                                SupplierID    INT IDENTITY(1,1) NOT NULL,
                                SupplierName  NVARCHAR(150)     NOT NULL,
                                ContactPerson NVARCHAR(100)     NULL,
                                PhoneNumber   NVARCHAR(30)      NULL,
                                Address       NVARCHAR(200)     NULL,
                                Balance       DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Suppliers_Balance DEFAULT 0,
                                CreatedAt     DATETIME          NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT GETDATE(),
                                CONSTRAINT PK_Suppliers PRIMARY KEY CLUSTERED (SupplierID ASC)
                            );
                            CREATE NONCLUSTERED INDEX IX_Suppliers_Name ON dbo.Suppliers (SupplierName ASC);
                        END;

                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseInvoices')
                        BEGIN
                            CREATE TABLE dbo.PurchaseInvoices
                            (
                                PurchaseInvoiceNumber NVARCHAR(30)  NOT NULL,
                                SupplierID            INT           NOT NULL,
                                InvoiceDate           DATETIME      NOT NULL CONSTRAINT DF_PI_InvoiceDate DEFAULT GETDATE(),
                                TotalAmount           DECIMAL(12,2) NOT NULL CONSTRAINT DF_PI_TotalAmount DEFAULT 0,
                                Notes                 NVARCHAR(500) NULL,
                                CreatedBy             NVARCHAR(50)  NULL,
                                CONSTRAINT PK_PurchaseInvoices PRIMARY KEY CLUSTERED (PurchaseInvoiceNumber ASC),
                                CONSTRAINT FK_PI_Suppliers FOREIGN KEY (SupplierID) REFERENCES dbo.Suppliers(SupplierID)
                            );
                            CREATE NONCLUSTERED INDEX IX_PI_SupplierID ON dbo.PurchaseInvoices (SupplierID ASC);
                            CREATE NONCLUSTERED INDEX IX_PI_InvoiceDate ON dbo.PurchaseInvoices (InvoiceDate DESC);
                        END;

                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseInvoiceDetails')
                        BEGIN
                            CREATE TABLE dbo.PurchaseInvoiceDetails
                            (
                                DetailID              INT IDENTITY(1,1) NOT NULL,
                                PurchaseInvoiceNumber NVARCHAR(30)      NOT NULL,
                                PartCode              NVARCHAR(30)      NOT NULL,
                                Quantity              INT               NOT NULL,
                                UnitCost              DECIMAL(10,2)     NOT NULL CONSTRAINT DF_PID_UnitCost DEFAULT 0,
                                TotalCost             DECIMAL(12,2)     NOT NULL CONSTRAINT DF_PID_TotalCost DEFAULT 0,
                                CONSTRAINT PK_PurchaseInvoiceDetails PRIMARY KEY CLUSTERED (DetailID ASC),
                                CONSTRAINT FK_PID_PI FOREIGN KEY (PurchaseInvoiceNumber) REFERENCES dbo.PurchaseInvoices(PurchaseInvoiceNumber) ON DELETE CASCADE,
                                CONSTRAINT FK_PID_SP FOREIGN KEY (PartCode) REFERENCES dbo.SpareParts(PartCode)
                            );
                            CREATE NONCLUSTERED INDEX IX_PID_PI ON dbo.PurchaseInvoiceDetails (PurchaseInvoiceNumber ASC);
                        END;

                        IF COL_LENGTH('RepairOrders', 'RowVersion') IS NULL
                            ALTER TABLE RepairOrders ADD RowVersion ROWVERSION;

                        IF COL_LENGTH('SpareParts', 'RowVersion') IS NULL
                            ALTER TABLE SpareParts ADD RowVersion ROWVERSION;

                        IF COL_LENGTH('Invoices', 'RowVersion') IS NULL
                            ALTER TABLE Invoices ADD RowVersion ROWVERSION;

                        IF COL_LENGTH('Customers', 'IsDeleted') IS NULL
                            ALTER TABLE Customers ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT 0;

                        IF COL_LENGTH('Vehicles', 'IsDeleted') IS NULL
                            ALTER TABLE Vehicles ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Vehicles_IsDeleted DEFAULT 0;

                        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RepairOrders_ActiveOrders' AND object_id = OBJECT_ID('RepairOrders'))
                        BEGIN
                            CREATE NONCLUSTERED INDEX IX_RepairOrders_ActiveOrders
                            ON RepairOrders (Status ASC, EntryDate DESC)
                            INCLUDE (RepairOrderNumber, PlateNumber, ReceptionistUsername, Mileage, Complaint)
                            WHERE Status IN ('Pending', 'InProgress');
                        END;

                        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notifications')
                        BEGIN
                            CREATE TABLE dbo.Notifications
                            (
                                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                Title NVARCHAR(150) NOT NULL,
                                Message NVARCHAR(500) NOT NULL,
                                Severity NVARCHAR(20) NOT NULL CONSTRAINT DF_Notifications_Severity DEFAULT 'Info',
                                Category NVARCHAR(50) NOT NULL CONSTRAINT DF_Notifications_Category DEFAULT 'General',
                                TargetReferenceId NVARCHAR(100) NULL,
                                IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
                                CreatedAt DATETIME NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT GETDATE(),
                                BranchId INT NULL
                            );
                            CREATE NONCLUSTERED INDEX IX_Notifications_IsRead ON dbo.Notifications (IsRead ASC, CreatedAt DESC);
                        END;";
                    using (var cmd = new SqlCommand(script, conn))
                    {
                        cmd.CommandTimeout = 10;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Silently skip if DB is temporarily unreachable during server probing
            }
        }

        public void UpdateConnectionString(string newConnStr)
        {
            if (!string.IsNullOrWhiteSpace(newConnStr))
            {
                ConnectionString = newConnStr;
            }
        }

        public string CurrentServer
        {
            get
            {
                try
                {
                    return new SqlConnectionStringBuilder(ConnectionString).DataSource;
                }
                catch
                {
                    return ".";
                }
            }
        }

        public string GetConnectionString() => ConnectionString;

        public string GetDatabaseName()
        {
            try
            {
                SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(ConnectionString);
                return string.IsNullOrEmpty(b.InitialCatalog) ? "AutoCare" : b.InitialCatalog;
            }
            catch
            {
                return "AutoCare";
            }
        }

        public string GetMasterConnectionString()
        {
            try
            {
                SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(ConnectionString);
                b.InitialCatalog = "master";
                return b.ConnectionString;
            }
            catch
            {
                return "Server=.;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";
            }
        }

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = "";
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public void ExecuteTransaction(Action<SqlConnection, SqlTransaction> action)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        action(conn, tran);
                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Database ExecuteTransaction Error: " + ex.ToString());
                        try { tran.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        public T ExecuteTransaction<T>(Func<SqlConnection, SqlTransaction, T> func)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        T result = func(conn, tran);
                        tran.Commit();
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Database ExecuteTransaction<T> Error: " + ex.ToString());
                        try { tran.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        public int ExecuteNonQuery(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null)
        {
            try
            {
                if (conn != null)
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn, tran))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        return cmd.ExecuteNonQuery();
                    }
                }

                using (SqlConnection localConn = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, localConn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        localConn.Open();
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Database ExecuteNonQuery Error: " + ex.ToString());
                throw;
            }
        }

        public int ExecuteNonQuery(string query, SqlParameter[] parameters)
        {
            return ExecuteNonQuery(query, parameters, null, null);
        }

        public DataTable ExecuteDataTable(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null)
        {
            try
            {
                if (conn != null)
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn, tran))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            return dt;
                        }
                    }
                }

                using (SqlConnection localConn = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, localConn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Database ExecuteDataTable Error: " + ex.ToString());
                throw;
            }
        }

        public DataTable ExecuteDataTable(string query, SqlParameter[] parameters)
        {
            return ExecuteDataTable(query, parameters, null, null);
        }

        public T ExecuteScalar<T>(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null)
        {
            try
            {
                if (conn != null)
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn, tran))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                            return default(T);
                        return (T)Convert.ChangeType(result, typeof(T));
                    }
                }

                using (SqlConnection localConn = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, localConn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        localConn.Open();
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                            return default(T);
                        return (T)Convert.ChangeType(result, typeof(T));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Database ExecuteScalar Error: " + ex.ToString());
                throw;
            }
        }

        public T ExecuteScalar<T>(string query, SqlParameter[] parameters)
        {
            return ExecuteScalar<T>(query, parameters, null, null);
        }

        public List<T> ExecuteList<T>(string query, SqlParameter[] parameters, Func<SqlDataReader, T> mapper, SqlConnection conn = null, SqlTransaction tran = null)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            var list = new List<T>();

            try
            {
                if (conn != null)
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn, tran))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(mapper(reader));
                            }
                        }
                    }
                    return list;
                }

                using (SqlConnection localConn = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, localConn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        localConn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(mapper(reader));
                            }
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Database ExecuteList Error: " + ex.ToString());
                throw;
            }
        }

        public T ExecuteSingle<T>(string query, SqlParameter[] parameters, Func<SqlDataReader, T> mapper, SqlConnection conn = null, SqlTransaction tran = null)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            try
            {
                if (conn != null)
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn, tran))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return mapper(reader);
                            }
                        }
                    }
                    return default(T);
                }

                using (SqlConnection localConn = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, localConn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);
                        localConn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return mapper(reader);
                            }
                        }
                    }
                }
                return default(T);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Database ExecuteSingle Error: " + ex.ToString());
                throw;
            }
        }
    }
}
