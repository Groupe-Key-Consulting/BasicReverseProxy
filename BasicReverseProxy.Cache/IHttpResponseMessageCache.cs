using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Cache
{
    public interface IHttpResponseMessageCache
    {
        Task<string> GetKeyAsync(HttpContext httpContext);
        Task<string> AddAsync(string key, HttpResponseMessage httpResponseMessage, int? expirationInSeconds);
        Task<HttpResponseMessage> GetAsync(string key);
        Task RefreshAsync(string key);
    }
}
