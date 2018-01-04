using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Sesame.Web.Services;
using Microsoft.Extensions.Caching.Distributed;
using OpenIddict.Core;
using OpenIddict.Models;
using Sesame.Web.Extensions;
using Sesame.Web.Helpers;
using Microsoft.AspNetCore.Http;
using Sesame.Web.Contracts;
using Sesame.Web.DatabaseContexts;
using Universal.Microsoft.CognitiveServices.SpeakerRecognition;

namespace Sesame.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            if (hostingEnvironment.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }

            Configuration = builder.Build();

            HostingEnvironment = hostingEnvironment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            
            services.AddSingleton<ISessionStateService, SessionStateService>();

            services.AddTransient<ITokenHelper, TokenHelper>();

            services.AddTransient<IPersistentStorageService, PersistentStorageService>();

            services.AddSingleton<SpeakerRecognitionClient>(new SpeakerRecognitionClient(Configuration["SpeakerRecognitionKey"]));

            if (HostingEnvironment.IsDevelopment())
            {
                services.AddFileCache();
            }
            else
            {
                //See readme on how to add connections strings to your private User Secrets
                //See readme on how to initialise this database
                //this database is used to cache the AAD token/refresh token for later check
                services.AddDistributedSqlServerCache(options =>
                {
                    options.ConnectionString = Configuration.GetValue<string>("ConnectionStrings:TokenCache");
                    options.SchemaName = "dbo";
                    options.TableName = "Cache";
                });
            }

            //See readme on how to add connections strings to your private User Secrets
            //See readme on how to initialise this database
            //this database is used to store session state in a way that works across scalable servers
            services.AddDistributedSqlServerCache(options =>
            {
                options.ConnectionString = Configuration.GetValue<string>("ConnectionStrings:SessionState");
                options.SchemaName = "dbo";
                options.TableName = "State";
            });
           
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromDays(90); // Configure this...?
            });

            services.AddDbContext<MappedUserContext>(options =>
            {
                var config = Configuration.GetValue<string>("ConnectionStrings:UserMaps");
                // Configure the context to use Microsoft SQL Server.
                options.UseSqlServer(config);
            });

            //this database is used by openiddict for configuration and token staging
            //see https://github.com/openiddict
            services.AddDbContext<DbContext>(options =>
            {
                var config = Configuration.GetValue<string>("ConnectionStrings:OidcCache");
                // Configure the context to use Microsoft SQL Server.
                options.UseSqlServer(config);

                // Register the entity sets needed by OpenIddict.
                // Note: use the generic overload if you need
                // to replace the default OpenIddict entities.
                options.UseOpenIddict();
            });

            //Prepare for AAD authentication (for the enrolment side)
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            //call the extension method to set up OpenIddict
            services.ConfigureOpenIddict();

            //Prepare for AAD authentication (for the enrolment side)
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddOpenIdConnect(opts =>
            {
                Configuration.GetSection("OpenIdConnect").Bind(opts);
                //use an extension method to register events on the open id connect stuff. 
                opts.AddOpenIdConnectIntercepts();
            })
            .AddCookie();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();
            
            app.UseSession();

            //do not use the default template code for route registration, make sure to use this one 
            app.UseMvcWithDefaultRoute();

            //if the OpenIddict database has not been initialised, this will prep it for you. 
            //you may like to do this outside the app and remove this line. 
            OpenIdDictDatabaseHelpers.InitializeAsync(app.ApplicationServices, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}