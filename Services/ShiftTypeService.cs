using MongoDB.Driver;
using PaieApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace PaieApi.Services
{
    public class ShiftTypeService
    {
        private readonly IMongoCollection<ShiftType> _shiftTypes;
        private readonly SystemDataService _systemDataService;

        public ShiftTypeService(MongoDbService mongoDbService, SystemDataService systemDataService)
        {
            _shiftTypes = mongoDbService.ShiftTypes;
            _systemDataService = systemDataService;
        }

        public async Task<List<ShiftType>> GetShiftTypesAsync(string userId)
        {
            var globals = await _systemDataService.GetShiftTypesAsync(); 
            var userTypes = await _shiftTypes.Find(s => s.UserId == userId).ToListAsync();
            
            // Deduplicate user types (prefer last modified? or just first). Robustness against existing DB dupes.
            userTypes = userTypes.GroupBy(u => u.NumericId).Select(g => g.First()).ToList();

            // Shadowing Logic: Exclude globals if user has a type with same NumericId
            var userNumericIds = new HashSet<int>(userTypes.Select(u => u.NumericId));
            var effectiveGlobals = globals.Where(g => !userNumericIds.Contains(g.NumericId));
            
            return effectiveGlobals.Concat(userTypes).OrderBy(s => s.NumericId).ToList();
        }

        public async Task<ShiftType> GetShiftTypeAsync(string id, string userId)
        {
            // First check user types
            var userType = await _shiftTypes.Find(s => s.Id == id && s.UserId == userId).FirstOrDefaultAsync();
            if (userType != null) return userType;

            // Then check globals
            var globalType = await _shiftTypes.Find(s => s.Id == id && s.UserId == null).FirstOrDefaultAsync();
            return globalType;
        }

        public async Task CreateShiftTypeAsync(ShiftType shiftType)
        {
            await _shiftTypes.InsertOneAsync(shiftType);
        }

        public async Task<ShiftType> UpdateShiftTypeAsync(string id, ShiftType shiftType)
        {
            // Check if exists as User Type (Direct Update)
            var existingUserType = await _shiftTypes.Find(s => s.Id == id && s.UserId == shiftType.UserId).FirstOrDefaultAsync();
            
            if (existingUserType != null)
            {
                // Normal Update
                await _shiftTypes.ReplaceOneAsync(s => s.Id == id && s.UserId == shiftType.UserId, shiftType);
                return shiftType;
            }
            
            // Check if it's a Global Type being overridden
            var existingGlobal = await _shiftTypes.Find(s => s.Id == id && s.UserId == null).FirstOrDefaultAsync();
            if (existingGlobal != null)
            {
                // SAFETY CHECK: Does the user ALREADY have a shadow copy for this NumericId?
                // This prevents duplicates if frontend still sends Global ID.
                var existingShadow = await _shiftTypes.Find(s => s.NumericId == existingGlobal.NumericId && s.UserId == shiftType.UserId).FirstOrDefaultAsync();
                
                if (existingShadow != null)
                {
                    // Redirect update to the existing shadow
                    shiftType.Id = existingShadow.Id;
                    await _shiftTypes.ReplaceOneAsync(s => s.Id == existingShadow.Id, shiftType);
                    return shiftType;
                }

                // Create New Shadow
                shiftType.Id = null; 
                // UserId is already set in controller
                
                await _shiftTypes.InsertOneAsync(shiftType);
                return shiftType; 
            }
            
            // Not found
            return null;
        }

        public async Task DeleteShiftTypeAsync(string id, string userId)
        {
            await _shiftTypes.DeleteOneAsync(s => s.Id == id && s.UserId == userId);
        }
    }
}
