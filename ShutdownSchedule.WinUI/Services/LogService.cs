using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqEnumerable = System.Linq.Enumerable;

namespace ShutdownSchedule.WinUI.Services
{
    public class LogService : ILogService
    {
        private readonly string _logFilePath;
        private readonly SemaphoreSlim _logLock = new(1, 1);

        public LogService()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShutdownSchedule");
            Directory.CreateDirectory(appData);
            _logFilePath = Path.Combine(appData, "activity.log");
        }

        public async Task AppendAsync(string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            await _logLock.WaitAsync().ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
                await File.AppendAllTextAsync(_logFilePath, line + Environment.NewLine).ConfigureAwait(false);
            }
            finally
            {
                _logLock.Release();
            }
        }

        public async Task<IReadOnlyList<string>> GetRecentAsync(int count)
        {
            await _logLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    return Array.Empty<string>();
                }

                var lines = await File.ReadAllLinesAsync(_logFilePath).ConfigureAwait(false);
                var result = LinqEnumerable.Reverse(LinqEnumerable.Take(LinqEnumerable.Reverse(lines), count)).ToArray();
                return result;
            }
            finally
            {
                _logLock.Release();
            }
        }
    }
}
