using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShutdownSchedule.WinUI.Models;

namespace ShutdownSchedule.WinUI.Services
{
    public interface ISchedulerService
    {
        Task<ScheduleEntry?> GetCurrentAsync();
        Task<(bool Success, string ErrorMessage)> ScheduleAsync(ScheduleEntry entry);
        Task<(bool Success, string ErrorMessage)> CancelAsync();
        Task SaveAsync();
    }
}
