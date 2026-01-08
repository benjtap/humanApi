using MongoDB.Driver;
using PaieApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaieApi.Services
{
    public class WeeklyPlanService
    {
        private readonly IMongoCollection<WeeklyPlan> _weeklyPlans;
        private readonly MongoDbService _mongoDbService;

        public WeeklyPlanService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
            _weeklyPlans = mongoDbService.WeeklyPlans;
        }

        public async Task<List<WeeklyPlan>> GetWeeklyPlansAsync(string userId)
        {
            return await _weeklyPlans.Find(p => p.UserId == userId).ToListAsync();
        }

        public async Task<WeeklyPlan> GetWeeklyPlanAsync(string id)
        {
            return await _weeklyPlans.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<WeeklyPlan> CreateWeeklyPlanAsync(WeeklyPlan plan)
        {
            await _weeklyPlans.InsertOneAsync(plan);
            return plan;
        }

        public async Task UpdateWeeklyPlanAsync(string id, WeeklyPlan planIn)
        {
            await _weeklyPlans.ReplaceOneAsync(p => p.Id == id, planIn);
        }

        public async Task RemoveWeeklyPlanAsync(string id)
        {
            await _weeklyPlans.DeleteOneAsync(p => p.Id == id);
        }
    }
}
