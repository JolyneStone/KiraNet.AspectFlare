using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using KiraNet.AspectFlare;
using KiraNet.AspectFlare.DynamicProxy;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            T1();
            Console.ReadKey();
        }

        static async void T1()
        {
            var config = new ProxyConfiguration(1)
            {
                ProxyDomain = AppDomain.CurrentDomain,
                ProxyAssemblyName = new AssemblyName("KiraNet.AspectFlare.DynamicProxy.Dynamic")
            };
            config.ProxyAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(config.ProxyAssemblyName, AssemblyBuilderAccess.RunAndSave);
            config.ProxyModuleBuilder = config.ProxyAssemblyBuilder.DefineDynamicModule("DynamicModule");

            IProxyTypeGenerator proxyTypeGenerator = new ProxyTypeGenerator(config.ProxyModuleBuilder);
            var type = proxyTypeGenerator.GenerateProxyByClass(typeof(T1));

            config.ProxyAssemblyBuilder.Save(config.ProxyAssemblyName.Name + ".dll");
            var t = (T1)Activator.CreateInstance(type);
            await t.Test1();
        }
    }

}
