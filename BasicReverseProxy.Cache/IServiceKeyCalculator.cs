using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Cache
{
    public interface IServiceKeyCalculator
    {
        string ComputeKey(HttpContext httpContext, string identifier);
    }
}