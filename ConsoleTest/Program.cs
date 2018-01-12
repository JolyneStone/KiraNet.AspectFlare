using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KiraNet.AspectFlare.Test;
using System.Reflection;
using System.Collections.Generic;
using KiraNet.AspectFlare.DynamicProxy;
using System.Reflection.Emit;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            IProxyTypeGenerator proxyTypeGenerator = new ProxyTypeGenerator();
            var baseType = proxyTypeGenerator.GenerateProxyByClass(typeof(T));
            //var asd = typeof(T).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)[0];
            //foreach (var x in asd.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            //{
            //    Console.WriteLine(x);
            //}

            //Console.WriteLine(typeof(T).GetMethod("T0", BindingFlags.Public | BindingFlags.Instance).GetParameters()== null);

            var t1 = (T)Activator.CreateInstance(baseType);
            //var type = t1.GetType().GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)[0];
            //var st = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance)[0].Invoke(null);
            //Console.WriteLine();
            //foreach(var x in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            //{
            //    Console.WriteLine(x);
            //}
            ////type.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(st, null);
            InterceptResult x = default(InterceptResult);
            InterceptResult y = default(InterceptResult);
            Exception ex1 = null;
            Exception ex2 = null;
            //t1.T2(x, y, ex1, ex2);
            InterceptResult xx = default(InterceptResult);
            Exception yy = null;

            var q = t1.T1(ref x, out y, ref ex1, out ex2, xx, yy);
            Console.ReadKey();
        } 
    }
}
