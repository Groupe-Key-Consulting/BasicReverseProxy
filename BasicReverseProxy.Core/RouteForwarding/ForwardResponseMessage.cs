using System;
using System.Net.Http;

namespace BasicReverseProxy.Core.RouteForwarding
{
    public class ForwardResponseMessage : IDisposable
    {
        public HttpResponseMessage Response { get; set; }
        public bool HasBeenForwarded { get; set; }
        public bool IsCancelled { get; set; } = false;
        public void Dispose()
        {
            Response?.Dispose();
        }
    }
}
