using System;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class RepairOrderSparePartRepository : IRepairOrderSparePartRepository
    {
        private readonly IDbHelper _dbHelper;

        public RepairOrderSparePartRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetSparePartsByRepairOrder(string repairOrderNumber)
        {
            string query = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSparePart]') AND type in (N'U'))
                    SELECT rosp.PartCode, sp.PartName, rosp.Quantity, rosp.UnitPrice
                    FROM RepairOrderSparePart rosp
                    INNER JOIN SpareParts sp ON rosp.PartCode = sp.PartCode
                    WHERE rosp.RepairOrderNumber = @orderNum
                ELSE
                    SELECT rosp.PartCode, sp.PartName, rosp.Quantity, rosp.UnitPrice
                    FROM RepairOrderSpareParts rosp
                    INNER JOIN SpareParts sp ON rosp.PartCode = sp.PartCode
                    WHERE rosp.RepairOrderNumber = @orderNum";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public void AddSparePartToOrder(string repairOrderNumber, string partCode, int quantity, decimal unitPrice)
        {
            string query = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSparePart]') AND type in (N'U'))
                BEGIN
                    IF EXISTS (SELECT 1 FROM RepairOrderSparePart WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode)
                    BEGIN
                        UPDATE RepairOrderSparePart 
                        SET Quantity = Quantity + @qty 
                        WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode
                    END
                    ELSE
                    BEGIN
                        INSERT INTO RepairOrderSparePart (RepairOrderNumber, PartCode, Quantity, UnitPrice) 
                        VALUES (@orderNum, @partCode, @qty, @price)
                    END
                END
                ELSE
                BEGIN
                    IF EXISTS (SELECT 1 FROM RepairOrderSpareParts WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode)
                    BEGIN
                        UPDATE RepairOrderSpareParts 
                        SET Quantity = Quantity + @qty 
                        WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode
                    END
                    ELSE
                    BEGIN
                        INSERT INTO RepairOrderSpareParts (RepairOrderNumber, PartCode, Quantity, UnitPrice) 
                        VALUES (@orderNum, @partCode, @qty, @price)
                    END
                END";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@partCode", partCode),
                new SqlParameter("@qty", quantity),
                new SqlParameter("@price", unitPrice)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void RemoveSparePartFromOrder(string repairOrderNumber, string partCode)
        {
            string query = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSparePart]') AND type in (N'U'))
                    DELETE FROM RepairOrderSparePart 
                    WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode
                ELSE
                    DELETE FROM RepairOrderSpareParts 
                    WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber),
                new SqlParameter("@partCode", partCode)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void ClearSparePartsForOrder(string repairOrderNumber)
        {
            string query = @"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSparePart]') AND type in (N'U'))
                    DELETE FROM RepairOrderSparePart WHERE RepairOrderNumber = @orderNum
                ELSE
                    DELETE FROM RepairOrderSpareParts WHERE RepairOrderNumber = @orderNum";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@orderNum", repairOrderNumber)
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void AddSparePartWithStockDeduction(string repairOrderNumber, string partCode, int quantity, decimal unitPrice)
        {
            _dbHelper.ExecuteTransaction((conn, tran) =>
            {
                // 1. Lock row and verify available stock
                string checkStockQuery = "SELECT QuantityInStock FROM SpareParts WITH (UPDLOCK, ROWLOCK) WHERE PartCode = @partCode";
                int currentStock = _dbHelper.ExecuteScalar<int>(checkStockQuery, new[] { new SqlParameter("@partCode", partCode) }, conn, tran);
                if (currentStock < quantity)
                {
                    throw new InvalidOperationException("الكمية المطلوبة (" + quantity + ") غير متوفرة في المخزون (المتوفر حالياً: " + currentStock + ")");
                }

                // 2. Deduct stock atomically
                string deductQuery = "UPDATE SpareParts SET QuantityInStock = QuantityInStock - @qty WHERE PartCode = @partCode";
                _dbHelper.ExecuteNonQuery(deductQuery, new[]
                {
                    new SqlParameter("@qty", quantity),
                    new SqlParameter("@partCode", partCode)
                }, conn, tran);

                // 3. Insert or update line item in RepairOrderSparePart
                string addItemQuery = @"
                    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSparePart]') AND type in (N'U'))
                    BEGIN
                        IF EXISTS (SELECT 1 FROM RepairOrderSparePart WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode)
                            UPDATE RepairOrderSparePart SET Quantity = Quantity + @qty WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode
                        ELSE
                            INSERT INTO RepairOrderSparePart (RepairOrderNumber, PartCode, Quantity, UnitPrice) VALUES (@orderNum, @partCode, @qty, @price)
                    END
                    ELSE
                    BEGIN
                        IF EXISTS (SELECT 1 FROM RepairOrderSpareParts WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode)
                            UPDATE RepairOrderSpareParts SET Quantity = Quantity + @qty WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode
                        ELSE
                            INSERT INTO RepairOrderSpareParts (RepairOrderNumber, PartCode, Quantity, UnitPrice) VALUES (@orderNum, @partCode, @qty, @price)
                    END";

                _dbHelper.ExecuteNonQuery(addItemQuery, new[]
                {
                    new SqlParameter("@orderNum", repairOrderNumber),
                    new SqlParameter("@partCode", partCode),
                    new SqlParameter("@qty", quantity),
                    new SqlParameter("@price", unitPrice)
                }, conn, tran);
            });
        }

        public void RemoveSparePartWithStockRestoration(string repairOrderNumber, string partCode, int quantity)
        {
            _dbHelper.ExecuteTransaction((conn, tran) =>
            {
                // 1. Remove from RepairOrderSparePart
                string deleteQuery = @"
                    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RepairOrderSparePart]') AND type in (N'U'))
                        DELETE FROM RepairOrderSparePart WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode
                    ELSE
                        DELETE FROM RepairOrderSpareParts WHERE RepairOrderNumber = @orderNum AND PartCode = @partCode";

                _dbHelper.ExecuteNonQuery(deleteQuery, new[]
                {
                    new SqlParameter("@orderNum", repairOrderNumber),
                    new SqlParameter("@partCode", partCode)
                }, conn, tran);

                // 2. Restore stock in SpareParts
                string restoreQuery = "UPDATE SpareParts SET QuantityInStock = QuantityInStock + @qty WHERE PartCode = @partCode";
                _dbHelper.ExecuteNonQuery(restoreQuery, new[]
                {
                    new SqlParameter("@qty", quantity),
                    new SqlParameter("@partCode", partCode)
                }, conn, tran);
            });
        }
    }
}
