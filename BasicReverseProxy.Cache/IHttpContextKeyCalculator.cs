using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Cache
{
    public interface IHttpContextKeyCalculator
    {
        Task<string> ComputeKeyAsync(HttpContext httpContext);
    }
}
