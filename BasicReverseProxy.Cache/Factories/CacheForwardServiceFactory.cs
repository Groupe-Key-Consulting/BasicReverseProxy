using BasicReverseProxy.Cache.ForwarderServices;
using BasicReverseProxy.Cache.Settings;
using BasicReverseProxy.Core.RouteForwarding.Client;
using BasicReverseProxy.Core.RouteForwarding.Factories;
using BasicReverseProxy.Core.RouteForwarding.Services;

namespace BasicReverseProxy.Cache.Factories
{
    public class CacheForwardServiceFactory : ForwardServiceFactory<CacheRouteForwardSettings>
    {
        private readonly IHttpResponseMessageCache _httpResponseMessageCache;
        private readonly IExpirationService _expirationService;

        public CacheForwardServiceFactory(
            IWebClientFactory webClientFactory,
            IHttpResponseMessageCache httpResponseMessageCache,
            IExpirationService expirationService
            ) 
            : base(webClientFactory)
        {
            _expirationService = expirationService;
            _httpResponseMessageCache = httpResponseMessageCache;
        }

        public override IForwarderService CreateForwarderService(CacheRouteForwardSettings routeForwardSettings)
        {
            var service = base.CreateForwarderService(routeForwardSettings);

            if (routeForwardSettings?.Cache != null
                && routeForwardSettings.Cache.Enable)
            {
                if (routeForwardSettings.Cache.Action == CacheActionType.Store
                    && routeForwardSettings.FormForward == null)
                {
                    service = new CacheForwarderService(service, _httpResponseMessageCache, routeForwardSettings.Cache.Expiration, routeForwardSettings.To);
                }
                else if (routeForwardSettings.Cache.Action == CacheActionType.Expire)
                {
                    service = new CacheExpirationForwarderService(service, _httpResponseMessageCache, _expirationService);
                }
            }

            return service;
        }
    }
}
