using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Sesame.Web.Services
{
    public interface ITokenHelper
    {
        Task<AuthenticationResult> Validate(IDistributedCache distributedCache, string userId, string authority, string resource, string clientId);
    }
}