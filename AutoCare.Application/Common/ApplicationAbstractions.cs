using System;
using System.Threading.Tasks;

namespace AutoCare.Application.Common
{
    public interface ICurrentUserService
    {
        string Username { get; }
        string FullName { get; }
        string Role { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
    }

    public interface IDialogService
    {
        void ShowInformation(string message, string title = "معلومات");
        void ShowWarning(string message, string title = "تنبيه");
        void ShowError(string message, string title = "خطأ");
        bool ShowConfirmation(string message, string title = "تأكيد العملية");
    }

    public interface ILoggerService
    {
        void LogInfo(string message, string context = null);
        void LogWarning(string message, string context = null);
        void LogError(string message, Exception ex = null, string context = null);
    }
}
