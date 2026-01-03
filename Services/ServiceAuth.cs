
using SelfApiproj.DBProviders;
using Webhttp.Models;

namespace SelfApiproj.Services
{

    public class ServiceAuth
    {
        private readonly IRepository _mongo;
        public ServiceAuth(IRepository mongo)
        {
            _mongo = mongo;
        }

        public async Task<userPost> getUser(loginPost user)
        {
            userPost myuser = await _mongo.Login(user);

            return myuser;
        }

    }
    
}
