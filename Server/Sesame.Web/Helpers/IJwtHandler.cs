using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace Sesame.Web.Helpers
{
    public interface IJwtHandler
    {
        JWT Create(IDictionary<string, string> claims);
        TokenValidationParameters Parameters { get; }
        RsaSecurityKey GetPublicRsaSecurityKey();
        RsaSecurityKey GetPrivateRsaSecurityKey();
    }
}
