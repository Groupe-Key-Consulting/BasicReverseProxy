using System;
using BasicReverseProxy.Cache.Factories;
using BasicReverseProxy.Cache.Settings;
using BasicReverseProxy.Core;
using BasicReverseProxy.Core.RouteForwarding;
using BasicReverseProxy.Core.RouteForwarding.Client;
using BasicReverseProxy.Core.RouteForwarding.Factories;
using BasicReverseProxy.Core.RouteForwarding.Services;
using BasicReverseProxy.Core.RouteForwarding.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BasicReverseProxy.Cache.Configuration
{
    public static class CacheReverseProxyConfiguration
    {
        public static IServiceCollection AddCachedReverseProxy<THttpContextKeyCalculator>(
            this IServiceCollection services,
            Action<CacheRouteMappingSettings> setupSettings = null,
            string routeMappingConfigurationSectionName = "RouteMapping"
        )
            where THttpContextKeyCalculator : class, IHttpContextKeyCalculator
        {
            return AddCachedReverseProxy<THttpContextKeyCalculator, EmptyExpirationService>(
                services, 
                setupSettings,
                routeMappingConfigurationSectionName);
        }

        public static IServiceCollection AddCachedReverseProxy<THttpContextKeyCalculator, TExpirationService>(
            this IServiceCollection services,
            Action<CacheRouteMappingSettings> setupSettings = null,
            string routeMappingConfigurationSectionName = "RouteMapping"
            ) 
            where THttpContextKeyCalculator : class, IHttpContextKeyCalculator
            where TExpirationService : class, IExpirationService
        {
            var routeMappingSettings = BindRouteMappingSettings(services, routeMappingConfigurationSectionName);
            setupSettings?.Invoke(routeMappingSettings);

            services.AddSingleton(_ => routeMappingSettings);
            services.AddSingleton<RouteMappingSettings<CacheRouteForwardSettings>>(_ => routeMappingSettings);
            services.AddSingleton<IReverseProxyService, ReverseProxyService>();
            services.AddSingleton<IForwardServiceFactory<CacheRouteForwardSettings>, CacheForwardServiceFactory>();
            services.AddSingleton<IRouteForwardManager, RouteForwardManager<CacheRouteForwardSettings>>();
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

            services.AddSingleton<IHttpContextKeyCalculator, THttpContextKeyCalculator>();
            services.AddSingleton<IHttpResponseMessageCache, HttpResponseMessageCache>();
            services.AddSingleton<IExpirationService, TExpirationService>();

            return services;
        }

        private static CacheRouteMappingSettings BindRouteMappingSettings(IServiceCollection services, string routeMappingConfigurationSectionName)
        {
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            var routeMappingSettings = new CacheRouteMappingSettings();
            configuration?.GetSection(routeMappingConfigurationSectionName).Bind(routeMappingSettings);
            return routeMappingSettings;
        }
    }
}
