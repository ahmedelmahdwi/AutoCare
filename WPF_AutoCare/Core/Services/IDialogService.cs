using System.Windows;
using C__project_Auto_Care.Theme;

namespace C__project_Auto_Care.Core.Services
{
    /// <summary>
    /// Decoupled Dialog and User Notification abstraction for ViewModels and Domain logic.
    /// Eliminates direct tight-coupling to static UI message boxes.
    /// </summary>
    public interface IDialogService
    {
        void ShowInfo(string message, string title = "معلومات", Window owner = null);
        void ShowSuccess(string message, string title = "نجاح", Window owner = null);
        void ShowWarning(string message, string title = "تنبيه", Window owner = null);
        void ShowError(string message, string title = "خطأ", Window owner = null);
        bool Confirm(string message, string title = "تأكيد", Window owner = null);
        bool ConfirmDelete(string itemName, Window owner = null);
    }

    /// <summary>
    /// Default DialogService implementation wrapping AutoCare custom WMessageBox.
    /// </summary>
    public class DialogService : IDialogService
    {
        public void ShowInfo(string message, string title = "معلومات", Window owner = null)
        {
            WMessageBox.Show(message, title, WMsgIcon.Info, owner);
        }

        public void ShowSuccess(string message, string title = "نجاح", Window owner = null)
        {
            WMessageBox.Show(message, title, WMsgIcon.Success, owner);
        }

        public void ShowWarning(string message, string title = "تنبيه", Window owner = null)
        {
            WMessageBox.Show(message, title, WMsgIcon.Warning, owner);
        }

        public void ShowError(string message, string title = "خطأ", Window owner = null)
        {
            WMessageBox.Show(message, title, WMsgIcon.Error, owner);
        }

        public bool Confirm(string message, string title = "تأكيد", Window owner = null)
        {
            return WMessageBox.Confirm(message, title, owner);
        }

        public bool ConfirmDelete(string itemName, Window owner = null)
        {
            return WMessageBox.Confirm($"هل أنت متأكد من حذف {itemName}؟\nلا يمكن التراجع عن هذه العملية.", "تأكيد الحذف", owner);
        }
    }
}
