using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BasicReverseProxy.Cache.Factories;
using BasicReverseProxy.Cache.Settings;
using BasicReverseProxy.Core;
using BasicReverseProxy.Core.RouteForwarding;
using BasicReverseProxy.Core.RouteForwarding.Client;
using BasicReverseProxy.Core.RouteForwarding.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace BasicReverseProxy.Cache.Test
{
    internal class ReverseProxyServiceTests
    {
        private IWebClient? _webClientMock;
        private ILogger<RouteForwardManager<CacheRouteForwardSettings>>? _loggerRouteForwardManagerMock;
        private IDistributedCache? _distributedCacheMock;
        private IHttpContextKeyCalculator? _httpContextKeyCalculatorMock;
        private IExpirationService? _expirationServiceMock;

        private ReverseProxyService GetTestedInstance(CacheRouteMappingSettings routeMappingSettings)
        {
            IAuthenticationService authenticationServiceMock = Substitute.For<IAuthenticationService>();
            IWebClientFactory webClientFactoryMock = Substitute.For<IWebClientFactory>();

            webClientFactoryMock.CreateWebClient(Arg.Any<string>(), Arg.Any<HttpContext>()).Returns(_webClientMock);
            webClientFactoryMock.CreateWebClient(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(_webClientMock);

            var httpResponseMessageCache = new HttpResponseMessageCache(_distributedCacheMock, _httpContextKeyCalculatorMock, routeMappingSettings);
            var forwardServiceFactory = new CacheForwardServiceFactory(webClientFactoryMock, httpResponseMessageCache, _expirationServiceMock);

            var routeForwardManager = new RouteForwardManager<CacheRouteForwardSettings>(_loggerRouteForwardManagerMock, routeMappingSettings,
                forwardServiceFactory, authenticationServiceMock);
            return new ReverseProxyService(routeForwardManager);
        }

        [SetUp]
        public void BeforeEachTest()
        {
            _webClientMock = Substitute.For<IWebClient>();
            _loggerRouteForwardManagerMock = Substitute.For<ILogger<RouteForwardManager<CacheRouteForwardSettings>>>();
            _distributedCacheMock = Substitute.For<IDistributedCache>();
            _httpContextKeyCalculatorMock = Substitute.For<IHttpContextKeyCalculator>();
            _expirationServiceMock = Substitute.For<IExpirationService>();

            _webClientMock.GetAsync(Arg.Any<string>()).Returns(new HttpResponseMessage(HttpStatusCode.OK));
            _webClientMock.DeleteAsync(Arg.Any<string>()).Returns(new HttpResponseMessage(HttpStatusCode.OK));
            _webClientMock.PostAsync(Arg.Any<string>(), Arg.Any<HttpContent>())
                .Returns(new HttpResponseMessage(HttpStatusCode.OK));
            _webClientMock.PutAsync(Arg.Any<string>(), Arg.Any<HttpContent>())
                .Returns(new HttpResponseMessage(HttpStatusCode.OK));
        }

        private HttpContext GivenHttpContextWithRequest(string method, string url)
        {
            return GivenHttpContextWithRequest(method, url, "The body");
        }

        private HttpContext GivenHttpContextWithRequest(string method, string url, string body)
        {
            var request = Substitute.For<HttpRequest>();
            if (url.Contains("?"))
            {
                request.QueryString = new QueryString(url.Substring(url.IndexOf('?')));
                url = url.Split("?").First();
            }

            request.Method.Returns(method);
            request.Path.Returns(new PathString(url));
            byte[] byteArray = Encoding.UTF8.GetBytes(body);
            MemoryStream stream = new MemoryStream(byteArray);
            request.Body.Returns(stream);
            request.ContentType.Returns("application/json");
            request.ContentLength.Returns(100L);

            var context = Substitute.For<HttpContext>();
            context.Request.Returns(request);
            var responseBody = new MemoryStream();
            context.Response.Body.Returns(responseBody);
            request.HttpContext.Returns(context);
            return context;
        }

        private void GivenCacheEntry(string key, string content)
        {
            this._distributedCacheMock!.GetAsync(key, Arg.Any<CancellationToken>())
                .Returns(Encoding.UTF8.GetBytes(content));
        }

        private void GivenPostResponse(string url, HttpResponseMessage response)
        {
            _webClientMock!.PostAsync(url, Arg.Any<HttpContent>()).Returns(response);
        }

        [Test]
        public async Task Should_return_response_from_cache_When_route_match_and_cache_is_activated()
        {
            var routeMappingSettings = new CacheRouteMappingSettings
            {
                Forwards = new[]
                {
                    new CacheRouteForwardSettings
                    {
                        Url = "/api/testUrl",
                        Verb = HttpVerb.Post,
                        To = "https://MyService",
                        Roles = new []{ "R1", "R2" },
                        Cache = new CacheSettings
                        {
                            Enable = true
                        }
                    }
                }
            };


            HttpContext context = GivenHttpContextWithRequest("POST", "/api/testUrl");
            context.User.Claims.Returns(new List<Claim> { new Claim(ClaimTypes.Role, "R1") });
            context.User.IsInRole("R1").Returns(true);

            GivenCacheEntry("MyKey", "The content from cache");
            this._httpContextKeyCalculatorMock!.ComputeKeyAsync(context).Returns("MyKey");

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(context.Response.Body, Encoding.UTF8))
            {
                Assert.That(reader.ReadToEnd(), Is.EqualTo("The content from cache"));
            }
        }

        [Test]
        public async Task Should_not_return_response_from_cache_When_route_match_and_cache_is_disabled()
        {
            var routeMappingSettings = new CacheRouteMappingSettings
            {
                Forwards = new[]
                {
                    new CacheRouteForwardSettings
                    {
                        Url = "/api/testUrl",
                        Verb = HttpVerb.Post,
                        To = "https://MyService",
                        Roles = new []{ "R1", "R2" },
                        Cache = new CacheSettings
                        {
                            Enable = false
                        }
                    }
                }
            };

            var context = GivenHttpContextWithRequest("POST", "/api/testUrl");
            context.User.Claims.Returns(new List<Claim> { new Claim(ClaimTypes.Role, "R1") });
            context.User.IsInRole("R1").Returns(true);

            GivenCacheEntry("MyKey", "The content from cache");
            this._httpContextKeyCalculatorMock!.ComputeKeyAsync(context).Returns("MyKey");

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            await this._distributedCacheMock!.DidNotReceive().GetAsync("MyKey", Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Should_add_response_to_cache_When_route_match_and_cache_is_activated()
        {
            var routeMappingSettings = new CacheRouteMappingSettings
            {
                Forwards = new[]
                {
                    new CacheRouteForwardSettings
                    {
                        Url = "/api/testUrl",
                        Verb = HttpVerb.Post,
                        To = "https://MyService",
                        Roles = new []{ "R1", "R2" },
                        Cache = new CacheSettings
                        {
                            Enable = true,
                            Expiration = 40
                        }
                    }
                }
            };

            var context = GivenHttpContextWithRequest("POST", "/api/testUrl");
            context.User.Claims.Returns(new List<Claim> { new Claim(ClaimTypes.Role, "R1") });
            context.User.IsInRole("R1").Returns(true);

            this._httpContextKeyCalculatorMock!.ComputeKeyAsync(context).Returns("MyKey");
            GivenPostResponse("/api/testUrl", new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("The result from remote service")
            });

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            await this._distributedCacheMock!.Received(1).SetAsync("MyKey",
                                                                    Arg.Any<byte[]>(),
                                                                    Arg.Is<DistributedCacheEntryOptions>(o => o.SlidingExpiration.HasValue
                                                                                                              && o.SlidingExpiration.Value.Seconds == 40),
                                                                    Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Should_refresh_cache_entry_When_response_is_retrieved_from_cache()
        {
            var routeMappingSettings = new CacheRouteMappingSettings
            {
                Forwards = new[]
                {
                    new CacheRouteForwardSettings
                    {
                        Url = "/api/testUrl",
                        Verb = HttpVerb.Post,
                        To = "https://MyService",
                        Roles = new []{ "R1", "R2" },
                        Cache = new CacheSettings
                        {
                            Enable = true
                        }
                    }
                }
            };


            var context = GivenHttpContextWithRequest("POST", "/api/testUrl");
            context.User.Claims.Returns(new List<Claim> { new Claim(ClaimTypes.Role, "R1") });
            context.User.IsInRole("R1").Returns(true);

            GivenCacheEntry("MyKey", "The content from cache");
            this._httpContextKeyCalculatorMock!.ComputeKeyAsync(context).Returns("MyKey");

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            await this._distributedCacheMock!.Received(1).RefreshAsync("MyKey", Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Should_expire_cache_entries_When_url_is_mapped_to_expire_and_response_is_sucess()
        {
            var routeMappingSettings = new CacheRouteMappingSettings
            {
                Forwards = new[]
                {
                    new CacheRouteForwardSettings
                    {
                        Url = "/api/testUrl",
                        Verb = HttpVerb.Post,
                        To = "https://MyService",
                        Roles = new []{ "R1", "R2" },
                        Cache = new CacheSettings
                        {
                            Enable = true,
                            Action = CacheActionType.Expire,
                        }
                    }
                }
            };

            var context = GivenHttpContextWithRequest("POST", "/api/testUrl");
            context.User.Claims.Returns(new List<Claim> { new Claim(ClaimTypes.Role, "R1") });
            context.User.IsInRole("R1").Returns(true);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            _expirationServiceMock!.Received(1).Expire(context);
        }
    }
}
