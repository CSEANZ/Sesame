using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Sesame.Web.Services
{
    /// <summary>
    /// Helper class to check and refresh tokens in the AdalDistributedTokenCache. 
    /// Helps ensure users are still valid in Azure Active Directory
    /// </summary>
    public class TokenHelper : ITokenHelper
    {
        public async Task<AuthenticationResult> Validate(IDistributedCache distributedCache, string userId, string authority, string resource, string clientId)
        {
            var adalCache = new AdalDistributedTokenCache(distributedCache, userId);

            var authContext = new AuthenticationContext(authority, adalCache);

            try
            {
                var result = await authContext.AcquireTokenSilentAsync(resource, clientId);

                return result;
            }
            catch (Exception ex)
            {
                // Log...

                return null;
            }
        }
    }
}