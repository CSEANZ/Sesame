using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sesame.Web.Helpers;

namespace Sesame.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args).UseKestrel(options =>
        {
            if(EnvHelper.IsDevelopmentEnvironment())
            {
                Console.WriteLine("Using development environment");
                options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                {
                    
                    if (File.Exists("devcert.pfx"))
                    {
                        listenOptions.UseHttps("devcert.pfx", "dev");
                    }
                    else
                    {
                        throw new FileNotFoundException("SSL Certificate Not Found (devcert.pfx)");
                    }
                });
            }
        })
        .UseIISIntegration()
        .UseStartup<Startup>()
        
        .Build();
    }
}
