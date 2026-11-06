using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using AutoCare.Application.Interfaces;
using AutoCare.Domain.Entities;
using AutoCare.Domain.Exceptions;

namespace AutoCare.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public CustomerRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<Customer> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return null;

            string sql = "SELECT TOP 1 Id, FullName, PhoneNumber, Email, Address, CreatedAt, IsDeleted, RowVersion FROM Customers WHERE PhoneNumber = @Phone AND IsDeleted = 0";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Phone", phoneNumber.Trim());
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapCustomer(reader);
                    }
                }
            }
            return null;
        }

        public async Task<Customer> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = "SELECT TOP 1 Id, FullName, PhoneNumber, Email, Address, CreatedAt, IsDeleted, RowVersion FROM Customers WHERE Id = @Id AND IsDeleted = 0";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapCustomer(reader);
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<Customer>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default)
        {
            var list = new List<Customer>();
            string sql = "SELECT Id, FullName, PhoneNumber, Email, Address, CreatedAt, IsDeleted, RowVersion FROM Customers WHERE IsDeleted = 0";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " AND (FullName LIKE @Search OR PhoneNumber LIKE @Search)";
            }
            sql += " ORDER BY FullName";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchTerm.Trim() + "%");
                }
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(MapCustomer(reader));
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            string sql = @"INSERT INTO Customers (FullName, PhoneNumber, Email, Address, CreatedAt, IsDeleted)
                           VALUES (@FullName, @PhoneNumber, @Email, @Address, @CreatedAt, 0);
                           SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@FullName", customer.FullName ?? "");
                cmd.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber ?? "");
                cmd.Parameters.AddWithValue("@Email", (object)customer.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object)customer.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", customer.CreatedAt == default ? DateTime.Now : customer.CreatedAt);

                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                customer.Id = Convert.ToInt32(res);
                return customer.Id;
            }
        }

        public async Task<bool> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            string sql = @"UPDATE Customers SET FullName = @FullName, PhoneNumber = @PhoneNumber, Email = @Email, Address = @Address
                           WHERE Id = @Id AND IsDeleted = 0";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", customer.Id);
                cmd.Parameters.AddWithValue("@FullName", customer.FullName ?? "");
                cmd.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber ?? "");
                cmd.Parameters.AddWithValue("@Email", (object)customer.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object)customer.Address ?? DBNull.Value);

                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        public async Task<bool> SoftDeleteAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE Customers SET IsDeleted = 1 WHERE PhoneNumber = @Phone";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Phone", phoneNumber.Trim());
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        private Customer MapCustomer(SqlDataReader r)
        {
            return new Customer
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                FullName = r.IsDBNull(r.GetOrdinal("FullName")) ? "" : r.GetString(r.GetOrdinal("FullName")),
                PhoneNumber = r.IsDBNull(r.GetOrdinal("PhoneNumber")) ? "" : r.GetString(r.GetOrdinal("PhoneNumber")),
                Email = r.IsDBNull(r.GetOrdinal("Email")) ? null : r.GetString(r.GetOrdinal("Email")),
                Address = r.IsDBNull(r.GetOrdinal("Address")) ? null : r.GetString(r.GetOrdinal("Address")),
                CreatedAt = r.IsDBNull(r.GetOrdinal("CreatedAt")) ? DateTime.Now : r.GetDateTime(r.GetOrdinal("CreatedAt")),
                IsDeleted = !r.IsDBNull(r.GetOrdinal("IsDeleted")) && r.GetBoolean(r.GetOrdinal("IsDeleted")),
                RowVersion = r.IsDBNull(r.GetOrdinal("RowVersion")) ? null : (byte[])r.GetValue(r.GetOrdinal("RowVersion"))
            };
        }
    }

    public class VehicleRepository : IVehicleRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public VehicleRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<Vehicle> GetByPlateAsync(string plateNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return null;

            string sql = "SELECT TOP 1 Id, PlateNumber, CarName, CarModel, Vin, CustomerPhone, CreatedAt, IsDeleted, RowVersion FROM Vehicles WHERE PlateNumber = @Plate AND IsDeleted = 0";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Plate", plateNumber.Trim());
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapVehicle(reader);
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<Vehicle>> GetByCustomerPhoneAsync(string phone, CancellationToken cancellationToken = default)
        {
            var list = new List<Vehicle>();
            string sql = "SELECT Id, PlateNumber, CarName, CarModel, Vin, CustomerPhone, CreatedAt, IsDeleted, RowVersion FROM Vehicles WHERE CustomerPhone = @Phone AND IsDeleted = 0";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Phone", phone.Trim());
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(MapVehicle(reader));
                    }
                }
            }
            return list;
        }

        public async Task<IReadOnlyList<Vehicle>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default)
        {
            var list = new List<Vehicle>();
            string sql = "SELECT Id, PlateNumber, CarName, CarModel, Vin, CustomerPhone, CreatedAt, IsDeleted, RowVersion FROM Vehicles WHERE IsDeleted = 0";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " AND (PlateNumber LIKE @Search OR CarName LIKE @Search OR CustomerPhone LIKE @Search)";
            }
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchTerm.Trim() + "%");
                }
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(MapVehicle(reader));
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
        {
            string sql = @"INSERT INTO Vehicles (PlateNumber, CarName, CarModel, Vin, CustomerPhone, CreatedAt, IsDeleted)
                           VALUES (@PlateNumber, @CarName, @CarModel, @Vin, @CustomerPhone, @CreatedAt, 0);
                           SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@PlateNumber", vehicle.PlateNumber ?? "");
                cmd.Parameters.AddWithValue("@CarName", (object)vehicle.CarName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CarModel", (object)vehicle.CarModel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Vin", (object)vehicle.Vin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerPhone", vehicle.CustomerPhone ?? "");
                cmd.Parameters.AddWithValue("@CreatedAt", vehicle.CreatedAt == default ? DateTime.Now : vehicle.CreatedAt);

                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                vehicle.Id = Convert.ToInt32(res);
                return vehicle.Id;
            }
        }

        public async Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
        {
            string sql = @"UPDATE Vehicles SET CarName = @CarName, CarModel = @CarModel, Vin = @Vin, CustomerPhone = @CustomerPhone
                           WHERE PlateNumber = @PlateNumber AND IsDeleted = 0";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@PlateNumber", vehicle.PlateNumber);
                cmd.Parameters.AddWithValue("@CarName", (object)vehicle.CarName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CarModel", (object)vehicle.CarModel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Vin", (object)vehicle.Vin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerPhone", vehicle.CustomerPhone ?? "");

                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        public async Task<bool> SoftDeleteAsync(string plateNumber, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE Vehicles SET IsDeleted = 1 WHERE PlateNumber = @Plate";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Plate", plateNumber.Trim());
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        private Vehicle MapVehicle(SqlDataReader r)
        {
            return new Vehicle
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                PlateNumber = r.IsDBNull(r.GetOrdinal("PlateNumber")) ? "" : r.GetString(r.GetOrdinal("PlateNumber")),
                CarName = r.IsDBNull(r.GetOrdinal("CarName")) ? null : r.GetString(r.GetOrdinal("CarName")),
                CarModel = r.IsDBNull(r.GetOrdinal("CarModel")) ? null : r.GetString(r.GetOrdinal("CarModel")),
                Vin = r.IsDBNull(r.GetOrdinal("Vin")) ? null : r.GetString(r.GetOrdinal("Vin")),
                CustomerPhone = r.IsDBNull(r.GetOrdinal("CustomerPhone")) ? "" : r.GetString(r.GetOrdinal("CustomerPhone")),
                CreatedAt = r.IsDBNull(r.GetOrdinal("CreatedAt")) ? DateTime.Now : r.GetDateTime(r.GetOrdinal("CreatedAt")),
                IsDeleted = !r.IsDBNull(r.GetOrdinal("IsDeleted")) && r.GetBoolean(r.GetOrdinal("IsDeleted")),
                RowVersion = r.IsDBNull(r.GetOrdinal("RowVersion")) ? null : (byte[])r.GetValue(r.GetOrdinal("RowVersion"))
            };
        }
    }

    public class SparePartRepository : ISparePartRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public SparePartRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<SparePartItem> GetByCodeAsync(string partNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partNumber)) return null;

            string sql = "SELECT TOP 1 Id, PartName, PartNumber, QuantityInStock, CostPrice, SellingPrice, WarrantyDays, MinStockLevel, IsDeleted, RowVersion FROM SpareParts WHERE PartNumber = @PartNumber AND IsDeleted = 0";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@PartNumber", partNumber.Trim());
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapSparePart(reader);
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<SparePartItem>> GetAllAsync(string searchTerm = "", bool lowStockOnly = false, CancellationToken cancellationToken = default)
        {
            var list = new List<SparePartItem>();
            string sql = "SELECT Id, PartName, PartNumber, QuantityInStock, CostPrice, SellingPrice, WarrantyDays, MinStockLevel, IsDeleted, RowVersion FROM SpareParts WHERE IsDeleted = 0";
            if (lowStockOnly)
            {
                sql += " AND QuantityInStock <= MinStockLevel";
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " AND (PartName LIKE @Search OR PartNumber LIKE @Search)";
            }
            sql += " ORDER BY PartName";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchTerm.Trim() + "%");
                }
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(MapSparePart(reader));
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(SparePartItem part, CancellationToken cancellationToken = default)
        {
            string sql = @"INSERT INTO SpareParts (PartName, PartNumber, QuantityInStock, CostPrice, SellingPrice, WarrantyDays, MinStockLevel, IsDeleted)
                           VALUES (@PartName, @PartNumber, @QuantityInStock, @CostPrice, @SellingPrice, @WarrantyDays, @MinStockLevel, 0);
                           SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@PartName", part.PartName ?? "");
                cmd.Parameters.AddWithValue("@PartNumber", part.PartNumber ?? "");
                cmd.Parameters.AddWithValue("@QuantityInStock", part.QuantityInStock);
                cmd.Parameters.AddWithValue("@CostPrice", part.CostPrice);
                cmd.Parameters.AddWithValue("@SellingPrice", part.SellingPrice);
                cmd.Parameters.AddWithValue("@WarrantyDays", part.WarrantyDays);
                cmd.Parameters.AddWithValue("@MinStockLevel", part.MinStockLevel);

                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                part.Id = Convert.ToInt32(res);
                return part.Id;
            }
        }

        public async Task<bool> UpdateAsync(SparePartItem part, CancellationToken cancellationToken = default)
        {
            string sql = @"UPDATE SpareParts SET PartName = @PartName, QuantityInStock = @QuantityInStock, CostPrice = @CostPrice, SellingPrice = @SellingPrice, WarrantyDays = @WarrantyDays, MinStockLevel = @MinStockLevel
                           WHERE PartNumber = @PartNumber AND IsDeleted = 0";

            if (part.RowVersion != null && part.RowVersion.Length > 0)
            {
                sql += " AND RowVersion = @RowVersion";
            }

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@PartName", part.PartName ?? "");
                cmd.Parameters.AddWithValue("@PartNumber", part.PartNumber ?? "");
                cmd.Parameters.AddWithValue("@QuantityInStock", part.QuantityInStock);
                cmd.Parameters.AddWithValue("@CostPrice", part.CostPrice);
                cmd.Parameters.AddWithValue("@SellingPrice", part.SellingPrice);
                cmd.Parameters.AddWithValue("@WarrantyDays", part.WarrantyDays);
                cmd.Parameters.AddWithValue("@MinStockLevel", part.MinStockLevel);

                if (part.RowVersion != null && part.RowVersion.Length > 0)
                {
                    cmd.Parameters.AddWithValue("@RowVersion", part.RowVersion);
                }

                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                if (rows == 0 && part.RowVersion != null)
                {
                    throw new OptimisticConcurrencyException("SpareParts", part.PartNumber);
                }
                return rows > 0;
            }
        }

        public async Task<bool> DeductStockAtomicAsync(string partNumber, int quantity, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partNumber) || quantity <= 0) return false;

            // Atomic decrement guaranteeing stock never drops negative
            string sql = @"UPDATE SpareParts 
                           SET QuantityInStock = QuantityInStock - @Qty 
                           WHERE PartNumber = @PartNumber AND QuantityInStock >= @Qty AND IsDeleted = 0";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Qty", quantity);
                cmd.Parameters.AddWithValue("@PartNumber", partNumber.Trim());

                int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return affected > 0;
            }
        }

        public async Task<bool> SoftDeleteAsync(string partNumber, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE SpareParts SET IsDeleted = 1 WHERE PartNumber = @PartNumber";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@PartNumber", partNumber.Trim());
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        private SparePartItem MapSparePart(SqlDataReader r)
        {
            return new SparePartItem
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                PartName = r.IsDBNull(r.GetOrdinal("PartName")) ? "" : r.GetString(r.GetOrdinal("PartName")),
                PartNumber = r.IsDBNull(r.GetOrdinal("PartNumber")) ? "" : r.GetString(r.GetOrdinal("PartNumber")),
                QuantityInStock = r.IsDBNull(r.GetOrdinal("QuantityInStock")) ? 0 : r.GetInt32(r.GetOrdinal("QuantityInStock")),
                CostPrice = r.IsDBNull(r.GetOrdinal("CostPrice")) ? 0m : r.GetDecimal(r.GetOrdinal("CostPrice")),
                SellingPrice = r.IsDBNull(r.GetOrdinal("SellingPrice")) ? 0m : r.GetDecimal(r.GetOrdinal("SellingPrice")),
                WarrantyDays = r.IsDBNull(r.GetOrdinal("WarrantyDays")) ? 0 : r.GetInt32(r.GetOrdinal("WarrantyDays")),
                MinStockLevel = r.IsDBNull(r.GetOrdinal("MinStockLevel")) ? 5 : r.GetInt32(r.GetOrdinal("MinStockLevel")),
                IsDeleted = !r.IsDBNull(r.GetOrdinal("IsDeleted")) && r.GetBoolean(r.GetOrdinal("IsDeleted")),
                RowVersion = r.IsDBNull(r.GetOrdinal("RowVersion")) ? null : (byte[])r.GetValue(r.GetOrdinal("RowVersion"))
            };
        }
    }

    public class RepairOrderRepository : IRepairOrderRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public RepairOrderRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<RepairOrder> GetByNumberAsync(string roNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roNumber)) return null;

            string sql = "SELECT TOP 1 Id, RepairOrderNumber, PlateNumber, EntryDate, ExpectedDate, CompletionDate, Status, Receptionist, Complaint, Mileage, FuelLevel, InspectionNotes, IsDeleted, RowVersion FROM RepairOrders WHERE RepairOrderNumber = @RoNum AND IsDeleted = 0";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@RoNum", roNumber.Trim());
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapRepairOrder(reader);
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<RepairOrder>> GetAllAsync(string searchTerm = "", string statusFilter = "", CancellationToken cancellationToken = default)
        {
            var list = new List<RepairOrder>();
            string sql = "SELECT Id, RepairOrderNumber, PlateNumber, EntryDate, ExpectedDate, CompletionDate, Status, Receptionist, Complaint, Mileage, FuelLevel, InspectionNotes, IsDeleted, RowVersion FROM RepairOrders WHERE IsDeleted = 0";
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "الكل")
            {
                sql += " AND Status = @Status";
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " AND (RepairOrderNumber LIKE @Search OR PlateNumber LIKE @Search)";
            }
            sql += " ORDER BY EntryDate DESC";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "الكل")
                {
                    cmd.Parameters.AddWithValue("@Status", statusFilter.Trim());
                }
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchTerm.Trim() + "%");
                }
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(MapRepairOrder(reader));
                    }
                }
            }
            return list;
        }

        public async Task<string> GenerateNewOrderNumberAsync(CancellationToken cancellationToken = default)
        {
            string datePrefix = DateTime.Now.ToString("yyyyMMdd");
            string sql = "SELECT COUNT(1) FROM RepairOrders WHERE RepairOrderNumber LIKE @Prefix";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Prefix", "RO-" + datePrefix + "-%");
                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                int count = Convert.ToInt32(res) + 1;
                return $"RO-{datePrefix}-{count:D4}";
            }
        }

        public async Task<int> AddAsync(RepairOrder order, CancellationToken cancellationToken = default)
        {
            string sql = @"INSERT INTO RepairOrders (RepairOrderNumber, PlateNumber, EntryDate, Status, Receptionist, Complaint, Mileage, FuelLevel, InspectionNotes, IsDeleted)
                           VALUES (@RoNum, @Plate, @EntryDate, @Status, @Receptionist, @Complaint, @Mileage, @FuelLevel, @InspectionNotes, 0);
                           SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@RoNum", order.RepairOrderNumber ?? "");
                cmd.Parameters.AddWithValue("@Plate", order.PlateNumber ?? "");
                cmd.Parameters.AddWithValue("@EntryDate", order.EntryDate == default ? DateTime.Now : order.EntryDate);
                cmd.Parameters.AddWithValue("@Status", order.Status ?? "معلق");
                cmd.Parameters.AddWithValue("@Receptionist", (object)order.Receptionist ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Complaint", (object)order.Complaint ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Mileage", (object)order.Mileage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FuelLevel", (object)order.FuelLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@InspectionNotes", (object)order.InspectionNotes ?? DBNull.Value);

                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                order.Id = Convert.ToInt32(res);
                return order.Id;
            }
        }

        public async Task<bool> UpdateStatusAsync(string roNumber, string status, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE RepairOrders SET Status = @Status WHERE RepairOrderNumber = @RoNum AND IsDeleted = 0";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@RoNum", roNumber.Trim());
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        public async Task<bool> UpdateAsync(RepairOrder order, CancellationToken cancellationToken = default)
        {
            string sql = @"UPDATE RepairOrders SET Complaint = @Complaint, Mileage = @Mileage, FuelLevel = @FuelLevel, InspectionNotes = @InspectionNotes
                           WHERE RepairOrderNumber = @RoNum AND IsDeleted = 0";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@RoNum", order.RepairOrderNumber);
                cmd.Parameters.AddWithValue("@Complaint", (object)order.Complaint ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Mileage", (object)order.Mileage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FuelLevel", (object)order.FuelLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@InspectionNotes", (object)order.InspectionNotes ?? DBNull.Value);

                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        public async Task<bool> SoftDeleteAsync(string roNumber, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE RepairOrders SET IsDeleted = 1 WHERE RepairOrderNumber = @RoNum";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@RoNum", roNumber.Trim());
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        public async Task<bool> HasActiveOrderForPlateAsync(string plateNumber, CancellationToken cancellationToken = default)
        {
            string sql = "SELECT COUNT(1) FROM RepairOrders WHERE PlateNumber = @Plate AND Status NOT IN ('مكتمل', 'ملغي', 'تمت الفوترة', 'Completed', 'Cancelled', 'Invoiced') AND IsDeleted = 0";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Plate", plateNumber.Trim());
                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(res) > 0;
            }
        }

        private RepairOrder MapRepairOrder(SqlDataReader r)
        {
            return new RepairOrder
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                RepairOrderNumber = r.IsDBNull(r.GetOrdinal("RepairOrderNumber")) ? "" : r.GetString(r.GetOrdinal("RepairOrderNumber")),
                PlateNumber = r.IsDBNull(r.GetOrdinal("PlateNumber")) ? "" : r.GetString(r.GetOrdinal("PlateNumber")),
                EntryDate = r.IsDBNull(r.GetOrdinal("EntryDate")) ? DateTime.Now : r.GetDateTime(r.GetOrdinal("EntryDate")),
                ExpectedDate = r.IsDBNull(r.GetOrdinal("ExpectedDate")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("ExpectedDate")),
                CompletionDate = r.IsDBNull(r.GetOrdinal("CompletionDate")) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal("CompletionDate")),
                Status = r.IsDBNull(r.GetOrdinal("Status")) ? "معلق" : r.GetString(r.GetOrdinal("Status")),
                Receptionist = r.IsDBNull(r.GetOrdinal("Receptionist")) ? null : r.GetString(r.GetOrdinal("Receptionist")),
                Complaint = r.IsDBNull(r.GetOrdinal("Complaint")) ? null : r.GetString(r.GetOrdinal("Complaint")),
                Mileage = r.IsDBNull(r.GetOrdinal("Mileage")) ? null : (int?)r.GetInt32(r.GetOrdinal("Mileage")),
                FuelLevel = r.IsDBNull(r.GetOrdinal("FuelLevel")) ? null : r.GetString(r.GetOrdinal("FuelLevel")),
                InspectionNotes = r.IsDBNull(r.GetOrdinal("InspectionNotes")) ? null : r.GetString(r.GetOrdinal("InspectionNotes")),
                IsDeleted = !r.IsDBNull(r.GetOrdinal("IsDeleted")) && r.GetBoolean(r.GetOrdinal("IsDeleted")),
                RowVersion = r.IsDBNull(r.GetOrdinal("RowVersion")) ? null : (byte[])r.GetValue(r.GetOrdinal("RowVersion"))
            };
        }
    }

    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public InvoiceRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<Invoice> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(invoiceNumber)) return null;

            string sql = "SELECT TOP 1 Id, InvoiceNumber, RepairOrderNumber, InvoiceDate, ServicesTotal, SparePartsTotal, DiscountAmount, TaxRate, TaxAmount, TotalAmount, PaidAmount, RemainingAmount, PaymentMethod, CashierName, RowVersion FROM Invoices WHERE InvoiceNumber = @InvNum";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@InvNum", invoiceNumber.Trim());
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapInvoice(reader);
                    }
                }
            }
            return null;
        }

        public async Task<Invoice> GetByOrderNumberAsync(string roNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roNumber)) return null;

            string sql = "SELECT TOP 1 Id, InvoiceNumber, RepairOrderNumber, InvoiceDate, ServicesTotal, SparePartsTotal, DiscountAmount, TaxRate, TaxAmount, TotalAmount, PaidAmount, RemainingAmount, PaymentMethod, CashierName, RowVersion FROM Invoices WHERE RepairOrderNumber = @RoNum";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@RoNum", roNumber.Trim());
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapInvoice(reader);
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<Invoice>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default)
        {
            var list = new List<Invoice>();
            string sql = "SELECT Id, InvoiceNumber, RepairOrderNumber, InvoiceDate, ServicesTotal, SparePartsTotal, DiscountAmount, TaxRate, TaxAmount, TotalAmount, PaidAmount, RemainingAmount, PaymentMethod, CashierName, RowVersion FROM Invoices";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " WHERE InvoiceNumber LIKE @Search OR RepairOrderNumber LIKE @Search";
            }
            sql += " ORDER BY InvoiceDate DESC";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchTerm.Trim() + "%");
                }
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(MapInvoice(reader));
                    }
                }
            }
            return list;
        }

        public async Task<string> GenerateNewInvoiceNumberAsync(CancellationToken cancellationToken = default)
        {
            string datePrefix = DateTime.Now.ToString("yyyyMMdd");
            string sql = "SELECT COUNT(1) FROM Invoices WHERE InvoiceNumber LIKE @Prefix";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Prefix", "INV-" + datePrefix + "-%");
                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                int count = Convert.ToInt32(res) + 1;
                return $"INV-{datePrefix}-{count:D4}";
            }
        }

        public async Task<int> AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            string sql = @"INSERT INTO Invoices (InvoiceNumber, RepairOrderNumber, InvoiceDate, ServicesTotal, SparePartsTotal, DiscountAmount, TaxRate, TaxAmount, TotalAmount, PaidAmount, RemainingAmount, PaymentMethod, CashierName)
                           VALUES (@InvNum, @RoNum, @InvDate, @ServicesTotal, @SparePartsTotal, @DiscountAmount, @TaxRate, @TaxAmount, @TotalAmount, @PaidAmount, @RemainingAmount, @PaymentMethod, @CashierName);
                           SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@InvNum", invoice.InvoiceNumber ?? "");
                cmd.Parameters.AddWithValue("@RoNum", invoice.RepairOrderNumber ?? "");
                cmd.Parameters.AddWithValue("@InvDate", invoice.InvoiceDate == default ? DateTime.Now : invoice.InvoiceDate);
                cmd.Parameters.AddWithValue("@ServicesTotal", invoice.ServicesTotal);
                cmd.Parameters.AddWithValue("@SparePartsTotal", invoice.SparePartsTotal);
                cmd.Parameters.AddWithValue("@DiscountAmount", invoice.DiscountAmount);
                cmd.Parameters.AddWithValue("@TaxRate", invoice.TaxRate);
                cmd.Parameters.AddWithValue("@TaxAmount", invoice.TaxAmount);
                cmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
                cmd.Parameters.AddWithValue("@PaidAmount", invoice.PaidAmount);
                cmd.Parameters.AddWithValue("@RemainingAmount", invoice.RemainingAmount);
                cmd.Parameters.AddWithValue("@PaymentMethod", invoice.PaymentMethod ?? "Cash");
                cmd.Parameters.AddWithValue("@CashierName", (object)invoice.CashierName ?? DBNull.Value);

                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                invoice.Id = Convert.ToInt32(res);
                return invoice.Id;
            }
        }

        private Invoice MapInvoice(SqlDataReader r)
        {
            return new Invoice
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                InvoiceNumber = r.IsDBNull(r.GetOrdinal("InvoiceNumber")) ? "" : r.GetString(r.GetOrdinal("InvoiceNumber")),
                RepairOrderNumber = r.IsDBNull(r.GetOrdinal("RepairOrderNumber")) ? "" : r.GetString(r.GetOrdinal("RepairOrderNumber")),
                InvoiceDate = r.IsDBNull(r.GetOrdinal("InvoiceDate")) ? DateTime.Now : r.GetDateTime(r.GetOrdinal("InvoiceDate")),
                ServicesTotal = r.IsDBNull(r.GetOrdinal("ServicesTotal")) ? 0m : r.GetDecimal(r.GetOrdinal("ServicesTotal")),
                SparePartsTotal = r.IsDBNull(r.GetOrdinal("SparePartsTotal")) ? 0m : r.GetDecimal(r.GetOrdinal("SparePartsTotal")),
                DiscountAmount = r.IsDBNull(r.GetOrdinal("DiscountAmount")) ? 0m : r.GetDecimal(r.GetOrdinal("DiscountAmount")),
                TaxRate = r.IsDBNull(r.GetOrdinal("TaxRate")) ? 0m : r.GetDecimal(r.GetOrdinal("TaxRate")),
                TaxAmount = r.IsDBNull(r.GetOrdinal("TaxAmount")) ? 0m : r.GetDecimal(r.GetOrdinal("TaxAmount")),
                TotalAmount = r.IsDBNull(r.GetOrdinal("TotalAmount")) ? 0m : r.GetDecimal(r.GetOrdinal("TotalAmount")),
                PaidAmount = r.IsDBNull(r.GetOrdinal("PaidAmount")) ? 0m : r.GetDecimal(r.GetOrdinal("PaidAmount")),
                RemainingAmount = r.IsDBNull(r.GetOrdinal("RemainingAmount")) ? 0m : r.GetDecimal(r.GetOrdinal("RemainingAmount")),
                PaymentMethod = r.IsDBNull(r.GetOrdinal("PaymentMethod")) ? "Cash" : r.GetString(r.GetOrdinal("PaymentMethod")),
                CashierName = r.IsDBNull(r.GetOrdinal("CashierName")) ? null : r.GetString(r.GetOrdinal("CashierName")),
                RowVersion = r.IsDBNull(r.GetOrdinal("RowVersion")) ? null : (byte[])r.GetValue(r.GetOrdinal("RowVersion"))
            };
        }
    }

    public class ServiceRepository : IServiceRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public ServiceRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<ServiceItem> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = "SELECT TOP 1 Id, ServiceName, Price, WarrantyDays, IsActive FROM Services WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return new ServiceItem
                        {
                            Id = reader.GetInt32(0),
                            ServiceName = reader.GetString(1),
                            Price = reader.GetDecimal(2),
                            WarrantyDays = reader.GetInt32(3),
                            IsActive = reader.GetBoolean(4)
                        };
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<ServiceItem>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default)
        {
            var list = new List<ServiceItem>();
            string sql = "SELECT Id, ServiceName, Price, WarrantyDays, IsActive FROM Services WHERE IsActive = 1";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " AND ServiceName LIKE @Search";
            }
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchTerm.Trim() + "%");
                }
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(new ServiceItem
                        {
                            Id = reader.GetInt32(0),
                            ServiceName = reader.GetString(1),
                            Price = reader.GetDecimal(2),
                            WarrantyDays = reader.GetInt32(3),
                            IsActive = reader.GetBoolean(4)
                        });
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(ServiceItem service, CancellationToken cancellationToken = default)
        {
            string sql = "INSERT INTO Services (ServiceName, Price, WarrantyDays, IsActive) VALUES (@Name, @Price, @Warranty, @IsActive); SELECT SCOPE_IDENTITY();";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Name", service.ServiceName ?? "");
                cmd.Parameters.AddWithValue("@Price", service.Price);
                cmd.Parameters.AddWithValue("@Warranty", service.WarrantyDays);
                cmd.Parameters.AddWithValue("@IsActive", service.IsActive);
                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                service.Id = Convert.ToInt32(res);
                return service.Id;
            }
        }

        public async Task<bool> UpdateAsync(ServiceItem service, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE Services SET ServiceName = @Name, Price = @Price, WarrantyDays = @Warranty, IsActive = @IsActive WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", service.Id);
                cmd.Parameters.AddWithValue("@Name", service.ServiceName ?? "");
                cmd.Parameters.AddWithValue("@Price", service.Price);
                cmd.Parameters.AddWithValue("@Warranty", service.WarrantyDays);
                cmd.Parameters.AddWithValue("@IsActive", service.IsActive);
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE Services SET IsActive = 0 WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }
    }

    public class TechnicianRepository : ITechnicianRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public TechnicianRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<Technician> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = "SELECT TOP 1 Id, FullName, PhoneNumber, Specialization, CommissionRate, IsActive FROM Technicians WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return new Technician
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            PhoneNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Specialization = reader.IsDBNull(3) ? null : reader.GetString(3),
                            CommissionRate = reader.GetDecimal(4),
                            IsActive = reader.GetBoolean(5)
                        };
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<Technician>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
        {
            var list = new List<Technician>();
            string sql = "SELECT Id, FullName, PhoneNumber, Specialization, CommissionRate, IsActive FROM Technicians";
            if (activeOnly) sql += " WHERE IsActive = 1";
            sql += " ORDER BY FullName";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(new Technician
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            PhoneNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Specialization = reader.IsDBNull(3) ? null : reader.GetString(3),
                            CommissionRate = reader.GetDecimal(4),
                            IsActive = reader.GetBoolean(5)
                        });
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(Technician technician, CancellationToken cancellationToken = default)
        {
            string sql = "INSERT INTO Technicians (FullName, PhoneNumber, Specialization, CommissionRate, IsActive) VALUES (@Name, @Phone, @Spec, @Comm, @IsActive); SELECT SCOPE_IDENTITY();";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Name", technician.FullName ?? "");
                cmd.Parameters.AddWithValue("@Phone", (object)technician.PhoneNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Spec", (object)technician.Specialization ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Comm", technician.CommissionRate);
                cmd.Parameters.AddWithValue("@IsActive", technician.IsActive);
                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                technician.Id = Convert.ToInt32(res);
                return technician.Id;
            }
        }

        public async Task<bool> UpdateAsync(Technician technician, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE Technicians SET FullName = @Name, PhoneNumber = @Phone, Specialization = @Spec, CommissionRate = @Comm, IsActive = @IsActive WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", technician.Id);
                cmd.Parameters.AddWithValue("@Name", technician.FullName ?? "");
                cmd.Parameters.AddWithValue("@Phone", (object)technician.PhoneNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Spec", (object)technician.Specialization ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Comm", technician.CommissionRate);
                cmd.Parameters.AddWithValue("@IsActive", technician.IsActive);
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }
    }

    public class SupplierRepository : ISupplierRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public SupplierRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<Supplier> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = "SELECT TOP 1 Id, SupplierName, ContactPerson, PhoneNumber, Address, Balance, CreatedAt FROM Suppliers WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return new Supplier
                        {
                            Id = reader.GetInt32(0),
                            SupplierName = reader.GetString(1),
                            ContactPerson = reader.IsDBNull(2) ? null : reader.GetString(2),
                            PhoneNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Balance = reader.GetDecimal(5),
                            CreatedAt = reader.GetDateTime(6)
                        };
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<Supplier>> GetAllAsync(string searchTerm = "", CancellationToken cancellationToken = default)
        {
            var list = new List<Supplier>();
            string sql = "SELECT Id, SupplierName, ContactPerson, PhoneNumber, Address, Balance, CreatedAt FROM Suppliers";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += " WHERE SupplierName LIKE @Search OR PhoneNumber LIKE @Search";
            }
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchTerm.Trim() + "%");
                }
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(new Supplier
                        {
                            Id = reader.GetInt32(0),
                            SupplierName = reader.GetString(1),
                            ContactPerson = reader.IsDBNull(2) ? null : reader.GetString(2),
                            PhoneNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Balance = reader.GetDecimal(5),
                            CreatedAt = reader.GetDateTime(6)
                        });
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            string sql = "INSERT INTO Suppliers (SupplierName, ContactPerson, PhoneNumber, Address, Balance, CreatedAt) VALUES (@Name, @Contact, @Phone, @Address, @Balance, @CreatedAt); SELECT SCOPE_IDENTITY();";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Name", supplier.SupplierName ?? "");
                cmd.Parameters.AddWithValue("@Contact", (object)supplier.ContactPerson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", (object)supplier.PhoneNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object)supplier.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Balance", supplier.Balance);
                cmd.Parameters.AddWithValue("@CreatedAt", supplier.CreatedAt == default ? DateTime.Now : supplier.CreatedAt);
                object res = await cmd.ExecuteScalarAsync(cancellationToken);
                supplier.Id = Convert.ToInt32(res);
                return supplier.Id;
            }
        }

        public async Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE Suppliers SET SupplierName = @Name, ContactPerson = @Contact, PhoneNumber = @Phone, Address = @Address, Balance = @Balance WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", supplier.Id);
                cmd.Parameters.AddWithValue("@Name", supplier.SupplierName ?? "");
                cmd.Parameters.AddWithValue("@Contact", (object)supplier.ContactPerson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", (object)supplier.PhoneNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object)supplier.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Balance", supplier.Balance);
                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }
    }

    public class AuditRepository : IAuditRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public AuditRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null) return;
            string sql = @"INSERT INTO AuditLogs (Timestamp, Username, Action, TargetTable, TargetId, Details, IpAddress)
                           VALUES (@Time, @User, @Action, @Table, @TargetId, @Details, @IP);";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Time", entry.Timestamp == default ? DateTime.Now : entry.Timestamp);
                cmd.Parameters.AddWithValue("@User", (object)entry.Username ?? "System");
                cmd.Parameters.AddWithValue("@Action", (object)entry.Action ?? "Action");
                cmd.Parameters.AddWithValue("@Table", (object)entry.TargetTable ?? "");
                cmd.Parameters.AddWithValue("@TargetId", (object)entry.TargetId ?? "");
                cmd.Parameters.AddWithValue("@Details", (object)entry.Details ?? "");
                cmd.Parameters.AddWithValue("@IP", (object)entry.IpAddress ?? "127.0.0.1");

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task<IReadOnlyList<AuditLogEntry>> GetLogsAsync(DateTime? from, DateTime? to, string action, string search, CancellationToken cancellationToken = default)
        {
            var list = new List<AuditLogEntry>();
            string sql = "SELECT Id, Timestamp, Username, Action, TargetTable, TargetId, Details, IpAddress FROM AuditLogs WHERE 1=1";
            if (from.HasValue) sql += " AND Timestamp >= @From";
            if (to.HasValue) sql += " AND Timestamp <= @To";
            if (!string.IsNullOrWhiteSpace(action) && action != "الكل") sql += " AND Action = @Action";
            if (!string.IsNullOrWhiteSpace(search)) sql += " AND (Details LIKE @Search OR Username LIKE @Search)";
            sql += " ORDER BY Timestamp DESC";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                if (from.HasValue) cmd.Parameters.AddWithValue("@From", from.Value);
                if (to.HasValue) cmd.Parameters.AddWithValue("@To", to.Value);
                if (!string.IsNullOrWhiteSpace(action) && action != "الكل") cmd.Parameters.AddWithValue("@Action", action.Trim());
                if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@Search", "%" + search.Trim() + "%");

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(new AuditLogEntry
                        {
                            Id = reader.GetInt32(0),
                            Timestamp = reader.GetDateTime(1),
                            Username = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Action = reader.IsDBNull(3) ? null : reader.GetString(3),
                            TargetTable = reader.IsDBNull(4) ? null : reader.GetString(4),
                            TargetId = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Details = reader.IsDBNull(6) ? null : reader.GetString(6),
                            IpAddress = reader.IsDBNull(7) ? null : reader.GetString(7)
                        });
                    }
                }
            }
            return list;
        }
    }

    public class NotificationRepository : INotificationRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public NotificationRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<NotificationItem> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = "SELECT TOP 1 Id, Title, Message, Severity, Category, TargetReferenceId, IsRead, CreatedAt, BranchId FROM Notifications WHERE Id = @Id";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            return MapNotification(reader);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task<IReadOnlyList<NotificationItem>> GetRecentAsync(int top = 25, CancellationToken cancellationToken = default)
        {
            var list = new List<NotificationItem>();
            string sql = $"SELECT TOP {top} Id, Title, Message, Severity, Category, TargetReferenceId, IsRead, CreatedAt, BranchId FROM Notifications WHERE IsRead = 0 ORDER BY CreatedAt DESC";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            list.Add(MapNotification(reader));
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        {
            string sql = "SELECT COUNT(1) FROM Notifications WHERE IsRead = 0";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    var result = await cmd.ExecuteScalarAsync(cancellationToken);
                    return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
            catch { return 0; }
        }

        public async Task<int> AddAsync(NotificationItem notification, CancellationToken cancellationToken = default)
        {
            if (notification == null) return 0;
            string sql = @"INSERT INTO Notifications (Title, Message, Severity, Category, TargetReferenceId, IsRead, CreatedAt, BranchId)
                           OUTPUT INSERTED.Id
                           VALUES (@Title, @Message, @Severity, @Category, @TargetId, @IsRead, @CreatedAt, @BranchId)";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    cmd.Parameters.AddWithValue("@Title", notification.Title ?? "");
                    cmd.Parameters.AddWithValue("@Message", notification.Message ?? "");
                    cmd.Parameters.AddWithValue("@Severity", notification.Severity.ToString());
                    cmd.Parameters.AddWithValue("@Category", notification.Category ?? "General");
                    cmd.Parameters.AddWithValue("@TargetId", (object)notification.TargetReferenceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsRead", notification.IsRead);
                    cmd.Parameters.AddWithValue("@CreatedAt", notification.CreatedAt == default ? DateTime.Now : notification.CreatedAt);
                    cmd.Parameters.AddWithValue("@BranchId", (object)notification.BranchId ?? DBNull.Value);

                    var id = await cmd.ExecuteScalarAsync(cancellationToken);
                    return id != null ? Convert.ToInt32(id) : 0;
                }
            }
            catch { return 0; }
        }

        public async Task<bool> MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE Notifications SET IsRead = 1 WHERE Id = @Id";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return (await cmd.ExecuteNonQueryAsync(cancellationToken)) > 0;
                }
            }
            catch { return false; }
        }

        public async Task<bool> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
        {
            string sql = "UPDATE Notifications SET IsRead = 1 WHERE IsRead = 0";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                    return true;
                }
            }
            catch { return false; }
        }

        public async Task GenerateLowStockAlertsAsync(CancellationToken cancellationToken = default)
        {
            string sql = @"
                UPDATE n SET n.IsRead = 0, n.CreatedAt = GETDATE()
                FROM Notifications n
                INNER JOIN SpareParts p ON n.TargetReferenceId = p.PartCode AND n.Category = 'Inventory'
                WHERE p.QuantityInStock <= p.MinimumStock
                  AND n.IsRead = 1
                  AND n.CreatedAt <= DATEADD(hour, -6, GETDATE());

                INSERT INTO Notifications (Title, Message, Severity, Category, TargetReferenceId, IsRead, CreatedAt)
                SELECT 
                    N'تنبيه مخزون منخفض: ' + p.PartName,
                    N'الكمية الحالية (' + CAST(p.QuantityInStock AS NVARCHAR(10)) + N') أقل من أو تساوي الحد الأدنى (' + CAST(p.MinimumStock AS NVARCHAR(10)) + N') للقطعة: ' + p.PartCode,
                    'Warning',
                    'Inventory',
                    p.PartCode,
                    0,
                    GETDATE()
                FROM SpareParts p
                WHERE p.QuantityInStock <= p.MinimumStock
                  AND NOT EXISTS (
                      SELECT 1 FROM Notifications n 
                      WHERE n.TargetReferenceId = p.PartCode 
                        AND n.Category = 'Inventory' 
                        AND n.CreatedAt >= DATEADD(hour, -6, GETDATE())
                  );";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch { }
        }

        public async Task GeneratePendingOrdersAlertsAsync(CancellationToken cancellationToken = default)
        {
            string sql = @"
                UPDATE n SET n.IsRead = 0, n.CreatedAt = GETDATE()
                FROM Notifications n
                INNER JOIN RepairOrders r ON n.TargetReferenceId = r.RepairOrderNumber AND n.Category = 'RepairOrder'
                WHERE r.Status IN ('Pending', 'قيد الانتظار', 'معلق', 'InProgress', 'قيد التنفيذ')
                  AND n.IsRead = 1
                  AND n.CreatedAt <= DATEADD(hour, -6, GETDATE());

                INSERT INTO Notifications (Title, Message, Severity, Category, TargetReferenceId, IsRead, CreatedAt)
                SELECT 
                    N'كرت صيانة متأخر: ' + r.RepairOrderNumber,
                    N'أمر الشغل للسيارة ذات اللوحة ' + r.PlateNumber + N' معلق منذ تاريخ ' + CONVERT(NVARCHAR(10), r.EntryDate, 120),
                    'Critical',
                    'RepairOrder',
                    r.RepairOrderNumber,
                    0,
                    GETDATE()
                FROM RepairOrders r
                WHERE r.Status IN ('Pending', 'قيد الانتظار', 'معلق', 'InProgress', 'قيد التنفيذ')
                  AND r.EntryDate <= DATEADD(day, -2, GETDATE())
                  AND NOT EXISTS (
                      SELECT 1 FROM Notifications n 
                      WHERE n.TargetReferenceId = r.RepairOrderNumber 
                        AND n.Category = 'RepairOrder' 
                        AND n.CreatedAt >= DATEADD(hour, -6, GETDATE())
                  );";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch { }
        }

        public async Task GenerateDeliveryDueAlertsAsync(CancellationToken cancellationToken = default)
        {
            string sql = @"
                UPDATE n SET n.IsRead = 0, n.CreatedAt = GETDATE()
                FROM Notifications n
                INNER JOIN RepairOrders r ON n.TargetReferenceId = r.RepairOrderNumber AND n.Category = 'Customer'
                WHERE r.ExpectedCompletionDate IS NOT NULL
                  AND r.Status NOT IN ('Completed', 'Canceled', 'مكتمل', 'ملغي', 'تم التسليم')
                  AND n.IsRead = 1
                  AND n.CreatedAt <= DATEADD(hour, -6, GETDATE());

                INSERT INTO Notifications (Title, Message, Severity, Category, TargetReferenceId, IsRead, CreatedAt)
                SELECT 
                    N'موعد تسليم سيارة: ' + r.PlateNumber,
                    N'موعد تسليم كرت الصيانة ' + r.RepairOrderNumber + N' محدد بتاريخ ' + CONVERT(NVARCHAR(10), r.ExpectedCompletionDate, 120),
                    'Warning',
                    'Customer',
                    r.RepairOrderNumber,
                    0,
                    GETDATE()
                FROM RepairOrders r
                WHERE r.ExpectedCompletionDate IS NOT NULL
                  AND r.ExpectedCompletionDate <= DATEADD(day, 1, GETDATE())
                  AND r.Status NOT IN ('Completed', 'Canceled', 'مكتمل', 'ملغي', 'تم التسليم')
                  AND NOT EXISTS (
                      SELECT 1 FROM Notifications n 
                      WHERE n.TargetReferenceId = r.RepairOrderNumber 
                        AND n.Category = 'Customer' 
                        AND n.CreatedAt >= DATEADD(hour, -6, GETDATE())
                  );";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch { }
        }

        public async Task GenerateUnpaidInvoicesAlertsAsync(CancellationToken cancellationToken = default)
        {
            string sql = @"
                UPDATE n SET n.IsRead = 0, n.CreatedAt = GETDATE()
                FROM Notifications n
                INNER JOIN Invoices i ON n.TargetReferenceId = i.InvoiceNumber AND n.Category = 'Invoice'
                WHERE i.IsPaid = 0
                  AND n.IsRead = 1
                  AND n.CreatedAt <= DATEADD(hour, -6, GETDATE());

                INSERT INTO Notifications (Title, Message, Severity, Category, TargetReferenceId, IsRead, CreatedAt)
                SELECT 
                    N'فاتورة غير مسددة: ' + i.InvoiceNumber,
                    N'الفاتورة الخاصة بكرت الصيانة ' + i.RepairOrderNumber + N' غير مسددة بالكامل.',
                    'Warning',
                    'Invoice',
                    i.InvoiceNumber,
                    0,
                    GETDATE()
                FROM Invoices i
                WHERE i.IsPaid = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM Notifications n 
                      WHERE n.TargetReferenceId = i.InvoiceNumber 
                        AND n.Category = 'Invoice' 
                        AND n.CreatedAt >= DATEADD(hour, -6, GETDATE())
                  );";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch { }
        }

        public async Task SyncActiveAlertsOnStartupAsync(CancellationToken cancellationToken = default)
        {
            string sql = @"
                -- Re-activate all active unresolved issues on application launch
                UPDATE n SET n.IsRead = 0, n.CreatedAt = GETDATE()
                FROM Notifications n
                INNER JOIN SpareParts p ON n.TargetReferenceId = p.PartCode AND n.Category = 'Inventory'
                WHERE p.QuantityInStock <= p.MinimumStock;

                UPDATE n SET n.IsRead = 0, n.CreatedAt = GETDATE()
                FROM Notifications n
                INNER JOIN RepairOrders r ON n.TargetReferenceId = r.RepairOrderNumber AND n.Category = 'RepairOrder'
                WHERE r.Status IN ('Pending', 'قيد الانتظار', 'معلق', 'InProgress', 'قيد التنفيذ');

                UPDATE n SET n.IsRead = 0, n.CreatedAt = GETDATE()
                FROM Notifications n
                INNER JOIN Invoices i ON n.TargetReferenceId = i.InvoiceNumber AND n.Category = 'Invoice'
                WHERE i.IsPaid = 0;

                UPDATE n SET n.IsRead = 0, n.CreatedAt = GETDATE()
                FROM Notifications n
                INNER JOIN RepairOrders r ON n.TargetReferenceId = r.RepairOrderNumber AND n.Category = 'Customer'
                WHERE r.Status NOT IN ('Completed', 'Canceled', 'مكتمل', 'ملغي', 'تم التسليم');
            ";
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch { }
        }

        private static NotificationItem MapNotification(SqlDataReader reader)
        {
            var severityStr = reader.IsDBNull(3) ? "Info" : reader.GetString(3);
            NotificationSeverity severity;
            if (!Enum.TryParse(severityStr, true, out severity))
            {
                severity = NotificationSeverity.Info;
            }

            return new NotificationItem
            {
                Id = reader.GetInt32(0),
                Title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Message = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Severity = severity,
                Category = reader.IsDBNull(4) ? "General" : reader.GetString(4),
                TargetReferenceId = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsRead = !reader.IsDBNull(6) && reader.GetBoolean(6),
                CreatedAt = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7),
                BranchId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
            };
        }
    }

    public class DashboardRepository : IDashboardRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public DashboardRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public IReadOnlyList<AutoCare.Application.DTOs.MonthlyRevenueDto> GetMonthlyRevenues(int months = 6, int? branchId = null)
        {
            var dict = new Dictionary<string, decimal>();
            var now = DateTime.Now;

            // Pre-populate last N months in ascending order
            var monthKeys = new List<string>();
            for (int i = months - 1; i >= 0; i--)
            {
                var dt = now.AddMonths(-i);
                string key = dt.ToString("yyyy-MM");
                monthKeys.Add(key);
                dict[key] = 0m;
            }

            string sql = @"
                SELECT 
                    FORMAT(InvoiceDate, 'yyyy-MM') AS [MonthPeriod],
                    SUM(PaidAmount) AS [TotalRevenue]
                FROM Invoices
                WHERE InvoiceDate >= DATEADD(MONTH, @MonthsBack, GETDATE())
                  AND (@BranchId IS NULL OR BranchId = @BranchId)
                GROUP BY FORMAT(InvoiceDate, 'yyyy-MM')
                ORDER BY [MonthPeriod] ASC";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@MonthsBack", -months);
                cmd.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string period = reader.GetString(0);
                        decimal rev = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                        dict[period] = rev;
                    }
                }
            }

            decimal maxRev = 0m;
            foreach (var kv in dict)
            {
                if (kv.Value > maxRev) maxRev = kv.Value;
            }
            if (maxRev <= 0) maxRev = 1m;

            var result = new List<AutoCare.Application.DTOs.MonthlyRevenueDto>();
            foreach (var key in monthKeys)
            {
                decimal rev = dict.ContainsKey(key) ? dict[key] : 0m;
                int monthNum = 1;
                if (DateTime.TryParse(key + "-01", out DateTime parsedDt))
                {
                    monthNum = parsedDt.Month;
                }
                string monthName = GetArabicMonthName(monthNum);

                double height = (double)(rev / maxRev) * 125.0;
                if (rev > 0 && height < 14.0) height = 14.0;
                if (rev == 0) height = 6.0;

                result.Add(new AutoCare.Application.DTOs.MonthlyRevenueDto
                {
                    MonthPeriod = key,
                    MonthNameArabic = monthName,
                    TotalRevenue = rev,
                    BarHeight = height
                });
            }

            return result;
        }

        public Task<IReadOnlyList<AutoCare.Application.DTOs.MonthlyRevenueDto>> GetMonthlyRevenuesAsync(int months = 6, int? branchId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetMonthlyRevenues(months, branchId));
        }

        public IReadOnlyList<AutoCare.Application.DTOs.OrderStatusDistributionDto> GetOrderStatusDistribution(int? branchId = null)
        {
            var list = new List<AutoCare.Application.DTOs.OrderStatusDistributionDto>();
            string sql = @"
                SELECT 
                    Status,
                    COUNT(*) AS [Count]
                FROM RepairOrders
                WHERE (@BranchId IS NULL OR BranchId = @BranchId)
                GROUP BY Status";

            int total = 0;
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string status = reader.IsDBNull(0) ? "Pending" : reader.GetString(0);
                        int count = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        total += count;

                        string arabic = "معلق";
                        string color = "#F59E0B";

                        switch (status.ToLowerInvariant())
                        {
                            case "pending":
                                arabic = "معلق";
                                color = "#F59E0B";
                                break;
                            case "inprogress":
                                arabic = "قيد التنفيذ";
                                color = "#06B6D4";
                                break;
                            case "completed":
                                arabic = "مكتمل";
                                color = "#10B981";
                                break;
                            case "canceled":
                            case "cancelled":
                                arabic = "ملغي";
                                color = "#F43F5E";
                                break;
                        }

                        list.Add(new AutoCare.Application.DTOs.OrderStatusDistributionDto
                        {
                            Status = status,
                            StatusArabic = arabic,
                            Count = count,
                            ColorHex = color
                        });
                    }
                }
            }

            if (total > 0)
            {
                foreach (var item in list)
                {
                    item.Percentage = Math.Round((double)item.Count / total * 100.0, 1);
                }
            }

            return list;
        }

        public Task<IReadOnlyList<AutoCare.Application.DTOs.OrderStatusDistributionDto>> GetOrderStatusDistributionAsync(int? branchId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetOrderStatusDistribution(branchId));
        }

        public IReadOnlyList<AutoCare.Application.DTOs.TopServiceDto> GetTopServices(int top = 5, int? branchId = null)
        {
            var list = new List<AutoCare.Application.DTOs.TopServiceDto>();
            string sql = $@"
                SELECT TOP ({top})
                    s.ServiceName,
                    COUNT(ros.ServiceCode) AS [UsageCount],
                    ISNULL(SUM(ros.UnitPrice), 0) AS [TotalIncome]
                FROM RepairOrderService ros
                INNER JOIN Services s ON ros.ServiceCode = s.ServiceCode
                LEFT JOIN RepairOrders ro ON ros.RepairOrderNumber = ro.RepairOrderNumber
                WHERE (@BranchId IS NULL OR ro.BranchId = @BranchId)
                GROUP BY s.ServiceName
                ORDER BY [UsageCount] DESC";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                using (var reader = cmd.ExecuteReader())
                {
                    int rank = 1;
                    while (reader.Read())
                    {
                        string name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        int count = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        decimal income = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);

                        list.Add(new AutoCare.Application.DTOs.TopServiceDto
                        {
                            Rank = rank++,
                            ServiceName = name,
                            UsageCount = count,
                            TotalIncome = income
                        });
                    }
                }
            }

            return list;
        }

        public Task<IReadOnlyList<AutoCare.Application.DTOs.TopServiceDto>> GetTopServicesAsync(int top = 5, int? branchId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetTopServices(top, branchId));
        }

        private static string GetArabicMonthName(int month)
        {
            switch (month)
            {
                case 1: return "يناير";
                case 2: return "فبراير";
                case 3: return "مارس";
                case 4: return "أبريل";
                case 5: return "مايو";
                case 6: return "يونيو";
                case 7: return "يوليو";
                case 8: return "أغسطس";
                case 9: return "سبتمبر";
                case 10: return "أكتوبر";
                case 11: return "نوفمبر";
                case 12: return "ديسمبر";
                default: return month.ToString();
            }
        }
    }

    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public AppointmentRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor;
        }

        public async Task<Appointment> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = @"
                SELECT a.Id, a.AppointmentNumber, a.CustomerPhone, a.CustomerName, a.PlateNumber,
                       a.CarModel, a.TechnicianID, a.BranchId, a.AppointmentDate, a.StartTime, a.EndTime,
                       a.Status, a.Notes, a.RepairOrderNumber, a.CreatedAt, a.IsDeleted,
                       c.FullName AS CustName, v.CarModel AS VehicleModel, t.FullName AS TechName
                FROM Appointments a
                LEFT JOIN Customers c ON a.CustomerPhone = c.PhoneNumber
                LEFT JOIN Vehicles v ON a.PlateNumber = v.PlateNumber
                LEFT JOIN Technicians t ON a.TechnicianID = t.TechnicianID
                WHERE a.Id = @Id AND a.IsDeleted = 0";

            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            return MapAppointment(reader);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task<IReadOnlyList<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int? branchId = null, CancellationToken cancellationToken = default)
        {
            var list = new List<Appointment>();
            string sql = @"
                SELECT a.Id, a.AppointmentNumber, a.CustomerPhone, a.CustomerName, a.PlateNumber,
                       a.CarModel, a.TechnicianID, a.BranchId, a.AppointmentDate, a.StartTime, a.EndTime,
                       a.Status, a.Notes, a.RepairOrderNumber, a.CreatedAt, a.IsDeleted,
                       c.FullName AS CustName, v.CarModel AS VehicleModel, t.FullName AS TechName
                FROM Appointments a
                LEFT JOIN Customers c ON a.CustomerPhone = c.PhoneNumber
                LEFT JOIN Vehicles v ON a.PlateNumber = v.PlateNumber
                LEFT JOIN Technicians t ON a.TechnicianID = t.TechnicianID
                WHERE a.IsDeleted = 0
                  AND a.AppointmentDate >= @startDate AND a.AppointmentDate <= @endDate
                  AND (@BranchId IS NULL OR a.BranchId = @BranchId)
                ORDER BY a.AppointmentDate ASC, a.StartTime ASC";

            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                    cmd.Parameters.AddWithValue("@endDate", endDate.Date);
                    cmd.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);

                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            list.Add(MapAppointment(reader));
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public async Task<bool> CheckTechnicianConflictAsync(int technicianId, DateTime date, TimeSpan start, TimeSpan end, int? excludeAppointmentId = null, CancellationToken cancellationToken = default)
        {
            string sql = @"
                SELECT COUNT(1) FROM Appointments
                WHERE IsDeleted = 0
                  AND TechnicianID = @TechnicianID
                  AND AppointmentDate = @AppointmentDate
                  AND Status NOT IN ('Cancelled', 'NoShow')
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                  AND (@Start < EndTime AND @End > StartTime)";

            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    cmd.Parameters.AddWithValue("@TechnicianID", technicianId);
                    cmd.Parameters.AddWithValue("@AppointmentDate", date.Date);
                    cmd.Parameters.AddWithValue("@Start", start);
                    cmd.Parameters.AddWithValue("@End", end);
                    cmd.Parameters.AddWithValue("@ExcludeId", (object)excludeAppointmentId ?? DBNull.Value);

                    var count = await cmd.ExecuteScalarAsync(cancellationToken);
                    return count != null && Convert.ToInt32(count) > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            if (appointment == null) throw new ArgumentNullException(nameof(appointment));

            if (string.IsNullOrWhiteSpace(appointment.AppointmentNumber))
            {
                appointment.AppointmentNumber = await GenerateNextAppointmentNumberAsync(cancellationToken);
            }

            string sql = @"
                INSERT INTO Appointments
                    (AppointmentNumber, CustomerPhone, CustomerName, PlateNumber, CarModel,
                     TechnicianID, BranchId, AppointmentDate, StartTime, EndTime,
                     Status, Notes, RepairOrderNumber, CreatedAt, IsDeleted)
                OUTPUT INSERTED.Id
                VALUES
                    (@AppointmentNumber, @CustomerPhone, @CustomerName, @PlateNumber, @CarModel,
                     @TechnicianID, @BranchId, @AppointmentDate, @StartTime, @EndTime,
                     @Status, @Notes, @RepairOrderNumber, @CreatedAt, 0)";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@AppointmentNumber", appointment.AppointmentNumber);
                cmd.Parameters.AddWithValue("@CustomerPhone", appointment.CustomerPhone ?? "");
                cmd.Parameters.AddWithValue("@CustomerName", (object)appointment.CustomerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PlateNumber", appointment.PlateNumber ?? "");
                cmd.Parameters.AddWithValue("@CarModel", (object)appointment.CarModel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TechnicianID", (object)appointment.TechnicianId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BranchId", appointment.BranchId);
                cmd.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate.Date);
                cmd.Parameters.AddWithValue("@StartTime", appointment.StartTime);
                cmd.Parameters.AddWithValue("@EndTime", appointment.EndTime);
                cmd.Parameters.AddWithValue("@Status", appointment.Status.ToString());
                cmd.Parameters.AddWithValue("@Notes", (object)appointment.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RepairOrderNumber", (object)appointment.RepairOrderNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", appointment.CreatedAt);

                var id = await cmd.ExecuteScalarAsync(cancellationToken);
                appointment.Id = Convert.ToInt32(id);
                return appointment.Id;
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, AppointmentStatus status, string repairOrderNumber = null, CancellationToken cancellationToken = default)
        {
            string sql = @"
                UPDATE Appointments
                SET Status = @Status,
                    RepairOrderNumber = COALESCE(@RO, RepairOrderNumber)
                WHERE Id = @Id AND IsDeleted = 0";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Status", status.ToString());
                cmd.Parameters.AddWithValue("@RO", (object)repairOrderNumber ?? DBNull.Value);

                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        public async Task<bool> RescheduleAsync(int id, DateTime newDate, TimeSpan newStart, TimeSpan newEnd, CancellationToken cancellationToken = default)
        {
            string sql = @"
                UPDATE Appointments
                SET AppointmentDate = @AppointmentDate,
                    StartTime = @StartTime,
                    EndTime = @EndTime
                WHERE Id = @Id AND IsDeleted = 0";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@AppointmentDate", newDate.Date);
                cmd.Parameters.AddWithValue("@StartTime", newStart);
                cmd.Parameters.AddWithValue("@EndTime", newEnd);

                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        public async Task<string> GenerateNextAppointmentNumberAsync(CancellationToken cancellationToken = default)
        {
            string prefix = "APT-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            string sql = "SELECT COUNT(1) FROM Appointments WHERE AppointmentNumber LIKE @Pattern";

            int count = 1;
            try
            {
                using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
                {
                    cmd.Parameters.AddWithValue("@Pattern", prefix + "%");
                    var result = await cmd.ExecuteScalarAsync(cancellationToken);
                    if (result != null && result != DBNull.Value)
                    {
                        count = Convert.ToInt32(result) + 1;
                    }
                }
            }
            catch { }

            return prefix + count.ToString("D3");
        }

        private static Appointment MapAppointment(SqlDataReader reader)
        {
            var apt = new Appointment
            {
                Id = reader.GetInt32(0),
                AppointmentNumber = reader.GetString(1),
                CustomerPhone = reader.GetString(2),
                CustomerName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                PlateNumber = reader.GetString(4),
                CarModel = reader.IsDBNull(5) ? "" : reader.GetString(5),
                TechnicianId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                BranchId = reader.GetInt32(7),
                AppointmentDate = reader.GetDateTime(8),
                StartTime = reader.GetTimeSpan(9),
                EndTime = reader.GetTimeSpan(10),
                Notes = reader.IsDBNull(12) ? "" : reader.GetString(12),
                RepairOrderNumber = reader.IsDBNull(13) ? null : reader.GetString(13),
                CreatedAt = reader.GetDateTime(14),
                IsDeleted = reader.GetBoolean(15)
            };

            string statusStr = reader.GetString(11);
            if (Enum.TryParse<AppointmentStatus>(statusStr, true, out var status))
            {
                apt.Status = status;
            }

            // Joined columns
            if (reader.FieldCount > 16 && !reader.IsDBNull(16)) apt.CustomerName = reader.GetString(16);
            if (reader.FieldCount > 17 && !reader.IsDBNull(17)) apt.CarModel = reader.GetString(17);
            if (reader.FieldCount > 18 && !reader.IsDBNull(18)) apt.TechnicianName = reader.GetString(18);

            return apt;
        }
    }

    public class BranchRepository : IBranchRepository
    {
        private readonly SqlConnection _connection;
        private readonly Func<SqlTransaction> _txAccessor;

        public BranchRepository(SqlConnection connection, Func<SqlTransaction> txAccessor)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _txAccessor = txAccessor ?? (() => null);
        }

        public async Task<Branch> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            string sql = "SELECT * FROM Branches WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapBranch(reader);
                    }
                }
            }
            return null;
        }

        public async Task<Branch> GetByCodeAsync(string branchCode, CancellationToken cancellationToken = default)
        {
            string sql = "SELECT * FROM Branches WHERE BranchCode = @Code";
            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Code", branchCode);
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        return MapBranch(reader);
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<Branch>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
        {
            var list = new List<Branch>();
            string sql = activeOnly 
                ? "SELECT * FROM Branches WHERE IsActive = 1 ORDER BY Id ASC" 
                : "SELECT * FROM Branches ORDER BY Id ASC";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        list.Add(MapBranch(reader));
                    }
                }
            }
            return list;
        }

        public async Task<int> AddAsync(Branch branch, CancellationToken cancellationToken = default)
        {
            string sql = @"
                INSERT INTO Branches (BranchCode, BranchName, City, Address, PhoneNumber, ManagerName, IsActive, CreatedAt)
                VALUES (@Code, @Name, @City, @Address, @Phone, @Manager, @IsActive, GETDATE());
                SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Code", branch.BranchCode ?? "");
                cmd.Parameters.AddWithValue("@Name", branch.BranchName ?? "");
                cmd.Parameters.AddWithValue("@City", branch.City ?? "");
                cmd.Parameters.AddWithValue("@Address", (object)branch.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", (object)branch.PhoneNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Manager", (object)branch.ManagerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", branch.IsActive);

                var res = await cmd.ExecuteScalarAsync(cancellationToken);
                branch.Id = Convert.ToInt32(res);
                return branch.Id;
            }
        }

        public async Task<bool> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
        {
            string sql = @"
                UPDATE Branches 
                SET BranchName = @Name,
                    City = @City,
                    Address = @Address,
                    PhoneNumber = @Phone,
                    ManagerName = @Manager,
                    IsActive = @IsActive
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(sql, _connection, _txAccessor()))
            {
                cmd.Parameters.AddWithValue("@Name", branch.BranchName ?? "");
                cmd.Parameters.AddWithValue("@City", branch.City ?? "");
                cmd.Parameters.AddWithValue("@Address", (object)branch.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", (object)branch.PhoneNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Manager", (object)branch.ManagerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", branch.IsActive);
                cmd.Parameters.AddWithValue("@Id", branch.Id);

                int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return rows > 0;
            }
        }

        private static Branch MapBranch(SqlDataReader reader)
        {
            return new Branch
            {
                Id = Convert.ToInt32(reader["Id"]),
                BranchCode = reader["BranchCode"]?.ToString() ?? "",
                BranchName = reader["BranchName"]?.ToString() ?? "",
                City = reader["City"]?.ToString() ?? "",
                Address = reader["Address"] is DBNull ? "" : reader["Address"]?.ToString() ?? "",
                PhoneNumber = reader["PhoneNumber"] is DBNull ? "" : reader["PhoneNumber"]?.ToString() ?? "",
                ManagerName = reader["ManagerName"] is DBNull ? "" : reader["ManagerName"]?.ToString() ?? "",
                IsActive = reader["IsActive"] is DBNull || Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = reader["CreatedAt"] is DBNull ? DateTime.Now : Convert.ToDateTime(reader["CreatedAt"])
            };
        }
    }
}

