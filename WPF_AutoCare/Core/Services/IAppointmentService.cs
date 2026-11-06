using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCare.Domain.Entities;

namespace C__project_Auto_Care.Core.Services
{
    public interface IAppointmentService
    {
        Task<IReadOnlyList<Appointment>> GetAppointmentsByDateAsync(DateTime date, int? branchId = null);
        Task<Appointment> GetByIdAsync(int id);
        Task<bool> CheckConflictAsync(int technicianId, DateTime date, TimeSpan start, TimeSpan end, int? excludeId = null);
        Task<int> CreateAppointmentAsync(Appointment appointment);
        Task<bool> CancelAppointmentAsync(int id);
        Task<bool> RescheduleAppointmentAsync(int id, DateTime newDate, TimeSpan start, TimeSpan end);
        Task<string> ConvertAppointmentToRepairOrderAsync(int appointmentId, string receptionistUsername);
    }
}
