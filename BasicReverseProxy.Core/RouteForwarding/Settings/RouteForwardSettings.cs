using System.Collections.Generic;

namespace BasicReverseProxy.Core.RouteForwarding.Settings
{
    public class RouteForwardSettings
    {
        public string Url { get; set; }
        public HttpVerb Verb { get; set; }
        public string To { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public FormForwardSettings FormForward { get; set; }
        public RedirectRouteSettings Redirect { get; set; }
    }
}
