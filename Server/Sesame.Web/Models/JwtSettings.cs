using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sesame.Web.Models
{
    public class JwtSettings
    {
        public string HmacSecretKey { get; set; }
        public int ExpiryDays { get; set; }
        public string Issuer { get; set; }
        public bool UseRsa { get; set; }
        public string RsaPrivateKeyXml { get; set; }
        public string RsaPublicKeyXml { get; set; }
    }
}
