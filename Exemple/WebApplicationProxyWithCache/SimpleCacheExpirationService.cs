using BasicReverseProxy.Cache;
using Microsoft.Extensions.Caching.Distributed;

namespace WebApplicationProxyWithCache
{
    public class SimpleCacheExpirationService : IExpirationService
    {
        private readonly IDistributedCache _distributedCache;

        public SimpleCacheExpirationService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }
        public void Expire(HttpContext httpContext)
        {
            _distributedCache.Remove("https://localhost:7158/api/longcall");
        }
    }
}
