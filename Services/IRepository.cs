
using Webhttp.Models;

namespace SelfApiproj.DBProviders
{
    public interface IRepository
    {
       Task<userPost> Login(loginPost post);
    }
}
