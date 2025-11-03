using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShutdownSchedule.WinUI.Services
{
    public interface ILogService
    {
        Task AppendAsync(string message);
        Task<IReadOnlyList<string>> GetRecentAsync(int count);
    }
}
