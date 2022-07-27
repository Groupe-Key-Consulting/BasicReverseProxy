using BasicReverseProxy.Core.RouteForwarding.Services;
using BasicReverseProxy.Core.RouteForwarding.Settings;

namespace BasicReverseProxy.Core.RouteForwarding.Factories
{
    public interface IForwardServiceFactory<in T> where T : RouteForwardSettings
    {
        IForwarderService CreateForwarderService(T routeForwardSettings);
    }
}