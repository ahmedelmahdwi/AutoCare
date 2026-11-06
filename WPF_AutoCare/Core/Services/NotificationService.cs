using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AutoCare.Application.Interfaces;
using AutoCare.Domain.Entities;
using C__project_Auto_Care.Core.DI;

namespace C__project_Auto_Care.Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly ILoggerService _logger;
        private readonly DispatcherTimer _pollTimer;

        public ObservableCollection<NotificationItem> Notifications { get; } = new ObservableCollection<NotificationItem>();
        public int UnreadCount => Notifications.Count(n => !n.IsRead);
        public bool HasUnreadNotifications => UnreadCount > 0;

        public event Action NotificationsChanged;

        public NotificationService(IUnitOfWorkFactory uowFactory = null, ILoggerService logger = null)
        {
            _uowFactory = uowFactory;
            _logger = logger;

            _pollTimer = new DispatcherTimer();
            _pollTimer.Tick += async (s, e) => await RefreshNotificationsAsync();
        }

        private bool _isFirstRefresh = true;

        public void StartMonitoring(int intervalSeconds = 60)
        {
            _pollTimer.Interval = TimeSpan.FromSeconds(Math.Max(15, intervalSeconds));
            _pollTimer.Start();
            _ = RefreshNotificationsAsync();
        }

        public void StopMonitoring()
        {
            _pollTimer.Stop();
        }

        public async Task RefreshNotificationsAsync()
        {
            try
            {
                var factory = _uowFactory ?? AppContainer.Resolve<IUnitOfWorkFactory>();
                if (factory == null) return;

                using (var uow = factory.Create())
                {
                    if (uow?.Notifications == null) return;

                    if (_isFirstRefresh)
                    {
                        _isFirstRefresh = false;
                        try
                        {
                            await uow.Notifications.SyncActiveAlertsOnStartupAsync();
                        }
                        catch { }
                    }

                    // Automatically generate all system alerts
                    try
                    {
                        await uow.Notifications.GenerateLowStockAlertsAsync();
                        await uow.Notifications.GeneratePendingOrdersAlertsAsync();
                        await uow.Notifications.GenerateDeliveryDueAlertsAsync();
                        await uow.Notifications.GenerateUnpaidInvoicesAlertsAsync();
                    }
                    catch { }

                    var items = await uow.Notifications.GetRecentAsync(25);

                    RunOnUI(() =>
                    {
                        Notifications.Clear();
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                if (!item.IsRead)
                                {
                                    Notifications.Add(item);
                                }
                            }
                        }
                        NotificationsChanged?.Invoke();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.Warn($"Notification refresh skipped: {ex.Message}", "NotificationService");
            }
        }

        public async Task MarkAsReadAsync(int id)
        {
            try
            {
                var factory = _uowFactory ?? AppContainer.Resolve<IUnitOfWorkFactory>();
                if (factory != null)
                {
                    using (var uow = factory.Create())
                    {
                        await uow.Notifications.MarkAsReadAsync(id);
                    }
                }

                RunOnUI(() =>
                {
                    var existing = Notifications.FirstOrDefault(n => n.Id == id);
                    if (existing != null)
                    {
                        existing.IsRead = true;
                        Notifications.Remove(existing);
                    }
                    NotificationsChanged?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _logger?.Warn($"MarkAsRead failed for #{id}: {ex.Message}", "NotificationService");
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            try
            {
                var factory = _uowFactory ?? AppContainer.Resolve<IUnitOfWorkFactory>();
                if (factory != null)
                {
                    using (var uow = factory.Create())
                    {
                        await uow.Notifications.MarkAllAsReadAsync();
                    }
                }

                RunOnUI(() =>
                {
                    foreach (var n in Notifications)
                    {
                        n.IsRead = true;
                    }
                    Notifications.Clear();
                    NotificationsChanged?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _logger?.Warn($"MarkAllAsRead failed: {ex.Message}", "NotificationService");
            }
        }

        public async Task AddNotificationAsync(string title, string message, NotificationSeverity severity = NotificationSeverity.Info, string category = "General", string targetRefId = null)
        {
            try
            {
                var item = new NotificationItem
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                    Category = category,
                    TargetReferenceId = targetRefId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                var factory = _uowFactory ?? AppContainer.Resolve<IUnitOfWorkFactory>();
                if (factory != null)
                {
                    using (var uow = factory.Create())
                    {
                        item.Id = await uow.Notifications.AddAsync(item);
                    }
                }

                RunOnUI(() =>
                {
                    Notifications.Insert(0, item);
                    NotificationsChanged?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _logger?.Warn($"AddNotification failed: {ex.Message}", "NotificationService");
            }
        }

        private static void RunOnUI(Action action)
        {
            if (action == null) return;
            var app = Application.Current;
            if (app != null && app.Dispatcher != null && !app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
