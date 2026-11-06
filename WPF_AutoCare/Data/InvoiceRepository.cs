using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly IDbHelper _dbHelper;

        public InvoiceRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public decimal GetTodaySales()
        {
            DateTime todayStart = DateTime.Today;
            DateTime tomorrowStart = todayStart.AddDays(1);
            int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;
            string query = @"
                SELECT ISNULL(SUM(PaidAmount), 0)
                FROM Invoices
                WHERE InvoiceDate >= @todayStart AND InvoiceDate < @tomorrowStart";
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@todayStart", todayStart),
                new SqlParameter("@tomorrowStart", tomorrowStart)
            };
            if (currentBranch.HasValue)
            {
                query += " AND (BranchId = @branchId OR BranchId IS NULL)";
                parameters.Add(new SqlParameter("@branchId", currentBranch.Value));
            }
            try
            {
                object val = _dbHelper.ExecuteScalar<object>(query, parameters.ToArray());
                if (val == null || val == DBNull.Value) return 0;
                return Convert.ToDecimal(val);
            }
            catch
            {
                return 0;
            }
        }

        public decimal GetTodaySparePartsCost()
        {
            DateTime todayStart = DateTime.Today;
            DateTime tomorrowStart = todayStart.AddDays(1);
            int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;
            string query = @"
                SELECT ISNULL(SUM(rosp.Quantity * sp.BuyingPrice), 0)
                FROM Invoices inv
                INNER JOIN RepairOrderSparePart rosp ON inv.RepairOrderNumber = rosp.RepairOrderNumber
                INNER JOIN SpareParts sp ON rosp.PartCode = sp.PartCode
                WHERE inv.InvoiceDate >= @todayStart AND inv.InvoiceDate < @tomorrowStart";
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@todayStart", todayStart),
                new SqlParameter("@tomorrowStart", tomorrowStart)
            };
            if (currentBranch.HasValue)
            {
                query += " AND (inv.BranchId = @branchId OR inv.BranchId IS NULL)";
                parameters.Add(new SqlParameter("@branchId", currentBranch.Value));
            }
            try
            {
                object val = _dbHelper.ExecuteScalar<object>(query, parameters.ToArray());
                if (val == null || val == DBNull.Value) return 0;
                return Convert.ToDecimal(val);
            }
            catch
            {
                return 0;
            }
        }

        public string GenerateNewInvoiceNumber()
        {
            // Phase 5 fix: use SQL SEQUENCE for atomic, race-condition-free ID generation.
            // Falls back to MAX()+1 if the sequence does not exist yet.
            try
            {
                string seqQuery = @"
                    DECLARE @next INT = NEXT VALUE FOR dbo.seq_InvoiceNumber;
                    SELECT 'INV-' + FORMAT(@next, 'D3')";
                string result = _dbHelper.ExecuteScalar<string>(seqQuery, null);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }
            catch
            {
                // Sequence not yet created — fall back to legacy MAX() method
            }

            // Legacy fallback
            string fallbackQuery = "SELECT MAX(InvoiceNumber) FROM Invoices";
            string maxInv = _dbHelper.ExecuteScalar<string>(fallbackQuery, null);
            if (string.IsNullOrEmpty(maxInv))
                return "INV-001";

            string numPart = "";
            foreach (char c in maxInv)
            {
                if (char.IsDigit(c)) numPart += c;
            }
            if (int.TryParse(numPart, out int number))
            {
                number++;
                return "INV-" + number.ToString("D3");
            }
            return "INV-001";
        }

        public void InsertInvoice(string invoiceNumber, string repairOrderNumber, string createdByUsername, decimal paidAmount, decimal discountAmount = 0, string paymentMethod = "Cash") =>
            InsertInvoice(invoiceNumber, repairOrderNumber, createdByUsername, paidAmount, discountAmount, paymentMethod, null, null);

        public void InsertInvoice(string invoiceNumber, string repairOrderNumber, string createdByUsername, decimal paidAmount, SqlConnection conn, SqlTransaction tran) =>
            InsertInvoice(invoiceNumber, repairOrderNumber, createdByUsername, paidAmount, 0m, "Cash", conn, tran);

        public void InsertInvoice(string invoiceNumber, string repairOrderNumber, string createdByUsername, decimal paidAmount, decimal discountAmount, string paymentMethod, SqlConnection conn, SqlTransaction tran)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod)) paymentMethod = "Cash";
            int branchId = AppServiceLocator.BranchContext?.CurrentBranchId ?? 1;
            string query = @"
                INSERT INTO Invoices (InvoiceNumber, RepairOrderNumber, InvoiceDate, PaymentMethod, PaidAmount, DiscountAmount, IsPaid, CreatedByUsername, BranchId)
                VALUES (@invNum, @orderNum, @now, @payMethod, @paidAmount, @discount, 1, @createdBy, @branchId)";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@invNum", invoiceNumber),
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@payMethod", paymentMethod),
                new SqlParameter("@paidAmount", paidAmount),
                new SqlParameter("@discount", discountAmount),
                new SqlParameter("@now", DateTime.Now),
                new SqlParameter("@createdBy", string.IsNullOrEmpty(createdByUsername) ? (object)DBNull.Value : (object)createdByUsername),
                new SqlParameter("@branchId", branchId)
            };
            int res = _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
            if (res < 0)
                throw new Exception("فشل في إدخال الفاتورة في قاعدة البيانات.");
        }

        public void DeleteInvoice(string repairOrderNumber)
        {
            string query = "DELETE FROM Invoices WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public bool InvoiceExistsByRepairOrder(string repairOrderNumber)
        {
            string query = "SELECT COUNT(1) FROM Invoices WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public DataTable GetCompletedOrdersWithoutInvoice(string searchTerm = "")
        {
            string query = @"
                SELECT ro.RepairOrderNumber, ro.CompletionDate,
                       c.PhoneNumber AS CustomerPhone, c.FullName AS CustomerName,
                       v.VIN, v.CarName, v.CarModel
                FROM RepairOrders ro
                INNER JOIN Vehicles v ON ro.PlateNumber = v.PlateNumber
                INNER JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                LEFT JOIN Invoices inv ON ro.RepairOrderNumber = inv.RepairOrderNumber
                WHERE ro.Status = 'Completed' AND inv.InvoiceNumber IS NULL
                  AND (@search = '' OR c.FullName LIKE @search OR c.PhoneNumber LIKE @search OR v.PlateNumber LIKE @search OR v.CarName LIKE @search)
                ORDER BY ro.CompletionDate DESC";

            string searchVal = string.IsNullOrEmpty(searchTerm) ? "" : "%" + searchTerm + "%";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@search", searchVal)
            };

            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public List<C__project_Auto_Care.Core.DTOs.CompletedOrderWithoutInvoiceDto> GetCompletedOrdersWithoutInvoiceList(string searchTerm = "")
        {
            int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;
            string query = @"
                SELECT ro.RepairOrderNumber, ro.CompletionDate,
                       c.PhoneNumber AS CustomerPhone, c.FullName AS CustomerName,
                       v.VIN, v.CarName, v.CarModel
                FROM RepairOrders ro
                INNER JOIN Vehicles v ON ro.PlateNumber = v.PlateNumber
                INNER JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                LEFT JOIN Invoices inv ON ro.RepairOrderNumber = inv.RepairOrderNumber
                WHERE ro.Status = 'Completed' AND inv.InvoiceNumber IS NULL
                  AND (@search = '' OR c.FullName LIKE @search OR c.PhoneNumber LIKE @search OR v.PlateNumber LIKE @search OR v.CarName LIKE @search)";

            string searchVal = string.IsNullOrEmpty(searchTerm) ? "" : "%" + searchTerm + "%";
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@search", searchVal)
            };

            if (currentBranch.HasValue)
            {
                query += " AND (ro.BranchId = @branchId OR ro.BranchId IS NULL)";
                parameters.Add(new SqlParameter("@branchId", currentBranch.Value));
            }

            query += " ORDER BY ro.CompletionDate DESC";

            return _dbHelper.ExecuteList(query, parameters.ToArray(), r =>
            {
                string completionDate = "";
                if (r["CompletionDate"] != DBNull.Value)
                    completionDate = Convert.ToDateTime(r["CompletionDate"]).ToString("yyyy-MM-dd HH:mm");

                return new C__project_Auto_Care.Core.DTOs.CompletedOrderWithoutInvoiceDto
                {
                    PhoneNumber = r["CustomerPhone"] != DBNull.Value ? r["CustomerPhone"].ToString() : "",
                    CustomerName = r["CustomerName"] != DBNull.Value ? r["CustomerName"].ToString() : "",
                    VIN = r["VIN"] != DBNull.Value ? r["VIN"].ToString() : "",
                    CarName = r["CarName"] != DBNull.Value ? r["CarName"].ToString() : "",
                    CarModel = r["CarModel"] != DBNull.Value ? r["CarModel"].ToString() : "",
                    CompletionDate = completionDate,
                    RoNumber = r["RepairOrderNumber"] != DBNull.Value ? r["RepairOrderNumber"].ToString() : ""
                };
            });
        }

        public List<C__project_Auto_Care.Core.DTOs.IssuedInvoiceSummaryDto> GetIssuedInvoicesList(string searchTerm = "")
        {
            int? currentBranch = AppServiceLocator.BranchContext?.CurrentBranchId;
            string query = @"
                SELECT inv.InvoiceNumber, inv.RepairOrderNumber, inv.InvoiceDate,
                       inv.PaidAmount, inv.DiscountAmount, inv.PaymentMethod, inv.CreatedByUsername,
                       c.PhoneNumber AS CustomerPhone, c.FullName AS CustomerName,
                       v.PlateNumber, v.CarName, v.CarModel
                FROM Invoices inv
                INNER JOIN RepairOrders ro ON inv.RepairOrderNumber = ro.RepairOrderNumber
                INNER JOIN Vehicles v ON ro.PlateNumber = v.PlateNumber
                INNER JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                WHERE (@search = '' 
                   OR inv.InvoiceNumber LIKE @search 
                   OR inv.RepairOrderNumber LIKE @search
                   OR c.FullName LIKE @search 
                   OR c.PhoneNumber LIKE @search 
                   OR v.PlateNumber LIKE @search 
                   OR v.CarName LIKE @search
                   OR CONVERT(VARCHAR(20), inv.InvoiceDate, 120) LIKE @search
                   OR CONVERT(VARCHAR(20), inv.InvoiceDate, 103) LIKE @search)";

            string searchVal = string.IsNullOrEmpty(searchTerm) ? "" : "%" + searchTerm + "%";
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@search", searchVal)
            };

            if (currentBranch.HasValue)
            {
                query += " AND (inv.BranchId = @branchId OR inv.BranchId IS NULL)";
                parameters.Add(new SqlParameter("@branchId", currentBranch.Value));
            }

            query += " ORDER BY inv.InvoiceDate DESC";

            return _dbHelper.ExecuteList(query, parameters.ToArray(), r =>
            {
                string invDate = "";
                if (r["InvoiceDate"] != DBNull.Value)
                    invDate = Convert.ToDateTime(r["InvoiceDate"]).ToString("yyyy-MM-dd HH:mm");

                decimal paid = r["PaidAmount"] != DBNull.Value ? Convert.ToDecimal(r["PaidAmount"]) : 0m;
                decimal discount = r["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(r["DiscountAmount"]) : 0m;

                return new C__project_Auto_Care.Core.DTOs.IssuedInvoiceSummaryDto
                {
                    InvoiceNumber = r["InvoiceNumber"]?.ToString() ?? "",
                    RepairOrderNumber = r["RepairOrderNumber"]?.ToString() ?? "",
                    InvoiceDate = invDate,
                    CustomerName = r["CustomerName"]?.ToString() ?? "",
                    CustomerPhone = r["CustomerPhone"]?.ToString() ?? "",
                    PlateNumber = r["PlateNumber"]?.ToString() ?? "",
                    CarName = r["CarName"]?.ToString() ?? "",
                    CarModel = r["CarModel"]?.ToString() ?? "",
                    PaidAmount = paid,
                    DiscountAmount = discount,
                    PaymentMethod = r["PaymentMethod"]?.ToString() ?? "نقداً (Cash)",
                    CreatedByUsername = r["CreatedByUsername"]?.ToString() ?? ""
                };
            });
        }

        public void RevertToInProgress(string repairOrderNumber)
        {
            string query = "UPDATE RepairOrders SET Status = 'InProgress', CompletionDate = NULL WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public DataTable GetInvoicesWithDetailsForReport(DateTime startDate, DateTime endDate, string receptionistUsername = null)
        {
            // Phase 2 fix: replace CAST(InvoiceDate AS DATE) with index-friendly range
            // startDate inclusive (start of day), endDate exclusive (start of next day)
            string query = @"
                SELECT 
                    inv.InvoiceNumber,
                    inv.InvoiceDate,
                    inv.CreatedByUsername,
                    ISNULL(inv.PaymentMethod, 'Cash') AS PaymentMethod,
                    ISNULL(inv.DiscountAmount, 0) AS DiscountAmount,
                    c.FullName,
                    ISNULL(srv.TotalServices, 0) AS TotalServices,
                    ISNULL(prt.TotalParts, 0) AS TotalParts,
                    (ISNULL(srv.TotalServices, 0) + ISNULL(prt.TotalParts, 0)) AS SubTotal,
                    inv.PaidAmount AS GrandTotal
                FROM Invoices inv
                LEFT JOIN RepairOrders ro ON inv.RepairOrderNumber = ro.RepairOrderNumber
                LEFT JOIN Vehicles v ON ro.PlateNumber = v.PlateNumber
                LEFT JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                LEFT JOIN (
                    SELECT RepairOrderNumber, SUM(UnitPrice) AS TotalServices
                    FROM RepairOrderService
                    GROUP BY RepairOrderNumber
                ) srv ON inv.RepairOrderNumber = srv.RepairOrderNumber
                LEFT JOIN (
                    SELECT RepairOrderNumber, SUM(Quantity * UnitPrice) AS TotalParts
                    FROM RepairOrderSparePart
                    GROUP BY RepairOrderNumber
                ) prt ON inv.RepairOrderNumber = prt.RepairOrderNumber
                WHERE inv.InvoiceDate >= @startDate
                  AND inv.InvoiceDate < @endDateExclusive
                  AND (@receptionist IS NULL OR inv.CreatedByUsername = @receptionist)
                ORDER BY inv.InvoiceDate DESC";

            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@startDate", startDate.Date),
                new SqlParameter("@endDateExclusive", endDate.Date.AddDays(1)),
                new SqlParameter("@receptionist", string.IsNullOrEmpty(receptionistUsername) ? (object)DBNull.Value : (object)receptionistUsername)
            };

            return _dbHelper.ExecuteDataTable(query, parameters.ToArray());
        }

        public List<C__project_Auto_Care.Core.DTOs.FinancialReportItemDto> GetInvoicesWithDetailsForReportList(DateTime startDate, DateTime endDate, string receptionistUsername = null)
        {
            string query = @"
                SELECT 
                    inv.InvoiceNumber,
                    inv.RepairOrderNumber,
                    inv.InvoiceDate,
                    inv.CreatedByUsername,
                    ISNULL(c.FullName, '') AS CustomerName,
                    ISNULL(c.PhoneNumber, ISNULL(v.CustomerPhone, '')) AS CustomerPhone,
                    ISNULL(srv.TotalServices, 0) AS TotalServices,
                    ISNULL(prt.TotalParts, 0) AS TotalParts,
                    ISNULL(inv.DiscountAmount, 0) AS DiscountAmount,
                    ISNULL(inv.TaxAmount, 0) AS TaxAmount,
                    inv.PaidAmount AS GrandTotal,
                    ISNULL(inv.PaymentMethod, 'Cash') AS PaymentMethod
                FROM Invoices inv
                LEFT JOIN RepairOrders ro ON inv.RepairOrderNumber = ro.RepairOrderNumber
                LEFT JOIN Vehicles v ON ro.PlateNumber = v.PlateNumber
                LEFT JOIN Customers c ON v.CustomerPhone = c.PhoneNumber
                LEFT JOIN (
                    SELECT RepairOrderNumber, SUM(UnitPrice) AS TotalServices
                    FROM RepairOrderService
                    GROUP BY RepairOrderNumber
                ) srv ON inv.RepairOrderNumber = srv.RepairOrderNumber
                LEFT JOIN (
                    SELECT RepairOrderNumber, SUM(Quantity * UnitPrice) AS TotalParts
                    FROM RepairOrderSparePart
                    GROUP BY RepairOrderNumber
                ) prt ON inv.RepairOrderNumber = prt.RepairOrderNumber
                WHERE inv.InvoiceDate >= @startDate
                  AND inv.InvoiceDate < @endDateExclusive
                  AND (@receptionist IS NULL OR inv.CreatedByUsername = @receptionist)
                ORDER BY inv.InvoiceDate DESC";

            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@startDate", startDate.Date),
                new SqlParameter("@endDateExclusive", endDate.Date.AddDays(1)),
                new SqlParameter("@receptionist", string.IsNullOrEmpty(receptionistUsername) ? (object)DBNull.Value : (object)receptionistUsername)
            };

            return _dbHelper.ExecuteList(query, parameters.ToArray(), r =>
            {
                string invDate = "";
                if (r["InvoiceDate"] != DBNull.Value)
                    invDate = Convert.ToDateTime(r["InvoiceDate"]).ToString("yyyy-MM-dd HH:mm");

                decimal svc = r["TotalServices"] != DBNull.Value ? Convert.ToDecimal(r["TotalServices"]) : 0m;
                decimal prt = r["TotalParts"] != DBNull.Value ? Convert.ToDecimal(r["TotalParts"]) : 0m;
                decimal disc = r["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(r["DiscountAmount"]) : 0m;
                decimal tax = r["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(r["TaxAmount"]) : 0m;
                decimal total = r["GrandTotal"] != DBNull.Value ? Convert.ToDecimal(r["GrandTotal"]) : 0m;
                if (total == 0m)
                    total = Math.Max(0m, svc + prt - disc + tax);

                string payMethod = r["PaymentMethod"] != DBNull.Value ? r["PaymentMethod"].ToString() : "نقداً (Cash)";
                if (string.Equals(payMethod, "Cash", StringComparison.OrdinalIgnoreCase)) payMethod = "نقداً (Cash)";
                else if (string.Equals(payMethod, "POS", StringComparison.OrdinalIgnoreCase)) payMethod = "شبكة / مدى (POS)";
                else if (string.Equals(payMethod, "Credit", StringComparison.OrdinalIgnoreCase)) payMethod = "آجل / ذمم (Credit)";
                else if (string.Equals(payMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase)) payMethod = "تحويل بنكي (Bank Transfer)";

                return new C__project_Auto_Care.Core.DTOs.FinancialReportItemDto
                {
                    InvoiceNumber = r["InvoiceNumber"] != DBNull.Value ? r["InvoiceNumber"].ToString() : "",
                    RepairOrderNumber = r["RepairOrderNumber"] != DBNull.Value ? r["RepairOrderNumber"].ToString() : "",
                    InvoiceDate = invDate,
                    CustomerName = r["CustomerName"] != DBNull.Value ? r["CustomerName"].ToString() : "",
                    CustomerPhone = r["CustomerPhone"] != DBNull.Value ? r["CustomerPhone"].ToString() : "",
                    CreatedBy = r["CreatedByUsername"] != DBNull.Value ? r["CreatedByUsername"].ToString() : "",
                    ServicesTotal = svc,
                    PartsTotal = prt,
                    DiscountAmount = disc,
                    TaxAmount = tax,
                    GrandTotal = total,
                    PaymentMethod = payMethod
                };
            });
        }

        public DataTable GetInvoicesForCrystalReport(DateTime startDate, DateTime endDate)
        {
            // Phase 2 fix: index-friendly date range (no CAST on column)
            string query = @"
                SELECT 
                    inv.InvoiceNumber,
                    inv.RepairOrderNumber,
                    inv.InvoiceDate,
                    inv.CreatedByUsername,
                    u.FullName
                FROM Invoices inv
                LEFT JOIN Users u ON inv.CreatedByUsername = u.Username
                WHERE inv.InvoiceDate >= @startDate
                  AND inv.InvoiceDate < @endDateExclusive
                ORDER BY inv.InvoiceDate DESC";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@startDate", startDate.Date),
                new SqlParameter("@endDateExclusive", endDate.Date.AddDays(1))
            };

            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public DataTable GetInvoiceByRepairOrder(string repairOrderNumber)
        {
            string query = "SELECT InvoiceNumber, RepairOrderNumber, PaidAmount, ISNULL(DiscountAmount, 0) AS DiscountAmount, ISNULL(TaxRate, 0) AS TaxRate, ISNULL(TaxAmount, 0) AS TaxAmount, ISNULL(PaymentMethod, 'Cash') AS PaymentMethod, InvoiceDate, CreatedByUsername FROM Invoices WHERE RepairOrderNumber = @orderNum";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }
    }
}
