using BasicReverseProxy.Core.RouteForwarding.Settings;
using Microsoft.AspNetCore.Routing;

namespace BasicReverseProxy.Core.RouteForwarding
{
    internal class RouteForwardMappingMatchingResult
    {
        public bool IsMatch { get; set; }
        public RouteValueDictionary RouteValueDictionary { get; set; }
        public RouteForwardSettings Settings { get; set; }
    }
}
