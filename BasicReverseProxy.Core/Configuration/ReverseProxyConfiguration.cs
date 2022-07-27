using System;
using BasicReverseProxy.Core.RouteForwarding;
using BasicReverseProxy.Core.RouteForwarding.Client;
using BasicReverseProxy.Core.RouteForwarding.Factories;
using BasicReverseProxy.Core.RouteForwarding.Services;
using BasicReverseProxy.Core.RouteForwarding.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BasicReverseProxy.Core.Configuration
{
    public static class ReverseProxyConfiguration
    {
        public static IServiceCollection AddReverseProxy(
            this IServiceCollection services,
            Action<RouteMappingSettings<RouteForwardSettings>> setupSettings = null,
            string routeMappingConfigurationSectionName = "RouteMapping"
            )
        {
            var routeMappingSettings = BindRouteMappingSettings(services, routeMappingConfigurationSectionName);
            setupSettings?.Invoke(routeMappingSettings);

            services.AddSingleton(_ => routeMappingSettings);
            services.AddSingleton<IReverseProxyService, ReverseProxyService>();
            services.AddSingleton<IForwardServiceFactory<RouteForwardSettings>, ForwardServiceFactory<RouteForwardSettings>>();
            services.AddSingleton<IRouteForwardManager, RouteForwardManager<RouteForwardSettings>>();
            services.AddSingleton<IWebClientFactory, WebClientFactory>();
            services.AddSingleton<IWebClientBuilderFactory, WebClientBuilderFactory>();

            switch (routeMappingSettings.AuthenticationType)
            {
                case AuthenticationType.Cookie:
                    services.AddSingleton<IAuthenticationService, CookieAuthenticationService>();
                    break;
                case AuthenticationType.None:
                    services.AddSingleton<IAuthenticationService, NoAuthenticationService>();
                    break;
            }

            return services;
        }

        private static RouteMappingSettings<RouteForwardSettings> BindRouteMappingSettings(IServiceCollection services, string routeMappingConfigurationSectionName)
        {
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            var routeMappingSettings = new RouteMappingSettings<RouteForwardSettings>();
            configuration?.GetSection(routeMappingConfigurationSectionName).Bind(routeMappingSettings);
            return routeMappingSettings;
        }

        public static IApplicationBuilder UseReverseProxy(this IApplicationBuilder app)
        {
            app.UseMiddleware<IReverseProxyService>();
            return app;
        }
    }
}
