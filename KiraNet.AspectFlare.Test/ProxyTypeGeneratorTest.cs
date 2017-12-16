using KiraNet.AspectFlare.DynamicProxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace KiraNet.AspectFlare.Test
{
    public class ProxyTypeGeneratorTest
    {
        [Fact]
        public void GenerateProxyByClassTest()
        {
            IProxyTypeGenerator proxyTypeGenerator = new ProxyTypeGenerator();
            var fooBaseType = proxyTypeGenerator.GenerateProxyByClass(typeof(FooBase));
            Assert.NotNull(fooBaseType);
            var fooBase1 = Activator.CreateInstance(fooBaseType, 1, new char[] { '2', '3' });
            Assert.NotNull(fooBase1);
            int x = 0;
            char y = '0';
            InterceptResult r = default(InterceptResult);
            InterceptResult u = default(InterceptResult);
            int z = 0;
            int t = 0;
            Exception ex = null;
            Exception ex1 = null;
            var fooBase2 = Activator.CreateInstance(
                fooBaseType,
                x, y, r, u, z, t, ex1, ex, new Exception(), new Exception(), new Exception(), new Exception(), new Exception());
            Assert.NotNull(fooBase2);
        }

        [Fact]
        public void GenerateProxyByGenericClassTest()
        {
            IProxyTypeGenerator proxyTypeGenerator = new ProxyTypeGenerator();
            var genericTsRawType = proxyTypeGenerator.GenerateProxyByClass(typeof(GenericTs<,>));
            var genericTsType = genericTsRawType.MakeGenericType(typeof(List<ArrayList>), typeof(ArrayList));
            var genericTs = Activator.CreateInstance(genericTsType, new List<ArrayList>(), new ArrayList());
            Assert.NotNull(genericTs);
        }

        [Fact]
        public void GenerateProxyMethodByClassTest()
        {
            IProxyTypeGenerator proxyTypeGenerator = new ProxyTypeGenerator();
            var fooBaseType = proxyTypeGenerator.GenerateProxyByClass(typeof(FooBase));
            var foo = Activator.CreateInstance(fooBaseType, 1, new char[] { '2', '3' }) as FooBase;
            Assert.NotNull(fooBaseType);
            var x = 0;
            var y = 0;
            var ex1 = new Exception();
            Exception ex2;
            foo.NoReturn(ref x, out y, ref ex1, out ex2);
            Assert.Equal(1, y);
            Assert.Equal(typeof(NotImplementedException), ex2.GetType());

            var result1 = foo.HasReturn(ref x, out y, ref ex1, out ex2);
            Assert.Equal(1, result1);
            Assert.Equal(2, y);
            Assert.Equal(typeof(NotImplementedException), ex2.GetType());

            var result2 = foo.GenericHasReturn(new NotImplementedException(), new List<int>());
            Assert.Equal(1, result2);
        }
    }

    public class CallingAttribute : CallingInterceptAttribute
    {

    }

    public class CalledAttribute : CalledInterceptAttribute
    {

    }

    public class ExceptionAttribute : ExceptionInterceptAttribute
    {

    }


    [Calling]
    [Called]
    [Exception]
    public class FooBase
    {
        public FooBase() { }
        public FooBase(int x)
        {
        }

        public FooBase(int x, string s)
        {
        }

        public FooBase(Exception a)
        {

        }

        public FooBase(Exception a, Exception b)
        {

        }

        public FooBase(Exception a, Exception b, Exception c, Exception d)
        {

        }

        public FooBase(int x, params char[] cs)
        {

        }

        public FooBase(int x, string s, Exception a, Exception b, int q, int w, int e, int r, int t, int y, int u, int i)
        {
        }

        public FooBase(ref int x, out char y, ref InterceptResult r, out InterceptResult u, int z, int t, out Exception ex1, ref Exception ex2, Exception ex3, Exception ex4, Exception ex5, Exception ex6, Exception ex7)
        {
            y = '1';
            u = default(InterceptResult);
            ex1 = null;
        }

        public virtual void NoReturn(ref int x, out int y, ref Exception ex1, out Exception ex2)
        {
            y = 1;
            ex2 = new NotImplementedException();
        }

        public virtual int HasReturn(ref int x, out int y, ref Exception ex1, out Exception ex2)
        {
            y = 2;
            ex2 = new NotImplementedException();
            return 1;
        }

        public virtual int GenericHasReturn<T1, T2>(T1 t1, T2 t2)
            where T1: Exception,new()
            where T2: class, IList<int>
        {
            return 1;
        }
    }

    [Calling]
    [Called]
    [Exception]
    public class Foo : FooBase
    {
        private ICallingInterceptor[] _callingInterceptors;
        private ICalledInterceptor[] _calledInterceptors;
        private IExceptionInterceptor _exceptionInterceptor;
        public Foo() : base()
        {
        }
        public Foo(int x) : base(x)
        {

        }

        public Foo(int x, string y) : base(x, y)
        {

        }

        public Foo(int l, string s, Exception a, Exception b, int q, int w, int e, int r, int t, int y, int u, int i) : base(l, s, a, b, q, w, e, r, t, y, u, i)
        {
            InitProxy();
            var method = MethodInfo.GetCurrentMethod();
        }

        public Foo(Exception a) : base(a)
        {

        }

        public Foo(Exception a, Exception b) : base(a, b)
        {

        }

        public Foo(Exception a, Exception b, Exception c, Exception d) : base(a, b, c, d)
        {

        }

        public Foo(int x, params char[] cs) : base(x, cs)
        {

        }

        private void InitProxy()
        {
            var baseType = typeof(FooBase);
            _callingInterceptors = baseType.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>().ToArray();
            _calledInterceptors = baseType.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>().ToArray();
            _exceptionInterceptor = baseType.GetCustomAttribute<ExceptionInterceptAttribute>(true) as IExceptionInterceptor;
        }
    }
}
