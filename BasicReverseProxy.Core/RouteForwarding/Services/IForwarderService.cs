using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BasicReverseProxy.Core.RouteForwarding.Services
{
    public interface IForwarderService
    {
        Task<HttpResponseMessage> SendAsync(HttpRequest request, RouteValueDictionary routeValueDictionary);
    }
}