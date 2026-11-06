using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AutoCare.Domain.Entities;

namespace C__project_Auto_Care.Core.Services
{
    public interface INotificationService
    {
        ObservableCollection<NotificationItem> Notifications { get; }
        int UnreadCount { get; }
        bool HasUnreadNotifications { get; }
        event Action NotificationsChanged;

        Task RefreshNotificationsAsync();
        Task MarkAsReadAsync(int id);
        Task MarkAllAsReadAsync();
        Task AddNotificationAsync(string title, string message, NotificationSeverity severity = NotificationSeverity.Info, string category = "General", string targetRefId = null);
        void StartMonitoring(int intervalSeconds = 60);
        void StopMonitoring();
    }
}
