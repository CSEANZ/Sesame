using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreSample.Extensions
{
    public static class SesameAuthenticationBuilderExtensions
    {        
        public static AuthenticationBuilder AddSesame(this AuthenticationBuilder builder)
            => builder.AddSesame(_ => { });

        public static AuthenticationBuilder AddSesame(this AuthenticationBuilder builder, Action<SesameOidcConfiguration> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureSesameOptions>();
            builder.AddOpenIdConnect();
            return builder;
        }

        private class ConfigureSesameOptions: IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly SesameOidcConfiguration _sesameOptions;

            public ConfigureSesameOptions(IOptions<SesameOidcConfiguration> sesameOptions)
            {
                _sesameOptions = sesameOptions.Value;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                options.ClientId = _sesameOptions.ClientId;
                options.Authority = _sesameOptions.Authority;
                options.ClientSecret = _sesameOptions.ClientSecret;
                options.CallbackPath = _sesameOptions.CallbackPath;
                
                options.UseTokenLifetime = false;
                
                options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                options.ResponseType = "code";
               
                options.RequireHttpsMetadata = false;

                //so we can acces them later (see HomeController.cs)
                options.SaveTokens = true;
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}
