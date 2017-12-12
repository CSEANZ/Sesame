using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Sesame.Web.DatabaseContexts
{
    public class OpenIddictContext : DbContext
    {
        public OpenIddictContext(DbContextOptions<OpenIddictContext> options)
            : base(options)
        {
            
        }
    }
}
