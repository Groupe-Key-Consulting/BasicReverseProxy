using BasicReverseProxy.Core.RouteForwarding.Settings;

namespace BasicReverseProxy.Cache.Settings
{
    public class CacheRouteForwardSettings : RouteForwardSettings
    {
        public CacheSettings Cache { get; set; }
    }
}
