using MongoDB.Driver;
using PaieApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaieApi.Services
{
    public class SystemDataService
    {
        private readonly IMongoCollection<PaymentType> _paymentTypes;
        private readonly IMongoCollection<SickType> _sickTypes;
        private readonly IMongoCollection<ShiftType> _shiftTypes;

        public SystemDataService(MongoDbService mongoDbService)
        {
            _paymentTypes = mongoDbService.PaymentTypes;
            _sickTypes = mongoDbService.SickTypes;
            _shiftTypes = mongoDbService.ShiftTypes;
        }

        public async Task<List<PaymentType>> GetPaymentTypesAsync()
        {
            var types = await _paymentTypes.Find(_ => true).ToListAsync();
            if (types.Count == 0)
            {
                // Seed Default Payment Types
                var defaults = new List<PaymentType>
                {
                    new PaymentType { NumericId = 1, Name = "חופש" },
                    new PaymentType { NumericId = 2, Name = "חג" },
                    new PaymentType { NumericId = 3, Name = "מחלה" },
                    new PaymentType { NumericId = 4, Name = "תשלום חודשי" },
                    new PaymentType { NumericId = 5, Name = "הבראה" },
                    new PaymentType { NumericId = 6, Name = "מילואים" }
                };
                await _paymentTypes.InsertManyAsync(defaults);
                return defaults;
            }
            return types;
        }

        public async Task<List<SickType>> GetSickTypesAsync()
        {
            var types = await _sickTypes.Find(_ => true).ToListAsync();
            if (types.Count == 0)
            {
                // Seed Default Sick Types
                var defaults = new List<SickType>
                {
                    new SickType { NumericId = 1, Name = "יום מחלה א" },
                    new SickType { NumericId = 2, Name = "יום מחלה ב" },
                    new SickType { NumericId = 3, Name = "יום מחלה ג" },
                    new SickType { NumericId = 4, Name = "יום מחלה ד ומעלה" }
                };
                await _sickTypes.InsertManyAsync(defaults);
                return defaults;
            }
            return types;
        }
        public async Task<List<ShiftType>> GetShiftTypesAsync()
        {
            var types = await _shiftTypes.Find(s => s.UserId == null).ToListAsync();
            if (types.Count == 0)
            {
                var defaults = new List<ShiftType>
                {
                    new ShiftType { NumericId = 1, Name = "בוקר", Entry = "07:00", Exit = "15:00", Color = "#FFC107", UserId = null },
                    new ShiftType { NumericId = 2, Name = "ערב", Entry = "15:00", Exit = "23:00", Color = "#2196F3", UserId = null },
                    new ShiftType { NumericId = 3, Name = "לילה", Entry = "23:00", Exit = "07:00", Color = "#3F51B5", UserId = null },
                    new ShiftType { NumericId = 4, Name = "יום ו", Entry = "07:00", Exit = "13:00", Color = "#4CAF50", UserId = null },
                    new ShiftType { NumericId = 5, Name = "מוצ״ש", Entry = "18:00", Exit = "23:00", Color = "#9C27B0", UserId = null }
                };
                await _shiftTypes.InsertManyAsync(defaults);
                return defaults;
            }
            return types;
        }
    }
}
