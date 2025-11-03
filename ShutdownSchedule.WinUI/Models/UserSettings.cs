using System;

namespace ShutdownSchedule.WinUI.Models
{
    public sealed class UserSettings
    {
        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }
        public bool IsDarkMode { get; set; }
        public DateTimeOffset? LastScheduledTime { get; set; }
        public ScheduledAction LastScheduledAction { get; set; } = ScheduledAction.Shutdown;
    }
}
