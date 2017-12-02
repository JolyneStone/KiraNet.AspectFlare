using System;
using System.Collections.Concurrent;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyGeneratorCollection : IProxyGeneratorCollection
    {
        private static readonly ConcurrentDictionary<Type, ProxyDescriptor> _proxyContainer = new ConcurrentDictionary<Type, ProxyDescriptor>();
        public IProxyGeneratorCollection AddProxy(ProxyDescriptor proxyDescriptor)
        {
            if (proxyDescriptor == null)
            {
                throw new ArgumentNullException(nameof(proxyDescriptor));
            }

            if(proxyDescriptor.ServiceType == null || proxyDescriptor.ProxyType == null)
            {
                throw new InvalidOperationException("ServiceType or ProxyType are not allowed to be null");
            }

            if(!_proxyContainer.TryAdd(proxyDescriptor.ServiceType, proxyDescriptor))
            {
                throw new InvalidOperationException($"add this {proxyDescriptor.ServiceType.FullName} failed");
            }

            return this;
        }

        public IProxyGenerator Build()
        {
            throw new NotImplementedException();
        }
    }
}
