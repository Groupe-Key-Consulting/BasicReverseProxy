using System;
using System.Net.Http;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BasicReverseProxy.Cache.ForwarderServices
{
    public class CacheForwarderService : IForwarderService
    {
        private readonly IHttpResponseMessageCache _httpResponseMessageCache;
        private readonly int? _cacheExpirationTime;
        private readonly IForwarderService _forwarderService;
        private readonly string _destinationUrl;

        public CacheForwarderService(
            IForwarderService forwarderService,
            IHttpResponseMessageCache httpResponseMessageCache,
            int? cacheExpirationTime,
            string destinationUrl
            )
        {
            _destinationUrl = destinationUrl;
            _forwarderService = forwarderService;
            _cacheExpirationTime = cacheExpirationTime;
            _httpResponseMessageCache = httpResponseMessageCache;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequest request, RouteValueDictionary routeValueDictionary)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var cacheKey = await this._httpResponseMessageCache.GetKeyAsync(request.HttpContext).ConfigureAwait(true);
            var cachedResponse = await GetFromCacheAsync(cacheKey).ConfigureAwait(true);
            if (cachedResponse != null)
            {
                return cachedResponse;
            }

            var response = await _forwarderService.SendAsync(request, routeValueDictionary).ConfigureAwait(false);

            await PutIntoCacheIfSuccessAsync(response, cacheKey).ConfigureAwait(true);

            return response;
        }

        private async Task<HttpResponseMessage> GetFromCacheAsync(string cacheKey)
        {
            var cachedResponse = await this._httpResponseMessageCache.GetAsync(cacheKey).ConfigureAwait(true);
            if (cachedResponse != null)
            {
                await this._httpResponseMessageCache.RefreshAsync(cacheKey).ConfigureAwait(true);
            }
            return cachedResponse;
        }

        private async Task PutIntoCacheIfSuccessAsync(HttpResponseMessage response, string cacheKey)
        {
            if (response.IsSuccessStatusCode)
            {
                await this._httpResponseMessageCache
                    .AddAsync(cacheKey, response, _cacheExpirationTime).ConfigureAwait(true);
            }
        }
    }
}
