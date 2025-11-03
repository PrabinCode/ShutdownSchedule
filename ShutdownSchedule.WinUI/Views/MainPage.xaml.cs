using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ShutdownSchedule.WinUI.Models;
using ShutdownSchedule.WinUI.Services;

namespace ShutdownSchedule.WinUI.Views
{
    public sealed partial class MainPage : Page, IDisposable
    {
        private readonly Window _window;
        private readonly DispatcherTimer _statusTimer;
        private readonly ObservableCollection<string> _logs = new();
        private readonly ISettingsService _settingsService = new SettingsService();
        private readonly ILogService _logService = new LogService();
        private readonly IExecutionService _executionService = new ExecutionService();
        private readonly ISchedulerService _schedulerService;
        private readonly TrayService _trayService = new TrayService();
        private readonly DispatcherQueue _dispatcher;

        private UserSettings _settings = new();
        private ScheduleEntry? _currentSchedule;
        private AppWindow? _appWindow;
        private bool _allowClose;
        private bool _isHiddenToTray;
        private bool _initialized;

        public MainPage(Window window)
        {
            InitializeComponent();
            _window = window;
            _dispatcher = DispatcherQueue;
            _schedulerService = new SchedulerService(_executionService);
            LogListView.ItemsSource = _logs;

            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statusTimer.Tick += OnStatusTimerTick;

            Loaded += OnLoaded;

            _trayService.ShowRequested += (_, _) => _dispatcher.TryEnqueue(ShowWindowFromTray);
            _trayService.ExitRequested += (_, _) => _dispatcher.TryEnqueue(ExitFromTray);
            _trayService.CancelRequested += async (_, _) => await _dispatcher.EnqueueAsync(CancelScheduledShutdownAsync);
            _trayService.ImmediateActionRequested += async (_, action) => await _dispatcher.EnqueueAsync(() => PerformImmediateActionAsync(action, fromTray: true));
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            InitializeWindowHooks();
            await InitializeAsync().ConfigureAwait(false);
        }

        private void InitializeWindowHooks()
        {
            _appWindow = _window.AppWindow;
            if (_appWindow is null)
            {
                return;
            }

            _appWindow.Closing += OnAppWindowClosing;
            _appWindow.Changed += OnAppWindowChanged;
        }

        private async Task InitializeAsync()
        {
            _settings = await _settingsService.LoadAsync().ConfigureAwait(false);
            ApplyTheme(_settings.IsDarkMode);

            var now = DateTimeOffset.Now.AddMinutes(10);
            var stored = await _schedulerService.GetCurrentAsync().ConfigureAwait(false);
            _currentSchedule = stored;

            await _dispatcher.EnqueueAsync(() =>
            {
                DatePickerSchedule.Date = stored?.Time ?? now;
                TimePickerSchedule.Time = stored?.Time.TimeOfDay ?? now.TimeOfDay;
            }).ConfigureAwait(false);

            await RefreshLogsAsync().ConfigureAwait(false);
            await _dispatcher.EnqueueAsync(UpdateScheduledStatus).ConfigureAwait(false);

            _statusTimer.Start();
        }

        private async void OnScheduleClicked(object sender, RoutedEventArgs e)
            => await ScheduleShutdownAsync().ConfigureAwait(false);

        private async void OnCancelClicked(object sender, RoutedEventArgs e)
            => await CancelScheduledShutdownAsync().ConfigureAwait(false);

        private async void OnShutdownNowClicked(object sender, RoutedEventArgs e)
            => await PerformImmediateActionAsync(ScheduledAction.Shutdown).ConfigureAwait(false);

        private async void OnRestartNowClicked(object sender, RoutedEventArgs e)
            => await PerformImmediateActionAsync(ScheduledAction.Restart).ConfigureAwait(false);

        private async void OnLogOffClicked(object sender, RoutedEventArgs e)
            => await PerformImmediateActionAsync(ScheduledAction.Logoff).ConfigureAwait(false);

        private async void OnHibernateClicked(object sender, RoutedEventArgs e)
            => await PerformImmediateActionAsync(ScheduledAction.Hibernate).ConfigureAwait(false);

        private async void OnSetPasswordClicked(object sender, RoutedEventArgs e)
            => await SetPasswordAsync().ConfigureAwait(false);

        private async void OnThemeToggleClicked(object sender, RoutedEventArgs e)
        {
            ApplyTheme(!_settings.IsDarkMode);
            _settings.IsDarkMode = !_settings.IsDarkMode;
            await _settingsService.SaveAsync(_settings).ConfigureAwait(false);
        }

        private async Task ScheduleShutdownAsync()
        {
            var date = DatePickerSchedule.Date;
            var time = TimePickerSchedule.Time;
            var targetDateTime = date.Date + time;
            var target = new DateTimeOffset(targetDateTime, DateTimeOffset.Now.Offset);

            var entry = new ScheduleEntry
            {
                Name = "Scheduled shutdown",
                Time = target,
                Action = ScheduledAction.Shutdown
            };

            var result = await _schedulerService.ScheduleAsync(entry).ConfigureAwait(false);
            if (!result.Success)
            {
                await ShowMessageAsync("Unable to schedule", result.ErrorMessage).ConfigureAwait(false);
                return;
            }

            _currentSchedule = entry;
            _settings.LastScheduledTime = entry.Time;
            _settings.LastScheduledAction = entry.Action;
            await _settingsService.SaveAsync(_settings).ConfigureAwait(false);

            await _logService.AppendAsync($"Scheduled shutdown for {entry.Time:yyyy-MM-dd HH:mm:ss}.").ConfigureAwait(false);
            await RefreshLogsAsync().ConfigureAwait(false);
            await _dispatcher.EnqueueAsync(UpdateScheduledStatus).ConfigureAwait(false);

            _trayService.ShowNotification("Shutdown Scheduled", $"System will shut down at {entry.Time:HH:mm:ss}.", H.NotifyIcon.Core.NotificationIcon.Info);
        }

        private async Task CancelScheduledShutdownAsync()
        {
            if (_currentSchedule is null)
            {
                await ShowMessageAsync("Nothing to cancel", "There is no scheduled shutdown to cancel.").ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.PasswordHash) || string.IsNullOrWhiteSpace(_settings.PasswordSalt))
            {
                await ShowMessageAsync("Password required", "Set a password before attempting to cancel a shutdown.").ConfigureAwait(false);
                return;
            }

            var password = await PromptForPasswordAsync("Enter password", requireConfirmation: false).ConfigureAwait(false);
            if (password is null)
            {
                return;
            }

            if (!_settingsService.VerifyPassword(_settings, password))
            {
                await ShowMessageAsync("Authentication failed", "Incorrect password. Cancellation aborted.").ConfigureAwait(false);
                return;
            }

            var result = await _schedulerService.CancelAsync().ConfigureAwait(false);
            if (!result.Success)
            {
                await ShowMessageAsync("Unable to cancel", result.ErrorMessage).ConfigureAwait(false);
                return;
            }

            _currentSchedule = null;
            await _logService.AppendAsync("Scheduled shutdown canceled.").ConfigureAwait(false);
            await RefreshLogsAsync().ConfigureAwait(false);
            await _dispatcher.EnqueueAsync(UpdateScheduledStatus).ConfigureAwait(false);

            _trayService.ShowNotification("Shutdown Canceled", "The pending shutdown has been canceled.", H.NotifyIcon.Core.NotificationIcon.Info);
        }

        private async Task PerformImmediateActionAsync(ScheduledAction action, bool fromTray = false)
        {
            var arguments = action switch
            {
                ScheduledAction.Restart => "/r /t 0",
                ScheduledAction.Logoff => "/l",
                ScheduledAction.Hibernate => "/h",
                _ => "/s /t 0"
            };

            var (success, error) = await _executionService.ExecuteShutdownCommandAsync(arguments).ConfigureAwait(false);
            if (!success)
            {
                if (!fromTray)
                {
                    await ShowMessageAsync("Command failed", error).ConfigureAwait(false);
                }
                return;
            }

            var actionDescription = action switch
            {
                ScheduledAction.Restart => "Restart command executed.",
                ScheduledAction.Logoff => "Log off command executed.",
                ScheduledAction.Hibernate => "Hibernate command executed.",
                _ => "Shutdown initiated immediately."
            };

            _currentSchedule = null;
            await _schedulerService.SaveAsync().ConfigureAwait(false);
            await _logService.AppendAsync(actionDescription).ConfigureAwait(false);
            await RefreshLogsAsync().ConfigureAwait(false);
            await _dispatcher.EnqueueAsync(UpdateScheduledStatus).ConfigureAwait(false);

            var title = action switch
            {
                ScheduledAction.Restart => "Restarting",
                ScheduledAction.Logoff => "Logging Off",
                ScheduledAction.Hibernate => "Hibernating",
                _ => "Shutdown Now"
            };

            var icon = action == ScheduledAction.Logoff ? H.NotifyIcon.Core.NotificationIcon.Info : H.NotifyIcon.Core.NotificationIcon.Warning;
            _trayService.ShowNotification(title, actionDescription, icon);
        }

        private async Task SetPasswordAsync()
        {
            var password = await PromptForPasswordAsync("Set Password", requireConfirmation: true).ConfigureAwait(false);
            if (password is null)
            {
                return;
            }

            await _settingsService.SetPasswordAsync(_settings, password).ConfigureAwait(false);
            await ShowMessageAsync("Password", "Password saved successfully.").ConfigureAwait(false);
        }

        private void UpdateScheduledStatus()
        {
            if (_currentSchedule is null)
            {
                StatusTextBlock.Text = "No shutdown scheduled.";
                _trayService.UpdateStatus("Shutdown Scheduler - idle");
                return;
            }

            var remaining = _currentSchedule.Time - DateTimeOffset.Now;
            string suffix = remaining > TimeSpan.Zero ? $" ({remaining:hh\\:mm\\:ss} remaining)" : " (pending)";
            StatusTextBlock.Text = $"Shutdown scheduled for {_currentSchedule.Time:yyyy-MM-dd HH:mm:ss}{suffix}";
            _trayService.UpdateStatus($"Shutdown at {_currentSchedule.Time:HH:mm:ss}");
        }

        private async Task RefreshLogsAsync()
        {
            var entries = await _logService.GetRecentAsync(10).ConfigureAwait(false);
            await _dispatcher.EnqueueAsync(() =>
            {
                _logs.Clear();
                foreach (var entry in entries)
                {
                    _logs.Add(entry);
                }
            }).ConfigureAwait(false);
        }

        private async Task<string?> PromptForPasswordAsync(string title, bool requireConfirmation)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                XamlRoot = Content.XamlRoot,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };

            var passwordBox = new PasswordBox { PlaceholderText = "Password" };
            PasswordBox? confirmBox = null;

            if (requireConfirmation)
            {
                confirmBox = new PasswordBox { PlaceholderText = "Confirm password", Margin = new Thickness(0, 12, 0, 0) };
                dialog.Content = new StackPanel { Children = { passwordBox, confirmBox } };
            }
            else
            {
                dialog.Content = passwordBox;
            }

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                await ShowMessageAsync("Validation", "Password cannot be empty.").ConfigureAwait(false);
                return null;
            }

            if (requireConfirmation && confirmBox is not null && passwordBox.Password != confirmBox.Password)
            {
                await ShowMessageAsync("Validation", "Passwords do not match.").ConfigureAwait(false);
                return null;
            }

            return passwordBox.Password;
        }

        private void ApplyTheme(bool isDark)
        {
            RootGrid.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
            ThemeToggleButton.Content = isDark ? "Switch to Light" : "Switch to Dark";
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            await _dispatcher.EnqueueAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = new TextBlock { Text = message, TextWrapping = TextWrapping.WrapWholeWords },
                    PrimaryButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };

                await dialog.ShowAsync();
            }).ConfigureAwait(false);
        }

        private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_allowClose)
            {
                Dispose();
                return;
            }

            args.Cancel = true;
            HideToTray();
        }

        private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (sender.Presenter is OverlappedPresenter presenter && presenter.State == OverlappedPresenterState.Minimized && !_isHiddenToTray)
            {
                HideToTray();
            }
        }

        private void HideToTray()
        {
            if (_isHiddenToTray)
            {
                return;
            }

            _isHiddenToTray = true;
            _appWindow?.Hide();
            _trayService.ShowNotification("Shutdown Scheduler", "Application continues running in the system tray.", H.NotifyIcon.Core.NotificationIcon.Info);
        }

        private void ShowWindowFromTray()
        {
            if (!_isHiddenToTray)
            {
                return;
            }

            _isHiddenToTray = false;
            _appWindow?.Show();
            _window.Activate();
        }

        private void ExitFromTray()
        {
            _allowClose = true;
            _trayService.Dispose();
            _statusTimer.Stop();
            _window.Close();
        }

        public void Dispose()
        {
            _statusTimer.Stop();
            _statusTimer.Tick -= OnStatusTimerTick;
            Loaded -= OnLoaded;
            if (_appWindow is not null)
            {
                _appWindow.Closing -= OnAppWindowClosing;
                _appWindow.Changed -= OnAppWindowChanged;
            }
            _trayService.Dispose();
        }

        private void OnStatusTimerTick(object? sender, object? e)
            => UpdateScheduledStatus();
    }

    internal static class DispatcherQueueExtensions
    {
        public static ValueTask EnqueueAsync(this DispatcherQueue dispatcher, Action action)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        action();
                        completion.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        completion.SetException(ex);
                    }
                }))
            {
                completion.SetCanceled();
            }

            return new ValueTask(completion.Task);
        }

        public static ValueTask EnqueueAsync(this DispatcherQueue dispatcher, Func<Task> func)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!dispatcher.TryEnqueue(async () =>
                {
                    try
                    {
                        await func().ConfigureAwait(false);
                        completion.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        completion.SetException(ex);
                    }
                }))
            {
                completion.SetCanceled();
            }

            return new ValueTask(completion.Task);
        }
    }
}
