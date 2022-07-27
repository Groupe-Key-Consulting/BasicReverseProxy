using System.Net.Http;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BasicReverseProxy.Core.RouteForwarding.Services
{
    public class SimpleForwarderService : IForwarderService
    {
        private readonly IForwardRepository _forwardRepository;

        public SimpleForwarderService(IForwardRepository forwardRepository)
        {
            _forwardRepository = forwardRepository;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequest request, RouteValueDictionary routeValueDictionary)
        {
            return await this._forwardRepository.SendAsync(request, routeValueDictionary);
        }
    }
}
