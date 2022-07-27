using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Cache
{
    internal class EmptyExpirationService : IExpirationService
    {
        public void Expire(HttpContext httpContext)
        {
            
        }
    }
}
