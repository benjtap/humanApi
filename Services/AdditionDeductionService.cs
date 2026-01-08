using MongoDB.Driver;
using PaieApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaieApi.Services
{
    public class AdditionDeductionService
    {
        private readonly IMongoCollection<AdditionDeduction> _collection;

        public AdditionDeductionService(MongoDbService mongoDbService)
        {
            _collection = mongoDbService.AdditionsDeductions;
        }

        public async Task<List<AdditionDeduction>> GetAllAsync(string userId)
        {
            return await _collection.Find(x => x.UserId == userId).ToListAsync();
        }

        public async Task CreateAsync(AdditionDeduction item)
        {
            await _collection.InsertOneAsync(item);
        }

        public async Task UpdateAsync(string id, AdditionDeduction item)
        {
            await _collection.ReplaceOneAsync(x => x.Id == id && x.UserId == item.UserId, item);
        }

        public async Task DeleteAsync(string id, string userId)
        {
            await _collection.DeleteOneAsync(x => x.Id == id && x.UserId == userId);
        }
    }
}
