using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sesame.Web.Contracts;

namespace Sesame.Web.Services
{
    /// <summary>
    /// Helper service to set information in the HttpContext session state
    /// </summary>
    public class SessionStateService : ISessionStateService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionStateService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

            // The default session provider in ASP.NET Core loads the session record from the underlying IDistributedCache store asynchronously only if the ISession.LoadAsync method is explicitly called before the TryGetValue, Set, or Remove methods.If LoadAsync is not called first, the underlying session record is loaded synchronously, which could potentially impact the ability of the app to scale.
           _httpContextAccessor.HttpContext.Session.LoadAsync();

            // Or, to have applications enforce this pattern, wrap the DistributedSessionStore and DistributedSession implementations with versions that throw an exception if the LoadAsync method is not called before TryGetValue, Set, or Remove. Register the wrapped versions in the services container.
        }

        public T Get<T>(string key)
        {
            var value = _httpContextAccessor.HttpContext.Session.GetString(key);

            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);

        }
        public void Set<T>(string key, T value)
        {
            _httpContextAccessor.HttpContext.Session.SetString(key, JsonConvert.SerializeObject(value));
        }
    }
}