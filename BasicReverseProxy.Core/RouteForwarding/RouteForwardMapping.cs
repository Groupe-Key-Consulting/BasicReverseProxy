using BasicReverseProxy.Core.RouteForwarding.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace BasicReverseProxy.Core.RouteForwarding
{
    internal class RouteForwardMapping
    {
        private readonly TemplateMatcher _templateMatcher;
        private readonly HttpVerb _httpVerb;
        private readonly RouteForwardSettings _routeForwardSettings;

        public RouteForwardMapping(RouteForwardSettings routeForwardSettings)
        {
            _routeForwardSettings = routeForwardSettings;
            var routeTemplate = TemplateParser.Parse(routeForwardSettings.Url);
            _templateMatcher = new TemplateMatcher(routeTemplate, null);
            _httpVerb = routeForwardSettings.Verb;
        }

        private HttpVerb? GetHttpVerb(string method)
        {
            if (HttpMethods.IsGet(method))
            {
                return HttpVerb.Get;
            }

            if (HttpMethods.IsPost(method))
            {
                return HttpVerb.Post;
            }

            if (HttpMethods.IsPut(method))
            {
                return HttpVerb.Put;
            }

            if (HttpMethods.IsDelete(method))
            {
                return HttpVerb.Delete;
            }

            return null;
        }

        public RouteForwardMappingMatchingResult IsMatch(HttpRequest request)
        {
            var requestHttpVerb = GetHttpVerb(request.Method);
            if (!requestHttpVerb.HasValue)
            {
                return new RouteForwardMappingMatchingResult {IsMatch = false};
            }

            if (requestHttpVerb.Value != this._httpVerb)
            {
                return new RouteForwardMappingMatchingResult { IsMatch = false };
            }

            var routeValueDictionary = new RouteValueDictionary();
            var isMatch = this._templateMatcher.TryMatch(request.Path.Value, routeValueDictionary);
            return new RouteForwardMappingMatchingResult
                {IsMatch = isMatch, RouteValueDictionary = routeValueDictionary, Settings = this._routeForwardSettings};
        }
    }
}
