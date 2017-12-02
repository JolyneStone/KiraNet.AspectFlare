using System;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IProxyGenerator
    {
        object Generate(Type serviceType, params object[] parameters);
    }
}
