using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class SparePartRepository : ISparePartRepository
    {
        private readonly IDbHelper _dbHelper;

        public SparePartRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetAllSpareParts(string searchTerm = "")
        {
            // Phase 4: Use Full-Text CONTAINS() if a search term is provided.
            // Falls back to LIKE if FTS is not installed/available on the server.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // FTS path: CONTAINS searches PartName using FTS index
                // PartCode is still searched with LIKE (short code, not natural language)
                string ftsQuery = @"
                    SELECT PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category
                    FROM SpareParts
                    WHERE CONTAINS(PartName, @ftsTerm) OR PartCode LIKE @codeTerm
                    ORDER BY PartName";
                string likeFallbackQuery = @"
                    SELECT PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category
                    FROM SpareParts
                    WHERE PartName LIKE @likeTerm OR PartCode LIKE @codeTerm
                    ORDER BY PartName";
                // Build a prefix-aware FTS term: wrap in quotes for phrase/prefix search
                string ftsTerm = '"' + searchTerm.Replace('"', ' ').Trim() + '*' + '"';
                string likeTerm = "%" + searchTerm + "%";
                string codeTerm = "%" + searchTerm + "%";
                try
                {
                    var ftsParams = new SqlParameter[]
                    {
                        new SqlParameter("@ftsTerm",  ftsTerm),
                        new SqlParameter("@codeTerm", codeTerm)
                    };
                    return _dbHelper.ExecuteDataTable(ftsQuery, ftsParams);
                }
                catch
                {
                    // FTS not available — fall back to LIKE
                    var likeParams = new SqlParameter[]
                    {
                        new SqlParameter("@likeTerm", likeTerm),
                        new SqlParameter("@codeTerm", codeTerm)
                    };
                    return _dbHelper.ExecuteDataTable(likeFallbackQuery, likeParams);
                }
            }
            else
            {
                // No search term — return all (no filter needed, scan is expected)
                string query = "SELECT PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category FROM SpareParts ORDER BY PartName";
                return _dbHelper.ExecuteDataTable(query, null);
            }
        }

        public DataTable GetSparePartByCode(string partCode)
        {
            string query = "SELECT PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category FROM SpareParts WHERE PartCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", partCode)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public C__project_Auto_Care.Core.DTOs.SparePartRowDto GetSparePartDtoByCode(string partCode)
        {
            if (string.IsNullOrWhiteSpace(partCode)) return null;
            string query = "SELECT PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category FROM SpareParts WHERE PartCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", partCode.Trim())
            };
            return _dbHelper.ExecuteSingle(query, parameters, r => new C__project_Auto_Care.Core.DTOs.SparePartRowDto
            {
                PartCode = r["PartCode"]?.ToString(),
                PartName = r["PartName"]?.ToString(),
                Qty = r["QuantityInStock"] != DBNull.Value ? r["QuantityInStock"].ToString() : "0",
                MinStock = r["MinimumStock"] != DBNull.Value ? r["MinimumStock"].ToString() : "0",
                BuyingPrice = r["BuyingPrice"] != DBNull.Value ? Convert.ToDecimal(r["BuyingPrice"]).ToString("N2") : "0.00",
                SellingPrice = r["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(r["SellingPrice"]).ToString("N2") : "0.00",
                Category = r["Category"]?.ToString()
            });
        }

        public List<C__project_Auto_Care.Core.DTOs.SparePartRowDto> GetAllSparePartsList(string searchTerm = "")
        {
            string query = "SELECT PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category FROM SpareParts";
            List<SqlParameter> listParams = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += " WHERE PartName LIKE @term OR PartCode LIKE @term";
                listParams.Add(new SqlParameter("@term", "%" + searchTerm.Trim() + "%"));
            }
            query += " ORDER BY PartName";
            return _dbHelper.ExecuteList(query, listParams.ToArray(), r => new C__project_Auto_Care.Core.DTOs.SparePartRowDto
            {
                PartCode = r["PartCode"]?.ToString(),
                PartName = r["PartName"]?.ToString(),
                Qty = r["QuantityInStock"] != DBNull.Value ? r["QuantityInStock"].ToString() : "0",
                MinStock = r["MinimumStock"] != DBNull.Value ? r["MinimumStock"].ToString() : "0",
                BuyingPrice = r["BuyingPrice"] != DBNull.Value ? Convert.ToDecimal(r["BuyingPrice"]).ToString("N2") : "0.00",
                SellingPrice = r["SellingPrice"] != DBNull.Value ? Convert.ToDecimal(r["SellingPrice"]).ToString("N2") : "0.00",
                Category = r["Category"]?.ToString()
            });
        }

        public bool TryDeductStock(string partCode, int quantity)
        {
            if (string.IsNullOrWhiteSpace(partCode) || quantity <= 0) return false;
            string query = @"
                UPDATE SpareParts 
                SET QuantityInStock = QuantityInStock - @qty, UpdatedAt = GETDATE()
                WHERE PartCode = @code AND QuantityInStock >= @qty;";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@qty", quantity),
                new SqlParameter("@code", partCode.Trim())
            };
            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool UpdatePriceWithConcurrency(string partCode, decimal newPrice, byte[] rowVersion)
        {
            if (string.IsNullOrWhiteSpace(partCode) || rowVersion == null) return false;
            string query = @"
                UPDATE SpareParts 
                SET SellingPrice = @price, UpdatedAt = GETDATE() 
                WHERE PartCode = @code AND RowVersion = @rowVersion";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@price", newPrice),
                new SqlParameter("@code", partCode.Trim()),
                new SqlParameter("@rowVersion", rowVersion)
            };
            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public byte[] GetSparePartRowVersion(string partCode)
        {
            if (string.IsNullOrWhiteSpace(partCode)) return null;
            string query = "SELECT RowVersion FROM SpareParts WHERE PartCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", partCode.Trim())
            };
            return _dbHelper.ExecuteScalar<byte[]>(query, parameters);
        }

        public bool PartCodeExists(string partCode)
        {
            string query = "SELECT COUNT(1) FROM SpareParts WHERE PartCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", partCode)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public void AddSparePart(string partCode, string partName, int quantity, int minStock, decimal buyingPrice, decimal sellingPrice, string location, string category)
        {
            string query = "INSERT INTO SpareParts (PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category) VALUES (@code, @name, @qty, @min, @buying, @selling, @loc, @cat)";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", partCode),
                new SqlParameter("@name", partName),
                new SqlParameter("@qty", quantity),
                new SqlParameter("@min", minStock),
                new SqlParameter("@buying", buyingPrice),
                new SqlParameter("@selling", sellingPrice),
                new SqlParameter("@loc", location),
                new SqlParameter("@cat", category)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void UpdateSparePart(string partCode, string partName, int quantity, int minStock, decimal buyingPrice, decimal sellingPrice, string location, string category)
        {
            string query = "UPDATE SpareParts SET PartName = @name, QuantityInStock = @qty, MinimumStock = @min, BuyingPrice = @buying, SellingPrice = @selling, Location = @loc, Category = @cat WHERE PartCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", partCode),
                new SqlParameter("@name", partName),
                new SqlParameter("@qty", quantity),
                new SqlParameter("@min", minStock),
                new SqlParameter("@buying", buyingPrice),
                new SqlParameter("@selling", sellingPrice),
                new SqlParameter("@loc", location),
                new SqlParameter("@cat", category)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void DeleteSparePart(string partCode)
        {
            string checkQuery = @"
                DECLARE @count INT = 0;
                IF OBJECT_ID(N'dbo.RepairOrderSparePart', N'U') IS NOT NULL
                    SELECT @count = @count + COUNT(1) FROM RepairOrderSparePart WHERE PartCode = @code;
                IF OBJECT_ID(N'dbo.PurchaseInvoiceDetails', N'U') IS NOT NULL
                    SELECT @count = @count + COUNT(1) FROM PurchaseInvoiceDetails WHERE PartCode = @code;
                SELECT @count;";
            SqlParameter[] checkParams = new SqlParameter[]
            {
                new SqlParameter("@code", partCode)
            };
            int count = _dbHelper.ExecuteScalar<int>(checkQuery, checkParams);
            if (count > 0)
            {
                throw new InvalidOperationException("لا يمكن حذف القطعة لأنها مرتبطة بأوامر إصلاح أو فواتير توريد مخزون.");
            }

            string deleteQuery = "DELETE FROM SpareParts WHERE PartCode = @code";
            SqlParameter[] deleteParams = new SqlParameter[]
            {
                new SqlParameter("@code", partCode)
            };
            _dbHelper.ExecuteNonQuery(deleteQuery, deleteParams);
        }

        public void UpdateStock(string partCode, int quantityDelta)
        {
            string query = "UPDATE SpareParts SET QuantityInStock = QuantityInStock + @delta WHERE PartCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@delta", quantityDelta),
                new SqlParameter("@code", partCode)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public DataTable GetTopSpareParts(DateTime startDate, DateTime endDate, int topN)
        {
            string query = @"
                SELECT TOP (@topN)
                    sp.PartCode,
                    sp.PartName,
                    SUM(rops.Quantity) AS TotalQuantity,
                    SUM(rops.Quantity * rops.UnitPrice) AS TotalRevenue,
                    sp.QuantityInStock AS StockLeft
                FROM RepairOrderSparePart rops
                INNER JOIN SpareParts sp ON rops.PartCode = sp.PartCode
                INNER JOIN RepairOrders ro ON rops.RepairOrderNumber = ro.RepairOrderNumber
                WHERE ro.CompletionDate >= @startDate AND ro.CompletionDate <= @endDate
                GROUP BY sp.PartCode, sp.PartName, sp.QuantityInStock
                ORDER BY SUM(rops.Quantity) DESC";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@startDate", startDate),
                new SqlParameter("@endDate", endDate),
                new SqlParameter("@topN", topN)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public List<string> GetDistinctCategories()
        {
            List<string> list = new List<string>();
            string query = "SELECT DISTINCT Category FROM SpareParts WHERE Category IS NOT NULL AND Category <> '' ORDER BY Category";
            DataTable dt = _dbHelper.ExecuteDataTable(query, null);
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string cat = row["Category"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(cat) && !list.Contains(cat))
                    {
                        list.Add(cat);
                    }
                }
            }
            return list;
        }

        public DataTable GetSparePartsByCategory(string category)
        {
            // Phase 3 fix: removed LTRIM(RTRIM(Category)) wrapper which prevents index use.
            // Data is cleaned at DB level by Phase3_DataCleanup.sql.
            // Parameter is already trimmed by the caller (.Trim()).
            string query = "SELECT PartCode, PartName, QuantityInStock, MinimumStock, BuyingPrice, SellingPrice, Location, Category FROM SpareParts WHERE Category = @category";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@category", category.Trim())
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public int GetAvailableStock(string partCode)
        {
            string query = "SELECT QuantityInStock FROM SpareParts WHERE PartCode = @code";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@code", partCode)
            };
            return _dbHelper.ExecuteScalar<int>(query, parameters);
        }

        public int GetLowStockCount()
        {
            string query = "SELECT COUNT(1) FROM SpareParts WHERE QuantityInStock <= MinimumStock";
            try
            {
                return _dbHelper.ExecuteScalar<int>(query, null);
            }
            catch
            {
                return 0;
            }
        }
    }
}
