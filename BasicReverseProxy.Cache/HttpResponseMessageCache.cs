using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BasicReverseProxy.Cache.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace BasicReverseProxy.Cache
{
    public class HttpResponseMessageCache : IHttpResponseMessageCache
    {
        private readonly IHttpContextKeyCalculator _httpContextKeyCalculator;
        private readonly IDistributedCache _distributedCache;
        private readonly CacheRouteMappingSettings _cacheRouteMappingSettings;

        public HttpResponseMessageCache(
            IDistributedCache distributedCache,
            IHttpContextKeyCalculator httpContextKeyCalculator,
            CacheRouteMappingSettings cacheRouteMappingSettings
        )
        {
            _cacheRouteMappingSettings = cacheRouteMappingSettings;
            _distributedCache = distributedCache;
            _httpContextKeyCalculator = httpContextKeyCalculator;
        }

        public async Task<string> GetKeyAsync(HttpContext httpContext)
        {
            return await _httpContextKeyCalculator.ComputeKeyAsync(httpContext).ConfigureAwait(true);
        }

        public async Task<string> AddAsync(string key, HttpResponseMessage httpResponseMessage, int? expirationInSeconds)
        {
            if (httpResponseMessage?.Content == null)
            {
                return null;
            }

            var value = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(true);
            await _distributedCache.SetStringAsync(key, value, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(expirationInSeconds ?? _cacheRouteMappingSettings.CacheDefaultSettings.Expiration)
            }).ConfigureAwait(true);
            return key;
        }

        public async Task<HttpResponseMessage> GetAsync(string key)
        {
            var cachedObjectString = await _distributedCache.GetStringAsync(key).ConfigureAwait(true);
            if (string.IsNullOrEmpty(cachedObjectString))
            {
                return null;
            }
            var content = cachedObjectString;
            return new HttpResponseMessage
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
                StatusCode = HttpStatusCode.OK
            };
        }

        public Task RefreshAsync(string key)
        {
            return _distributedCache.RefreshAsync(key);
        }
    }
}
