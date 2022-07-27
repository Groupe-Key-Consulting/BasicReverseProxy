using System;
using System.Collections.Generic;
using System.Text;
using BasicReverseProxy.Core.RouteForwarding.Settings;

namespace BasicReverseProxy.Cache.Settings
{
    public class CacheRouteMappingSettings : RouteMappingSettings<CacheRouteForwardSettings>
    {
        public CacheDefaultSettings CacheDefaultSettings { get; set; } = new CacheDefaultSettings();
    }
}
