using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sesame.Web.Models;

namespace Sesame.Web.DatabaseContexts
{
    public class MappedUserContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public MappedUserContext(DbContextOptions<MappedUserContext> options)
            : base(options)
        {
            
        }

        public DbSet<MappedAuthentication> UserMaps { get; set; }
        public DbSet<PinMap> PinMaps { get; set; }
        public DbSet<PhraseMap> PhraseMaps { get; set; }
        public DbSet<SimpleClaimDb> SimpleClaims { get; set; }
    }

    public class MappedAuthentication
    {
        [Key]
        public Guid UserId { get; set; }
        public string UserPrinipleName { get; set; }
        public SpeakerProfileType ProfileType { get; set; }
        public string ProfileId { get; set; }
        
    }

    public class PinMap
    {
        [Key]
        public Guid PinMapId { get; set; }
        public string UserPrinipleName { get; set; }
        public string Pin { get; set; }
    }

    public class PhraseMap
    {
        [Key]
        public Guid PhraseMapId { get; set; }
        public string UserPrinipleName { get; set; }
        public string Phrase { get; set; }
    }

    public class SimpleClaimDb : SimpleClaim
    {
        [Key]
        public Guid SimpleClaimId { get; set; }
    }
}
