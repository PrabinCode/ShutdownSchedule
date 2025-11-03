using System;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownSchedule.WinUI.Models;

namespace ShutdownSchedule.WinUI.Services
{
    public class TrayService : IDisposable
    {
        private readonly TaskbarIcon _taskbarIcon;
        private bool _disposed;

        public event EventHandler? ShowRequested;
        public event EventHandler? ExitRequested;
        public event EventHandler? CancelRequested;
        public event EventHandler<ScheduledAction>? ImmediateActionRequested;

        public TrayService()
        {
            _taskbarIcon = new TaskbarIcon
            {
                ToolTipText = "Shutdown Scheduler",
                ContextMenuMode = H.NotifyIcon.ContextMenuMode.SecondWindow
            };

            BuildMenu();
            _taskbarIcon.ForceCreate();
        }

        private void BuildMenu()
        {
            var menu = new MenuFlyout();

            menu.Items.Add(CreateMenuItem("Show", (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty)));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(CreateMenuItem("Shutdown Now", (_, _) => ImmediateActionRequested?.Invoke(this, ScheduledAction.Shutdown)));
            menu.Items.Add(CreateMenuItem("Restart Now", (_, _) => ImmediateActionRequested?.Invoke(this, ScheduledAction.Restart)));
            menu.Items.Add(CreateMenuItem("Log Off", (_, _) => ImmediateActionRequested?.Invoke(this, ScheduledAction.Logoff)));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(CreateMenuItem("Cancel Scheduled", (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty)));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(CreateMenuItem("Exit", (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty)));

            _taskbarIcon.ContextFlyout = menu;
        }

        private static MenuFlyoutItem CreateMenuItem(string text, RoutedEventHandler handler)
        {
            var item = new MenuFlyoutItem { Text = text };
            item.Click += handler;
            return item;
        }

        public void UpdateStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "Shutdown Scheduler";
            }

            _taskbarIcon.ToolTipText = status;
        }

        public void ShowNotification(string title, string message, H.NotifyIcon.Core.NotificationIcon icon = H.NotifyIcon.Core.NotificationIcon.Info)
        {
            _taskbarIcon.ShowNotification(title, message, icon);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _taskbarIcon?.Dispose();
        }
    }
}
