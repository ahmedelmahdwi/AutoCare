using System;
using AutoCare.Domain.Common;

namespace AutoCare.Domain.Entities
{
    public enum AppointmentStatus
    {
        Pending,
        Confirmed,
        InProgress,
        Completed,
        Cancelled,
        NoShow
    }

    public class Appointment : Entity<int>, IAggregateRoot
    {
        public new int Id { get => base.Id; set => base.Id = value; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int VehicleId { get; set; }
        public int? TechnicianId { get; set; }
        public int BranchId { get; set; } = 1;
        public DateTime AppointmentDate { get; set; } = DateTime.Today;
        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan EndTime { get; set; } = new TimeSpan(10, 0, 0);
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
        public string Notes { get; set; } = string.Empty;
        public string RepairOrderNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        public byte[] RowVersion { get; set; }

        // Navigation / Display Properties
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string CarModel { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;

        public string TimeRangeFormatted
        {
            get
            {
                var startDt = DateTime.Today.Add(StartTime);
                var endDt = DateTime.Today.Add(EndTime);
                return string.Format("{0:hh\\:mm} - {1:hh\\:mm}", startDt, endDt);
            }
        }

        public string StatusArabic
        {
            get
            {
                switch (Status)
                {
                    case AppointmentStatus.Pending:    return "قيد الانتظار";
                    case AppointmentStatus.Confirmed:  return "مؤكد";
                    case AppointmentStatus.InProgress: return "قيد الصيانة";
                    case AppointmentStatus.Completed:  return "مكتمل";
                    case AppointmentStatus.Cancelled:  return "ملغي";
                    case AppointmentStatus.NoShow:     return "لم يحضر";
                    default:                           return "مؤكد";
                }
            }
        }

        public string StatusBadgeColor
        {
            get
            {
                switch (Status)
                {
                    case AppointmentStatus.Pending:    return "#D97706";
                    case AppointmentStatus.Confirmed:  return "#0284C7";
                    case AppointmentStatus.InProgress: return "#2563EB";
                    case AppointmentStatus.Completed:  return "#059669";
                    case AppointmentStatus.Cancelled:  return "#DC2626";
                    case AppointmentStatus.NoShow:     return "#6B7280";
                    default:                           return "#0284C7";
                }
            }
        }

        public string StatusBadgeBgColor
        {
            get
            {
                switch (Status)
                {
                    case AppointmentStatus.Pending:    return "#FFFBEB";
                    case AppointmentStatus.Confirmed:  return "#F0F9FF";
                    case AppointmentStatus.InProgress: return "#EFF6FF";
                    case AppointmentStatus.Completed:  return "#ECFDF5";
                    case AppointmentStatus.Cancelled:  return "#FEF2F2";
                    case AppointmentStatus.NoShow:     return "#F3F4F6";
                    default:                           return "#F0F9FF";
                }
            }
        }

        public bool CanConvertToOrder => Status == AppointmentStatus.Confirmed || Status == AppointmentStatus.Pending;
        public bool CanCancel => Status == AppointmentStatus.Pending || Status == AppointmentStatus.Confirmed;
    }
}
