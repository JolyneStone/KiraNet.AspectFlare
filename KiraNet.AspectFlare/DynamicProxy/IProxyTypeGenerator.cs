using System;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IProxyTypeGenerator
    {
        Type GenerateProxyByClass(Type proxyType);
        Type GenerateProxyByInterface(Type interfaceType, Type proxyType);
    }
}
