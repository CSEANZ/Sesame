using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Sesame.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Sesame.Web.Services;
using Newtonsoft.Json;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sesame.Web.Controllers
{
    [Authorize]
    public class EnrollmentController : Controller
    {
        public EnrollmentController()
        {
        }

        /// <summary>
        /// The user is authentication so save their information in the user mapping database
        /// so they can be retrieved later for access via the voice interation system
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var objectIdentifierClaim = User.Claims.FirstOrDefault(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
            var upnClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Upn);
            var givenNameClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName);
            var surnameClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname);

            var claims = new List<Claim>() { objectIdentifierClaim, upnClaim, givenNameClaim, surnameClaim };

            if (!claims.TrueForAll(x => x != null))
            {
                // TODO this is bad
                // should reject this
                // also - this code should not be acessible
            }
            else
            {
                var simpleClaim = new SimpleClaim() { ObjectIdentifier = objectIdentifierClaim.Value, UserPrincipalName = upnClaim.Value, GivenName = givenNameClaim.Value, Surname = surnameClaim.Value };
                await PersistentStorage.UpdateClaim(simpleClaim);
            }

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel());
        }
    }
}
