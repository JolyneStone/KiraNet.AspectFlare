using System;

namespace KiraNet.AspectFlare.DynamicProxy.Exensions
{
    public static class ProxyGeneratorExensions
    {
        public static object Generate(this IProxyGenerator proxyGenerator, Type serviceType)
        {
            return proxyGenerator.Generate(serviceType, null);
        }

        public static T Generate<T>(this IProxyGenerator proxyGenerator)
            where T : class
        {
            return proxyGenerator.Generate(typeof(T), null) as T;
        }

        public static T Generate<T>(this IProxyGenerator proxyGenerator, params object[] parameters)
        where T : class
        {
            return proxyGenerator.Generate(typeof(T), parameters) as T;
        }
    }
}
