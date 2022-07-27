using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BasicReverseProxy.Core.RouteForwarding.Repositories
{
    public interface IForwardRepository
    {
        Task<HttpResponseMessage> SendAsync(HttpRequest request, RouteValueDictionary routeValueDictionary);
        Task<HttpResponseMessage> SendAsync<T>(HttpRequest request, T data, RouteValueDictionary routeValueDictionary);
    }
}
