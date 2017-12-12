using Microsoft.Extensions.Caching.Distributed;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sesame.Cache;
using Sesame.Web.Services;
using System.IO;
using System.Threading.Tasks;

namespace Sesame.Web.Tests
{
    [TestClass]
    public class TokenHelperTests
    {
        [TestInitialize]

        [TestMethod]
        public async Task Validate_ValidKey_ExpectAuthenticationResult()
        {
            string authority = "https://login.microsoftonline.com/***REMOVED***/";
            string clientId = "***REMOVED***";
            string resource = "https://graph.microsoft.com/";
            string userId = "4832e139-8e1d-4083-b0ef-e9888411c42f";

            IDistributedCache distributedCache = new FileCache();

            var tokenHelper = new TokenHelper();

            var result = await tokenHelper.Validate(distributedCache, userId, authority, resource, clientId);

            Assert.IsNotNull(result);
        }
    }
}
