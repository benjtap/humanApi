

using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Webhttp.Models;

namespace SelfApiproj.DBProviders
{
    public class Repository: IRepository
    {
        public IMongoClient _mongoClient;
        public IMongoDatabase _mongoDatabase;
        public IMongoCollection<registerPostMongo> registerPostsMon { get; }
        public Repository(IMongoClient mongoClient)
        {
           
            _mongoClient = mongoClient;
            _mongoDatabase = _mongoClient.GetDatabase("contact_group");
            registerPostsMon = _mongoDatabase.GetCollection<registerPostMongo>("user");
        }


        public async Task<userPost> Login(loginPost post)
        {
            var Builder = Builders<registerPostMongo>.Filter;
            var query = Builder.Where(r => r.username == post.userName) &
                               Builder.Where(r => r.password == post.password);


            var myres = await registerPostsMon.FindAsync(query);


            var response = myres.FirstOrDefault();

            if (response != null)
            {
                userPost myuser = new userPost
                {
                    id = response.id.ToString(),
                    username = response.username

                };



                return myuser;
            }
            else
                return null;


        }


    }
}
