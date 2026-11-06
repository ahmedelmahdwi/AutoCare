using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class PurchaseItemDto
    {
        public string PartCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
    }

    public class SupplierRepository : ISupplierRepository
    {
        private readonly IDbHelper _dbHelper;

        public SupplierRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetAllSuppliers(string search = "")
        {
            string query = @"
                SELECT SupplierID, SupplierName, ContactPerson, PhoneNumber, Address, Balance, CreatedAt
                FROM Suppliers
                WHERE (@search = '' OR SupplierName LIKE @search OR PhoneNumber LIKE @search OR ContactPerson LIKE @search)
                ORDER BY SupplierName ASC";

            string sVal = string.IsNullOrEmpty(search) ? "" : "%" + search + "%";
            return _dbHelper.ExecuteDataTable(query, new SqlParameter[] { new SqlParameter("@search", sVal) });
        }

        public DataRow GetSupplierById(int supplierId)
        {
            string query = "SELECT * FROM Suppliers WHERE SupplierID = @id";
            DataTable dt = _dbHelper.ExecuteDataTable(query, new SqlParameter[] { new SqlParameter("@id", supplierId) });
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public int AddSupplier(string name, string contact, string phone, string address)
        {
            string query = @"
                INSERT INTO Suppliers (SupplierName, ContactPerson, PhoneNumber, Address, Balance, CreatedAt)
                VALUES (@name, @contact, @phone, @address, 0, GETDATE());
                SELECT SCOPE_IDENTITY();";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@name", name ?? ""),
                new SqlParameter("@contact", string.IsNullOrEmpty(contact) ? (object)DBNull.Value : contact),
                new SqlParameter("@phone", string.IsNullOrEmpty(phone) ? (object)DBNull.Value : phone),
                new SqlParameter("@address", string.IsNullOrEmpty(address) ? (object)DBNull.Value : address)
            };

            object res = _dbHelper.ExecuteScalar<object>(query, parameters);
            return Convert.ToInt32(res);
        }

        public void UpdateSupplier(int id, string name, string contact, string phone, string address)
        {
            string query = @"
                UPDATE Suppliers
                SET SupplierName = @name, ContactPerson = @contact, PhoneNumber = @phone, Address = @address
                WHERE SupplierID = @id";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@id", id),
                new SqlParameter("@name", name ?? ""),
                new SqlParameter("@contact", string.IsNullOrEmpty(contact) ? (object)DBNull.Value : contact),
                new SqlParameter("@phone", string.IsNullOrEmpty(phone) ? (object)DBNull.Value : phone),
                new SqlParameter("@address", string.IsNullOrEmpty(address) ? (object)DBNull.Value : address)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void DeleteSupplier(int id)
        {
            string checkQuery = "SELECT COUNT(1) FROM PurchaseInvoices WHERE SupplierID = @id";
            int count = _dbHelper.ExecuteScalar<int>(checkQuery, new SqlParameter[] { new SqlParameter("@id", id) });
            if (count > 0)
                throw new InvalidOperationException("لا يمكن حذف المورد لوجود فواتير شراء مسجلة باسمه.");

            string query = "DELETE FROM Suppliers WHERE SupplierID = @id";
            _dbHelper.ExecuteNonQuery(query, new SqlParameter[] { new SqlParameter("@id", id) });
        }

        public string GenerateNewPurchaseInvoiceNumber()
        {
            string query = "SELECT MAX(PurchaseInvoiceNumber) FROM PurchaseInvoices";
            string max = _dbHelper.ExecuteScalar<string>(query, null);
            if (string.IsNullOrEmpty(max)) return "PINV-001";

            string numPart = "";
            foreach (char c in max) if (char.IsDigit(c)) numPart += c;

            if (int.TryParse(numPart, out int num))
            {
                num++;
                return "PINV-" + num.ToString("D3");
            }
            return "PINV-001";
        }

        public void CreatePurchaseInvoice(string invoiceNumber, int supplierId, decimal totalAmount, string notes, string createdBy, List<PurchaseItemDto> items)
        {
            _dbHelper.ExecuteTransaction((conn, tran) =>
            {
                // 1. Insert header
                string hSql = @"
                    INSERT INTO PurchaseInvoices (PurchaseInvoiceNumber, SupplierID, InvoiceDate, TotalAmount, Notes, CreatedBy)
                    VALUES (@num, @supId, GETDATE(), @total, @notes, @by)";

                using (var cmd = new SqlCommand(hSql, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@num", invoiceNumber);
                    cmd.Parameters.AddWithValue("@supId", supplierId);
                    cmd.Parameters.AddWithValue("@total", totalAmount);
                    cmd.Parameters.AddWithValue("@notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                    cmd.Parameters.AddWithValue("@by", string.IsNullOrEmpty(createdBy) ? (object)DBNull.Value : createdBy);
                    cmd.ExecuteNonQuery();
                }

                // 2. Insert items & update spare parts inventory
                string dSql = @"
                    INSERT INTO PurchaseInvoiceDetails (PurchaseInvoiceNumber, PartCode, Quantity, UnitCost, TotalCost)
                    VALUES (@num, @code, @qty, @cost, @tCost)";

                string updStockSql = @"
                    UPDATE SpareParts 
                    SET QuantityInStock = QuantityInStock + @qty,
                        BuyingPrice = @cost,
                        UpdatedAt = GETDATE()
                    WHERE PartCode = @code";

                foreach (var it in items)
                {
                    using (var cmd = new SqlCommand(dSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@num", invoiceNumber);
                        cmd.Parameters.AddWithValue("@code", it.PartCode);
                        cmd.Parameters.AddWithValue("@qty", it.Quantity);
                        cmd.Parameters.AddWithValue("@cost", it.UnitCost);
                        cmd.Parameters.AddWithValue("@tCost", it.TotalCost);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SqlCommand(updStockSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@qty", it.Quantity);
                        cmd.Parameters.AddWithValue("@cost", it.UnitCost);
                        cmd.Parameters.AddWithValue("@code", it.PartCode);
                        cmd.ExecuteNonQuery();
                    }
                }

                // 3. Update supplier balance
                string supBalSql = "UPDATE Suppliers SET Balance = Balance + @total WHERE SupplierID = @supId";
                using (var cmd = new SqlCommand(supBalSql, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@total", totalAmount);
                    cmd.Parameters.AddWithValue("@supId", supplierId);
                    cmd.ExecuteNonQuery();
                }
            });

            AppServiceLocator.AuditRepo?.Log("فاتورة شراء مخزون", "PurchaseInvoices", invoiceNumber, $"إدخال فاتورة شراء للمورد #{supplierId} بإجمالي: {totalAmount:N2} د.ل لتوريد قطع غيار");
        }

        public DataTable GetAllPurchaseInvoices(string search = "")
        {
            string query = @"
                SELECT pi.PurchaseInvoiceNumber, pi.InvoiceDate, s.SupplierName, pi.TotalAmount, pi.Notes, pi.CreatedBy
                FROM PurchaseInvoices pi
                INNER JOIN Suppliers s ON pi.SupplierID = s.SupplierID
                WHERE (@search = '' OR pi.PurchaseInvoiceNumber LIKE @search OR s.SupplierName LIKE @search)
                ORDER BY pi.InvoiceDate DESC";

            string sVal = string.IsNullOrEmpty(search) ? "" : "%" + search + "%";
            return _dbHelper.ExecuteDataTable(query, new SqlParameter[] { new SqlParameter("@search", sVal) });
        }

        public DataTable GetPurchaseInvoiceDetails(string invoiceNumber)
        {
            string query = @"
                SELECT pid.PartCode, sp.PartName, pid.Quantity, pid.UnitCost, pid.TotalCost
                FROM PurchaseInvoiceDetails pid
                INNER JOIN SpareParts sp ON pid.PartCode = sp.PartCode
                WHERE pid.PurchaseInvoiceNumber = @num";

            return _dbHelper.ExecuteDataTable(query, new SqlParameter[] { new SqlParameter("@num", invoiceNumber) });
        }
    }
}
