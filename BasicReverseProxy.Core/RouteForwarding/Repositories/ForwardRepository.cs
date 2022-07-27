using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BasicReverseProxy.Core.RouteForwarding.Repositories
{
    public abstract class ForwardRepository : WebServiceRepositoryBase, IForwardRepository
    {
        private readonly HttpVerb _httpVerb;
        private readonly string _urlTemplate;

        protected ForwardRepository(string defaultWebServiceUrl,
            IWebClientFactory webClientFactory,
            HttpVerb httpVerb,
            string urlTemplate)
            : base(defaultWebServiceUrl, webClientFactory)
        {
            _urlTemplate = urlTemplate;
            _httpVerb = httpVerb;
        }

        protected abstract Task<HttpResponseMessage> GetInternalAsync(HttpRequest request, string url);

        protected abstract Task<HttpResponseMessage> PutInternalAsync(HttpRequest request, string url);
        protected abstract Task<HttpResponseMessage> PostInternalAsync(HttpRequest request, string url);
        protected abstract Task<HttpResponseMessage> DeleteInternalAsync(HttpRequest request, string url);

        protected abstract Task<HttpResponseMessage> PutInternalAsync<T>(HttpRequest request, string url, T data);

        protected abstract Task<HttpResponseMessage> PostInternalAsync<T>(HttpRequest request, string url, T data);

        public async Task<HttpResponseMessage> SendAsync(HttpRequest request, RouteValueDictionary routeValueDictionary)
        {
            var url = ApplyRouteValues(_urlTemplate, routeValueDictionary);
            url = ApplyQueryParams(url, request);

            HttpResponseMessage httpResponseMessage = null;
            switch (_httpVerb)
            {
                case HttpVerb.Get:
                    httpResponseMessage = await this.GetInternalAsync(request, url);
                    break;
                case HttpVerb.Put:
                    httpResponseMessage = await this.PutInternalAsync(request, url);
                    break;
                case HttpVerb.Post:
                    httpResponseMessage = await this.PostInternalAsync(request, url);
                    break;
                case HttpVerb.Delete:
                    httpResponseMessage = await this.DeleteInternalAsync(request, url);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return httpResponseMessage;
        }

        public async Task<HttpResponseMessage> SendAsync<T>(HttpRequest request, T data, RouteValueDictionary routeValueDictionary)
        {
            var url = ApplyRouteValues(_urlTemplate, routeValueDictionary);
            url = ApplyQueryParams(url, request);

            HttpResponseMessage httpResponseMessage = null;
            switch (_httpVerb)
            {
                case HttpVerb.Put:
                    httpResponseMessage = await this.PutInternalAsync(request, url, data);
                    break;
                case HttpVerb.Post:
                    httpResponseMessage = await this.PostInternalAsync(request, url, data);
                    break;
                case HttpVerb.Get:
                case HttpVerb.Delete:
                    throw new ArgumentException($"{_httpVerb} is not compatible with data forward");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return httpResponseMessage;
        }


        private static string ApplyRouteValues(string urlTemplate, RouteValueDictionary routeValueDictionary)
        {
            var url = urlTemplate;
            foreach (var routeValue in routeValueDictionary)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append(routeValue.Key);
                sb.Append("}");
                var keyToReplace = sb.ToString();
                url = Replace(url, keyToReplace, routeValue.Value.ToString());
            }

            return url;
        }

        private static string Replace(string url, string keyToReplace, string replaceValue)
        {
            var stringBuilder = new StringBuilder(url);
            stringBuilder.Replace(keyToReplace, replaceValue);
            return stringBuilder.ToString();
        }

        private static string ApplyQueryParams(string urlTemplate, HttpRequest request)
        {
            if (request.QueryString.HasValue)
            {
                var sb = new StringBuilder(urlTemplate);
                sb.Append(request.QueryString.Value);
                return sb.ToString();
            }

            return urlTemplate;
        }
    }
}