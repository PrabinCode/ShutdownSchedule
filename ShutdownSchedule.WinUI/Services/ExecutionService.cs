using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ShutdownSchedule.WinUI.Services
{
    public class ExecutionService : IExecutionService
    {
        public async Task<(bool Success, string ErrorMessage)> ExecuteShutdownCommandAsync(string arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var errorTask = process.StandardError.ReadToEndAsync();
                var outputTask = process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync().ConfigureAwait(false);

                if (process.ExitCode == 0)
                {
                    return (true, string.Empty);
                }

                var error = await errorTask.ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(error))
                {
                    error = await outputTask.ConfigureAwait(false);
                }

                return (false, string.IsNullOrWhiteSpace(error) ? $"Command exited with code {process.ExitCode}." : error.Trim());
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
