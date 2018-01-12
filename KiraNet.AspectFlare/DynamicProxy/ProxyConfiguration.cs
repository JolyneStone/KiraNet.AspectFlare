using System;
using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public sealed class ProxyConfiguration : IProxyConfiguration
    {
        public AppDomain ProxyDomain { get; private set; }
        public ModuleBuilder ProxyModuleBuilder { get; private set; }
        public AssemblyName ProxyAssemblyName { get; private set; }
        public AssemblyBuilder ProxyAssemblyBuilder { get; private set; }

        private static readonly Lazy<ProxyConfiguration> _configuration = new Lazy<ProxyConfiguration>(() => new ProxyConfiguration(), true);

        public static ProxyConfiguration Configuration
            => _configuration.Value;

        private ProxyConfiguration()
        {
            InitConfiguration();
        }

        private void InitConfiguration()
        {
            ProxyDomain = AppDomain.CurrentDomain;
            ProxyAssemblyName = new AssemblyName("KiraNet.AspectFlare.DynamicProxy.Dynamic");
            ProxyAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(ProxyAssemblyName, AssemblyBuilderAccess.RunAndCollect);
            ProxyModuleBuilder = ProxyAssemblyBuilder.DefineDynamicModule("DynamicModule");
        }
    }
}
