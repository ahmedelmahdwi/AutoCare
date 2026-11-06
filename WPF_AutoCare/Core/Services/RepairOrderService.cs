using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Entities;
using C__project_Auto_Care.Core.Enums;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    /// <summary>
    /// Clean Architecture Facade and Orchestrator for Repair Orders.
    /// Delegates sub-domain operations to dedicated Single-Responsibility services:
    /// - <see cref="IRepairOrderLifecycleService"/> (Lifecycle transitions & invariants)
    /// - <see cref="IRepairOrderItemsService"/> (Order services, spare parts & stock deduction)
    /// - <see cref="IVehicleHistoryService"/> (Vehicle repair history & active orders)
    /// - <see cref="IServiceCatalogService"/> (Services catalog management)
    /// </summary>
    public class RepairOrderService : IRepairOrderService, IServiceCatalogService, IRepairOrderLifecycleService, IRepairOrderItemsService, IVehicleHistoryService
    {
        private readonly IDbHelper _dbHelper;
        private readonly IRepairOrderRepository _roRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IRepairOrderServiceRepository _rosRepo;
        private readonly IRepairOrderSparePartRepository _rospRepo;
        private readonly IServiceRepository _serviceRepo;
        private readonly ISparePartRepository _sparePartRepo;
        private readonly IAuditRepository _auditRepo;

        // Sub-domain decomposed services
        private readonly IRepairOrderLifecycleService _lifecycleService;
        private readonly IRepairOrderItemsService _itemsService;
        private readonly IVehicleHistoryService _historyService;
        private readonly IServiceCatalogService _catalogService;

        /// <summary>
        /// Modern Clean Architecture constructor with dependency inversion.
        /// </summary>
        public RepairOrderService(
            IDbHelper dbHelper,
            IRepairOrderRepository roRepo,
            ICustomerRepository customerRepo,
            IVehicleRepository vehicleRepo,
            IRepairOrderServiceRepository rosRepo,
            IRepairOrderSparePartRepository rospRepo,
            IServiceRepository serviceRepo,
            ISparePartRepository sparePartRepo,
            IAuditRepository auditRepo,
            IRepairOrderLifecycleService lifecycleService,
            IRepairOrderItemsService itemsService,
            IVehicleHistoryService historyService,
            IServiceCatalogService catalogService)
        {
            _dbHelper = dbHelper;
            _roRepo = roRepo;
            _customerRepo = customerRepo;
            _vehicleRepo = vehicleRepo;
            _rosRepo = rosRepo;
            _rospRepo = rospRepo;
            _serviceRepo = serviceRepo;
            _sparePartRepo = sparePartRepo;
            _auditRepo = auditRepo;

            _lifecycleService = lifecycleService ?? new RepairOrderLifecycleService(roRepo, rosRepo, rospRepo, auditRepo);
            _itemsService = itemsService ?? new RepairOrderItemsService(rosRepo, rospRepo, serviceRepo, sparePartRepo, auditRepo);
            _historyService = historyService ?? new VehicleHistoryService(roRepo);
            _catalogService = catalogService ?? new ServiceCatalogService(serviceRepo, auditRepo);
        }

        /// <summary>
        /// Backward-compatible constructor for existing callers and AppServiceLocator.
        /// </summary>
        public RepairOrderService(
            IDbHelper dbHelper,
            IRepairOrderRepository roRepo,
            ICustomerRepository customerRepo,
            IVehicleRepository vehicleRepo,
            IRepairOrderServiceRepository rosRepo,
            IRepairOrderSparePartRepository rospRepo,
            IServiceRepository serviceRepo,
            ISparePartRepository sparePartRepo,
            IAuditRepository auditRepo)
            : this(
                dbHelper, roRepo, customerRepo, vehicleRepo, rosRepo, rospRepo, serviceRepo, sparePartRepo, auditRepo,
                new RepairOrderLifecycleService(roRepo, rosRepo, rospRepo, auditRepo),
                new RepairOrderItemsService(rosRepo, rospRepo, serviceRepo, sparePartRepo, auditRepo),
                new VehicleHistoryService(roRepo),
                new ServiceCatalogService(serviceRepo, auditRepo))
        {
        }

        #region Order Validation & Identification

        public string GenerateNewRepairOrderNumber()
        {
            return _roRepo.GenerateNewRepairOrderNumber();
        }

        public OperationResult ValidateOrderData(Customer customer, Vehicle vehicle, RepairOrder order)
        {
            if (customer == null || string.IsNullOrWhiteSpace(customer.FullName))
                return OperationResult.Fail("الرجاء إدخال اسم الزبون.");

            if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
                return OperationResult.Fail("الرجاء إدخال رقم هاتف الزبون.");

            string phone = customer.PhoneNumber.Trim();
            if (phone.Length != 10 || !phone.All(char.IsDigit))
                return OperationResult.Fail("يجب أن يتكون رقم هاتف الزبون من 10 أرقام.");

            if (vehicle == null || string.IsNullOrWhiteSpace(vehicle.PlateNumber))
                return OperationResult.Fail("الرجاء إدخال رقم اللوحة.");

            string plate = vehicle.PlateNumber.Trim();
            // Business rule: strictly numeric and 7 digits
            if (plate.Length != 7 || !plate.All(char.IsDigit))
                return OperationResult.Fail("يجب أن يتكون رقم اللوحة من 7 أرقام (أرقام فقط).");

            if (string.IsNullOrWhiteSpace(vehicle.Vin))
                return OperationResult.Fail("الرجاء إدخال رقم الهيكل (VIN).");

            string vin = vehicle.Vin.Trim();
            if (vin.Length != 17)
                return OperationResult.Fail("يجب أن يتكون رقم الهيكل من 17 رمزاً.");

            if (string.IsNullOrWhiteSpace(vehicle.CarName))
                return OperationResult.Fail("الرجاء إدخال اسم السيارة.");

            if (string.IsNullOrWhiteSpace(vehicle.CarModel))
                return OperationResult.Fail("الرجاء إدخال موديل السيارة.");

            string model = vehicle.CarModel.Trim();
            if (model.Length != 4 || !int.TryParse(model, out _))
                return OperationResult.Fail("يجب أن يتكون موديل السيارة من 4 أرقام (مثال: 2024).");

            if (order == null || string.IsNullOrWhiteSpace(order.Complaint))
                return OperationResult.Fail("الرجاء إدخال وصف الشكوى أو العطل.");

            if (order.Complaint.Length > 500)
                return OperationResult.Fail("يجب ألا يتجاوز وصف الشكوى 500 حرف.");

            if (order.Mileage.HasValue && order.Mileage.Value < 0)
                return OperationResult.Fail("عداد الكيلومترات لا يمكن أن يكون سالباً.");

            return OperationResult.Ok();
        }

        #endregion

        #region Core Order Operations (Create / Update / Delete / GetDetails)

        public OperationResult<string> CreateRepairOrder(Customer customer, Vehicle vehicle, RepairOrder repairOrder)
        {
            var valResult = ValidateOrderData(customer, vehicle, repairOrder);
            if (!valResult.Success)
                return OperationResult<string>.Fail(valResult.Message);

            string phone = customer.PhoneNumber.Trim();
            string name = customer.FullName.Trim();
            string plate = vehicle.PlateNumber.Trim();
            string vin = vehicle.Vin.Trim();
            string carName = vehicle.CarName.Trim();
            int carModel = int.Parse(vehicle.CarModel.Trim());
            string complaint = repairOrder.Complaint.Trim();
            int? mileage = repairOrder.Mileage;
            string fuelLevel = string.IsNullOrWhiteSpace(repairOrder.FuelLevel) ? "غير محدد" : repairOrder.FuelLevel.Trim();
            string inspectionNotes = repairOrder.InspectionNotes?.Trim() ?? "";

            try
            {
                // 1. Vehicle & Customer integrity checks
                if (_vehicleRepo.VehicleExists(plate))
                {
                    DataTable dtVeh = _vehicleRepo.GetVehicleByPlateNumber(plate);
                    if (dtVeh.Rows.Count > 0)
                    {
                        string existingOwner = dtVeh.Rows[0]["CustomerPhone"]?.ToString();
                        if (existingOwner != phone)
                        {
                            return OperationResult<string>.Fail("هذه المركبة (رقم اللوحة) مسجلة باسم زبون آخر.");
                        }
                    }
                }
                else
                {
                    if (_vehicleRepo.VinExists(vin))
                    {
                        return OperationResult<string>.Fail("رقم الهيكل (VIN) مسجل لمركبة أخرى مسبقاً.");
                    }
                }

                // 2. Active RO check via VehicleHistoryService
                if (_historyService.HasActiveRepairOrder(plate))
                {
                    return OperationResult<string>.Fail("هذه المركبة لديها أمر إصلاح نشط (معلق أو قيد التنفيذ) حالياً ولا يمكن فتح أمر جديد لها.");
                }

                // 3. Generate RO number & determine receptionist
                string newRoNum = _roRepo.GenerateNewRepairOrderNumber();
                string receptionist = string.IsNullOrEmpty(UserSession.Username) ? "admin" : UserSession.Username;

                // 4. Atomic transaction using Unit of Work pattern
                using (var uow = AppServiceLocator.CreateUnitOfWork())
                {
                    uow.ExecuteInTransaction(u =>
                    {
                        if (!u.Customers.CustomerExists(phone))
                            u.Customers.AddCustomer(phone, name);
                        else
                            u.Customers.UpdateCustomer(phone, name);

                        if (!u.Vehicles.VehicleExists(plate))
                            u.Vehicles.AddVehicle(plate, vin, phone, carName, carModel, "");
                        else
                            u.Vehicles.UpdateVehicle(plate, vin, phone, carName, carModel, "");

                        u.RepairOrders.AddRepairOrder(newRoNum, plate, receptionist, complaint, RepairOrderStatus.Pending.ToDbString(), null, mileage, fuelLevel, inspectionNotes);
                    });
                }

                // 5. Audit log
                _auditRepo?.Log("فتح كرت صيانة", "RepairOrders", newRoNum,
                    $"فتح كرت صيانة جديد للمركبة {plate} ({carName}) - العميل {name} ({phone})", receptionist);

                return OperationResult<string>.Ok(newRoNum, "تم فتح كرت صيانة جديد بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail("حدث خطأ أثناء فتح كرت الصيانة: " + ex.Message);
            }
        }

        public OperationResult UpdateRepairOrder(Customer customer, Vehicle vehicle, RepairOrder repairOrder)
        {
            var valResult = ValidateOrderData(customer, vehicle, repairOrder);
            if (!valResult.Success)
                return valResult;

            if (string.IsNullOrWhiteSpace(repairOrder.RepairOrderNumber))
                return OperationResult.Fail("رقم كرت الصيانة مطلوب للتعديل.");

            string phone = customer.PhoneNumber.Trim();
            string name = customer.FullName.Trim();
            string plate = vehicle.PlateNumber.Trim();
            string vin = vehicle.Vin.Trim();
            string carName = vehicle.CarName.Trim();
            int carModel = int.Parse(vehicle.CarModel.Trim());
            string complaint = repairOrder.Complaint.Trim();
            int? mileage = repairOrder.Mileage;
            string fuelLevel = string.IsNullOrWhiteSpace(repairOrder.FuelLevel) ? "غير محدد" : repairOrder.FuelLevel.Trim();
            string inspectionNotes = repairOrder.InspectionNotes?.Trim() ?? "";

            try
            {
                if (_vehicleRepo.VinExists(vin, plate))
                {
                    return OperationResult.Fail("رقم الهيكل (VIN) الجديد مسجل لمركبة أخرى مسبقاً.");
                }

                DataTable dtRo = _roRepo.GetRepairOrderByNumber(repairOrder.RepairOrderNumber);
                string currentStatus = "Pending";
                if (dtRo.Rows.Count > 0)
                    currentStatus = dtRo.Rows[0]["Status"]?.ToString() ?? "Pending";

                using (var uow = AppServiceLocator.CreateUnitOfWork())
                {
                    uow.ExecuteInTransaction(u =>
                    {
                        if (!u.Customers.CustomerExists(phone))
                            u.Customers.AddCustomer(phone, name);
                        else
                            u.Customers.UpdateCustomer(phone, name);

                        u.Vehicles.UpdateVehicle(plate, vin, phone, carName, carModel, "");
                        u.RepairOrders.UpdateRepairOrder(repairOrder.RepairOrderNumber, complaint, currentStatus, null, mileage, fuelLevel, inspectionNotes);
                    });
                }

                string receptionist = string.IsNullOrEmpty(UserSession.Username) ? "admin" : UserSession.Username;
                _auditRepo?.Log("تعديل كرت صيانة", "RepairOrders", repairOrder.RepairOrderNumber,
                    $"تعديل كرت الصيانة للمركبة {plate} ({carName}) - العميل {name} ({phone})", receptionist);

                return OperationResult.Ok("تم تعديل بيانات كرت الصيانة بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تعديل كرت الصيانة: " + ex.Message);
            }
        }

        public RepairOrderDetailsDto GetRepairOrderDetails(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                throw new ArgumentException("رقم أمر الإصلاح غير موجود.");

            DataTable dtRo = _roRepo.GetRepairOrderByNumber(repairOrderNumber);
            if (dtRo.Rows.Count == 0)
                throw new Exception("لم يتم العثور على أمر الإصلاح في قاعدة البيانات.");

            DataRow roRow = dtRo.Rows[0];
            string plateNumber = roRow["PlateNumber"]?.ToString() ?? "";

            var dto = new RepairOrderDetailsDto
            {
                Order = new RepairOrder
                {
                    RepairOrderNumber = repairOrderNumber,
                    PlateNumber = plateNumber,
                    Receptionist = roRow["ReceptionistUsername"]?.ToString() ?? "",
                    Status = roRow["Status"]?.ToString() ?? "",
                    Complaint = roRow["Complaint"]?.ToString() ?? "",
                    EntryDate = roRow["EntryDate"] != DBNull.Value ? Convert.ToDateTime(roRow["EntryDate"]) : DateTime.Now,
                    ExpectedDate = roRow["ExpectedCompletionDate"] != DBNull.Value ? Convert.ToDateTime(roRow["ExpectedCompletionDate"]) : (DateTime?)null,
                    CompletionDate = roRow["CompletionDate"] != DBNull.Value ? Convert.ToDateTime(roRow["CompletionDate"]) : (DateTime?)null,
                    Mileage = roRow.Table.Columns.Contains("Mileage") && roRow["Mileage"] != DBNull.Value ? Convert.ToInt32(roRow["Mileage"]) : (int?)null,
                    FuelLevel = roRow.Table.Columns.Contains("FuelLevel") ? roRow["FuelLevel"]?.ToString() : "",
                    InspectionNotes = roRow.Table.Columns.Contains("InspectionNotes") ? roRow["InspectionNotes"]?.ToString() : ""
                }
            };

            DataTable dtVehicle = _vehicleRepo.GetVehicleByPlateNumber(plateNumber);
            if (dtVehicle.Rows.Count > 0)
            {
                DataRow vRow = dtVehicle.Rows[0];
                string phone = vRow["CustomerPhone"]?.ToString() ?? "";
                dto.Vehicle = new Vehicle
                {
                    PlateNumber = plateNumber,
                    CarName = vRow["CarName"]?.ToString() ?? "",
                    CarModel = vRow["CarModel"]?.ToString() ?? "",
                    Vin = vRow["VIN"]?.ToString() ?? "",
                    CustomerPhone = phone
                };

                DataTable dtCust = _customerRepo.GetCustomerByPhone(phone);
                if (dtCust.Rows.Count > 0)
                {
                    DataRow cRow = dtCust.Rows[0];
                    dto.Customer = new Customer
                    {
                        PhoneNumber = phone,
                        FullName = cRow["FullName"]?.ToString() ?? ""
                    };
                }
            }

            // Services
            DataTable dtServices = _rosRepo.GetServicesByRepairOrder(repairOrderNumber);
            foreach (DataRow s in dtServices.Rows)
            {
                decimal unitPrice = s["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(s["UnitPrice"]) : 0m;
                dto.Services.Add(new RepairOrderServiceItem
                {
                    RepairOrderNumber = repairOrderNumber,
                    ServiceName = s["ServiceName"]?.ToString() ?? "",
                    TechnicianName = s["TechnicianName"]?.ToString() ?? "",
                    Price = unitPrice,
                    TechnicianId = s["TechnicianID"] != DBNull.Value ? Convert.ToInt32(s["TechnicianID"]) : (int?)null,
                    CommissionRate = s.Table.Columns.Contains("CommissionPercent") && s["CommissionPercent"] != DBNull.Value ? Convert.ToDecimal(s["CommissionPercent"]) : 0m
                });
            }

            // Spare Parts
            DataTable dtParts = _rospRepo.GetSparePartsByRepairOrder(repairOrderNumber);
            foreach (DataRow p in dtParts.Rows)
            {
                int qty = p["Quantity"] != DBNull.Value ? Convert.ToInt32(p["Quantity"]) : 0;
                decimal unitPrice = p["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(p["UnitPrice"]) : 0m;
                dto.SpareParts.Add(new RepairOrderSparePartItem
                {
                    RepairOrderNumber = repairOrderNumber,
                    PartName = p["PartName"]?.ToString() ?? "",
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    TotalPrice = qty * unitPrice
                });
            }

            return dto;
        }

        public OperationResult DeleteRepairOrder(string repairOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(repairOrderNumber))
                return OperationResult.Fail("رقم أمر الإصلاح مطلوب.");

            try
            {
                _roRepo.DeleteRepairOrder(repairOrderNumber.Trim());
                _auditRepo?.Log("حذف أمر إصلاح", "RepairOrders", repairOrderNumber.Trim(), $"تم حذف أمر الإصلاح {repairOrderNumber}", UserSession.Username ?? "System");
                return OperationResult.Ok("تم حذف أمر الإصلاح بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex.Message);
            }
        }

        #endregion

        #region Vehicle History (Delegated to IVehicleHistoryService)

        public bool HasActiveRepairOrder(string plateNumber) => _historyService.HasActiveRepairOrder(plateNumber);

        public DataTable GetVehicleServiceHistory(string plateNumber) => _historyService.GetVehicleServiceHistory(plateNumber);

        public List<VehicleServiceHistoryRowDto> GetVehicleServiceHistoryList(string plateNumber) => _historyService.GetVehicleServiceHistoryList(plateNumber);

        #endregion

        #region Lifecycle & Status (Delegated to IRepairOrderLifecycleService)

        public OperationResult UpdateOrderStatus(string repairOrderNumber, string status) => _lifecycleService.UpdateOrderStatus(repairOrderNumber, status);

        public OperationResult ConfirmOrderServices(string repairOrderNumber) => _lifecycleService.ConfirmOrderServices(repairOrderNumber);

        public OperationResult StartOrderProgress(string repairOrderNumber) => _lifecycleService.StartOrderProgress(repairOrderNumber);

        public OperationResult RevertToPending(string repairOrderNumber) => _lifecycleService.RevertToPending(repairOrderNumber);

        public OperationResult CompleteRepairOrder(string repairOrderNumber) => _lifecycleService.CompleteRepairOrder(repairOrderNumber);

        public OperationResult CancelRepairOrder(string repairOrderNumber, string reason = "") => _lifecycleService.CancelRepairOrder(repairOrderNumber, reason);

        public string GetRepairOrderStatus(string repairOrderNumber) => _lifecycleService.GetRepairOrderStatus(repairOrderNumber);

        public List<CarStatusRowDto> GetCarStatusList(string search = "", string statusFilter = "الكل") => _lifecycleService.GetCarStatusList(search, statusFilter);

        #endregion

        #region Line Items & Spare Parts (Delegated to IRepairOrderItemsService)

        public OperationResult AddServiceToOrder(string repairOrderNumber, string serviceCode, int technicianId) => _itemsService.AddServiceToOrder(repairOrderNumber, serviceCode, technicianId);

        public OperationResult RemoveServiceFromOrder(string repairOrderNumber, string serviceCode, int technicianId) => _itemsService.RemoveServiceFromOrder(repairOrderNumber, serviceCode, technicianId);

        public OperationResult AddSparePartToOrder(string repairOrderNumber, string partCode, int quantity) => _itemsService.AddSparePartToOrder(repairOrderNumber, partCode, quantity);

        public OperationResult RemoveSparePartFromOrder(string repairOrderNumber, string partCode, int quantity) => _itemsService.RemoveSparePartFromOrder(repairOrderNumber, partCode, quantity);

        public List<OrderServiceItemDto> GetOrderServices(string repairOrderNumber) => _itemsService.GetOrderServices(repairOrderNumber);

        public List<OrderSparePartItemDto> GetOrderSpareParts(string repairOrderNumber) => _itemsService.GetOrderSpareParts(repairOrderNumber);

        #endregion

        #region Services Catalog (Delegated to IServiceCatalogService)

        public List<ServiceRowDto> GetServicesCatalog(string search = "") => _catalogService.GetServicesCatalog(search);

        public OperationResult DeleteServiceFromCatalog(string serviceCode) => _catalogService.DeleteServiceFromCatalog(serviceCode);

        public List<string> GetServiceCategories() => _catalogService.GetServiceCategories();

        public List<ServiceCatalogItemDto> GetServicesByCategory(string category) => _catalogService.GetServicesByCategory(category);

        public List<TopServiceItemDto> GetTopServices(DateTime startDate, DateTime endDate, int topN) => _catalogService.GetTopServices(startDate, endDate, topN);

        public ServiceRowDto GetServiceByCode(string serviceCode) => _catalogService.GetServiceByCode(serviceCode);

        public OperationResult SaveService(string serviceCode, string serviceName, decimal price, string category, bool isEdit) => _catalogService.SaveService(serviceCode, serviceName, price, category, isEdit);

        #endregion
    }
}
