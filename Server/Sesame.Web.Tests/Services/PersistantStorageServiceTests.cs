using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sesame.Web.DatabaseContexts;
using Sesame.Web.Models;
using Sesame.Web.Services;

namespace Sesame.Web.Tests.Services
{
    [TestClass]
    public class PersistantStorageServiceTests
    {
        async Task<MappedUserContext> _getDbContext()
        {
            var config =
                "Server=(localdb)\\mssqllocaldb;Database=SesameUserMaps;Trusted_Connection=True;MultipleActiveResultSets=true;";

            var optionsBuilder = new DbContextOptionsBuilder<MappedUserContext>();
            optionsBuilder.UseSqlServer(config);

            var context = new MappedUserContext(optionsBuilder.Options);
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        async Task<IPersistentStorageService> _getStorageService()
        {
            return new PersistentStorageService(await _getDbContext());
        }

        [TestMethod]
        public async Task TestCreatesSpeakerProfile()
        {
            var service = await _getStorageService();

            var upn = Guid.NewGuid().ToString();
            var speakerProfileType = SpeakerProfileType.Verification;
            var profileId = Guid.NewGuid().ToString();

            await service.CreateSpeakerProfileAsync(upn, speakerProfileType, profileId);

            var check = await service.GetSpeakerProfileAsync(upn, speakerProfileType);

            Assert.IsNotNull(check);

            Assert.AreEqual(profileId, check);
        }

        [TestMethod]
        public async Task TestEnrollSpeakerPin()
        {
            var service = await _getStorageService();

            var upn = Guid.NewGuid().ToString();
            var speakerProfileType = SpeakerProfileType.Verification;
            var profileId = Guid.NewGuid().ToString();

            await service.CreateSpeakerProfileAsync(upn, speakerProfileType, profileId);

            var pin = await service.EnrollSpeakerPin(upn);

            Assert.IsNotNull(pin);

            var pinResult = await service.GetPinBySpeakerAsync(upn);

            Assert.AreEqual(pin, pinResult);

            var speakerResult = await service.GetSpeakerByPinAsync(pin);

            Assert.AreEqual(upn, speakerResult);
        }

        [TestMethod]
        public async Task TestSpeakerPhrases()
        {
            var service = await _getStorageService();
            var upn = Guid.NewGuid().ToString();
            var phrase = Guid.NewGuid().ToString();

            await service.UpdateSpeakerVerificationPhraseAsync(upn, phrase);

            var result1 = await service.GetSpeakerVerificationPhraseAsync(upn);

            Assert.AreEqual(phrase, result1);

            phrase = Guid.NewGuid().ToString();
            await service.UpdateSpeakerVerificationPhraseAsync(upn, phrase);

            var result2 = await service.GetSpeakerVerificationPhraseAsync(upn);

            Assert.AreEqual(phrase, result2);

        }

        [TestMethod]
        public async Task TestSimpleClaims()
        {
            var service = await _getStorageService();

            var sc = new SimpleClaim
            {
                GivenName = Guid.NewGuid().ToString(),
                ObjectIdentifier = Guid.NewGuid().ToString(),
                Surname = Guid.NewGuid().ToString(),
                UserPrincipalName = Guid.NewGuid().ToString()
            };

            await service.UpdateClaim(sc);

            var result1 = await service.GetSimpleClaimAsync(sc.UserPrincipalName);

            Assert.AreEqual(result1.GivenName, sc.GivenName);
            Assert.AreEqual(result1.ObjectIdentifier, sc.ObjectIdentifier);
            Assert.AreEqual(result1.Surname, sc.Surname);
            Assert.AreEqual(result1.UserPrincipalName, sc.UserPrincipalName);

            var sc2 = new SimpleClaim
            {
                GivenName = Guid.NewGuid().ToString(),
                ObjectIdentifier = Guid.NewGuid().ToString(),
                Surname = Guid.NewGuid().ToString(),
                UserPrincipalName = sc.UserPrincipalName
            };

            await service.UpdateClaim(sc2);

            var result2 = await service.GetSimpleClaimAsync(sc2.UserPrincipalName);

            Assert.AreEqual(result2.GivenName, sc2.GivenName);
            Assert.AreEqual(result2.ObjectIdentifier, sc2.ObjectIdentifier);
            Assert.AreEqual(result2.Surname, sc2.Surname);
            Assert.AreEqual(result2.UserPrincipalName, sc2.UserPrincipalName);

        }
    }
}
