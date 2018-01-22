using System;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IProxyTypeGenerator
    {
        ProxyDescriptor GenerateProxyByClass(Type classType);
        ProxyDescriptor GenerateProxyByInterface(Type interfaceType, Type classType);
    }
}
