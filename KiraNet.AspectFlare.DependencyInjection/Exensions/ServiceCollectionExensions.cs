using KiraNet.AspectFlare.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using KiraNet.AspectFlare.Validator;

namespace KiraNet.AspectFlare.DependencyInjection
{
    public static class ServiceCollectionExensions
    {
        public static IServiceCollection UseDynamicProxyService(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new System.ArgumentNullException(nameof(services));
            }

            var proxyServices = new ProxyServiceCollection(services);

            services
                .AddSingleton<ProxyServiceCollection>(proxyServices)
                .AddSingleton<IProxyConfiguration, ProxyConfiguration>(_ => ProxyConfiguration.Configuration)
                .AddScoped<IProxyValidator, ProxyValidator>()
                .AddScoped<IProxyContainer, ProxyContainer>()
                .AddScoped<IProxyTypeGenerator, ProxyTypeGenerator>();

            return proxyServices;
        }
    }
}
