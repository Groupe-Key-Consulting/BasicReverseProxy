using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BasicReverseProxy.Core.RouteForwarding.Factories;
using BasicReverseProxy.Core.RouteForwarding.Services;
using BasicReverseProxy.Core.RouteForwarding.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BasicReverseProxy.Core.RouteForwarding
{
    public interface IRouteForwardManager
    {
        Task<ForwardResponseMessage> TryToForwardAsync(HttpContext context);
    }

    public class RouteForwardManager<T> : IRouteForwardManager where T : RouteForwardSettings
    {
        private readonly IForwardServiceFactory<T> _forwardServiceFactory;
        private readonly IDictionary<string,IEnumerable<RouteForwardMapping<T>>> _indexedMappings = new Dictionary<string, IEnumerable<RouteForwardMapping<T>>>();
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<RouteForwardManager<T>> _logger;

        public RouteForwardManager(
            ILogger<RouteForwardManager<T>> logger,
            RouteMappingSettings<T> routeMappingSettings,
            IForwardServiceFactory<T> forwardServiceFactory,
            IAuthenticationService authenticationService)
        {
            _logger = logger;
            _authenticationService = authenticationService;
            _forwardServiceFactory = forwardServiceFactory;

            if (routeMappingSettings?.Forwards != null)
            {
                _indexedMappings = routeMappingSettings.Forwards.GroupBy(r => GetKey(r.Url))
                    .ToDictionary(g => g.Key, v => v.Select(f => new RouteForwardMapping<T>(f)));
            }
        }

        private static string GetKey(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }
            var discriminator = url.Split('/').FirstOrDefault(s =>
                !string.IsNullOrEmpty(s) && !s.Equals("api", StringComparison.InvariantCultureIgnoreCase));

            return discriminator == null ? string.Empty : discriminator.ToLower();
        }

        private bool HasAnyRole(HttpContext context, IEnumerable<string> roles)
        {
            if (roles == null)
            {
                return true;
            }

            var rolesArray = roles as string[] ?? roles.ToArray();
            if (!rolesArray.Any())
            {
                return true;
            }

            return rolesArray.Any(role => context.User.IsInRole(role));
        }

        public async Task<ForwardResponseMessage> TryToForwardAsync(HttpContext context)
        {
            if (_indexedMappings == null)
            {
                return NotForwardedResponseMessage();
            }

            if (!_indexedMappings.TryGetValue(GetKey(context.Request.Path.Value), out var mappingCollection))
            {
                return NotForwardedResponseMessage();
            }

            try
            {
                foreach (var mapping in mappingCollection)
                {
                    var mappingResult = mapping.IsMatch(context.Request);
                    if (!mappingResult.IsMatch)
                    {
                        continue;
                    }

                    _logger.LogDebug($"Forwarding {context.Request.Path}");
                    await _authenticationService.AuthenticateAsync(context).ConfigureAwait(true);
                    if (!HasAnyRole(context, mappingResult.Settings.Roles))
                    {
                        return UnauthorizedResponseMessage();
                    }

                    var forwarderService = this._forwardServiceFactory.CreateForwarderService(mappingResult.Settings);
                    var response =
                        await forwarderService.SendAsync(context.Request, mappingResult.RouteValueDictionary)
                            .ConfigureAwait(true);

                    return ForwardedResponseMessage(response);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug($"Forward {context.Request.Path} cancelled");
                return CancelledResponseMessage();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error while trying to forward {context.Request.Path}, continuing with MVC controller");
            }
            

            return NotForwardedResponseMessage();
        }

        private static ForwardResponseMessage ForwardedResponseMessage(HttpResponseMessage response)
        {
            return new ForwardResponseMessage
            {
                HasBeenForwarded = true,
                Response = response,
                IsCancelled = false
            };
        }

        private static ForwardResponseMessage UnauthorizedResponseMessage()
        {
            return new ForwardResponseMessage
                {HasBeenForwarded = true, Response = new HttpResponseMessage(HttpStatusCode.Unauthorized)};
        }

        private static ForwardResponseMessage NotForwardedResponseMessage()
        {
            return new ForwardResponseMessage { HasBeenForwarded = false, IsCancelled = false };
        }

        private static ForwardResponseMessage CancelledResponseMessage()
        {
            return new ForwardResponseMessage { HasBeenForwarded = true, IsCancelled = true };
        }
    }
}
