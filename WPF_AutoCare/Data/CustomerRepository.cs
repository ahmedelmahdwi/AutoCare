using System;
using System.Data;
using System.Data.SqlClient;
using C__project_Auto_Care.Interfaces;

namespace C__project_Auto_Care.Data
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDbHelper _dbHelper;

        public CustomerRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetCustomerByPhone(string phoneNumber)
        {
            string query = "SELECT PhoneNumber, FullName FROM Customers WHERE PhoneNumber = @phone";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber)
            };
            return _dbHelper.ExecuteDataTable(query, parameters);
        }

        public C__project_Auto_Care.Core.Entities.Customer GetCustomerEntityByPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
            string query = "SELECT PhoneNumber, FullName FROM Customers WHERE PhoneNumber = @phone";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber.Trim())
            };
            return _dbHelper.ExecuteSingle(query, parameters, r => new C__project_Auto_Care.Core.Entities.Customer
            {
                PhoneNumber = r["PhoneNumber"]?.ToString(),
                FullName = r["FullName"]?.ToString()
            });
        }

        public System.Collections.Generic.List<C__project_Auto_Care.Core.Entities.Customer> GetAllCustomersList()
        {
            string query = "SELECT PhoneNumber, FullName FROM Customers WHERE IsDeleted = 0 ORDER BY FullName";
            return _dbHelper.ExecuteList(query, null, r => new C__project_Auto_Care.Core.Entities.Customer
            {
                PhoneNumber = r["PhoneNumber"]?.ToString(),
                FullName = r["FullName"]?.ToString()
            });
        }

        public bool CustomerExists(string phoneNumber)
        {
            string query = "SELECT COUNT(1) FROM Customers WHERE PhoneNumber = @phone";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber)
            };
            int count = _dbHelper.ExecuteScalar<int>(query, parameters);
            return count > 0;
        }

        public void AddCustomer(string phoneNumber, string fullName) => AddCustomer(phoneNumber, fullName, null, null);

        public void AddCustomer(string phoneNumber, string fullName, SqlConnection conn, SqlTransaction tran)
        {
            string query = "INSERT INTO Customers (PhoneNumber, FullName) VALUES (@phone, @name)";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber),
                new SqlParameter("@name", fullName)
            };
            _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
        }

        public void UpdateCustomer(string phoneNumber, string fullName) => UpdateCustomer(phoneNumber, fullName, null, null);

        public void UpdateCustomer(string phoneNumber, string fullName, SqlConnection conn, SqlTransaction tran)
        {
            string query = "UPDATE Customers SET FullName = @name WHERE PhoneNumber = @phone";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber),
                new SqlParameter("@name", fullName)
            };
            _dbHelper.ExecuteNonQuery(query, parameters, conn, tran);
        }

        public void DeleteCustomer(string phoneNumber)
        {
            string checkQuery = "SELECT COUNT(1) FROM Vehicles WHERE CustomerPhone = @phone";
            SqlParameter[] checkParams = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber)
            };
            int vehicleCount = _dbHelper.ExecuteScalar<int>(checkQuery, checkParams);
            if (vehicleCount > 0)
            {
                throw new InvalidOperationException("لا يمكن حذف العميل لوجود مركبات تابعة له.");
            }

            string deleteQuery = "DELETE FROM Customers WHERE PhoneNumber = @phone";
            SqlParameter[] deleteParams = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber)
            };
            _dbHelper.ExecuteNonQuery(deleteQuery, deleteParams);
        }

        public void SoftDeleteCustomer(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return;
            string query = "UPDATE Customers SET IsDeleted = 1 WHERE PhoneNumber = @phone";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber.Trim())
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }

        public void RestoreCustomer(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return;
            string query = "UPDATE Customers SET IsDeleted = 0 WHERE PhoneNumber = @phone";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@phone", phoneNumber.Trim())
            };
            _dbHelper.ExecuteNonQuery(query, parameters);
        }
    }
}
