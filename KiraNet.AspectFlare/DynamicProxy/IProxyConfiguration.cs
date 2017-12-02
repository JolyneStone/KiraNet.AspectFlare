using System;
using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IProxyConfiguration
    {
        AppDomain ProxyDomain { get; }
        ModuleBuilder ProxyModuleBuilder { get; }
        AssemblyName ProxyAssblyName { get; }
        AssemblyBuilder ProxyAssemblyBuilder { get; }

    }
}
