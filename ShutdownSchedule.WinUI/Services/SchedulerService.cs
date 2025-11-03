using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ShutdownSchedule.WinUI.Models;

namespace ShutdownSchedule.WinUI.Services
{
    public class SchedulerService : ISchedulerService
    {
        private readonly IExecutionService _executionService;
        private readonly string _dataFilePath;
        private ScheduleEntry? _currentEntry;

        public SchedulerService(IExecutionService executionService)
        {
            _executionService = executionService;
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShutdownSchedule");
            Directory.CreateDirectory(appData);
            _dataFilePath = Path.Combine(appData, "schedule.json");
            LoadFromFile();
        }

        private void LoadFromFile()
        {
            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    _currentEntry = null;
                    return;
                }

                var json = File.ReadAllText(_dataFilePath);
                _currentEntry = JsonSerializer.Deserialize(json, ScheduleEntryJsonContext.Default.ScheduleEntry);
            }
            catch
            {
                _currentEntry = null;
            }
        }

        public Task SaveAsync()
        {
            try
            {
                if (_currentEntry is null)
                {
                    if (File.Exists(_dataFilePath))
                    {
                        File.Delete(_dataFilePath);
                    }
                }
                else
                {
                    var json = JsonSerializer.Serialize(_currentEntry, ScheduleEntryJsonContext.Default.ScheduleEntry);
                    File.WriteAllText(_dataFilePath, json);
                }
            }
            catch
            {
                // Swallow persistence errors silently; UI will surface execution errors separately.
            }

            return Task.CompletedTask;
        }

        public Task<ScheduleEntry?> GetCurrentAsync()
        {
            return Task.FromResult(_currentEntry);
        }

        public async Task<(bool Success, string ErrorMessage)> ScheduleAsync(ScheduleEntry entry)
        {
            var now = DateTimeOffset.Now;
            if (entry.Time <= now)
            {
                return (false, "Please choose a future time.");
            }

            var seconds = (int)Math.Ceiling((entry.Time - now).TotalSeconds);
            seconds = Math.Clamp(seconds, 0, 315_360_000); // 10 years

            var arguments = entry.Action switch
            {
                ScheduledAction.Restart => $"/r /t {seconds}",
                ScheduledAction.Logoff => seconds == 0 ? "/l" : $"/l /t {seconds}",
                _ => $"/s /t {seconds}"
            };

            var result = await _executionService.ExecuteShutdownCommandAsync(arguments).ConfigureAwait(false);
            if (!result.Success)
            {
                return result;
            }

            _currentEntry = entry;
            await SaveAsync().ConfigureAwait(false);
            return (true, string.Empty);
        }

        public async Task<(bool Success, string ErrorMessage)> CancelAsync()
        {
            var result = await _executionService.ExecuteShutdownCommandAsync("/a").ConfigureAwait(false);
            if (!result.Success)
            {
                return result;
            }

            _currentEntry = null;
            await SaveAsync().ConfigureAwait(false);
            return (true, string.Empty);
        }
    }
}
