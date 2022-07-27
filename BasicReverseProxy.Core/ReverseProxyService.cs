using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Core
{
    public interface IReverseProxyService : IMiddleware
    {
    }

    public class ReverseProxyService : IReverseProxyService
    {
        private readonly IRouteForwardManager _routeForwardManager;

        public ReverseProxyService(IRouteForwardManager routeForwardManager)
        {
            _routeForwardManager = routeForwardManager;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            using (var forwardResult = await _routeForwardManager.TryToForwardAsync(context).ConfigureAwait(true))
            {
                if (forwardResult.IsCancelled)
                {
                    return;
                }

                if (forwardResult.HasBeenForwarded)
                {
                    var responseMessage = forwardResult.Response;
                    if (context == null)
                    {
                        return;
                    }
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    CopyFromTargetResponseHeaders(context, responseMessage);
                    if (responseMessage.Content != null)
                    {
                        await responseMessage.Content.CopyToAsync(context.Response.Body).ConfigureAwait(true);
                    }
                    return;
                }
            }

            if (next != null)
            {
                await next(context).ConfigureAwait(false);
            }
        }

        private static void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            if (responseMessage.Content != null)
            {
                foreach (var header in responseMessage.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }
            context.Response.Headers.Remove("transfer-encoding");
        }
    }
}
