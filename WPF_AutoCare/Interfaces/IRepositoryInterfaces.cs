using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace C__project_Auto_Care.Interfaces
{
    public interface IDbHelper
    {
        int ExecuteNonQuery(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null);
        DataTable ExecuteDataTable(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null);
        T ExecuteScalar<T>(string query, SqlParameter[] parameters, SqlConnection conn = null, SqlTransaction tran = null);
        List<T> ExecuteList<T>(string query, SqlParameter[] parameters, Func<SqlDataReader, T> mapper, SqlConnection conn = null, SqlTransaction tran = null);
        T ExecuteSingle<T>(string query, SqlParameter[] parameters, Func<SqlDataReader, T> mapper, SqlConnection conn = null, SqlTransaction tran = null);
        void ExecuteTransaction(Action<SqlConnection, SqlTransaction> action);
        T ExecuteTransaction<T>(Func<SqlConnection, SqlTransaction, T> func);
        bool TestConnection(out string errorMessage);
        string GetConnectionString();
        string GetDatabaseName();
        string GetMasterConnectionString();
        string CurrentServer { get; }
    }

    public interface IUserRepository
    {
        DataTable GetAllUsers(string searchTerm = "", string roleFilter = "");
        List<C__project_Auto_Care.Core.DTOs.UserListItemDto> GetAllUsersList(string searchTerm = "", string roleFilter = "");
        bool UserExists(string username);
        bool IsUserUsedInOrders(string username);
        bool IsUserUsedInInvoices(string username);
        void AddUser(string username, string fullName, string role, bool isActive, string plainPassword, int branchId = 1);
        void UpdateUser(string username, string fullName, string role, bool isActive, string newPlainPassword = null, int? branchId = null);
        void DeleteUser(string username);
        bool Authenticate(string username, string plainPassword);
        DataTable GetUserData(string username);
        C__project_Auto_Care.Core.DTOs.UserListItemDto GetUserDto(string username);
        int CountAdminUsers();
    }

    public interface ICustomerRepository
    {
        DataTable GetCustomerByPhone(string phoneNumber);
        C__project_Auto_Care.Core.Entities.Customer GetCustomerEntityByPhone(string phoneNumber);
        List<C__project_Auto_Care.Core.Entities.Customer> GetAllCustomersList();
        bool CustomerExists(string phoneNumber);
        void AddCustomer(string phoneNumber, string fullName);
        void UpdateCustomer(string phoneNumber, string fullName);
        void DeleteCustomer(string phoneNumber);
        void SoftDeleteCustomer(string phoneNumber);
        void RestoreCustomer(string phoneNumber);
    }

    public interface IVehicleRepository
    {
        DataTable GetAllVehiclesWithCustomers(string searchTerm = "", string statusFilter = "الكل");
        List<C__project_Auto_Care.Core.DTOs.CustomerVehicleRowDto> GetAllVehiclesWithCustomersList(string searchTerm = "", string statusFilter = "الكل");
        DataTable GetVehicleByPlateNumber(string plateNumber);
        C__project_Auto_Care.Core.Entities.Vehicle GetVehicleEntityByPlate(string plateNumber);
        C__project_Auto_Care.Core.DTOs.VehicleWithCustomerDto GetVehicleWithCustomerDto(string plateNumber);
        bool VehicleExists(string plateNumber);
        bool VinExists(string vin, string excludePlateNumber = null);
        void AddVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes);
        void UpdateVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes);
        void DeleteVehicle(string plateNumber);
        void SoftDeleteVehicle(string plateNumber);
        void RestoreVehicle(string plateNumber);
    }

    public interface IRepairOrderRepository
    {
        DataTable GetAllRepairOrdersWithDetails(string searchTerm = "", string statusFilter = "الكل");
        DataTable GetRepairOrderByNumber(string repairOrderNumber);
        DataTable GetLatestRepairOrderByPlate(string plateNumber);
        bool HasActiveRepairOrder(string plateNumber);
        string GenerateNewRepairOrderNumber();
        void AddRepairOrder(string repairOrderNumber, string plateNumber, string receptionistUsername, string complaint, string status, DateTime? expectedDate = null, int? mileage = null, string fuelLevel = null, string inspectionNotes = null);
        void AddRepairOrder(string repairOrderNumber, string plateNumber, string receptionistUsername, string complaint, string status, DateTime? expectedDate, System.Data.SqlClient.SqlConnection conn, System.Data.SqlClient.SqlTransaction tran);
        void UpdateOrderStatus(string repairOrderNumber, string status, DateTime? completionDate = null);
        bool UpdateOrderStatusWithConcurrency(string repairOrderNumber, string status, byte[] rowVersion, DateTime? completionDate = null);
        byte[] GetRepairOrderRowVersion(string repairOrderNumber);
        void UpdateRepairOrder(string repairOrderNumber, string complaint, string status, DateTime? completionDate = null, int? mileage = null, string fuelLevel = null, string inspectionNotes = null);
        void DeleteRepairOrder(string repairOrderNumber);
        bool HasServicesOrParts(string repairOrderNumber);
        DataTable GetVehicleServiceHistory(string plateNumber);
    }

    public interface IServiceRepository
    {
        DataTable GetAllServices(string searchTerm = "");
        List<C__project_Auto_Care.Core.DTOs.ServiceRowDto> GetAllServicesList(string searchTerm = "");
        bool ServiceCodeExists(string serviceCode);
        void AddService(string serviceCode, string serviceName, decimal standardPrice, string category);
        void UpdateService(string serviceCode, string serviceName, decimal standardPrice, string category);
        void DeleteService(string serviceCode);
        DataRow GetServiceByCode(string serviceCode);
        C__project_Auto_Care.Core.DTOs.ServiceRowDto GetServiceDtoByCode(string serviceCode);
        List<string> GetDistinctCategories();
        DataTable GetTopServices(DateTime startDate, DateTime endDate, int topN);
        DataTable GetServicesByCategory(string category);
    }

    public interface ISparePartRepository
    {
        DataTable GetAllSpareParts(string searchTerm = "");
        List<C__project_Auto_Care.Core.DTOs.SparePartRowDto> GetAllSparePartsList(string searchTerm = "");
        DataTable GetSparePartByCode(string partCode);
        C__project_Auto_Care.Core.DTOs.SparePartRowDto GetSparePartDtoByCode(string partCode);
        bool PartCodeExists(string partCode);
        void AddSparePart(string partCode, string partName, int quantity, int minStock, decimal buyingPrice, decimal sellingPrice, string location, string category);
        void UpdateSparePart(string partCode, string partName, int quantity, int minStock, decimal buyingPrice, decimal sellingPrice, string location, string category);
        void DeleteSparePart(string partCode);
        void UpdateStock(string partCode, int quantityDelta);
        bool TryDeductStock(string partCode, int quantity);
        bool UpdatePriceWithConcurrency(string partCode, decimal newPrice, byte[] rowVersion);
        byte[] GetSparePartRowVersion(string partCode);
        DataTable GetTopSpareParts(DateTime startDate, DateTime endDate, int topN);
        List<string> GetDistinctCategories();
        DataTable GetSparePartsByCategory(string category);
        int GetAvailableStock(string partCode);
        int GetLowStockCount();
    }

    public interface IInvoiceRepository
    {
        decimal GetTodaySales();
        decimal GetTodaySparePartsCost();
        string GenerateNewInvoiceNumber();
        void InsertInvoice(string invoiceNumber, string repairOrderNumber, string createdByUsername, decimal paidAmount, decimal discountAmount = 0, string paymentMethod = "Cash");
        void DeleteInvoice(string repairOrderNumber);
        bool InvoiceExistsByRepairOrder(string repairOrderNumber);
        DataTable GetCompletedOrdersWithoutInvoice(string searchTerm = "");
        List<C__project_Auto_Care.Core.DTOs.CompletedOrderWithoutInvoiceDto> GetCompletedOrdersWithoutInvoiceList(string searchTerm = "");
        List<C__project_Auto_Care.Core.DTOs.IssuedInvoiceSummaryDto> GetIssuedInvoicesList(string searchTerm = "");
        void RevertToInProgress(string repairOrderNumber);
        DataTable GetInvoicesWithDetailsForReport(DateTime startDate, DateTime endDate, string receptionistUsername = null);
        List<C__project_Auto_Care.Core.DTOs.FinancialReportItemDto> GetInvoicesWithDetailsForReportList(DateTime startDate, DateTime endDate, string receptionistUsername = null);
        DataTable GetInvoiceByRepairOrder(string repairOrderNumber);
        DataTable GetInvoicesForCrystalReport(DateTime startDate, DateTime endDate);
    }

    public interface ITechnicianRepository
    {
        DataTable GetActiveTechnicians();
        List<C__project_Auto_Care.Core.DTOs.TechnicianSummaryDto> GetActiveTechniciansList();
        DataTable GetAllTechnicians(string searchTerm = "", string specializationFilter = "");
        List<C__project_Auto_Care.Core.DTOs.TechnicianRowDto> GetAllTechniciansList(string searchTerm = "", string specializationFilter = "");
        DataTable GetSpecializations();
        List<string> GetSpecializationsList();
        bool TechnicianExists(int technicianId);
        bool IsTechnicianUsedInOrders(int technicianId);
        void AddTechnician(int technicianId, string fullName, string specialization, decimal commissionPercent, bool isActive, int branchId = 1);
        void UpdateTechnician(int technicianId, string fullName, string specialization, decimal commissionPercent, bool isActive, int branchId = 1);
        DataTable GetTechnicianById(int technicianId);
        C__project_Auto_Care.Core.DTOs.TechnicianRowDto GetTechnicianDtoById(int technicianId);
        DataTable GetTechniciansBySpecialization(string specialization);
        void DeleteTechnician(int technicianId);
        int GetNextTechnicianId();
    }

    public interface ITechnicianSettlementRepository
    {
        void InsertSettlement(string repairOrderNumber, int technicianId, decimal serviceTotal, decimal commissionRate, decimal commissionAmount);
        DataTable GetSettlements(int? technicianId = null, string status = "", DateTime? startDate = null, DateTime? endDate = null);
        void UpdateSettlementStatus(string settlementId, string newStatus);
        void UpdateSettlementsStatusByRepairOrder(string repairOrderNumber, string newStatus);
        void DeleteSettlementsByRepairOrder(string repairOrderNumber);
        int GetSettlementsCount(string repairOrderNumber);
        bool HasUnsettledSettlements(string repairOrderNumber);
        void SettleMultiple(IEnumerable<string> settlementIds);
    }

    public interface IRepairOrderServiceRepository
    {
        DataTable GetServicesByRepairOrder(string repairOrderNumber);
        void AddServiceToOrder(string repairOrderNumber, string serviceCode, int technicianId, decimal unitPrice);
        void RemoveServiceFromOrder(string repairOrderNumber, string serviceCode, int technicianId);
        void ClearServicesForOrder(string repairOrderNumber);
    }

    public interface IRepairOrderSparePartRepository
    {
        DataTable GetSparePartsByRepairOrder(string repairOrderNumber);
        void AddSparePartToOrder(string repairOrderNumber, string partCode, int quantity, decimal unitPrice);
        void RemoveSparePartFromOrder(string repairOrderNumber, string partCode);
        void ClearSparePartsForOrder(string repairOrderNumber);
        void AddSparePartWithStockDeduction(string repairOrderNumber, string partCode, int quantity, decimal unitPrice);
        void RemoveSparePartWithStockRestoration(string repairOrderNumber, string partCode, int quantity);
    }

    public interface IAuditRepository
    {
        void Log(string action, string targetTable, string recordKey, string details, string username = null);
        DataTable GetLogs(DateTime? fromDate = null, DateTime? toDate = null, string actionFilter = "", string search = "");
    }

    public interface ISupplierRepository
    {
        DataTable GetAllSuppliers(string search = "");
        DataRow GetSupplierById(int supplierId);
        int AddSupplier(string name, string contact, string phone, string address);
        void UpdateSupplier(int id, string name, string contact, string phone, string address);
        void DeleteSupplier(int id);
        string GenerateNewPurchaseInvoiceNumber();
        void CreatePurchaseInvoice(string invoiceNumber, int supplierId, decimal totalAmount, string notes, string createdBy, List<Data.PurchaseItemDto> items);
        DataTable GetAllPurchaseInvoices(string search = "");
        DataTable GetPurchaseInvoiceDetails(string invoiceNumber);
    }
}
