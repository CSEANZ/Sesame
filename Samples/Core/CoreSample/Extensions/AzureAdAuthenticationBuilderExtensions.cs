using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreSample.Extensions
{
    public static class AzureAdAuthenticationBuilderExtensions
    {        
        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder)
            => builder.AddAzureAd(_ => { });

        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder, Action<AzureAdOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureAzureOptions>();
            builder.AddOpenIdConnect();
            return builder;
        }

        private class ConfigureAzureOptions: IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly AzureAdOptions _azureOptions;

            public ConfigureAzureOptions(IOptions<AzureAdOptions> azureOptions)
            {
                _azureOptions = azureOptions.Value;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                //options.ClientId = _azureOptions.ClientId;
                //options.Authority = $"{_azureOptions.Instance}{_azureOptions.TenantId}";
                //options.UseTokenLifetime = true;
                //options.CallbackPath = _azureOptions.CallbackPath;
                //options.RequireHttpsMetadata = false;

                options.ClientId = "mvc";
                options.Authority = "https://localhost:44398/";
                options.UseTokenLifetime = false;
                
                options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                options.ResponseType = "code";
                options.ClientSecret =
                    "901564A5-E7FE-42CB-B10D-61EF6A8F3654";
                options.CallbackPath = "/signin-oidc";
                options.RequireHttpsMetadata = false;
                options.SaveTokens = true;
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}
