using MongoDB.Driver;
using PaieApi.Models;
using System.Threading.Tasks;

namespace PaieApi.Services
{
    public class SettingsService
    {
        private readonly IMongoCollection<UserSetting> _userSettings;

        public SettingsService(MongoDbService mongoDbService)
        {
            _userSettings = mongoDbService.UserSettings;
        }

        public async Task<UserSetting> GetSettingsAsync(string userId)
        {
            var settings = await _userSettings.Find(s => s.UserId == userId).FirstOrDefaultAsync();
            if (settings == null)
            {
                // Default settings if not found
                return new UserSetting
                {
                    UserId = userId,
                    HourlyWage = 29.12m, // Default min wage
                    SalaryStartDay = 1,
                    TaxSettings = new TaxSettings
                    {
                        CreditPoints = 2.25m,
                        PointValue = 242,
                        PensionFund = 6,
                        StudyFund = 0
                    }
                };
            }
            return settings;
        }

        public async Task SaveSettingsAsync(UserSetting settings)
        {
            var existing = await _userSettings.Find(s => s.UserId == settings.UserId).FirstOrDefaultAsync();
            if (existing == null)
            {
                await _userSettings.InsertOneAsync(settings);
            }
            else
            {
                settings.Id = existing.Id; // Ensure Id matches for replacement
                await _userSettings.ReplaceOneAsync(s => s.Id == existing.Id, settings);
            }
        }
    }
}
