using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sesame.Web.DatabaseContexts;

namespace Sesame.Web.Services
{
    public class PersistentStorageService : IPersistentStorageService
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
            var existing = _context.UserMaps.FirstOrDefault(_ => _.UserPrinipleName == userPrincipalName);

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
            await _context.SaveChangesAsync();
        }

        public async Task<string> GetSpeakerProfileAsync(string userPrincipalName,
            SpeakerProfileType speakerProfileType)
        {
            var existing = await _getSpeakerProfileAsync(userPrincipalName, speakerProfileType);
            return existing?.ProfileId;
        }

        private async Task<MappedAuthentication> _getSpeakerProfileAsync(string userPrincipalName,
            SpeakerProfileType speakerProfileType)
        {
            var existing = await _context.UserMaps.FirstOrDefaultAsync(_ =>
                _.UserPrinipleName == userPrincipalName && _.ProfileType == speakerProfileType);

            return existing;
        }

        public async Task<string> EnrollSpeakerPin(string userPrincipalName)
        {
            var r = new Random(Convert.ToInt32(DateTime.Now.Millisecond));

            var pin = r.Next(1000, 9999);

            var checkExisting = _context.PinMaps.FirstOrDefault(_ => _.Pin == pin.ToString());

            while (checkExisting != null)
            {
                pin = r.Next(1000, 9999);
                checkExisting = _context.PinMaps.FirstOrDefault(_ => _.Pin == pin.ToString());
            }

            var newPinEntry = new PinMap
            {
                UserPrinipleName = userPrincipalName,
                Pin = pin.ToString()
            };

            await _context.PinMaps.AddAsync(newPinEntry);
            await _context.SaveChangesAsync();

            return pin.ToString();
        }

        public async Task<string> GetPinBySpeakerAsync(string userPrincipalName)
        {
            var existing = await _context.PinMaps.FirstOrDefaultAsync(_ => _.UserPrinipleName == userPrincipalName);
            return existing?.Pin;
        }

        public async Task<string> GetSpeakerByPinAsync(string pin)
        {
            var existing = await


            (from u in _context.UserMaps
                join p in _context.PinMaps on
                    u.UserPrinipleName equals p.UserPrinipleName
                where p.Pin == pin
                select u).FirstOrDefaultAsync();

                //(from p in _context.PinMaps
                //join u in _context.UserMaps on
                //    p.UserPrinipleName equals u.UserPrinipleName
                //where p.Pin == pin
                //select u).FirstOrDefaultAsync();


            return existing?.UserPrinipleName;
        }

        public async Task<string> GetSpeakerVerificationProfileByPinAsync(string pin)
        {
            var p = await GetSpeakerByPinAsync(pin);

            if (p == null)
            {
                return null;
            }

            var existing = await GetSpeakerProfileAsync(p, SpeakerProfileType.Verification);

            return existing;
        }

        public async Task<string> GetSpeakerVerificationPhraseAsync(string userPrincipalName)
        {
            var existing = await _context.PhraseMaps.FirstOrDefaultAsync(_ => _.UserPrinipleName == userPrincipalName);
            return existing?.Phrase;
        }

        public async Task UpdateSpeakerVerificationPhraseAsync(string userPrincipalName,
            string speakerVerificationPhrase)
        {
            var existingPhrase = await _context.PhraseMaps
                .FirstOrDefaultAsync(_ => _.UserPrinipleName == userPrincipalName);

            if (existingPhrase == null)
            {
                existingPhrase = new PhraseMap
                {
                    UserPrinipleName = userPrincipalName
                };

                await _context.PhraseMaps.AddAsync(existingPhrase);
            }

            existingPhrase.Phrase = speakerVerificationPhrase;

            await _context.SaveChangesAsync();
        }

        public async Task<SimpleClaim> GetSimpleClaimAsync(string userPrincipalName)
        {
            var existing = await _context.SimpleClaims
                .FirstOrDefaultAsync(_ => _.UserPrincipalName == userPrincipalName);

            return existing;
        }

        public async Task UpdateClaim(SimpleClaim simpleClaim)
        {
            var dbClaim = await _context.SimpleClaims
                .FirstOrDefaultAsync(_ => _.UserPrincipalName == simpleClaim.UserPrincipalName);

            if (dbClaim == null)
            {
                dbClaim = new SimpleClaimDb
                {
                    UserPrincipalName = simpleClaim.UserPrincipalName
                };

                await _context.AddAsync(dbClaim);
            }

            dbClaim.GivenName = simpleClaim.GivenName;
            dbClaim.ObjectIdentifier = simpleClaim.ObjectIdentifier;
            dbClaim.Surname = simpleClaim.Surname;

            await _context.SaveChangesAsync();
        }
    }
}
