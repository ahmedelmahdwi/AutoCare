using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Entities;

namespace C__project_Auto_Care.Core.Services
{
    public interface IAuthService
    {
        OperationResult<User> Authenticate(string username, string password);
        OperationResult<User> Authenticate(string username, string password, bool rememberMe);
        void Logout();
        bool TryLoadSavedCredentials(out string username, out string password);
        List<UserListItemDto> GetAllUsers(string search = "", string roleFilter = "");
        UserListItemDto GetUserByUsername(string username);
        OperationResult CreateUser(string username, string fullName, string role, bool isActive, string password, int branchId = 1);
        OperationResult UpdateUser(string username, string fullName, string role, bool isActive, string password, int? branchId = null);
        OperationResult DeleteUser(string username, string currentUsername);
    }
    public interface IRepairOrderLifecycleService
    {
        OperationResult UpdateOrderStatus(string repairOrderNumber, string status);
        OperationResult ConfirmOrderServices(string repairOrderNumber);
        OperationResult StartOrderProgress(string repairOrderNumber);
        OperationResult RevertToPending(string repairOrderNumber);
        OperationResult CompleteRepairOrder(string repairOrderNumber);
        OperationResult CancelRepairOrder(string repairOrderNumber, string reason = "");
        string GetRepairOrderStatus(string repairOrderNumber);
        List<CarStatusRowDto> GetCarStatusList(string search = "", string statusFilter = "الكل");
    }

    public interface IRepairOrderItemsService
    {
        OperationResult AddServiceToOrder(string repairOrderNumber, string serviceCode, int technicianId);
        OperationResult RemoveServiceFromOrder(string repairOrderNumber, string serviceCode, int technicianId);
        OperationResult AddSparePartToOrder(string repairOrderNumber, string partCode, int quantity);
        OperationResult RemoveSparePartFromOrder(string repairOrderNumber, string partCode, int quantity);
        List<OrderServiceItemDto> GetOrderServices(string repairOrderNumber);
        List<OrderSparePartItemDto> GetOrderSpareParts(string repairOrderNumber);
    }

    public interface IVehicleHistoryService
    {
        bool HasActiveRepairOrder(string plateNumber);
        DataTable GetVehicleServiceHistory(string plateNumber);
        List<VehicleServiceHistoryRowDto> GetVehicleServiceHistoryList(string plateNumber);
    }

    public interface IServiceCatalogService
    {
        List<ServiceRowDto> GetServicesCatalog(string search = "");
        OperationResult DeleteServiceFromCatalog(string serviceCode);
        List<string> GetServiceCategories();
        List<ServiceCatalogItemDto> GetServicesByCategory(string category);
        List<TopServiceItemDto> GetTopServices(DateTime startDate, DateTime endDate, int topN);
        ServiceRowDto GetServiceByCode(string serviceCode);
        OperationResult SaveService(string serviceCode, string serviceName, decimal price, string category, bool isEdit);
    }

    public interface IRepairOrderService
    {
        OperationResult<string> CreateRepairOrder(Customer customer, Vehicle vehicle, RepairOrder repairOrder);
        OperationResult UpdateRepairOrder(Customer customer, Vehicle vehicle, RepairOrder repairOrder);
        RepairOrderDetailsDto GetRepairOrderDetails(string repairOrderNumber);
        bool HasActiveRepairOrder(string plateNumber);
        DataTable GetVehicleServiceHistory(string plateNumber);
        string GenerateNewRepairOrderNumber();
        OperationResult AddServiceToOrder(string repairOrderNumber, string serviceCode, int technicianId);
        OperationResult RemoveServiceFromOrder(string repairOrderNumber, string serviceCode, int technicianId);
        OperationResult AddSparePartToOrder(string repairOrderNumber, string partCode, int quantity);
        OperationResult RemoveSparePartFromOrder(string repairOrderNumber, string partCode, int quantity);
        OperationResult UpdateOrderStatus(string repairOrderNumber, string status);
        OperationResult ConfirmOrderServices(string repairOrderNumber);
        OperationResult StartOrderProgress(string repairOrderNumber);
        OperationResult RevertToPending(string repairOrderNumber);
        List<ServiceRowDto> GetServicesCatalog(string search = "");
        OperationResult DeleteServiceFromCatalog(string serviceCode);
        OperationResult DeleteRepairOrder(string repairOrderNumber);
        List<CarStatusRowDto> GetCarStatusList(string search = "", string statusFilter = "الكل");
        string GetRepairOrderStatus(string repairOrderNumber);
        OperationResult CompleteRepairOrder(string repairOrderNumber);
        OperationResult CancelRepairOrder(string repairOrderNumber, string reason = "");
        List<VehicleServiceHistoryRowDto> GetVehicleServiceHistoryList(string plateNumber);
        List<string> GetServiceCategories();
        List<ServiceCatalogItemDto> GetServicesByCategory(string category);
        List<OrderServiceItemDto> GetOrderServices(string repairOrderNumber);
        List<OrderSparePartItemDto> GetOrderSpareParts(string repairOrderNumber);
        List<TopServiceItemDto> GetTopServices(DateTime startDate, DateTime endDate, int topN);
        ServiceRowDto GetServiceByCode(string serviceCode);
        OperationResult SaveService(string serviceCode, string serviceName, decimal price, string category, bool isEdit);
    }

    public interface IInvoiceService
    {
        InvoiceCalculationResult Calculate(decimal servicesTotal, decimal partsTotal, decimal discountAmount, decimal taxRatePercent = 0m, decimal paidAmount = 0m);
        OperationResult<string> CreateFinalInvoice(string repairOrderNumber, decimal netTotal, decimal discountAmount, string paymentMethod, string createdByUsername);
        List<CompletedOrderWithoutInvoiceDto> GetCompletedOrdersWithoutInvoice(string searchTerm = "");
        List<IssuedInvoiceSummaryDto> GetIssuedInvoices(string searchTerm = "");
        OperationResult RevertToInProgress(string repairOrderNumber);
        bool DoesRepairOrderExist(string repairOrderNumber);
        FinancialReportSummaryDto GetFinancialReport(DateTime startDate, DateTime endDate);
        FinalInvoicePrintDto GetInvoiceForPrint(string invoiceNumber, string repairOrderNumber);
    }

    public interface IInventoryService
    {
        DataTable GetAllSpareParts(string searchTerm = "");
        List<SparePartRowDto> GetSparePartsList(string searchTerm = "");
        OperationResult DeleteSparePart(string partCode);
        OperationResult AdjustStock(string partCode, int delta, string reason);
        int GetLowStockCount();
        SparePartRowDto GetSparePartByCode(string partCode);
        List<string> GetDistinctCategories();
        List<SparePartCatalogItemDto> GetSparePartsByCategory(string category);
        OperationResult AddSparePart(string partCode, string partName, int quantity, int minStock, decimal buyingPrice, decimal sellingPrice, string location, string category);
        OperationResult UpdateSparePart(string partCode, string partName, int quantity, int minStock, decimal buyingPrice, decimal sellingPrice, string location, string category);
        InventoryReportSummaryDto GetInventoryReport(string search = "", bool lowStockOnly = false);
        List<TopPartItemDto> GetTopSpareParts(DateTime startDate, DateTime endDate, int topN);
    }

    public interface ISupplierService
    {
        DataTable GetAllSuppliers(string search = "");
        List<SupplierSummaryDto> GetSuppliersList(string search = "");
        List<PurchaseInvoiceSummaryDto> GetPurchaseInvoicesList(string search = "");
        OperationResult AddSupplier(string name, string contact, string phone, string address);
        OperationResult UpdateSupplier(int supplierId, string name, string contact, string phone, string address);
        OperationResult DeleteSupplier(int supplierId, string supplierName = "");
        SupplierSummaryDto GetSupplierById(int supplierId);
        OperationResult RecordPurchase(int supplierId, string invoiceNumber, decimal paidAmount, List<Data.PurchaseItemDto> items, string notes);
        DataTable GetSupplierLedger(int supplierId);
        string GenerateNewPurchaseInvoiceNumber();
        List<PurchaseInvoiceDetailDto> GetPurchaseInvoiceDetails(string invoiceNumber);
    }

    public interface ICustomerService
    {
        OperationResult RegisterCustomer(string phoneNumber, string fullName);
        OperationResult UpdateCustomer(string phoneNumber, string fullName);
        OperationResult DeleteCustomer(string phoneNumber);
        DataTable GetCustomerByPhone(string phoneNumber);
        OperationResult RegisterVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes);
        OperationResult UpdateVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes);
        OperationResult DeleteVehicle(string plateNumber);
        DataTable GetVehicleByPlate(string plateNumber);
        VehicleWithCustomerDto GetVehicleWithCustomer(string plateNumber);
        DataTable GetAllVehiclesWithCustomers(string searchTerm = "", string statusFilter = "الكل");
        List<CustomerVehicleRowDto> GetVehiclesWithCustomersList(string searchTerm = "", string statusFilter = "الكل");
    }

    public interface ITechnicianService
    {
        DataTable GetActiveTechnicians();
        List<TechnicianSummaryDto> GetActiveTechniciansList();
        DataTable GetAllTechnicians(string searchTerm = "", string specializationFilter = "");
        OperationResult AddTechnician(int technicianId, string fullName, string specialization, decimal commissionPercent, bool isActive, int branchId = 1);
        OperationResult UpdateTechnician(int technicianId, string fullName, string specialization, decimal commissionPercent, bool isActive, int branchId = 1);
        OperationResult DeleteTechnician(int technicianId);
        DataTable GetSettlements(int? technicianId = null, string status = "", DateTime? startDate = null, DateTime? endDate = null);
        List<SettlementRecordDto> GetSettlementRecords(int? technicianId = null, string status = "", DateTime? startDate = null, DateTime? endDate = null);
        OperationResult SettleCommissions(IEnumerable<string> settlementIds);
        List<string> GetSpecializations();
        List<TechnicianRowDto> GetTechniciansList(string searchTerm = "", string specializationFilter = "");
        TechnicianRowDto GetTechnicianById(int technicianId);
        int GetNextTechnicianId();
    }

    public interface IAuditService
    {
        AuditQueryResultDto GetAuditLogs(DateTime? from, DateTime? to, string action, string search);
        void Log(string action, string targetTable, string recordKey, string details, string username = null);
    }
}
