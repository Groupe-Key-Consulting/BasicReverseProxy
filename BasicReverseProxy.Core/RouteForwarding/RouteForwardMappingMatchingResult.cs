using BasicReverseProxy.Core.RouteForwarding.Settings;
using Microsoft.AspNetCore.Routing;

namespace BasicReverseProxy.Core.RouteForwarding
{
    internal class RouteForwardMappingMatchingResult<T> where T : RouteForwardSettings
    {
        public bool IsMatch { get; set; }
        public RouteValueDictionary RouteValueDictionary { get; set; }
        public T Settings { get; set; }
    }
}
