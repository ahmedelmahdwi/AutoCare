using System;

namespace AutoCare.Domain.Enums
{
    public enum RepairOrderStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Invoiced = 4,
        Cancelled = 5
    }

    public enum UserRole
    {
        Admin = 1,
        Manager = 2,
        Receptionist = 3,
        Technician = 4,
        Accountant = 5
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Card = 2,
        BankTransfer = 3,
        Deferred = 4
    }

    public enum AuditActionType
    {
        Create,
        Update,
        Delete,
        Login,
        Logout,
        Backup,
        Restore,
        Invoice,
        Settlement
    }

    public static class EnumExtensions
    {
        public static string ToDbString(this RepairOrderStatus status)
        {
            switch (status)
            {
                case RepairOrderStatus.Pending: return "Pending";
                case RepairOrderStatus.InProgress: return "InProgress";
                case RepairOrderStatus.Completed: return "Completed";
                case RepairOrderStatus.Invoiced: return "Invoiced";
                case RepairOrderStatus.Cancelled: return "Cancelled";
                default: return "Pending";
            }
        }

        public static string ToDisplayArabic(this RepairOrderStatus status)
        {
            switch (status)
            {
                case RepairOrderStatus.Pending: return "معلق";
                case RepairOrderStatus.InProgress: return "قيد الصيانة";
                case RepairOrderStatus.Completed: return "مكتمل";
                case RepairOrderStatus.Invoiced: return "تمت الفوترة";
                case RepairOrderStatus.Cancelled: return "ملغي";
                default: return "غير محدد";
            }
        }

        public static RepairOrderStatus ParseRepairOrderStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return RepairOrderStatus.Pending;
            string v = value.Trim().ToLowerInvariant();
            if (v == "pending" || v == "معلق") return RepairOrderStatus.Pending;
            if (v == "inprogress" || v == "قيد التنفيذ" || v == "قيد الصيانة") return RepairOrderStatus.InProgress;
            if (v == "completed" || v == "مكتمل") return RepairOrderStatus.Completed;
            if (v == "invoiced" || v == "تمت الفوترة" || v == "مفوتر") return RepairOrderStatus.Invoiced;
            if (v == "cancelled" || v == "ملغي") return RepairOrderStatus.Cancelled;
            return RepairOrderStatus.Pending;
        }

        public static string ToDbString(this UserRole role)
        {
            switch (role)
            {
                case UserRole.Admin: return "Admin";
                case UserRole.Manager: return "Manager";
                case UserRole.Receptionist: return "Receptionist";
                case UserRole.Technician: return "Technician";
                case UserRole.Accountant: return "Accountant";
                default: return "Receptionist";
            }
        }

        public static string ToDisplayArabic(this UserRole role)
        {
            switch (role)
            {
                case UserRole.Admin: return "مدير النظام";
                case UserRole.Manager: return "مدير صيانة";
                case UserRole.Receptionist: return "موظف استقبال";
                case UserRole.Technician: return "فني";
                case UserRole.Accountant: return "محاسب";
                default: return "مستخدم";
            }
        }

        public static UserRole ParseUserRole(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return UserRole.Receptionist;
            string v = value.Trim().ToLowerInvariant();
            if (v == "admin" || v.Contains("مدير النظام") || v == "مدير") return UserRole.Admin;
            if (v == "accountant" || v.Contains("محاسب")) return UserRole.Accountant;
            if (v == "manager" || v.Contains("مدير صيانة") || v.Contains("مشرف")) return UserRole.Manager;
            if (v == "technician" || v.Contains("فني")) return UserRole.Technician;
            return UserRole.Receptionist;
        }

        public static string ToDbString(this PaymentMethod method)
        {
            switch (method)
            {
                case PaymentMethod.Cash: return "Cash";
                case PaymentMethod.Card: return "Card";
                case PaymentMethod.BankTransfer: return "BankTransfer";
                case PaymentMethod.Deferred: return "Deferred";
                default: return "Cash";
            }
        }

        public static string ToDisplayArabic(this PaymentMethod method)
        {
            switch (method)
            {
                case PaymentMethod.Cash: return "نقداً (Cash)";
                case PaymentMethod.Card: return "بطاقة / شبكة (Card)";
                case PaymentMethod.BankTransfer: return "تحويل بنكي (Transfer)";
                case PaymentMethod.Deferred: return "آجل (Credit)";
                default: return "نقداً (Cash)";
            }
        }

        public static PaymentMethod ParsePaymentMethod(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return PaymentMethod.Cash;
            string v = value.Trim().ToLowerInvariant();
            if (v.Contains("card") || v.Contains("بطاقة") || v.Contains("شبكة")) return PaymentMethod.Card;
            if (v.Contains("transfer") || v.Contains("تحويل") || v.Contains("بنك")) return PaymentMethod.BankTransfer;
            if (v.Contains("defer") || v.Contains("credit") || v.Contains("آجل")) return PaymentMethod.Deferred;
            return PaymentMethod.Cash;
        }
    }

    public static class AppConstants
    {
        public const string CurrencySymbol = "د.ع";
        public const string SystemName = "AutoCare";
        public const string SystemTitleArabic = "AutoCare - نظام إدارة مراكز صيانة السيارات";
        public const int RequiredPlateLength = 7;
        public const int RequiredVinLength = 17;
        public const int RequiredPhoneLength = 10;
        public const decimal DefaultTaxRate = 0.0m;
        public const int DefaultLowStockThreshold = 5;
    }
}
