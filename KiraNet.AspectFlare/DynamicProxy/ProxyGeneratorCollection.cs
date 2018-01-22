//using System;
//using System.Collections.Concurrent;

//namespace KiraNet.AspectFlare.DynamicProxy
//{
//    public class ProxyGeneratorCollection : IProxyGeneratorCollection
//    {
//        private static readonly ConcurrentDictionary<Type, ProxyDescriptor> _proxyContainer = new ConcurrentDictionary<Type, ProxyDescriptor>();

//        public IProxyGeneratorCollection AddProxy(ProxyDescriptor proxyDescriptor)
//        {
//            if (proxyDescriptor == null)
//            {
//                throw new ArgumentNullException(nameof(proxyDescriptor));
//            }

//            if(proxyDescriptor.ClassType == null || proxyDescriptor.ProxyType == null)
//            {
//                throw new InvalidOperationException("ServiceType or ProxyType are not allowed to be null");
//            }

//            if(!_proxyContainer.TryAdd(proxyDescriptor.ClassType, proxyDescriptor))
//            {
//                throw new InvalidOperationException($"add this {proxyDescriptor.ClassType.FullName} failed");
//            }

//            return this;
//        }

//        public IProxyContainer Build()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
