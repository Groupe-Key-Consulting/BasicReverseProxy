using System.Net.Http;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding.Client;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Core.RouteForwarding.Repositories
{
    public class BodyForwardRepository : ForwardRepository
    {
        public BodyForwardRepository(string defaultWebServiceUrl, 
                                IWebClientFactory webClientFactory,
                                HttpVerb httpVerb,
                                string urlTemplate) 
            : base(defaultWebServiceUrl, webClientFactory, httpVerb, urlTemplate)
        {
        }

        protected override Task<HttpResponseMessage> GetInternalAsync(HttpRequest request, string url)
        {
            return this.GetAsync(request, url);
        }

        protected override Task<HttpResponseMessage> PutInternalAsync(HttpRequest request, string url)
        {
            return this.PutRequestBodyAsync(request, url);
        }

        protected override Task<HttpResponseMessage> PostInternalAsync(HttpRequest request, string url)
        {
            return this.PostRequestBodyAsync(request, url);
        }

        protected override Task<HttpResponseMessage> DeleteInternalAsync(HttpRequest request, string url)
        {
            return this.ExecuteDeleteAsync(request, url);
        }

        protected override Task<HttpResponseMessage> PutInternalAsync<T>(HttpRequest request, string url, T data)
        {
            return this.PutDataAsync(request, url, data);
        }

        protected override Task<HttpResponseMessage> PostInternalAsync<T>(HttpRequest request, string url, T data)
        {
            return this.PostDataAsync(request, url, data);
        }
    }
}
