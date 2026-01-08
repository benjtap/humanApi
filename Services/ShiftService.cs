using MongoDB.Driver;
using PaieApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaieApi.Services
{
    public class ShiftService
    {
        private readonly IMongoCollection<Shift> _shifts;
        private readonly MongoDbService _mongoDbService;

        public ShiftService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
            _shifts = mongoDbService.Shifts;
        }

        public async Task<List<Shift>> GetShiftsAsync(string userId)
        {
            return await _shifts.Find(s => s.UserId == userId).ToListAsync();
        }

        public async Task<Shift> GetShiftAsync(string id)
        {
            return await _shifts.Find(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Shift> CreateShiftAsync(Shift shift)
        {
            await _shifts.InsertOneAsync(shift);
            return shift;
        }

        public async Task UpdateShiftAsync(string id, Shift shiftIn)
        {
            await _shifts.ReplaceOneAsync(s => s.Id == id, shiftIn);
        }

        public async Task RemoveShiftAsync(string id)
        {
            await _shifts.DeleteOneAsync(s => s.Id == id);
        }
    }
}
