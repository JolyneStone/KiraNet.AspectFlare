using KiraNet.AspectFlare.DynamicProxy;
using System;
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
            var fooBase = Activator.CreateInstance(fooBaseType, 1, new char[] { '2', '3' });
            Assert.NotNull(fooBase);
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
