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
            // Check for overlaps
            var existingShifts = await _shifts.Find(s => s.UserId == shift.UserId && s.Date == shift.Date).ToListAsync();
            foreach (var existing in existingShifts)
            {
                if (IsOverlapping(shift, existing))
                {
                    throw new System.InvalidOperationException("Shift overlaps with an existing shift.");
                }
            }

            await _shifts.InsertOneAsync(shift);
            return shift;
        }

        private bool IsOverlapping(Shift newShift, Shift existingShift)
        {
            // Simple overlap check based on Entry/Exit times
            // Valid formats assumed "HH:mm"
            if (string.IsNullOrEmpty(newShift.Entry) || string.IsNullOrEmpty(newShift.Exit) ||
                string.IsNullOrEmpty(existingShift.Entry) || string.IsNullOrEmpty(existingShift.Exit))
                return false;

            int start1 = ParseTime(newShift.Entry);
            int end1 = ParseTime(newShift.Exit);
            int start2 = ParseTime(existingShift.Entry);
            int end2 = ParseTime(existingShift.Exit);

            if (end1 < start1) end1 += 24 * 60; // Spans midnight
            if (end2 < start2) end2 += 24 * 60; // Spans midnight

            return Math.Max(start1, start2) < Math.Min(end1, end2);
        }

        private int ParseTime(string time)
        {
            var parts = time.Split(':');
            if (parts.Length != 2) return 0;
            if (int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
            {
                return h * 60 + m;
            }
            return 0;
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
