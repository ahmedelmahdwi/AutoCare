using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    /// <summary>
    /// Specialized Domain Service for managing the services catalog and categories.
    /// Follows Single Responsibility Principle (Clean Architecture).
    /// </summary>
    public class ServiceCatalogService : IServiceCatalogService
    {
        private readonly IServiceRepository _serviceRepo;
        private readonly IAuditRepository _auditRepo;

        public ServiceCatalogService(IServiceRepository serviceRepo, IAuditRepository auditRepo)
        {
            _serviceRepo = serviceRepo ?? throw new ArgumentNullException(nameof(serviceRepo));
            _auditRepo = auditRepo;
        }

        public List<ServiceRowDto> GetServicesCatalog(string search = "")
        {
            var list = new List<ServiceRowDto>();
            try
            {
                DataTable dt = _serviceRepo.GetAllServices(search?.Trim() ?? "");
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new ServiceRowDto
                        {
                            ServiceCode = row["ServiceCode"]?.ToString() ?? "",
                            ServiceName = row["ServiceName"]?.ToString() ?? "",
                            StandardPrice = row["StandardPrice"] != DBNull.Value ? Convert.ToDecimal(row["StandardPrice"]).ToString("0.00") : "0.00",
                            Category = row["Category"] != DBNull.Value ? row["Category"].ToString() : ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ServiceCatalogService.GetServicesCatalog failed", ex);
            }
            return list;
        }

        public OperationResult DeleteServiceFromCatalog(string serviceCode)
        {
            if (string.IsNullOrWhiteSpace(serviceCode))
                return OperationResult.Fail("رمز الخدمة مطلوب.");

            try
            {
                _serviceRepo.DeleteService(serviceCode.Trim());
                _auditRepo?.Log("حذف خدمة", "Services", serviceCode.Trim(), $"تم حذف الخدمة {serviceCode}", UserSession.Username ?? "System");
                return OperationResult.Ok("تم حذف الخدمة بنجاح.");
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء محاولة الحذف: " + ex.Message);
            }
        }

        public List<string> GetServiceCategories()
        {
            try
            {
                var cats = _serviceRepo.GetDistinctCategories();
                return cats ?? new List<string>();
            }
            catch (Exception ex)
            {
                AppLogger.Error("ServiceCatalogService.GetServiceCategories failed", ex);
                return new List<string>();
            }
        }

        public List<ServiceCatalogItemDto> GetServicesByCategory(string category)
        {
            var list = new List<ServiceCatalogItemDto>();
            if (string.IsNullOrWhiteSpace(category)) return list;
            try
            {
                DataTable dt = _serviceRepo.GetServicesByCategory(category.Trim());
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new ServiceCatalogItemDto
                        {
                            ServiceCode = row["ServiceCode"]?.ToString() ?? "",
                            ServiceName = row["ServiceName"]?.ToString() ?? "",
                            Category = row["Category"]?.ToString() ?? "",
                            Price = row["StandardPrice"] != DBNull.Value ? Convert.ToDecimal(row["StandardPrice"]) : 0m
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ServiceCatalogService.GetServicesByCategory failed", ex);
            }
            return list;
        }

        public List<TopServiceItemDto> GetTopServices(DateTime startDate, DateTime endDate, int topN)
        {
            var list = new List<TopServiceItemDto>();
            try
            {
                DataTable dt = _serviceRepo.GetTopServices(startDate.Date, endDate.Date, topN);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new TopServiceItemDto
                        {
                            ServiceCode = row["ServiceCode"]?.ToString() ?? "",
                            ServiceName = row["ServiceName"]?.ToString() ?? "",
                            Count = row["Count"] != DBNull.Value ? Convert.ToInt32(row["Count"]) : 0,
                            TotalRevenue = row["TotalRevenue"] != DBNull.Value ? Convert.ToDecimal(row["TotalRevenue"]) : 0m
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("ServiceCatalogService.GetTopServices failed", ex);
            }
            return list;
        }

        public ServiceRowDto GetServiceByCode(string serviceCode)
        {
            if (string.IsNullOrWhiteSpace(serviceCode)) return null;
            try
            {
                DataRow row = _serviceRepo.GetServiceByCode(serviceCode.Trim());
                if (row == null) return null;
                return new ServiceRowDto
                {
                    ServiceCode = row["ServiceCode"]?.ToString() ?? "",
                    ServiceName = row["ServiceName"]?.ToString() ?? "",
                    StandardPrice = row["StandardPrice"] != DBNull.Value ? Convert.ToDecimal(row["StandardPrice"]).ToString("0.00") : "0.00",
                    Category = row["Category"] != DBNull.Value ? row["Category"].ToString() : ""
                };
            }
            catch (Exception ex)
            {
                AppLogger.Error("ServiceCatalogService.GetServiceByCode failed", ex);
                return null;
            }
        }

        public OperationResult SaveService(string serviceCode, string serviceName, decimal price, string category, bool isEdit)
        {
            if (string.IsNullOrWhiteSpace(serviceCode))
                return OperationResult.Fail("رمز الخدمة مطلوب.");
            if (string.IsNullOrWhiteSpace(serviceName))
                return OperationResult.Fail("اسم الخدمة مطلوب.");
            if (price < 0)
                return OperationResult.Fail("سعر الخدمة لا يمكن أن يكون سالباً.");
            if (string.IsNullOrWhiteSpace(category))
                return OperationResult.Fail("يرجى اختيار الفئة.");

            try
            {
                string code = serviceCode.Trim();
                string name = serviceName.Trim();
                string cat = category.Trim();

                if (!isEdit)
                {
                    if (_serviceRepo.ServiceCodeExists(code))
                        return OperationResult.Fail("رمز الخدمة هذا موجود بالفعل مسبقاً. الرجاء استخدام رمز آخر.");

                    _serviceRepo.AddService(code, name, price, cat);
                    _auditRepo?.Log("إضافة خدمة", "Services", code, $"إضافة خدمة جديدة: {name} بسعر {price}", UserSession.Username ?? "System");
                    return OperationResult.Ok("تم إضافة الخدمة بنجاح.");
                }
                else
                {
                    _serviceRepo.UpdateService(code, name, price, cat);
                    _auditRepo?.Log("تعديل خدمة", "Services", code, $"تعديل بيانات الخدمة: {name} بسعر {price}", UserSession.Username ?? "System");
                    return OperationResult.Ok("تم تعديل الخدمة بنجاح.");
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("حدث خطأ أثناء الحفظ: " + ex.Message);
            }
        }
    }
}
