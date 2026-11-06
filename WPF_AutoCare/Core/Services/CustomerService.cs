using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Enums;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IAuditRepository _auditRepo;

        public CustomerService(ICustomerRepository customerRepo, IVehicleRepository vehicleRepo, IAuditRepository auditRepo)
        {
            _customerRepo = customerRepo ?? throw new ArgumentNullException(nameof(customerRepo));
            _vehicleRepo = vehicleRepo ?? throw new ArgumentNullException(nameof(vehicleRepo));
            _auditRepo = auditRepo;
        }

        public OperationResult RegisterCustomer(string phoneNumber, string fullName)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return OperationResult.Fail("رقم الهاتف مطلوب.");
            if (string.IsNullOrWhiteSpace(fullName))
                return OperationResult.Fail("اسم العميل مطلوب.");

            string phone = phoneNumber.Trim();
            string name = fullName.Trim();

            if (_customerRepo.CustomerExists(phone))
                return OperationResult.Fail("العميل مسجل مسبقاً بهذا الرقم.");

            try
            {
                _customerRepo.AddCustomer(phone, name);
                _auditRepo?.Log(UserSession.Username ?? "System", "إضافة عميل", "Customers", phone, $"الاسم: {name}");
                return OperationResult.Ok("تم تسجيل بيانات العميل بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء حفظ العميل: " + ex.Message);
            }
        }

        public OperationResult UpdateCustomer(string phoneNumber, string fullName)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return OperationResult.Fail("رقم الهاتف مطلوب.");
            if (string.IsNullOrWhiteSpace(fullName))
                return OperationResult.Fail("اسم العميل مطلوب.");

            string phone = phoneNumber.Trim();
            string name = fullName.Trim();

            try
            {
                _customerRepo.UpdateCustomer(phone, name);
                _auditRepo?.Log(UserSession.Username ?? "System", "تعديل عميل", "Customers", phone, $"الاسم الجديد: {name}");
                return OperationResult.Ok("تم تحديث بيانات العميل بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تحديث بيانات العميل: " + ex.Message);
            }
        }

        public OperationResult DeleteCustomer(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return OperationResult.Fail("رقم الهاتف مطلوب.");

            try
            {
                string phone = phoneNumber.Trim();
                _customerRepo.SoftDeleteCustomer(phone);
                _auditRepo?.Log(UserSession.Username ?? "System", "نقل عميل لسلة المحذوفات", "Customers", phone, "تم نقل العميل إلى سلة المحذوفات");
                return OperationResult.Ok("تم نقل العميل إلى سلة المحذوفات بنجاح. يمكنك استعادته أو حذفه نهائياً من شاشة سلة المحذوفات.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء نقل العميل إلى سلة المحذوفات: " + ex.Message);
            }
        }

        public DataTable GetCustomerByPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
            return _customerRepo.GetCustomerByPhone(phoneNumber.Trim());
        }

        public OperationResult RegisterVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes)
        {
            if (string.IsNullOrWhiteSpace(plateNumber))
                return OperationResult.Fail("رقم اللوحة مطلوب.");

            string plate = plateNumber.Trim();
            // Strict Domain Rule: Plate must be exactly 7 numeric digits
            if (plate.Length != AppConstants.RequiredPlateLength || !plate.All(char.IsDigit))
            {
                return OperationResult.Fail($"رقم اللوحة غير صالح! يجب أن يتكون رقم اللوحة من {AppConstants.RequiredPlateLength} أرقام متتالية حصراً.");
            }

            if (string.IsNullOrWhiteSpace(customerPhone))
                return OperationResult.Fail("رقم هاتف العميل المالك للمركبة مطلوب.");

            if (string.IsNullOrWhiteSpace(carName))
                return OperationResult.Fail("نوع أو اسم السيارة مطلوب.");

            if (_vehicleRepo.VehicleExists(plate))
                return OperationResult.Fail($"المركبة ذات اللوحة ({plate}) مسجلة مسبقاً في النظام.");

            string trimmedVin = string.IsNullOrWhiteSpace(vin) ? null : vin.Trim();
            if (!string.IsNullOrEmpty(trimmedVin) && _vehicleRepo.VinExists(trimmedVin))
            {
                return OperationResult.Fail("رقم الشاسي (VIN) مسجل بالفعل لمركبة أخرى!");
            }

            try
            {
                _vehicleRepo.AddVehicle(plate, trimmedVin, customerPhone.Trim(), carName.Trim(), carModel, notes ?? "");
                _auditRepo?.Log(UserSession.Username ?? "System", "إضافة مركبة", "Vehicles", plate, $"السيارة: {carName} ({carModel})");
                return OperationResult.Ok("تم تسجيل المركبة بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تسجيل المركبة: " + ex.Message);
            }
        }

        public OperationResult UpdateVehicle(string plateNumber, string vin, string customerPhone, string carName, int carModel, string notes)
        {
            if (string.IsNullOrWhiteSpace(plateNumber))
                return OperationResult.Fail("رقم اللوحة مطلوب.");

            string plate = plateNumber.Trim();
            if (plate.Length != AppConstants.RequiredPlateLength || !plate.All(char.IsDigit))
            {
                return OperationResult.Fail($"رقم اللوحة غير صالح! يجب أن يتكون رقم اللوحة من {AppConstants.RequiredPlateLength} أرقام متتالية حصراً.");
            }

            string trimmedVin = string.IsNullOrWhiteSpace(vin) ? null : vin.Trim();
            if (!string.IsNullOrEmpty(trimmedVin) && _vehicleRepo.VinExists(trimmedVin, plate))
            {
                return OperationResult.Fail("رقم الشاسي (VIN) مسجل بالفعل لمركبة أخرى!");
            }

            try
            {
                _vehicleRepo.UpdateVehicle(plate, trimmedVin, customerPhone.Trim(), carName.Trim(), carModel, notes ?? "");
                _auditRepo?.Log(UserSession.Username ?? "System", "تعديل مركبة", "Vehicles", plate, $"السيارة: {carName} ({carModel})");
                return OperationResult.Ok("تم تحديث بيانات المركبة بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء تعديل المركبة: " + ex.Message);
            }
        }

        public OperationResult DeleteVehicle(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber))
                return OperationResult.Fail("رقم اللوحة مطلوب.");

            try
            {
                string plate = plateNumber.Trim();
                bool hasActiveRo = AppServiceLocator.RepairOrderRepo?.HasActiveRepairOrder(plate) ?? false;
                if (hasActiveRo)
                {
                    return OperationResult.Fail("لا يمكن حذف المركبة لوجود كرت صيانة نشط قيد التنفيذ بالورشة حالياً.");
                }

                _vehicleRepo.SoftDeleteVehicle(plate);
                _auditRepo?.Log(UserSession.Username ?? "System", "نقل مركبة لسلة المحذوفات", "Vehicles", plate, "تم نقل المركبة إلى سلة المحذوفات");
                return OperationResult.Ok("تم نقل المركبة إلى سلة المحذوفات بنجاح. يمكنك استعادتها أو حذفها نهائياً من شاشة سلة المحذوفات.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء نقل المركبة إلى سلة المحذوفات: " + ex.Message);
            }
        }

        public DataTable GetVehicleByPlate(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return null;
            return _vehicleRepo.GetVehicleByPlateNumber(plateNumber.Trim());
        }

        public VehicleWithCustomerDto GetVehicleWithCustomer(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return null;
            try
            {
                return _vehicleRepo.GetVehicleWithCustomerDto(plateNumber.Trim());
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("CustomerService.GetVehicleWithCustomer failed", ex);
                return null;
            }
        }

        public DataTable GetAllVehiclesWithCustomers(string searchTerm = "", string statusFilter = "الكل")
        {
            return _vehicleRepo.GetAllVehiclesWithCustomers(searchTerm, statusFilter);
        }

        public List<CustomerVehicleRowDto> GetVehiclesWithCustomersList(string searchTerm = "", string statusFilter = "الكل")
        {
            try
            {
                return _vehicleRepo.GetAllVehiclesWithCustomersList(searchTerm?.Trim() ?? "", statusFilter ?? "الكل");
            }
            catch (Exception ex)
            {
                Data.AppLogger.Error("CustomerService.GetVehiclesWithCustomersList failed", ex);
                return new List<CustomerVehicleRowDto>();
            }
        }
    }
}


