using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sesame.Web
{
    public class SimpleClaim
    {
        public string ObjectIdentifier { get; set; }
        public string UserPrincipalName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
    }
}
