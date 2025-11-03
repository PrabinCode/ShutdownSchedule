using System.Threading.Tasks;
using ShutdownSchedule.WinUI.Models;

namespace ShutdownSchedule.WinUI.Services
{
    public interface ISettingsService
    {
        Task<UserSettings> LoadAsync();
        Task SaveAsync(UserSettings settings);
        Task SetPasswordAsync(UserSettings settings, string password);
        bool VerifyPassword(UserSettings settings, string password);
    }
}
