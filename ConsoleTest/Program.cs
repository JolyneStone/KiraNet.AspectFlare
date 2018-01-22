using System;
using KiraNet.AspectFlare.DynamicProxy;
using KiraNet.AspectFlare.Test;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //IProxyTypeGenerator proxyTypeGenerator = new ProxyTypeGenerator();
            //var interfaceType = proxyTypeGenerator.GenerateProxyByInterface(typeof(ITs), typeof(Tss));
            //var t0 = (ITs)Activator.CreateInstance(interfaceType, 1);
            //t0.T0();

            //var baseType = proxyTypeGenerator.GenerateProxyByClass(typeof(T));
            //var t1 = (T)Activator.CreateInstance(baseType);
            InterceptResult x = default(InterceptResult);
            InterceptResult y = default(InterceptResult);
            Exception ex1 = null;
            Exception ex2 = null;
            InterceptResult xx = default(InterceptResult);
            Exception yy = null;
            ////var s = t1.Tz(out x);
            //var q = t1.T1(ref x, out y, ref ex1, out ex2, xx, yy);

            IProxyContainer proxyContainer = new ProxyContainer();
            //var fuck = (T)proxyContainer.GetProxy(typeof(T));
            //fuck.T1(ref x, out y, ref ex1, out ex2, xx, yy);

            var tss = (ITs)proxyContainer.GetProxy(typeof(ITs), typeof(Tss), 1);
            tss.T1(ref x, out y, ref ex1, out ex2);
            Console.ReadKey();
        } 
    }
}
