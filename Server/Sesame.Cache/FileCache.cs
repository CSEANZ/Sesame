using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sesame.Cache
{
    public class FileCache : IDistributedCache
    {
        IDictionary<string, byte[]> cache = new Dictionary<string, byte[]>();
        object fileLock = new object();
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SesameCache.json");

        static readonly Task CompletedTask = Task.FromResult<object>(null);

        public FileCache()
        {
            ReadCache();
        }

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            byte[] value;

            cache.TryGetValue(key, out value);

            return value;
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            byte[] value;

            cache.TryGetValue(key, out value);
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Refresh(key);

            return CompletedTask;
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            cache.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Remove(key);

            return CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Remove(key);

            cache.Add(key, value);

            FlushCache();
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Set(key, value, options);

            return CompletedTask;
        }

        public void FlushCache()
        {
            string json = JsonConvert.SerializeObject(cache, Formatting.Indented);

            lock (fileLock)
            {
                File.WriteAllText(path, json);
            }
        }

        private void ReadCache()
        {
            string json = string.Empty;

            if (!File.Exists(path))
            {
                json = JsonConvert.SerializeObject(cache, Formatting.Indented);

                lock (fileLock)
                {
                    File.WriteAllText(path, json);
                }
            }

            json = File.ReadAllText(path);

            cache = JsonConvert.DeserializeObject<Dictionary<string, byte[]>>(json);
        }
    }
}
