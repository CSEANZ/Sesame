using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sesame.Web.DatabaseContexts
{
    public class MappedUserContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public MappedUserContext(DbContextOptions<MappedUserContext> options)
            : base(options)
        {
            
        }

        public DbSet<MappedAuthentication> Maps { get; set; }
    }

    public class MappedAuthentication
    {
        [Key]
        public Guid UserId { get; set; }
        public string UserPrinipleName { get; set; }
        public SpeakerProfileType ProfileType { get; set; }
        public string ProfileId { get; set; }
        public string Pin { get; set; }
    }
}
