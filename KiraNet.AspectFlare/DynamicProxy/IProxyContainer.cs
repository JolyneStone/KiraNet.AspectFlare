using System;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IProxyContainer
    {
        object GetProxy(Type classType, params object[] parameters);
        object GetProxy(Type interfaceType, Type classType, params object[] parameters);
    }
}
