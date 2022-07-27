using System.Collections.Generic;

namespace BasicReverseProxy.Core.RouteForwarding.Settings
{
    public enum AuthenticationType
    {
        None,
        Cookie
    }
    public class RouteMappingSettings
    {
        public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Cookie;
        public IEnumerable<RouteForwardSettings> Forwards { get; set; }
    }
}
