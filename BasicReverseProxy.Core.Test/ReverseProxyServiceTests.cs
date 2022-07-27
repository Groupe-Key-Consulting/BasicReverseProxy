using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding;
using BasicReverseProxy.Core.RouteForwarding.Client;
using BasicReverseProxy.Core.RouteForwarding.Factories;
using BasicReverseProxy.Core.RouteForwarding.Services;
using BasicReverseProxy.Core.RouteForwarding.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace BasicReverseProxy.Core.Test
{
    public class ReverseProxyServiceTests
    {
        private IWebClient? _webClientMock;
        private ILogger<RouteForwardManager<RouteForwardSettings>>? _loggerRouteForwardManagerMock;

        private ReverseProxyService GetTestedInstance(RouteMappingSettings<RouteForwardSettings> routeMappingSettings)
        {
            IAuthenticationService authenticationServiceMock = Substitute.For<IAuthenticationService>();
            IWebClientFactory webClientFactoryMock = Substitute.For<IWebClientFactory>();

            webClientFactoryMock.CreateWebClient(Arg.Any<string>(), Arg.Any<HttpContext>()).Returns(_webClientMock);
            webClientFactoryMock.CreateWebClient(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(_webClientMock);

            var forwardServiceFactory = new ForwardServiceFactory<RouteForwardSettings>(webClientFactoryMock);

            var routeForwardManager = new RouteForwardManager<RouteForwardSettings>(_loggerRouteForwardManagerMock, routeMappingSettings,
                forwardServiceFactory, authenticationServiceMock);
            return new ReverseProxyService(routeForwardManager);
        }

        [SetUp]
        public void BeforeEachTest()
        {
            _webClientMock = Substitute.For<IWebClient>();
            _loggerRouteForwardManagerMock = Substitute.For<ILogger<RouteForwardManager<RouteForwardSettings>>>();

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
            return context;
        }

        [Test]
        public async Task Should_call_next_middelware_When_route_is_not_mapped()
        {
            var service = GetTestedInstance(new RouteMappingSettings<RouteForwardSettings>());

            var context = Substitute.For<HttpContext>();
            var requestDelegate = Substitute.For<RequestDelegate>();
            await service.InvokeAsync(context, requestDelegate);
            await requestDelegate.Received().Invoke(context);
        }

        [Test]
        public async Task Should_not_call_next_middelware_When_route_is_mapped()
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings {Url = "api/TestUrl", Verb = HttpVerb.Get, To = "https://MyService"}
                }
            };

            var context = GivenHttpContextWithRequest("Get", "/api/TestUrl");

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await requestDelegate.DidNotReceive().Invoke(context);
        }

        [TestCase("/api/TestUrl")]
        public async Task Should_forward_route_with_get_verb_When_simple_route_is_mapped_with_get_verb(string mappedUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings {Url = mappedUrl, Verb = HttpVerb.Get, To = "https://MyService"}
                }
            };

            var context = GivenHttpContextWithRequest(HttpVerb.Get.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().GetAsync(mappedUrl);
        }

        [TestCase("/api/TestUrl")]
        public async Task Should_forward_route_with_post_verb_When_simple_route_is_mapped_with_post_verb(
            string mappedUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings {Url = mappedUrl, Verb = HttpVerb.Post, To = "https://MyService"}
                }
            };

            var context = GivenHttpContextWithRequest(HttpVerb.Post.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().PostAsync(mappedUrl, Arg.Any<HttpContent>());
        }

        [TestCase("/api/TestUrl")]
        public async Task Should_forward_route_with_put_verb_When_simple_route_is_mapped_with_put_verb(string mappedUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings {Url = mappedUrl, Verb = HttpVerb.Put, To = "https://MyService"}
                }
            };

            var context = GivenHttpContextWithRequest(HttpVerb.Put.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().PutAsync(mappedUrl, Arg.Any<HttpContent>());
        }

        [TestCase("/api/TestUrl")]
        public async Task Should_forward_route_with_delete_verb_When_simple_route_is_mapped_with_delete_verb(
            string mappedUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings {Url = mappedUrl, Verb = HttpVerb.Delete, To = "https://MyService"}
                }
            };

            var context = GivenHttpContextWithRequest(HttpVerb.Delete.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().DeleteAsync(mappedUrl);
        }

        [TestCase(HttpVerb.Put)]
        [TestCase(HttpVerb.Post)]
        [TestCase(HttpVerb.Get)]
        [TestCase(HttpVerb.Delete)]
        public async Task Should_forward_route_with_query_params_When_simple_route_is_mapped(HttpVerb httpVerb)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings {Url = "/api/TestUrl", Verb = httpVerb, To = "https://MyService"}
                }
            };

            var context = GivenHttpContextWithRequest(httpVerb.ToString(), "/api/TestUrl?Test=0&Test2=1");

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            var expectedUrl = "/api/TestUrl?Test=0&Test2=1";
            switch (httpVerb)
            {
                case HttpVerb.Get:
                    await this._webClientMock!.Received().GetAsync(expectedUrl);
                    break;
                case HttpVerb.Post:
                    await this._webClientMock!.Received().PostAsync(expectedUrl, Arg.Any<HttpContent>());
                    break;
                case HttpVerb.Put:
                    await this._webClientMock!.Received().PutAsync(expectedUrl, Arg.Any<HttpContent>());
                    break;
                case HttpVerb.Delete:
                    await this._webClientMock!.Received().DeleteAsync(expectedUrl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expectedUrl), expectedUrl, null);
            }

        }

        [TestCase(HttpVerb.Get, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Post, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Put, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Delete, "/api/TestUrl", "/TestRedirect")]
        public async Task Should_forward_route_to_redirect_url_with_get_verb_When_redirect_route_is_mapped(
            HttpVerb mappedVerb, string mappedUrl, string redirectUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                    {
                        Url = mappedUrl, Verb = mappedVerb, To = "https://MyService",
                        Redirect = new RedirectRouteSettings {Url = redirectUrl, Verb = HttpVerb.Get}
                    }
                }
            };

            var context = GivenHttpContextWithRequest(mappedVerb.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().GetAsync(redirectUrl);
        }

        [TestCase(HttpVerb.Put)]
        [TestCase(HttpVerb.Post)]
        [TestCase(HttpVerb.Get)]
        [TestCase(HttpVerb.Delete)]
        public async Task Should_forward_route_to_redirect_url_with_query_params_When_simple_route_is_mapped(
            HttpVerb httpVerb)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                    {
                        Url = "/api/TestUrl", Verb = httpVerb, To = "https://MyService",
                        Redirect = new RedirectRouteSettings {Url = "/TestRedirect", Verb = httpVerb}
                    }
                }
            };

            var context = GivenHttpContextWithRequest(httpVerb.ToString(), "/api/TestUrl?Test=0&Test2=1");

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            var expectedUrl = "/TestRedirect?Test=0&Test2=1";
            switch (httpVerb)
            {
                case HttpVerb.Get:
                    await this._webClientMock!.Received().GetAsync(expectedUrl);
                    break;
                case HttpVerb.Post:
                    await this._webClientMock!.Received().PostAsync(expectedUrl, Arg.Any<HttpContent>());
                    break;
                case HttpVerb.Put:
                    await this._webClientMock!.Received().PutAsync(expectedUrl, Arg.Any<HttpContent>());
                    break;
                case HttpVerb.Delete:
                    await this._webClientMock!.Received().DeleteAsync(expectedUrl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expectedUrl), expectedUrl, null);
            }

        }

        [TestCase(HttpVerb.Get, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Post, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Put, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Delete, "/api/TestUrl", "/TestRedirect")]
        public async Task Should_forward_route_to_redirect_url_with_post_verb_When_redirect_route_is_mapped(
            HttpVerb mappedVerb, string mappedUrl, string redirectUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                    {
                        Url = mappedUrl, Verb = mappedVerb, To = "https://MyService",
                        Redirect = new RedirectRouteSettings {Url = redirectUrl, Verb = HttpVerb.Post}
                    }
                }
            };

            var context = GivenHttpContextWithRequest(mappedVerb.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().PostAsync(redirectUrl, Arg.Any<HttpContent>());
        }

        [TestCase(HttpVerb.Get, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Post, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Put, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Delete, "/api/TestUrl", "/TestRedirect")]
        public async Task
            Should_forward_route_to_redirect_url_with_post_verb_When_redirect_route_is_mapped_with_form_forward(
                HttpVerb mappedVerb, string mappedUrl, string redirectUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                    {
                        Url = mappedUrl,
                        Verb = mappedVerb,
                        To = "https://MyService",
                        Redirect = new RedirectRouteSettings {Url = redirectUrl, Verb = HttpVerb.Post},
                        FormForward = new FormForwardSettings {FileFormPropertyName = "testFile"}
                    }
                }
            };

            var context = GivenHttpContextWithRequest(mappedVerb.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().PostAsync(redirectUrl, Arg.Any<HttpContent>());
        }

        [TestCase(HttpVerb.Get, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Post, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Put, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Delete, "/api/TestUrl", "/TestRedirect")]
        public async Task Should_forward_route_to_redirect_url_with_put_verb_When_redirect_route_is_mapped(
            HttpVerb mappedVerb, string mappedUrl, string redirectUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                    {
                        Url = mappedUrl, Verb = mappedVerb, To = "https://MyService",
                        Redirect = new RedirectRouteSettings {Url = redirectUrl, Verb = HttpVerb.Put}
                    }
                }
            };

            var context = GivenHttpContextWithRequest(mappedVerb.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().PutAsync(redirectUrl, Arg.Any<HttpContent>());
        }

        [TestCase(HttpVerb.Get, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Post, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Put, "/api/TestUrl", "/TestRedirect")]
        [TestCase(HttpVerb.Delete, "/api/TestUrl", "/TestRedirect")]
        public async Task Should_forward_route_to_redirect_url_with_delete_verb_When_redirect_route_is_mapped(
            HttpVerb mappedVerb, string mappedUrl, string redirectUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                    {
                        Url = mappedUrl, Verb = mappedVerb, To = "https://MyService",
                        Redirect = new RedirectRouteSettings {Url = redirectUrl, Verb = HttpVerb.Delete}
                    }
                }
            };

            var context = GivenHttpContextWithRequest(mappedVerb.ToString(), mappedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            await this._webClientMock!.Received().DeleteAsync(redirectUrl);
        }

        [TestCase(HttpVerb.Get, "/api/TestUrl/{p1}/{p2}", "/api/TestUrl/0/test")]
        [TestCase(HttpVerb.Post, "/api/TestUrl/{p1}/{p2}", "/api/TestUrl/0/test")]
        [TestCase(HttpVerb.Put, "/api/TestUrl/{p1}/{p2}", "/api/TestUrl/0/test")]
        [TestCase(HttpVerb.Delete, "/api/TestUrl/{p1}/{p2}", "/api/TestUrl/0/test")]
        public async Task Should_forward_route_parameters_When_simple_route_is_mapped(HttpVerb mappedVerb,
            string mappedUrl, string receivedUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings {Url = mappedUrl, Verb = mappedVerb, To = "https://MyService"}
                }
            };

            var context = GivenHttpContextWithRequest(mappedVerb.ToString(), receivedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            switch (mappedVerb)
            {
                case HttpVerb.Get:
                    await this._webClientMock!.Received().GetAsync(receivedUrl);
                    break;
                case HttpVerb.Post:
                    await this._webClientMock!.Received().PostAsync(receivedUrl, Arg.Any<HttpContent>());
                    break;
                case HttpVerb.Put:
                    await this._webClientMock!.Received().PutAsync(receivedUrl, Arg.Any<HttpContent>());
                    break;
                case HttpVerb.Delete:
                    await this._webClientMock!.Received().DeleteAsync(receivedUrl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mappedVerb), mappedVerb, null);
            }
        }

        [TestCase(HttpVerb.Get, "/api/TestUrl/{p1}/{p2}", "/api/TestUrl/0/test", HttpVerb.Post,
            "/TestRedirect/{p2}/{p1}", "/TestRedirect/test/0")]
        [TestCase(HttpVerb.Post, "/api/TestUrl/{p1}/{p2}", "/api/TestUrl/0/test", HttpVerb.Put,
            "/TestRedirect/{p2}/{p1}", "/TestRedirect/test/0")]
        [TestCase(HttpVerb.Put, "/api/TestUrl/{p1}/{p2}", "/api/TestUrl/0/test", HttpVerb.Delete,
            "/TestRedirect/{p2}/{p1}", "/TestRedirect/test/0")]
        [TestCase(HttpVerb.Delete, "/api/TestUrl/{p1}/{p2}", "/api/TestUrl/0/test", HttpVerb.Get,
            "/TestRedirect/{p2}/{p1}", "/TestRedirect/test/0")]
        public async Task Should_forward_route_to_redirect_url_with_parameters_When_redirect_route_is_mapped(
            HttpVerb mappedVerb, string mappedUrl, string receivedUrl, HttpVerb redirectVerb, string redirectUrl,
            string sentUrl)
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                    {
                        Url = mappedUrl, Verb = mappedVerb, To = "https://MyService",
                        Redirect = new RedirectRouteSettings {Url = redirectUrl, Verb = redirectVerb}
                    }
                }
            };

            var context = GivenHttpContextWithRequest(mappedVerb.ToString(), receivedUrl);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            switch (redirectVerb)
            {
                case HttpVerb.Get:
                    await this._webClientMock!.Received().GetAsync(sentUrl);
                    break;
                case HttpVerb.Post:
                    await this._webClientMock!.Received().PostAsync(sentUrl, Arg.Any<HttpContent>());
                    break;
                case HttpVerb.Put:
                    await this._webClientMock!.Received().PutAsync(sentUrl, Arg.Any<HttpContent>());
                    break;
                case HttpVerb.Delete:
                    await this._webClientMock!.Received().DeleteAsync(sentUrl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(redirectVerb), redirectVerb, null);
            }
        }

        [Test]
        public async Task Should_return_unauthorized_When_route_match_but_the_user_has_not_the_role()
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                        {Url = "/api/testUrl", Verb = HttpVerb.Post, To = "https://MyService", Roles = new[] {"R1"}}
                }
            };

            var context = GivenHttpContextWithRequest("POST", "/api/testUrl");
            context.User.Claims.Returns(new List<Claim> {new Claim(ClaimTypes.Role, "R2")});

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task Should_return_ok_When_route_match_and_the_user_has_the_requested_role()
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                        {Url = "/api/testUrl", Verb = HttpVerb.Post, To = "https://MyService", Roles = new[] {"R1"}}
                }
            };

            var context = GivenHttpContextWithRequest("POST", "/api/testUrl");
            context.User.Claims.Returns(new List<Claim> {new Claim(ClaimTypes.Role, "R1")});
            context.User.IsInRole("R1").Returns(true);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task Should_return_ok_When_route_match_and_the_user_has_only_one_of_the_requested_roles()
        {
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>
            {
                Forwards = new[]
                {
                    new RouteForwardSettings
                    {
                        Url = "/api/testUrl", Verb = HttpVerb.Post, To = "https://MyService", Roles = new[] {"R1", "R2"}
                    }
                }
            };

            var context = GivenHttpContextWithRequest("POST", "/api/testUrl");
            context.User.Claims.Returns(new List<Claim> {new Claim(ClaimTypes.Role, "R1")});
            context.User.IsInRole("R1").Returns(true);

            var requestDelegate = Substitute.For<RequestDelegate>();

            var service = GetTestedInstance(routeMappingSettings);
            await service.InvokeAsync(context, requestDelegate);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

    }

}
