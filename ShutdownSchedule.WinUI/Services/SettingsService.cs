using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using ShutdownSchedule.WinUI.Models;

namespace ShutdownSchedule.WinUI.Services
{
    public class SettingsService : ISettingsService
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShutdownSchedule");
            Directory.CreateDirectory(appData);
            _settingsFilePath = Path.Combine(appData, "settings.json");
        }

        public async Task<UserSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return new UserSettings();
                }

                await using var stream = File.OpenRead(_settingsFilePath);
                var settings = await JsonSerializer.DeserializeAsync<UserSettings>(stream);
                return settings ?? new UserSettings();
            }
            catch
            {
                return new UserSettings();
            }
        }

        public async Task SaveAsync(UserSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            await using var stream = File.Create(_settingsFilePath);
            await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task SetPasswordAsync(UserSettings settings, string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var hash = deriveBytes.GetBytes(HashSize);
            settings.PasswordSalt = Convert.ToBase64String(salt);
            settings.PasswordHash = Convert.ToBase64String(hash);
            await SaveAsync(settings).ConfigureAwait(false);
        }

        public bool VerifyPassword(UserSettings settings, string password)
        {
            if (string.IsNullOrWhiteSpace(settings.PasswordHash) || string.IsNullOrWhiteSpace(settings.PasswordSalt))
            {
                return false;
            }

            try
            {
                var saltBytes = Convert.FromBase64String(settings.PasswordSalt);
                var expectedHash = Convert.FromBase64String(settings.PasswordHash);
                using var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
                var actualHash = deriveBytes.GetBytes(expectedHash.Length);
                return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
