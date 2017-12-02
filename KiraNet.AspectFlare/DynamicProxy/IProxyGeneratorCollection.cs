using System;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IProxyGeneratorCollection
    {
        IProxyGeneratorCollection AddProxy(ProxyDescriptor proxyDescriptor);
        IProxyGenerator Build();
    }
}
