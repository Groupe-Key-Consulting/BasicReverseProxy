using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BasicReverseProxy.Core.RouteForwarding.Client
{
    public class WebClientFactory : IWebClientFactory
    {

        private readonly IWebClientBuilderFactory _webClientBuilderFactory;

        public WebClientFactory(IWebClientBuilderFactory webClientBuilderFactory)
        {
            this._webClientBuilderFactory = webClientBuilderFactory;
        }

        public IWebClient CreateWebClient(string baseUrl, HttpContext context)
        {
            var accessToken = GetAccessToken(context);
            return this._webClientBuilderFactory.CreateWebClientBuilder()
                .WithUrl(baseUrl)
                .WithCustomHeaderCopyFrom(context.Request)
                .WithBearerAuthentication(accessToken)
                .WithCancellationToken(context.RequestAborted)
                .Build();
        }

        public IWebClient CreateWebClient(string baseUrl, string userName, string password)
        {
            return this._webClientBuilderFactory.CreateWebClientBuilder()
                .WithUrl(baseUrl)
                .WithBasicAuthentication(userName, password)
                .Build();
        }

        private static string GetAccessToken(HttpContext context)
        {
            return context.User.Claims.FirstOrDefault(c => c.Type.Equals("access_token"))?.Value;
        }
    }
}