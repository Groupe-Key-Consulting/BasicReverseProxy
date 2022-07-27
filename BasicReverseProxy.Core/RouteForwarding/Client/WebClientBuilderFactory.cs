using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Core.RouteForwarding.Client
{
    public class WebClientBuilderFactory : IWebClientBuilderFactory
    {
        public IWebClientBuilder CreateWebClientBuilder()
        {
            return new WebClientBuilder();
        }

        private class WebClientBuilder : IWebClientBuilder
        {
            private readonly System.Net.Http.HttpClient _httpClient;
            private CancellationToken _cancellationToken = CancellationToken.None;

            public WebClientBuilder()
            {
                _httpClient = new System.Net.Http.HttpClient();
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            public IWebClientBuilder WithUrl(string url)
            {
                _httpClient.BaseAddress = new Uri(url);
                return this;
            }

            public IWebClientBuilder WithBearerAuthentication(string accessToken)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return this;
            }

            public IWebClientBuilder WithBasicAuthentication(string username, string password)
            {
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                return this;
            }

            public IWebClientBuilder WithCancellationToken(CancellationToken cancellationToken)
            {
                this._cancellationToken = cancellationToken;
                return this;
            }

            public IWebClient Build()
            {
                return new WebClient(this._httpClient, this._cancellationToken);
            }

            public IWebClientBuilder WithCustomHeaderCopyFrom(HttpRequest contextRequest)
            {
                foreach (var requestHeader in contextRequest.Headers.Where(h => h.Key.StartsWith("x-papp-")))
                {
                    _httpClient.DefaultRequestHeaders.Add(requestHeader.Key, requestHeader.Value.ToArray());
                }

                return this;
            }
        }
    }
}