using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BasicReverseProxy.Core.RouteForwarding.Client
{
    public interface IWebClient : IDisposable
    {
        Task<HttpResponseMessage> GetAsync(string requestUri);
        Task<HttpResponseMessage> DeleteAsync(string requestUri);
        Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content);
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
    }
}