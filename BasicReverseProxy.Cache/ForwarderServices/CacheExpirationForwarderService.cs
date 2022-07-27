using System;
using System.Net.Http;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BasicReverseProxy.Cache.ForwarderServices
{
    public class CacheExpirationForwarderService : IForwarderService
    {
        private readonly IHttpResponseMessageCache _httpResponseMessageCache;
        private readonly IForwarderService _forwarderService;
        private readonly IExpirationService _expirationService;

        public CacheExpirationForwarderService(
            IForwarderService forwarderService,
            IHttpResponseMessageCache httpResponseMessageCache,
            IExpirationService expirationService
            )
        {
            _expirationService = expirationService;
            _forwarderService = forwarderService;
            _httpResponseMessageCache = httpResponseMessageCache;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequest request, RouteValueDictionary routeValueDictionary)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var response = await _forwarderService.SendAsync(request, routeValueDictionary).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _expirationService.Expire(request.HttpContext);
            }

            return response;
        }
    }
}
