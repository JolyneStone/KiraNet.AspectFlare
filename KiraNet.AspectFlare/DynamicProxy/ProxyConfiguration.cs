using System;
using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyConfiguration : IProxyConfiguration
    {
        public AppDomain ProxyDomain { get; protected set; }
        public ModuleBuilder ProxyModuleBuilder { get; protected set; }
        public AssemblyName ProxyAssblyName { get; protected set; }
        public AssemblyBuilder ProxyAssemblyBuilder { get; protected set; }

        private static readonly Lazy<ProxyConfiguration> _configuration = new Lazy<ProxyConfiguration>(() => new ProxyConfiguration(), true);

        public static ProxyConfiguration Configuration
            => _configuration.Value;

        private ProxyConfiguration()
        {
            InitConfiguration();
        }

        protected virtual void InitConfiguration()
        {
            ProxyDomain = AppDomain.CurrentDomain;
            ProxyAssblyName = new AssemblyName("KiraNet.AspectFlare.DynamicProxy.Dynamic");
            ProxyAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(ProxyAssblyName, AssemblyBuilderAccess.RunAndCollect);
            ProxyModuleBuilder = ProxyAssemblyBuilder.DefineDynamicModule("DynamicModule");
        }
    }
}
