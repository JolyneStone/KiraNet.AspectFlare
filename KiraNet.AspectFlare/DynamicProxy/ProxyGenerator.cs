using System;
using System.Collections.Concurrent;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyGenerator : IProxyGenerator
    {
        internal ProxyGenerator(ConcurrentDictionary<Type, ProxyDescriptor> concurrentDictionary)
        {
            _proxyContainer = concurrentDictionary ?? throw new ArgumentNullException(nameof(concurrentDictionary));
        }

        private readonly ConcurrentDictionary<Type, ProxyDescriptor> _proxyContainer;

        public object Generate(Type serviceType, params object[] parameters)
        {
            return null;
        }
    }
}
