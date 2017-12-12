using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sesame.Web.DatabaseContexts;

namespace Sesame.Web.Services
{
    public class PersistentStorageService
    {
        private readonly MappedUserContext _context;

        public PersistentStorageService(MappedUserContext context)
        {
            _context = context;
        }

        public async Task CreateSpeakerProfileAsync(string userPrincipalName, 
            SpeakerProfileType speakerProfileType,
            string profileId)
        {
            var existing = _context.Maps.FirstOrDefault(_ => _.UserPrinipleName == userPrincipalName);

            if (existing != null)
            {
                return;
            }

            var newItem = new MappedAuthentication
            {
                UserPrinipleName = userPrincipalName,
                ProfileType = speakerProfileType,
                ProfileId = profileId
            };

            await _context.AddAsync(newItem);
        }

        public async Task<string> GetSpeakerProfileAsync(string userPrincipalName,
            SpeakerProfileType speakerProfileType)
        {
            var existing = _context.Maps.FirstOrDefault(_ =>
                _.UserPrinipleName == userPrincipalName && _.ProfileType == speakerProfileType);

            return existing?.ProfileId;
        }

        private async Task<MappedAuthentication> _getSpeakerProfileAsync(string userPrincipalName,
            SpeakerProfileType speakerProfileType)
        {
            var existing = _context.Maps.FirstOrDefault(_ =>
                _.UserPrinipleName == userPrincipalName && _.ProfileType == speakerProfileType);

            return existing;
        }

        public async Task<int> EnrollSpeakerPin(string aUserPrincipalName)
        {
            var r = new Random(Convert.ToInt32(DateTime.Now.Ticks));

            var pin = r.Next(1000, 9999);

            var checkExisting = _context.Maps.FirstOrDefault(_ => _.Pin == pin.ToString());

            while (checkExisting != null)
            {
                pin = r.Next(1000, 9999);
                checkExisting = _context.Maps.FirstOrDefault(_ => _.Pin == pin.ToString());
            }


        }
}
