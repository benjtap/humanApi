
using Webhttp.Models;

namespace SelfApiproj.Services
{
    public interface IServiceAuth
    {
       Task<userPost> Login(loginPost post);
    }
}
