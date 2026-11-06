using System;
using System.Collections.Generic;
using System.Data;
using C__project_Auto_Care.Core.DTOs;
using C__project_Auto_Care.Core.Entities;
using C__project_Auto_Care.Core.Enums;
using C__project_Auto_Care.Data;
using C__project_Auto_Care.Interfaces;
using C__project_Auto_Care.Models;

namespace C__project_Auto_Care.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IAuditRepository _auditRepo;

        public AuthService(IUserRepository userRepo, IAuditRepository auditRepo)
        {
            _userRepo = userRepo;
            _auditRepo = auditRepo;
        }

        public OperationResult<User> Authenticate(string username, string password)
        {
            return Authenticate(username, password, false);
        }

        public OperationResult<User> Authenticate(string username, string password, bool rememberMe)
        {
            if (string.IsNullOrWhiteSpace(username))
                return OperationResult<User>.Fail("يرجى إدخال اسم المستخدم.");
            if (string.IsNullOrEmpty(password))
                return OperationResult<User>.Fail("يرجى إدخال كلمة المرور.");

            try
            {
                bool isValid = _userRepo.Authenticate(username.Trim(), password);
                if (!isValid)
                {
                    return OperationResult<User>.Fail("اسم المستخدم أو كلمة المرور غير صحيحة.");
                }

                var userDto = _userRepo.GetUserDto(username.Trim());
                if (userDto == null)
                {
                    return OperationResult<User>.Fail("لم يتم العثور على بيانات المستخدم.");
                }

                var user = new User
                {
                    Username = userDto.Username ?? username,
                    FullName = userDto.FullName ?? username,
                    Role = userDto.Role ?? "User",
                    IsActive = userDto.IsActive
                };

                // تأكيد مسح أي بيانات سابقة من القرص لأسباب أمنية
                CredentialStore.ClearCredentials();

                UserSession.Login(user.Username, user.FullName, user.Role);
                _auditRepo?.Log("تسجيل دخول", "Users", user.Username, "تسجيل دخول ناجح للمستخدم: " + user.FullName, user.Username);

                return OperationResult<User>.Ok(user, "تم تسجيل الدخول بنجاح.");
            }
            catch (Exception ex)
            {
                return OperationResult<User>.Fail("حدث خطأ أثناء المصادقة: " + ex.Message);
            }
        }

        public void Logout()
        {
            string username = UserSession.Username;
            if (!string.IsNullOrEmpty(username))
            {
                _auditRepo?.Log("تسجيل خروج", "Users", username, "تسجيل خروج المستخدم: " + username, username);
            }
            UserSession.Logout();
        }

        public bool TryLoadSavedCredentials(out string username, out string password)
        {
            username = string.Empty;
            password = string.Empty;
            return false;
        }

        public List<UserListItemDto> GetAllUsers(string search = "", string roleFilter = "")
        {
            try
            {
                return _userRepo.GetAllUsersList(search, roleFilter);
            }
            catch (Exception ex)
            {
                AppLogger.Error("AuthService.GetAllUsers failed", ex);
                return new List<UserListItemDto>();
            }
        }

        public UserListItemDto GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            try
            {
                return _userRepo.GetUserDto(username.Trim());
            }
            catch (Exception ex)
            {
                AppLogger.Error("AuthService.GetUserByUsername failed", ex);
                return null;
            }
        }

        public OperationResult CreateUser(string username, string fullName, string role, bool isActive, string password, int branchId = 1)
        {
            if (string.IsNullOrWhiteSpace(username))
                return OperationResult.Fail("الرجاء إدخال اسم المستخدم.");
            if (string.IsNullOrWhiteSpace(fullName))
                return OperationResult.Fail("الرجاء إدخال اسم الموظف.");
            if (string.IsNullOrWhiteSpace(password))
                return OperationResult.Fail("الرجاء إدخال كلمة المرور.");

            try
            {
                if (_userRepo.UserExists(username.Trim()))
                    return OperationResult.Fail("اسم المستخدم موجود مسبقاً. الرجاء استخدام اسم آخر.");

                _userRepo.AddUser(username.Trim(), fullName.Trim(), role, isActive, password, branchId);
                _auditRepo?.Log("إضافة مستخدم", "Users", username.Trim(), "إضافة المستخدم: " + fullName.Trim(), UserSession.Username);
                return OperationResult.Ok("تم إضافة المستخدم بنجاح.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("AuthService.CreateUser failed", ex);
                return OperationResult.Fail("حدث خطأ أثناء إضافة المستخدم: " + ex.Message);
            }
        }

        public OperationResult UpdateUser(string username, string fullName, string role, bool isActive, string password, int? branchId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return OperationResult.Fail("اسم المستخدم غير صالح.");
            if (string.IsNullOrWhiteSpace(fullName))
                return OperationResult.Fail("الرجاء إدخال اسم الموظف.");

            try
            {
                // Check if modifying the only admin
                var targetUser = _userRepo.GetUserDto(username.Trim());
                if (targetUser != null)
                {
                    if (targetUser.Role == "Admin" && role == "Receptionist" && _userRepo.CountAdminUsers() <= 1)
                        return OperationResult.Fail("لا يمكن تغيير دور المدير الوحيد إلى موظف استقبال.");

                    if (targetUser.Role == "Admin" && targetUser.IsActive && !isActive && _userRepo.CountAdminUsers() <= 1)
                        return OperationResult.Fail("لا يمكن تعطيل آخر حساب مدير في النظام.");
                }

                _userRepo.UpdateUser(username.Trim(), fullName.Trim(), role, isActive, string.IsNullOrWhiteSpace(password) ? null : password, branchId);
                _auditRepo?.Log("تعديل مستخدم", "Users", username.Trim(), "تعديل بيانات المستخدم: " + fullName.Trim(), UserSession.Username);
                return OperationResult.Ok("تم تحديث بيانات المستخدم بنجاح.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("AuthService.UpdateUser failed", ex);
                return OperationResult.Fail("حدث خطأ أثناء تحديث بيانات المستخدم: " + ex.Message);
            }
        }

        public OperationResult DeleteUser(string username, string currentUsername)
        {
            if (string.IsNullOrWhiteSpace(username))
                return OperationResult.Fail("الرجاء تحديد مستخدم للحذف.");

            if (username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
                return OperationResult.Fail("لا يمكن حذف حسابك المسجل به حاليًا.");

            try
            {
                var targetUser = _userRepo.GetUserDto(username.Trim());
                if (targetUser != null)
                {
                    if (targetUser.Role == "Admin" && _userRepo.CountAdminUsers() <= 1)
                        return OperationResult.Fail("لا يمكن حذف آخر حساب مدير في النظام.");
                }

                if (_userRepo.IsUserUsedInOrders(username) || _userRepo.IsUserUsedInInvoices(username))
                    return OperationResult.Fail("لا يمكن حذف هذا المستخدم لأنه مرتبط بأوامر إصلاح أو فواتير.");

                _userRepo.DeleteUser(username);
                _auditRepo?.Log("حذف مستخدم", "Users", username, "حذف المستخدم: " + username, currentUsername);
                return OperationResult.Ok("تم حذف المستخدم بنجاح.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("AuthService.DeleteUser failed", ex);
                return OperationResult.Fail("حدث خطأ أثناء الحذف: " + ex.Message);
            }
        }
    }
}
