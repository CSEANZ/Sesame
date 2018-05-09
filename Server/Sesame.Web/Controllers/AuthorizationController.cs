/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using AspNet.Security.OpenIdConnect.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Core;
using OpenIddict.Models;
using Sesame.Web.Contracts;
using Sesame.Web.Helpers;
using Sesame.Web.ViewModels.Authorization;
using Sesame.Web.ViewModels.Shared;
using WebApplication1.ViewModels.Authorization;


namespace Sesame.Web.Controllers
{
    /// <summary>
    /// Main controller that handles the OAuth flow
    /// </summary>
    public class AuthorizationController : Controller
    {
        private readonly OpenIddictApplicationManager<OpenIddictApplication> _applicationManager;
        private readonly IOptions<IdentityOptions> _identityOptions;
        private ISessionStateService _sessionStateService;

        public AuthorizationController(
            OpenIddictApplicationManager<OpenIddictApplication> applicationManager,
            IOptions<IdentityOptions> identityOptions,
            ISessionStateService sessionStateService
        )
        {
            _applicationManager = applicationManager;
            _identityOptions = identityOptions;
            _sessionStateService = sessionStateService;
        }

        /// <summary>
        /// This action handles the initial request and will show the login page. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("~/connect/authorize")]
        public async Task<IActionResult> Authorize(OpenIdConnectRequest request)
        {
            Debug.Assert(request.IsAuthorizationRequest(),
                "The OpenIddict binder for ASP.NET Core MVC is not registered. " +
                "Make sure services.AddOpenIddict().AddMvcBinders() is correctly called.");

            // Retrieve the application details from the OpenIddict database.
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId, HttpContext.RequestAborted);
            if (application == null)
            {
                return View("Error", new ErrorViewModel
                {
                    Error = OpenIdConnectConstants.Errors.InvalidClient,
                    ErrorDescription = "Details concerning the calling client application cannot be found in the database"
                });
            }

            // Flow the request_id to allow OpenIddict to restore
            // the original authorization request from the cache.
            return View(new AuthorizeViewModel
            {
                ApplicationName = application.DisplayName,
                RequestId = request.RequestId,
                Scope = request.Scope,
                RedirectUri=request.RedirectUri
            });
        }

        /// <summary>
        /// When the user side code indicates it is ready to accept the authentication request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
       
        [HttpPost("~/connect/authorize"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(OpenIdConnectRequest request)
        {
            if (!_sessionStateService.Get<bool>("VoiceAuthenticated"))
            {
               return Forbid(OpenIdConnectServerDefaults.AuthenticationScheme);
            }

            var userNameId = _sessionStateService.Get<string>("UserPrincipalName");

            if (userNameId == null)
            {
                return Forbid(OpenIdConnectServerDefaults.AuthenticationScheme);
            }

            var identity = new ClaimsIdentity(
                OpenIdConnectServerDefaults.AuthenticationScheme,
                OpenIdConnectConstants.Claims.Name,
                OpenIdConnectConstants.Claims.Role);

            // Add a "sub" claim containing the user identifier, and attach
            // the "access_token" destination to allow OpenIddict to store it
            //// in the access token, so it can be retrieved from your controllers.
            identity.AddClaim(OpenIdConnectConstants.Claims.Subject, userNameId,
                OpenIdConnectConstants.Destinations.AccessToken);
            identity.AddClaim(OpenIdConnectConstants.Claims.Name, userNameId,
                OpenIdConnectConstants.Destinations.AccessToken);

            identity.AddClaim("YourOwnClaim", "Your own claim value",
                OpenIdConnectConstants.Destinations.AccessToken);

            //Add a claim to the ID token
            identity.AddClaim("SomeIDTokenClaim", "SomeIDTokenClaimValue",
                OpenIdConnectConstants.Destinations.IdentityToken);
            // ... add other claims, if necessary.

            var principal = new ClaimsPrincipal(identity);
            // Ask OpenIddict to generate a new token and return an OAuth2 token response.
            return SignIn(principal, OpenIdConnectServerDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// The client side code has rejected the request
        /// </summary>
        /// <returns></returns>
        [FormValueRequired("submit.Deny")]
        [HttpPost("~/connect/authorize"), ValidateAntiForgeryToken]
        public IActionResult Deny()
        {
            // Notify OpenIddict that the authorization grant has been denied by the resource owner
            // to redirect the user agent to the client application using the appropriate response_mode.
            return Forbid(OpenIdConnectServerDefaults.AuthenticationScheme);
        }

        [HttpGet("~/connect/logout")]
        public IActionResult Logout(OpenIdConnectRequest request)
        {
            // Flow the request_id to allow OpenIddict to restore
            // the original logout request from the distributed cache.
            return View(new LogoutViewModel
            {
                RequestId = request.RequestId,
            });
        }

        [HttpPost("~/connect/logout"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Ask ASP.NET Core Identity to delete the local and external cookies created
            // when the user agent is redirected from the external identity provider
            // after a successful authentication flow (e.g Google or Facebook).
           // await _signInManager.SignOutAsync();

            // Returning a SignOutResult will ask OpenIddict to redirect the user agent
            // to the post_logout_redirect_uri specified by the client application.
            return SignOut(OpenIdConnectServerDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Generate an access token to send back for the client. This is the last step of the flow in this app. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange(OpenIdConnectRequest request)
        {
            Debug.Assert(request.IsTokenRequest(),
                "The OpenIddict binder for ASP.NET Core MVC is not registered. " +
                "Make sure services.AddOpenIddict().AddMvcBinders() is correctly called.");

            if (request.IsAuthorizationCodeGrantType())
            {
                // Retrieve the claims principal stored in the authorization code.
                var info = await HttpContext.AuthenticateAsync(OpenIdConnectServerDefaults.AuthenticationScheme);


                //You may be interested in this sample code from OpenIddict samples on how to do this with ASP.NET Core Authentication. 
                //In this case however we just pass through the token information that was generated in the Accept action. 


                // Retrieve the user profile corresponding to the authorization code.
                // Note: if you want to automatically invalidate the authorization code
                // when the user password/roles change, use the following line instead:
                // var user = _signInManager.ValidateSecurityStampAsync(info.Principal);
                //var user = await _userManager.GetUserAsync(info.Principal);
                //if (user == null)
                //{
                //    return BadRequest(new OpenIdConnectResponse
                //    {
                //        Error = OpenIdConnectConstants.Errors.InvalidGrant,
                //        ErrorDescription = "The authorization code is no longer valid."
                //    });
                //}

                // Ensure the user is still allowed to sign in.
                //if (!await _signInManager.CanSignInAsync(user))
                //{
                //    return BadRequest(new OpenIdConnectResponse
                //    {
                //        Error = OpenIdConnectConstants.Errors.InvalidGrant,
                //        ErrorDescription = "The user is no longer allowed to sign in."
                //    });
                //}

                // Create a new authentication ticket, but reuse the properties stored
                // in the authorization code, including the scopes originally granted.
                var ticket = await CreateTicketAsync(request, info);

                return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
            }

            return BadRequest(new OpenIdConnectResponse
            {
                Error = OpenIdConnectConstants.Errors.UnsupportedGrantType,
                ErrorDescription = "The specified grant type is not supported."
            });
        }

        //TODO: this is an example 
        private async Task<AuthenticationTicket> CreateTicketAsync(
            OpenIdConnectRequest request,
            AuthenticateResult authResult = null)
        {
            AuthenticationProperties properties = authResult.Properties;

            var userId = authResult.Principal.Claims.FirstOrDefault(_ => _.Type == OpenIdConnectConstants.Claims.Subject)?.Value;

            if (userId == null)
            {
                return null;
            }

           // string userPrincipalName = _sessionStateService.Get<string>("UserPrincipalName");
            SimpleClaim simpleClaim = new SimpleClaim()
            {
                ObjectIdentifier = userId,
                UserPrincipalName = "SomeUPN",
                GivenName = "Joe",
                Surname = "Citizen"
            };

            // Create a new ClaimsPrincipal containing the claims that
            // will be used to create an id_token, a token or a code.
            //var principal = await _signInManager.CreateUserPrincipalAsync(user);

            var identity = new ClaimsIdentity(
                OpenIdConnectServerDefaults.AuthenticationScheme,
                OpenIdConnectConstants.Claims.Name,
                OpenIdConnectConstants.Claims.Role);
            // Add a "sub" claim containing the user identifier, and attach
            // the "access_token" destination to allow OpenIddict to store it
            // in the access token, so it can be retrieved from your controllers.
            identity.AddClaim(OpenIdConnectConstants.Claims.Subject, simpleClaim.ObjectIdentifier,
                OpenIdConnectConstants.Destinations.IdentityToken);
            identity.AddClaim(OpenIdConnectConstants.Claims.Name, simpleClaim.GivenName,
                OpenIdConnectConstants.Destinations.IdentityToken);
            identity.AddClaim(OpenIdConnectConstants.Claims.GivenName, simpleClaim.GivenName,
                OpenIdConnectConstants.Destinations.IdentityToken);
            identity.AddClaim(OpenIdConnectConstants.Claims.FamilyName, simpleClaim.Surname,
                OpenIdConnectConstants.Destinations.IdentityToken);
            identity.AddClaim(ClaimTypes.Upn, simpleClaim.UserPrincipalName,
                OpenIdConnectConstants.Destinations.IdentityToken);
            // ... add other claims, if necessary.
            var principal = new ClaimsPrincipal(identity);

            // Create a new authentication ticket holding the user identity.
            var ticket = new AuthenticationTicket(principal, properties,
                OpenIdConnectServerDefaults.AuthenticationScheme);

            if (!request.IsAuthorizationCodeGrantType())
            {
                // Set the list of scopes granted to the client application.
                // Note: the offline_access scope must be granted
                // to allow OpenIddict to return a refresh token.
                ticket.SetScopes(new[]
                {
                    OpenIdConnectConstants.Scopes.OpenId,
                    OpenIdConnectConstants.Scopes.Email,
                    OpenIdConnectConstants.Scopes.Profile,
                    OpenIdConnectConstants.Scopes.OfflineAccess,
                    OpenIddictConstants.Scopes.Roles
                }.Intersect(request.GetScopes()));
            }

            ticket.SetResources("resource_server");

            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
            // whether they should be included in access tokens, in identity tokens or in both.

            foreach (var claim in ticket.Principal.Claims)
            {
                // Never include the security stamp in the access and identity tokens, as it's a secret value.
                if (claim.Type == _identityOptions.Value.ClaimsIdentity.SecurityStampClaimType)
                {
                    continue;
                }

                var destinations = new List<string>
                {
                    OpenIdConnectConstants.Destinations.AccessToken
                };

                // Only add the iterated claim to the id_token if the corresponding scope was granted to the client application.
                // The other claims will only be added to the access_token, which is encrypted when using the default format.
                if ((claim.Type == OpenIdConnectConstants.Claims.Name && ticket.HasScope(OpenIdConnectConstants.Scopes.Profile)) ||
                    (claim.Type == OpenIdConnectConstants.Claims.Email && ticket.HasScope(OpenIdConnectConstants.Scopes.Email)) ||
                    (claim.Type == OpenIdConnectConstants.Claims.Role && ticket.HasScope(OpenIddictConstants.Claims.Roles)))
                {
                    destinations.Add(OpenIdConnectConstants.Destinations.IdentityToken);
                }

                claim.SetDestinations(destinations);
            }

            return ticket;
        }
    }
}