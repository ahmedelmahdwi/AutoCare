using System;
using AutoCare.Domain.Common;

namespace AutoCare.Domain.Entities
{
    public enum NotificationSeverity
    {
        Info,
        Warning,
        Critical,
        Success
    }

    public class NotificationItem : Entity<int>
    {
        public new int Id { get => base.Id; set => base.Id = value; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
        public string Category { get; set; } = "General"; // Inventory, RepairOrder, Invoice, System
        public string TargetReferenceId { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? BranchId { get; set; }

        public string SeverityColor
        {
            get
            {
                switch (Severity)
                {
                    case NotificationSeverity.Critical: return "#EF4444";
                    case NotificationSeverity.Warning:  return "#F59E0B";
                    case NotificationSeverity.Success:  return "#10B981";
                    default:                            return "#0284C7";
                }
            }
        }

        public string SeverityBgColor
        {
            get
            {
                switch (Severity)
                {
                    case NotificationSeverity.Critical: return "#FEF2F2";
                    case NotificationSeverity.Warning:  return "#FFFBEB";
                    case NotificationSeverity.Success:  return "#F0FDF4";
                    default:                            return "#F0F9FF";
                }
            }
        }

        public string SeverityBorderColor
        {
            get
            {
                switch (Severity)
                {
                    case NotificationSeverity.Critical: return "#FECACA";
                    case NotificationSeverity.Warning:  return "#FDE68A";
                    case NotificationSeverity.Success:  return "#BBF7D0";
                    default:                            return "#BAE6FD";
                }
            }
        }

        public string SeverityIcon
        {
            get
            {
                switch (Severity)
                {
                    case NotificationSeverity.Critical: return "🚨";
                    case NotificationSeverity.Warning:  return "⚠️";
                    case NotificationSeverity.Success:  return "✅";
                    default:                            return "ℹ️";
                }
            }
        }

        public string FormattedTime => CreatedAt.ToString("HH:mm");
    }
}
