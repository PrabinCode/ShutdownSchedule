using System.Threading.Tasks;

namespace ShutdownSchedule.WinUI.Services
{
    public interface IExecutionService
    {
        Task<(bool Success, string ErrorMessage)> ExecuteShutdownCommandAsync(string arguments);
    }
}
