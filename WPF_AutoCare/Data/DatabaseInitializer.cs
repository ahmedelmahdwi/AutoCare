using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace C__project_Auto_Care.Data
{
    public static class DatabaseInitializer
    {
        public const string DatabaseName = "AutoCare";

        /// <summary>
        /// Probes candidate servers and initializes the database if needed.
        /// </summary>
        public static bool TryFindAndInitializeServer(string preferredServer, out string workingServer, out string errorMessage, Action<string> onProgress = null)
        {
            workingServer = null;
            errorMessage = "";

            var candidates = GetCandidateServers(preferredServer);
            List<string> attemptErrors = new List<string>();

            // Pass 1: Check if AutoCare is already active on any server
            foreach (var server in candidates)
            {
                onProgress?.Invoke($"جارٍ التحقق من السيرفر: {server} ...");
                string connStr = BuildConnectionString(server, DatabaseName);
                try
                {
                    using (var conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn))
                        {
                            cmd.CommandTimeout = 4;
                            cmd.ExecuteScalar();
                            workingServer = server;
                            onProgress?.Invoke($"تم الاتصال بنجاح مع السيرفر: {server}");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    attemptErrors.Add($"{server} (AutoCare): {ex.Message}");
                }
            }

            // Pass 2: Check if master is reachable and AutoCare needs creation
            foreach (var server in candidates)
            {
                onProgress?.Invoke($"فحص المحرك لتهيئة القاعدة على: {server} ...");
                string masterStr = BuildConnectionString(server, "master");
                bool canConnectMaster = false;
                try
                {
                    using (var conn = new SqlConnection(masterStr))
                    {
                        conn.Open();
                        canConnectMaster = true;
                    }
                }
                catch (Exception ex)
                {
                    attemptErrors.Add($"{server} (master): {ex.Message}");
                }

                if (canConnectMaster)
                {
                    onProgress?.Invoke($"تم العثور على محرك SQL ({server}). جارٍ تهيئة قاعدة البيانات تلقائياً...");
                    if (EnsureDatabaseReady(server, out string initErr, onProgress))
                    {
                        workingServer = server;
                        onProgress?.Invoke($"تمت تهيئة قاعدة البيانات بنجاح على: {server}");
                        return true;
                    }
                    else
                    {
                        attemptErrors.Add($"{server} (init): {initErr}");
                    }
                }
            }

            errorMessage = "تعذر الاتصال بأي خادم SQL Server محلي تلقائياً.\n" + string.Join("\n", attemptErrors);
            return false;
        }

        public static List<string> GetCandidateServers(string preferredServer = null)
        {
            var list = new List<string>();

            if (!string.IsNullOrWhiteSpace(preferredServer))
            {
                list.Add(preferredServer.Trim());
            }

            string machine = Environment.MachineName;
            string[] standards = {
                ".",
                @".\SQLEXPRESS",
                "localhost",
                @"localhost\SQLEXPRESS",
                "(local)",
                machine,
                $@"{machine}\SQLEXPRESS",
                @"(localdb)\MSSQLLocalDB",
                @"(localdb)\v11.0"
            };

            foreach (var s in standards)
            {
                if (!string.IsNullOrWhiteSpace(s) && !list.Contains(s, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(s);
                }
            }

            return list;
        }

        public static string BuildConnectionString(string server, string database, bool integratedSecurity = true, string user = null, string password = null, int timeoutSeconds = 3)
        {
            var b = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = integratedSecurity,
                ConnectTimeout = timeoutSeconds,
                Encrypt = true,
                TrustServerCertificate = true
            };

            if (!integratedSecurity && !string.IsNullOrEmpty(user))
            {
                b.UserID = user;
                b.Password = password ?? "";
            }

            return b.ConnectionString;
        }

        public static bool EnsureDatabaseReady(string server, out string errorMessage, Action<string> onProgress = null)
        {
            errorMessage = "";
            try
            {
                string masterStr = BuildConnectionString(server, "master", timeoutSeconds: 10);
                bool dbExists = false;

                using (var conn = new SqlConnection(masterStr))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand($"SELECT 1 FROM sys.databases WHERE name = N'{DatabaseName}'", conn))
                    {
                        var result = cmd.ExecuteScalar();
                        dbExists = (result != null && result != DBNull.Value);
                    }

                    if (!dbExists)
                    {
                        onProgress?.Invoke($"إنشاء قاعدة البيانات [{DatabaseName}]...");
                        using (var createCmd = new SqlCommand($"CREATE DATABASE [{DatabaseName}] COLLATE Arabic_CI_AS;", conn))
                        {
                            createCmd.CommandTimeout = 60;
                            createCmd.ExecuteNonQuery();
                        }
                    }
                }

                // Connect to AutoCare and verify or create schema
                string dbStr = BuildConnectionString(server, DatabaseName, timeoutSeconds: 15);
                using (var conn = new SqlConnection(dbStr))
                {
                    conn.Open();

                    bool tablesExist = false;
                    using (var cmd = new SqlCommand("SELECT 1 FROM sys.tables WHERE name = N'Users'", conn))
                    {
                        var res = cmd.ExecuteScalar();
                        tablesExist = (res != null && res != DBNull.Value);
                    }

                    if (!tablesExist)
                    {
                        onProgress?.Invoke("إنشاء الجداول والمخطط والمستخدم الافتراضي...");
                        ExecuteSchemaScript(conn, onProgress);
                    }
                    else
                    {
                        // Ensure default admin and seed data exist if Users table is empty
                        EnsureDefaultAdminExists(conn, onProgress);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static void ExecuteSchemaScript(SqlConnection conn, Action<string> onProgress)
        {
            // 1. Execute Schema Setup (DDL)
            string schemaScript = LoadScriptByName("01_AutoCare_SCHEMA_SETUP.sql");
            if (!string.IsNullOrWhiteSpace(schemaScript))
            {
                onProgress?.Invoke("إنشاء الجداول والفهارس والعروض (01_AutoCare_SCHEMA_SETUP.sql)...");
                ExecuteBatches(conn, schemaScript);
            }
            else
            {
                ExecuteBatches(conn, GetFallbackSchemaScript());
            }

            // 2. Execute Seed Data (DML)
            string seedScript = LoadScriptByName("02_AutoCare_SEED_DATA.sql");
            if (!string.IsNullOrWhiteSpace(seedScript))
            {
                onProgress?.Invoke("تغذية البيانات الأولية وحسابات الدخول (02_AutoCare_SEED_DATA.sql)...");
                ExecuteBatches(conn, seedScript);
            }
        }

        private static void EnsureDefaultAdminExists(SqlConnection conn, Action<string> onProgress)
        {
            try
            {
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn))
                {
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count == 0)
                    {
                        string seedScript = LoadScriptByName("02_AutoCare_SEED_DATA.sql");
                        if (!string.IsNullOrWhiteSpace(seedScript))
                        {
                            onProgress?.Invoke("تغذية الحسابات الافتراضية...");
                            ExecuteBatches(conn, seedScript);
                        }
                    }
                }
            }
            catch { }
        }

        private static void ExecuteBatches(SqlConnection conn, string scriptText)
        {
            if (string.IsNullOrWhiteSpace(scriptText)) return;

            string[] batches = Regex.Split(scriptText, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var rawBatch in batches)
            {
                string batch = rawBatch.Trim();
                if (string.IsNullOrWhiteSpace(batch)) continue;

                if (Regex.IsMatch(batch, @"^\s*USE\s+", RegexOptions.IgnoreCase))
                {
                    string stripped = Regex.Replace(batch, @"^\s*USE\s+\[?\w+\]?\s*;?", "", RegexOptions.IgnoreCase).Trim();
                    if (string.IsNullOrWhiteSpace(stripped)) continue;
                    batch = stripped;
                }

                if (batch.IndexOf("DROP DATABASE", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (batch.IndexOf("CREATE DATABASE", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                try
                {
                    using (var cmd = new SqlCommand(batch, conn))
                    {
                        cmd.CommandTimeout = 120;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    if (batch.IndexOf("FULLTEXT", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    throw new Exception($"خطأ أثناء تنفيذ الدفعة:\n{batch.Substring(0, Math.Min(150, batch.Length))}...\n\nالتفاصيل: {ex.Message}", ex);
                }
            }
        }

        private static string LoadScriptByName(string fileName)
        {
            string[] candidateDirs = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQL_Scripts"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "SQL_Scripts"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "AutoCare_Distribution", "SQL_Scripts"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "WPF_AutoCare", "SQL_Scripts")
            };

            foreach (var dir in candidateDirs)
            {
                string fullPath = Path.Combine(dir, fileName);
                if (File.Exists(fullPath))
                {
                    return File.ReadAllText(fullPath);
                }
            }

            return null;
        }

        public static string GetFallbackSchemaScript()
        {
            return @"
CREATE TABLE Users
(
    Username     NVARCHAR(50)   NOT NULL,
    PasswordHash NVARCHAR(256)  NOT NULL,
    FullName     NVARCHAR(150)  NOT NULL,
    Role         NVARCHAR(30)   NOT NULL,
    IsActive     BIT            NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Username ASC),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'Receptionist', 'Accountant', 'Manager', 'Technician'))
);
GO

CREATE TABLE Customers
(
    PhoneNumber  NVARCHAR(30)   NOT NULL,
    FullName     NVARCHAR(150)  NOT NULL,
    CreatedAt    DATETIME       NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (PhoneNumber ASC)
);
GO

CREATE TABLE Vehicles
(
    PlateNumber   NVARCHAR(30)   NOT NULL,
    VIN           NVARCHAR(50)   NULL,
    CustomerPhone NVARCHAR(30)   NOT NULL,
    CarName       NVARCHAR(100)  NOT NULL,
    CarModel      NVARCHAR(10)   NULL,
    Notes         NVARCHAR(500)  NULL,
    CreatedAt     DATETIME       NOT NULL CONSTRAINT DF_Vehicles_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Vehicles PRIMARY KEY CLUSTERED (PlateNumber ASC),
    CONSTRAINT FK_Vehicles_Customers FOREIGN KEY (CustomerPhone) REFERENCES Customers (PhoneNumber),
    CONSTRAINT UQ_Vehicles_VIN UNIQUE (VIN)
);
GO

CREATE TABLE Technicians
(
    TechnicianID      INT            NOT NULL,
    FullName          NVARCHAR(150)  NOT NULL,
    Specialization    NVARCHAR(100)  NULL,
    CommissionPercent DECIMAL(5,2)   NOT NULL CONSTRAINT DF_Tech_Commission DEFAULT 0,
    IsActive          BIT            NOT NULL CONSTRAINT DF_Tech_IsActive DEFAULT 1,
    CreatedAt         DATETIME       NOT NULL CONSTRAINT DF_Technicians_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Technicians PRIMARY KEY CLUSTERED (TechnicianID ASC),
    CONSTRAINT CK_Technicians_Commission CHECK (CommissionPercent >= 0 AND CommissionPercent <= 100)
);
GO

CREATE TABLE Services
(
    ServiceCode    NVARCHAR(30)    NOT NULL,
    ServiceName    NVARCHAR(200)   NOT NULL,
    StandardPrice  DECIMAL(10,2)   NOT NULL CONSTRAINT DF_Services_Price DEFAULT 0,
    Category       NVARCHAR(100)   NULL,
    CreatedAt      DATETIME        NOT NULL CONSTRAINT DF_Services_CreatedAt DEFAULT GETDATE(),
    UpdatedAt      DATETIME        NULL,
    CONSTRAINT PK_Services PRIMARY KEY CLUSTERED (ServiceCode ASC),
    CONSTRAINT CK_Services_Price CHECK (StandardPrice >= 0)
);
GO

CREATE TABLE SpareParts
(
    PartCode        NVARCHAR(30)    NOT NULL,
    PartName        NVARCHAR(200)   NOT NULL,
    QuantityInStock INT             NOT NULL CONSTRAINT DF_SP_Qty DEFAULT 0,
    MinimumStock    INT             NOT NULL CONSTRAINT DF_SP_MinStock DEFAULT 0,
    BuyingPrice     DECIMAL(10,2)   NOT NULL CONSTRAINT DF_SP_Buying DEFAULT 0,
    SellingPrice    DECIMAL(10,2)   NOT NULL CONSTRAINT DF_SP_Selling DEFAULT 0,
    Location        NVARCHAR(100)   NULL,
    Category        NVARCHAR(100)   NULL,
    CreatedAt       DATETIME        NOT NULL CONSTRAINT DF_SpareParts_CreatedAt DEFAULT GETDATE(),
    UpdatedAt       DATETIME        NULL,
    CONSTRAINT PK_SpareParts PRIMARY KEY CLUSTERED (PartCode ASC),
    CONSTRAINT CK_SpareParts_Stock  CHECK (QuantityInStock >= 0 AND MinimumStock >= 0),
    CONSTRAINT CK_SpareParts_Prices CHECK (BuyingPrice >= 0 AND SellingPrice >= 0)
);
GO

CREATE TABLE RepairOrders
(
    RepairOrderNumber       NVARCHAR(20)   NOT NULL,
    PlateNumber             NVARCHAR(30)   NOT NULL,
    ReceptionistUsername    NVARCHAR(50)   NULL,
    EntryDate               DATETIME       NOT NULL CONSTRAINT DF_RO_EntryDate DEFAULT GETDATE(),
    ExpectedCompletionDate  DATETIME       NULL,
    Complaint               NVARCHAR(MAX)  NULL,
    Status                  NVARCHAR(20)   NOT NULL CONSTRAINT DF_RO_Status DEFAULT 'Pending',
    CompletionDate          DATETIME       NULL,
    CONSTRAINT PK_RepairOrders PRIMARY KEY CLUSTERED (RepairOrderNumber ASC),
    CONSTRAINT FK_RepairOrders_Vehicles FOREIGN KEY (PlateNumber) REFERENCES Vehicles (PlateNumber),
    CONSTRAINT FK_RepairOrders_Users FOREIGN KEY (ReceptionistUsername) REFERENCES Users (Username),
    CONSTRAINT CK_RepairOrders_Status CHECK (Status IN ('Pending', 'InProgress', 'Completed', 'Canceled'))
);
GO

CREATE TABLE Invoices
(
    InvoiceNumber       NVARCHAR(20)    NOT NULL,
    RepairOrderNumber   NVARCHAR(20)    NOT NULL,
    InvoiceDate         DATETIME        NOT NULL CONSTRAINT DF_Invoices_Date DEFAULT GETDATE(),
    PaymentMethod       NVARCHAR(30)    NULL CONSTRAINT DF_Invoices_Payment DEFAULT N'Cash',
    PaidAmount          DECIMAL(10,2)   NOT NULL CONSTRAINT DF_Invoices_PaidAmount DEFAULT 0,
    IsPaid              BIT             NOT NULL CONSTRAINT DF_Invoices_IsPaid DEFAULT 1,
    CreatedByUsername   NVARCHAR(50)    NULL,
    CONSTRAINT PK_Invoices PRIMARY KEY CLUSTERED (InvoiceNumber ASC),
    CONSTRAINT FK_Invoices_RepairOrders FOREIGN KEY (RepairOrderNumber) REFERENCES RepairOrders (RepairOrderNumber),
    CONSTRAINT FK_Invoices_Users FOREIGN KEY (CreatedByUsername) REFERENCES Users (Username),
    CONSTRAINT UQ_Invoices_RepairOrder UNIQUE (RepairOrderNumber),
    CONSTRAINT CK_Invoices_IsPaid CHECK (IsPaid IN (0, 1)),
    CONSTRAINT CK_Invoices_PaidAmount CHECK (PaidAmount >= 0)
);
GO

CREATE TABLE RepairOrderService
(
    RepairOrderNumber  NVARCHAR(20)    NOT NULL,
    ServiceCode        NVARCHAR(30)    NOT NULL,
    TechnicianID       INT             NULL,
    UnitPrice          DECIMAL(10,2)   NOT NULL CONSTRAINT DF_ROS_UnitPrice DEFAULT 0,
    CONSTRAINT PK_RepairOrderService PRIMARY KEY CLUSTERED (RepairOrderNumber ASC, ServiceCode ASC),
    CONSTRAINT FK_ROS_RepairOrders FOREIGN KEY (RepairOrderNumber) REFERENCES RepairOrders (RepairOrderNumber) ON DELETE CASCADE,
    CONSTRAINT FK_ROS_Services FOREIGN KEY (ServiceCode) REFERENCES Services (ServiceCode),
    CONSTRAINT FK_ROS_Technicians FOREIGN KEY (TechnicianID) REFERENCES Technicians (TechnicianID),
    CONSTRAINT CK_ROS_UnitPrice CHECK (UnitPrice >= 0)
);
GO

CREATE TABLE RepairOrderSparePart
(
    RepairOrderNumber  NVARCHAR(20)    NOT NULL,
    PartCode           NVARCHAR(30)    NOT NULL,
    Quantity           INT             NOT NULL CONSTRAINT DF_ROSP_Qty DEFAULT 1,
    UnitPrice          DECIMAL(10,2)   NOT NULL CONSTRAINT DF_ROSP_UnitPrice DEFAULT 0,
    CONSTRAINT PK_RepairOrderSparePart PRIMARY KEY CLUSTERED (RepairOrderNumber ASC, PartCode ASC),
    CONSTRAINT FK_ROSP_RepairOrders FOREIGN KEY (RepairOrderNumber) REFERENCES RepairOrders (RepairOrderNumber) ON DELETE CASCADE,
    CONSTRAINT FK_ROSP_SpareParts FOREIGN KEY (PartCode) REFERENCES SpareParts (PartCode),
    CONSTRAINT CK_ROSP_Quantity  CHECK (Quantity  > 0),
    CONSTRAINT CK_ROSP_UnitPrice CHECK (UnitPrice >= 0)
);
GO

CREATE TABLE TechnicianSettlement
(
    SettlementID      INT             NOT NULL IDENTITY(1,1),
    TechnicianID      INT             NOT NULL,
    RepairOrderNumber NVARCHAR(20)    NOT NULL,
    ServiceTotal      DECIMAL(10,2)   NOT NULL CONSTRAINT DF_TS_ServiceTotal DEFAULT 0,
    CommissionRate    DECIMAL(5,2)    NOT NULL CONSTRAINT DF_TS_Rate DEFAULT 0,
    CommissionAmount  DECIMAL(10,2)   NOT NULL CONSTRAINT DF_TS_Amount DEFAULT 0,
    SettledDate       DATETIME        NULL,
    Status            NVARCHAR(20)    NOT NULL CONSTRAINT DF_TS_Status DEFAULT 'Pending',
    CONSTRAINT PK_TechnicianSettlement PRIMARY KEY CLUSTERED (SettlementID ASC),
    CONSTRAINT FK_TS_Technicians FOREIGN KEY (TechnicianID) REFERENCES Technicians (TechnicianID),
    CONSTRAINT CK_TS_Status CHECK (Status IN ('Pending', 'Settled'))
);
GO

CREATE SEQUENCE dbo.seq_RepairOrderNumber AS INT START WITH 1 INCREMENT BY 1 NO MAXVALUE NO CYCLE CACHE 10;
GO

CREATE SEQUENCE dbo.seq_InvoiceNumber AS INT START WITH 1 INCREMENT BY 1 NO MAXVALUE NO CYCLE CACHE 10;
GO

CREATE TRIGGER TR_SpareParts_UpdatedAt ON SpareParts AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    UPDATE SpareParts SET UpdatedAt = GETDATE() FROM SpareParts sp INNER JOIN inserted i ON sp.PartCode = i.PartCode;
END;
GO

CREATE TRIGGER TR_Services_UpdatedAt ON Services AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Services SET UpdatedAt = GETDATE() FROM Services s INNER JOIN inserted i ON s.ServiceCode = i.ServiceCode;
END;
GO

-- Seed Admin
INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
VALUES (N'admin', N'Admin@1234', N'مدير النظام', N'Admin', 1);
GO

-- Seed Accountant
INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
VALUES (N'accountant', N'accountant123', N'محاسب المركز', N'Accountant', 1);
GO

-- Seed Services
INSERT INTO Services (ServiceCode, ServiceName, StandardPrice, Category) VALUES
(N'SVC-001', N'تغيير زيت المحرك', 150.00, N'صيانة دورية'),
(N'SVC-002', N'فحص وضبط الفرامل', 200.00, N'سلامة'),
(N'SVC-003', N'تغيير فلتر الهواء', 80.00, N'صيانة دورية'),
(N'SVC-004', N'فحص منظومة التكييف', 120.00, N'تكييف'),
(N'SVC-005', N'فحص البطارية والشحن', 60.00, N'كهرباء');
GO

-- Seed Spare Parts
INSERT INTO SpareParts (PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category) VALUES
(N'PRT-001', N'فلتر زيت تويوتا', 20, 5, 25.00, 45.00, N'رف A-1', N'فلاتر'),
(N'PRT-002', N'فلتر هواء هوندا', 15, 5, 30.00, 55.00, N'رف A-2', N'فلاتر'),
(N'PRT-003', N'زيت محرك 5W-30 (لتر)', 50, 10, 18.00, 30.00, N'رف B-1', N'زيوت'),
(N'PRT-004', N'بطارية 60 أمبير', 8, 2, 180.00, 280.00, N'رف C-1', N'كهرباء'),
(N'PRT-005', N'طقم تيل أمامي', 12, 3, 90.00, 160.00, N'رف D-1', N'فرامل');
GO
";
        }
    }
}
