using System;

namespace ShutdownSchedule.WinUI.Models
{
    public enum ScheduledAction
    {
        Shutdown,
        Restart,
        Logoff,
        Hibernate
    }

    public class ScheduleEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public DateTimeOffset Time { get; set; }
        public bool Enabled { get; set; } = true;
        public ScheduledAction Action { get; set; } = ScheduledAction.Shutdown;
        /// <summary>
        /// Optional recurrence expression (simple human-friendly description or cron-like string).
        /// Keep nullable for initial implementation.
        /// </summary>
        public string? Recurrence { get; set; }
        /// <summary>
        /// If true, the action requires the user to provide password before performing.
        /// Password storage is not handled here (only flagged); implement secure storage separately.
        /// </summary>
        public bool PasswordProtected { get; set; } = false;

        public ScheduleEntry() { }

        public override string ToString()
        {
            return $"{Name ?? Id.ToString()} @ {Time:u} ({Action}) {(Enabled ? "Enabled" : "Disabled")}";
        }
    }
}
