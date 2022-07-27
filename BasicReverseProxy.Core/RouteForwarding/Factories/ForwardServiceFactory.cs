using BasicReverseProxy.Core.RouteForwarding.Client;
using BasicReverseProxy.Core.RouteForwarding.Repositories;
using BasicReverseProxy.Core.RouteForwarding.Services;
using BasicReverseProxy.Core.RouteForwarding.Settings;

namespace BasicReverseProxy.Core.RouteForwarding.Factories
{
    public interface IForwardServiceFactory
    {
        IForwarderService CreateForwarderService(RouteForwardSettings routeForwardSettings);
    }

    public class ForwardServiceFactory : IForwardServiceFactory
    {
        private readonly IWebClientFactory _webClientFactory;

        public ForwardServiceFactory(
            IWebClientFactory webClientFactory
            )
        {
            _webClientFactory = webClientFactory;
        }

        public IForwarderService CreateForwarderService(RouteForwardSettings routeForwardSettings)
        {
            var service = CreateForwarderServiceInternal(routeForwardSettings);

            return service;
        }

        private IForwarderService CreateForwarderServiceInternal(RouteForwardSettings routeForwardSettings)
        {
            if (routeForwardSettings == null)
            {
                return null;
            }

            var apiUrl = routeForwardSettings.To;

            var repository = CreateRepository(apiUrl, _webClientFactory, GetHttpVerb(routeForwardSettings),
                    GetUrlTemplate(routeForwardSettings), routeForwardSettings);

            return new SimpleForwarderService(repository);
        }

        private static IForwardRepository CreateRepository(string apiUrl, IWebClientFactory webClientFactory,
            HttpVerb httpVerb, string urlTemplate, RouteForwardSettings routeForwardSettings)
        {
            if (routeForwardSettings.FormForward != null)
            {
                return new FormForwardRepository(apiUrl, webClientFactory, httpVerb, urlTemplate, routeForwardSettings.FormForward.FileFormPropertyName);
            }

            return new BodyForwardRepository(apiUrl, webClientFactory, httpVerb, urlTemplate);
        }

        private static string GetUrlTemplate(RouteForwardSettings routeForwardSettings)
        {
            return routeForwardSettings?.Redirect != null ? routeForwardSettings.Redirect.Url : routeForwardSettings?.Url;
        }

        public static HttpVerb GetHttpVerb(RouteForwardSettings routeForwardSettings)
        {
            return routeForwardSettings?.Redirect?.Verb ?? routeForwardSettings?.Verb ?? HttpVerb.Get;
        }
    }
}
