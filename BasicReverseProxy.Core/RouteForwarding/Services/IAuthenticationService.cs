using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Core.RouteForwarding.Services
{
    public interface IAuthenticationService
    {
        Task AuthenticateAsync(HttpContext context);
    }
}