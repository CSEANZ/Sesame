using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Sesame.Web.Services;
using System.Security.Claims;

namespace Sesame.Web.Extensions
{
    /// <summary>
    /// Intercept things that happen during OpenId Connect logins (i.e. AAD auth)
    /// </summary>
    public static class OpenIdAuthenticationOptionsExtensions
    {
        public static void AddOpenIdConnectIntercepts(this OpenIdConnectOptions opts)
        {

            opts.Events = new OpenIdConnectEvents
            {
                // @Jordan, do you want any other events handled here?
                OnAuthorizationCodeReceived = async ctx =>
                {
                    var request = ctx.HttpContext.Request;

                    var currentUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);

                    var credential = new ClientCredential(ctx.Options.ClientId, ctx.Options.ClientSecret);

                    var distributedCache = ctx.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

                    string userId = ctx.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                    var cache = new AdalDistributedTokenCache(distributedCache, userId);

                    var authContext = new AuthenticationContext(ctx.Options.Authority, cache);

                    var result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                        ctx.ProtocolMessage.Code, new Uri(currentUri), credential, ctx.Options.Resource);

                    ctx.HandleCodeRedemption(result.AccessToken, result.IdToken);
                }
            };
        }
    }
}
