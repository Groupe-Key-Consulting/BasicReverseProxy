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
        public static IServiceCollection AddReverseProxy(this IServiceCollection services, Action<RouteMappingSettings> setupSettings = null)
        {
            var routeMappingSettings = BindRouteMappingSettings(services);
            setupSettings?.Invoke(routeMappingSettings);

            services.AddSingleton(s => routeMappingSettings);
            services.AddSingleton<IReverseProxyService, ReverseProxyService>();
            services.AddSingleton<IForwardServiceFactory, ForwardServiceFactory>();
            services.AddSingleton<IRouteForwardManager, RouteForwardManager>();
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

        private static RouteMappingSettings BindRouteMappingSettings(IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            var routeMappingSettings = new RouteMappingSettings();
            configuration?.GetSection("RouteMapping").Bind(routeMappingSettings);
            return routeMappingSettings;
        }

        public static IApplicationBuilder UseReverseProxy(this IApplicationBuilder app)
        {
            app.UseMiddleware<IReverseProxyService>();
            return app;
        }
    }
}
