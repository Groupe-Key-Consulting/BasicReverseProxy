using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Core.RouteForwarding.Client
{
    public interface IWebClientFactory
    {
        IWebClient CreateWebClient(string baseUrl, HttpContext context);
        IWebClient CreateWebClient(string baseUrl, string userName, string password);
    }
}