using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sesame.Cache;

namespace Sesame.Web.Extensions
{
    /// <summary>
    /// Helper class to register file cache extensions for working locally with unit testing around the token cache expiry
    /// </summary>
    public static class FileCacheServiceCollectionExtensions
    {
        public static IServiceCollection AddFileCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.AddSingleton<IDistributedCache, FileCache>();

            return services;
        }
    }
}
