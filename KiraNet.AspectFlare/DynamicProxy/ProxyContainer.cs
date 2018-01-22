using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyContainer : IProxyContainer
    {
        public ProxyContainer()
        {
            _generator = new ProxyTypeGenerator();
        }

        private static readonly object _sync = new object();
        private static readonly Dictionary<Type, ProxyDescriptor> _proxyClassContainer = new Dictionary<Type, ProxyDescriptor>();
        private static readonly Dictionary<Type, Dictionary<Type, ProxyDescriptor>> _proxyInterfaceContainer = new Dictionary<Type, Dictionary<Type, ProxyDescriptor>>();
        private readonly IProxyTypeGenerator _generator;

        public object GetProxy(Type classType, params object[] parameters)
        {
            if (!_proxyClassContainer.TryGetValue(classType, out var descriptor))
            {
                lock (_sync)
                {
                    descriptor = _generator.GenerateProxyByClass(classType);
                    if (!_proxyClassContainer.ContainsKey(classType))
                    {
                        _proxyClassContainer[classType] = descriptor;
                    }
                }
            }

            return ProxyConfiguration.Configuration.ProxyAssemblyBuilder.CreateInstance(
                descriptor.ProxyType.FullName,
                false,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameters,
                CultureInfo.CurrentCulture,
                null
            );
        }

        public object GetProxy(Type interfaceType, Type classType, params object[] parameters)
        {
            ProxyDescriptor descriptor;
            if (_proxyInterfaceContainer.TryGetValue(interfaceType, out var dict))
            {
                if (!dict.TryGetValue(interfaceType, out descriptor))
                {
                    lock (_sync)
                    {
                        descriptor = _generator.GenerateProxyByInterface(interfaceType, classType);
                        if (!_proxyInterfaceContainer.ContainsKey(interfaceType))
                        {
                            _proxyInterfaceContainer[interfaceType].Add(classType, descriptor);
                        }
                    }
                }
            }
            else
            {
                lock (_sync)
                {
                    descriptor = _generator.GenerateProxyByInterface(interfaceType, classType);
                    if (!_proxyInterfaceContainer.ContainsKey(interfaceType))
                    {
                        _proxyInterfaceContainer.Add(interfaceType, new Dictionary<Type, ProxyDescriptor>()
                        {
                            { classType, descriptor }
                        });
                    }
                }
            }

            return ProxyConfiguration.Configuration.ProxyAssemblyBuilder.CreateInstance(
                descriptor.ProxyType.FullName,
                false,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                parameters,
                CultureInfo.CurrentCulture,
                null
            );
        }
    }
}
