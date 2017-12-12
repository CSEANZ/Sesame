using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sesame.Web.Helpers
{
    public static class EnvHelper
    {
        public static bool IsDevelopmentEnvironment() => 
             string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development");
        
    }
}
