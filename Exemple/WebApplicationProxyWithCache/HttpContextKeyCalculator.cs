using BasicReverseProxy.Cache;
using Microsoft.AspNetCore.Http.Extensions;

namespace WebApplicationProxyWithCache
{
    public class HttpContextKeyCalculator : IHttpContextKeyCalculator
    {
        public Task<string> ComputeKeyAsync(HttpContext httpContext)
        {
            return Task.FromResult(httpContext.Request.GetDisplayUrl());
        }
    }
}
