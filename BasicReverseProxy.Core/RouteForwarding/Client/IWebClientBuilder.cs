using System.Threading;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Core.RouteForwarding.Client
{
    public interface IWebClientBuilder
    {
        IWebClientBuilder WithUrl(string url);
        IWebClientBuilder WithBearerAuthentication(string accessToken);
        IWebClientBuilder WithBasicAuthentication(string username, string password);
        IWebClientBuilder WithCancellationToken(CancellationToken cancellationToken);
        IWebClient Build();
        IWebClientBuilder WithCustomHeaderCopyFrom(HttpRequest contextRequest);
    }
}