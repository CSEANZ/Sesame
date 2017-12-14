using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Tokens;

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using IdentityModel.Client;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.ActiveDirectory;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace SampleMVCSite
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                openIdConnectOptions: new OpenIdConnectAuthenticationOptions
                {
                    // The `Authority` represents the v2.0 endpoint -
                    // https://login.microsoftonline.com/common/v2.0
                    // The `Scope` describes the initial permissions that your app will need.  
                    // See https://azure.microsoft.com/documentation/articles/active-directory-v2-scopes/   
                    //
                    ClientId = "mvc",
                    Authority = "https://localhost:44398/",
                    RedirectUri = "http://localhost:53660/",
                    ClientSecret = "901564A5-E7FE-42CB-B10D-61EF6A8F3654",
                    ResponseType = "code id_token token",
                    
                    Scope = "openid profile",
                    UseTokenLifetime = false,
                    AuthenticationMode = AuthenticationMode.Active,
                    
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        SecurityTokenValidated = async n =>
                        {
                            var n2 = n;
                        },
                        AuthorizationCodeReceived = async n =>
                        {
                            //https://github.com/IdentityServer/IdentityServer3.Samples/blob/master/source/Clients/MVC%20OWIN%20Client%20(Hybrid)/Startup.cs
                            var tokenClient = new TokenClient(
                                "https://localhost:44398/connect/token",
                                "mvc");
                           
                            var tokenResponse = await tokenClient.RequestAuthorizationCodeAsync(
                                n.Code, n.RedirectUri);

                            var token = new JwtSecurityToken(tokenResponse.AccessToken);

                            var id = new ClaimsIdentity(n.AuthenticationTicket.Identity.AuthenticationType);
                            id.AddClaims(token.Claims);

                            //string userObjectId = notification.AuthenticationTicket.Identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;

                            //string authority = "https://localhost:44398/connect/token";
                            //ClientCredential cred = new ClientCredential("901564A5-E7FE-42CB-B10D-61EF6A8F3654");

                            //// Here you ask for a token using the web app's clientId as the scope, since the web app and service share the same clientId.
                            //var a = new ConfidentialClientApplication("mvc", authority, "http://localhost:53660/",cred, new TokenCache(), new TokenCache()  );//Startup.clientId, redirectUri, cred, new NaiveSessionCache(userObjectId, notification.OwinContext.Environment["System.Web.HttpContextBase"] as HttpContextBase)) { };
                            //var authResult = await a.AcquireTokenByAuthorizationCodeAsync(notification.Code,
                            //    new string[] {""}); //new string[] { clientId }, notification.Code);

                            n.AuthenticationTicket = new AuthenticationTicket(
                                new ClaimsIdentity(id.Claims, n.AuthenticationTicket.Identity.AuthenticationType, "name", "role"),
                                n.AuthenticationTicket.Properties);
                        },
                        SecurityTokenReceived = async n =>
                        {
                            var n2 = n;
                        }





                        //app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                        //    new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                        //    {
                        //        Tenant = ConfigurationManager.AppSettings["ida:Tenant"],
                        //        TokenValidationParameters = new TokenValidationParameters {
                        //             ValidAudience = ConfigurationManager.AppSettings["ida:Audience"]
                        //        },
                        //    });
                    }
                });
        }
    }
}
