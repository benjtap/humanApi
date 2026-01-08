using MongoDB.Driver;
using PaieApi.Models;
using System.Collections.Generic;
using System.Linq;

namespace PaieApi.Services
{
    
   

    public class MongoDbService
    {
        private readonly IMongoDatabase _database;

        public IMongoCollection<Utilisateur> Utilisateurs { get; }
        public IMongoCollection<TentativeConnexion> TentativesConnexion { get; }
        public IMongoCollection<SessionVerification> SessionsVerification { get; }
        public IMongoCollection<Shift> Shifts { get; }
        public IMongoCollection<WeeklyPlan> WeeklyPlans { get; }
        public IMongoCollection<ShiftType> ShiftTypes { get; }
        public IMongoCollection<UserSetting> UserSettings { get; }
        public IMongoCollection<AdditionDeduction> AdditionsDeductions { get; }
        public IMongoCollection<PaymentType> PaymentTypes { get; }
        public IMongoCollection<SickType> SickTypes { get; }

        public MongoDbService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            // Collections
            Utilisateurs = _database.GetCollection<Utilisateur>("utilisateurs");
            TentativesConnexion = _database.GetCollection<TentativeConnexion>("tentatives_connexion");
            SessionsVerification = _database.GetCollection<SessionVerification>("sessions_verification");
            Shifts = _database.GetCollection<Shift>("shifts");
            WeeklyPlans = _database.GetCollection<WeeklyPlan>("weekly_plans");
            ShiftTypes = _database.GetCollection<ShiftType>("shift_types");
            UserSettings = _database.GetCollection<UserSetting>("user_settings");
            AdditionsDeductions = _database.GetCollection<AdditionDeduction>("additions_deductions");
            PaymentTypes = _database.GetCollection<PaymentType>("payment_types");
            SickTypes = _database.GetCollection<SickType>("sick_types");

            // Créer les index
            CreerIndex();
        }

        private void CreerIndex()
        {
            // Index unique sur username
            var usernameIndex = Builders<Utilisateur>.IndexKeys.Ascending(u => u.Username);
            Utilisateurs.Indexes.CreateOne(new CreateIndexModel<Utilisateur>(
                usernameIndex,
                new CreateIndexOptions { Unique = true }
            ));

            // Index unique sur téléphone
            var telephoneIndex = Builders<Utilisateur>.IndexKeys.Ascending(u => u.Telephone);
            Utilisateurs.Indexes.CreateOne(new CreateIndexModel<Utilisateur>(
                telephoneIndex,
                new CreateIndexOptions { Unique = true }
            ));

            // Index TTL pour sessions (expire automatiquement après 10 minutes)
            var sessionTTL = Builders<SessionVerification>.IndexKeys.Ascending(s => s.ExpireA);
            SessionsVerification.Indexes.CreateOne(new CreateIndexModel<SessionVerification>(
                sessionTTL,
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }
            ));
        }
    }
}
