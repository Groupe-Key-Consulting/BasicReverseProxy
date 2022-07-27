using BasicReverseProxy.Core.RouteForwarding.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace BasicReverseProxy.Core.RouteForwarding
{
    internal class RouteForwardMapping<T> where T : RouteForwardSettings
    {
        private readonly TemplateMatcher _templateMatcher;
        private readonly HttpVerb _httpVerb;
        private readonly T _routeForwardSettings;

        public RouteForwardMapping(T routeForwardSettings)
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

        public RouteForwardMappingMatchingResult<T> IsMatch(HttpRequest request)
        {
            var requestHttpVerb = GetHttpVerb(request.Method);
            if (!requestHttpVerb.HasValue)
            {
                return new RouteForwardMappingMatchingResult<T> {IsMatch = false};
            }

            if (requestHttpVerb.Value != this._httpVerb)
            {
                return new RouteForwardMappingMatchingResult<T> { IsMatch = false };
            }

            var routeValueDictionary = new RouteValueDictionary();
            var isMatch = this._templateMatcher.TryMatch(request.Path.Value, routeValueDictionary);
            return new RouteForwardMappingMatchingResult<T>
                {IsMatch = isMatch, RouteValueDictionary = routeValueDictionary, Settings = this._routeForwardSettings};
        }
    }
}
