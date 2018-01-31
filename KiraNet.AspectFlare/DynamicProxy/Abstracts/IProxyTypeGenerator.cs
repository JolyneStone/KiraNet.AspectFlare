using System;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IProxyTypeGenerator
    {
        Type GenerateProxyByClass(Type classType);
        Type GenerateProxyByInterface(Type interfaceType, Type classType);
    }
}
