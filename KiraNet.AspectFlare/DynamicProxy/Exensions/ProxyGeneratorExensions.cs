using System;

namespace KiraNet.AspectFlare.DynamicProxy.Exensions
{
    public static class ProxyGeneratorExensions
    {
        public static object Generate(this IProxyContainer proxyGenerator, Type serviceType)
        {
            return proxyGenerator.GetProxy(serviceType, (object[])null);
        }

        public static T Generate<T>(this IProxyContainer proxyGenerator)
            where T : class
        {
            return proxyGenerator.GetProxy(typeof(T), (object[])null) as T;
        }

        public static T Generate<T>(this IProxyContainer proxyGenerator, params object[] parameters)
        where T : class
        {
            return proxyGenerator.GetProxy(typeof(T), parameters) as T;
        }
    }
}
