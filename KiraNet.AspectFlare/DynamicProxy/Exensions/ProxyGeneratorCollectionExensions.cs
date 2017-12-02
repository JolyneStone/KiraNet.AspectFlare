using System;

namespace KiraNet.AspectFlare.DynamicProxy.Exensions
{
    public static class ProxyGeneratorCollectionExensions
    {
        public static IProxyGeneratorCollection AddProxy(this IProxyGeneratorCollection collection, Type proxyType)
        {
            if (proxyType == null)
            {
                throw new ArgumentNullException(nameof(proxyType));
            }

            if (proxyType.IsValueType)
            {
                throw new ArgumentException(nameof(proxyType));
            }

            return collection.AddProxy(new ProxyDescriptor
            {
                ServiceType = proxyType,
                ProxyType = proxyType
            });
        }

        public static IProxyGeneratorCollection AddProxy(this IProxyGeneratorCollection collection, Type serviceType, Type proxyType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (proxyType == null)
            {
                throw new ArgumentNullException(nameof(proxyType));
            }

            if (serviceType.IsValueType)
            {
                throw new ArgumentException(nameof(serviceType));
            }

            if (proxyType.IsValueType)
            {
                throw new ArgumentException(nameof(proxyType));
            }

            return collection.AddProxy(new ProxyDescriptor
            {
                ServiceType = serviceType,
                ProxyType = proxyType
            });
        }

        public static IProxyGeneratorCollection AddProxy<TProxy>(this IProxyGeneratorCollection collection)
            where TProxy : class

        {
            var type = typeof(TProxy);
            return collection.AddProxy(new ProxyDescriptor
            {
                ServiceType = type,
                ProxyType = type
            });
        }


        public static IProxyGeneratorCollection AddProxy<TService, TProxy>(
            this IProxyGeneratorCollection collection)
            where TService : class
            where TProxy : class, TService
        {
            return collection.AddProxy(new ProxyDescriptor
            {
                ServiceType = typeof(TService),
                ProxyType = typeof(TProxy)
            });
        }
    }
}
