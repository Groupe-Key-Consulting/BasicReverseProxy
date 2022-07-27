using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding.Client;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace BasicReverseProxy.Core.RouteForwarding.Repositories
{
    public abstract class WebServiceRepositoryBase
    {
        protected readonly string _defaultWebServiceUrl;
        protected string DefaultWebServiceUrl => this._defaultWebServiceUrl;

        protected IWebClientFactory WebClientFactory { get; }

        protected WebServiceRepositoryBase(string defaultWebServiceUrl, IWebClientFactory webClientFactory)
        {
            this.WebClientFactory = webClientFactory;
            this._defaultWebServiceUrl = defaultWebServiceUrl;
        }

        protected async Task<T> RequestDataAsync<T>(HttpRequest request, string url)
        {
            return await RequestDataAsync<T>(request, this._defaultWebServiceUrl, url);
        }

        protected async Task<HttpResponseMessage> GetAsync(HttpRequest request, string url)
        {
            return await GetAsync(request, this._defaultWebServiceUrl, url);
        }

        protected async Task<HttpResponseMessage> ExecuteDeleteAsync(HttpRequest request, string url)
        {
            return await ExecuteDeleteAsync(request, this._defaultWebServiceUrl, url);
        }

        protected async Task<HttpResponseMessage> PutRequestBodyAsync(HttpRequest request, string url)
        {
            return await PutRequestBodyAsync(request, this._defaultWebServiceUrl, url);
        }

        protected async Task<HttpResponseMessage> PostRequestBodyAsync(HttpRequest request, string url)
        {
            return await PostRequestBodyAsync(request, this._defaultWebServiceUrl, url);
        }

        protected async Task<HttpResponseMessage> PostDataAsync<T>(HttpRequest request, string url, T data)
        {
            return await PostDataAsync(request, this._defaultWebServiceUrl, url, data);
        }

        protected async Task<HttpResponseMessage> PutDataAsync<T>(HttpRequest request, string url, T data)
        {
            return await PutDataAsync(request, this._defaultWebServiceUrl, url, data);
        }

        protected async Task<HttpResponseMessage> PutRequestFormAsync(HttpRequest request, string url,
            string fileFormName)
        {
            return await PutRequestFormAsync(request, this._defaultWebServiceUrl, url, fileFormName);
        }

        protected async Task<HttpResponseMessage> PostRequestFormAsync(HttpRequest request, string url, string fileFormName)
        {
            return await PostRequestFormAsync(request, this._defaultWebServiceUrl, url, fileFormName);
        }

        private async Task<T> RequestDataAsync<T>(HttpRequest request, string webServiceUrl, string url)
        {
            using (var client = Client(request.HttpContext, webServiceUrl))
            {
                using (var response = await client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                }
            }

            return default(T);
        }

        private async Task<HttpResponseMessage> GetAsync(HttpRequest request, string webServiceUrl, string url)
        {
            using (var client = Client(request.HttpContext, webServiceUrl))
            {
                return await client.GetAsync(url);
            }
        }

        private async Task<HttpResponseMessage> ExecuteDeleteAsync(HttpRequest request, string webServiceUrl, string url)
        {
            using (var client = Client(request.HttpContext, webServiceUrl))
            {
                return await client.DeleteAsync(url);
            }
        }

        private async Task<HttpResponseMessage> PutRequestBodyAsync(HttpRequest request, string webServiceUrl, string url)
        {
            return await SendRequestBodyAsync(request, webServiceUrl, url, async (client, uri, content) => await client.PutAsync(uri, content));
        }

        private async Task<HttpResponseMessage> PostRequestBodyAsync(HttpRequest request, string webServiceUrl, string url)
        {
            return await SendRequestBodyAsync(request, webServiceUrl, url, async (client, uri, content) => await client.PostAsync(uri, content));
        }

        private async Task<HttpResponseMessage> PostDataAsync<T>(HttpRequest request, string webServiceUrl, string url, T data)
        {
            return await SendDataAsync(request, webServiceUrl, url, data, async(client, uri, content) => await client.PostAsync(uri, content));
        }

        private async Task<HttpResponseMessage> PutDataAsync<T>(HttpRequest request, string webServiceUrl, string url, T data)
        {
            return await SendDataAsync(request, webServiceUrl, url, data, async (client, uri, content) => await client.PutAsync(uri, content));
        }

        private async Task<HttpResponseMessage> PutRequestFormAsync(HttpRequest request, string webServiceUrl, string url, string fileFormName)
        {
            return await SendRequestFormAsync(request, webServiceUrl, url, fileFormName,
                async (client, uri, content) => await client.PutAsync(uri, content));
        }

        private async Task<HttpResponseMessage> PostRequestFormAsync(HttpRequest request, string webServiceUrl, string url, string fileFormName)
        {
            return await SendRequestFormAsync(request, webServiceUrl, url, fileFormName,
                async (client, uri, content) => await client.PostAsync(uri, content));
        }

        private IWebClient Client(HttpContext context, string baseUrl)
        {
            return this.WebClientFactory.CreateWebClient(baseUrl, context);
        }

        private async Task<HttpResponseMessage> SendRequestFormAsync(HttpRequest request, string webServiceUrl, string url, string fileFormName, Func<IWebClient, string, HttpContent, Task<HttpResponseMessage>> sendFunction)
        {
            using (var multiContent = new MultipartFormDataContent())
            {
                var files = request.Form.Files;
                foreach (var file in files)
                {
                    var fileStreamContent = new StreamContent(file.OpenReadStream());
                    fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    multiContent.Add(fileStreamContent, fileFormName, file.FileName);
                }

                var dataKeys = request.Form.Keys;
                foreach (var key in dataKeys)
                {
                    multiContent.Add(new StringContent(request.Form[key]), key);
                }

                using (var client = Client(request.HttpContext, webServiceUrl))
                {
                    return await sendFunction(client, url, multiContent);
                }
            }
        }

        private async Task<HttpResponseMessage> SendRequestBodyAsync(HttpRequest request, string webServiceUrl, string url, Func<IWebClient, string, HttpContent, Task<HttpResponseMessage>> sendFunction)
        {
            var json = await ExtractBodyContent(request);

            using (var httpContent = new StringContent(json, Encoding.UTF8, request.ContentType))
            {
                using (var client = Client(request.HttpContext, webServiceUrl))
                {
                    return await sendFunction(client, url, httpContent);
                }
            }
        }

        private async Task<HttpResponseMessage> SendDataAsync<T>(HttpRequest request, string webServiceUrl, string url, T data, Func<IWebClient, string, HttpContent, Task<HttpResponseMessage>> sendFunction)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);

            using (var httpContent = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                using (var client = Client(request.HttpContext, webServiceUrl))
                {
                    return await sendFunction(client, url, httpContent);
                }
            }
        }

        private static async Task<string> ExtractBodyContent(HttpRequest request)
        {
            using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
