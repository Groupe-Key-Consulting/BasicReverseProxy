using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BasicReverseProxy.Core.RouteForwarding.Client
{
    public class WebClient : IWebClient
    {
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly CancellationToken _cancellationToken;

        public WebClient(System.Net.Http.HttpClient httpClient, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return await _httpClient.GetAsync(requestUri, _cancellationToken);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            return await _httpClient.DeleteAsync(requestUri, _cancellationToken);
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            return await _httpClient.PutAsync(requestUri, content, _cancellationToken);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            return await _httpClient.PostAsync(requestUri, content, _cancellationToken);
        }

        public void Dispose()
        {
            this._httpClient?.Dispose();
        }
    }
}