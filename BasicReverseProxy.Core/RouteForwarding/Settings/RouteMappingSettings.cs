using System.Collections.Generic;

namespace BasicReverseProxy.Core.RouteForwarding.Settings
{
    public enum AuthenticationType
    {
        None,
        Cookie
    }
    public class RouteMappingSettings<T> where T : RouteForwardSettings
    {
        public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Cookie;
        public IEnumerable<T> Forwards { get; set; }
    }
}
