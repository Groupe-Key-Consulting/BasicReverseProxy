using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Cache
{
    public interface IExpirationService
    {
        void Expire(HttpContext httpContext);
    }
}
