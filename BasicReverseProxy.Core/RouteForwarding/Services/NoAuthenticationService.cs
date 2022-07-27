using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Core.RouteForwarding.Services
{
    public class NoAuthenticationService : IAuthenticationService
    {
        public Task AuthenticateAsync(HttpContext context)
        {
            return Task.CompletedTask;
        }
    }
}