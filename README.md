# KiraNet.AspectFlare

KiraNet.AspectFlare是一个轻量级的AOP解决方案，使用Emit技术进行动态代理，可轻松的集成依赖注入。目前还只支持Microsoft.Extensions.DependencyInjection。

### 拦截器
ICallingInterceptor：在方法调用前进行拦截

ICalledInterceptor: 在方法调用后进行拦截

IExceptionInterceptor: 用于处理异常

分别集成并实现CallInterceptorAttribute、CalledInterceptorAttribute、 ExceptionAttribute这三种特性即可。

### 配置（依赖注入）
使用扩展方法UseDynamicProxyService()，然后注册服务

### 示例
``` c#
    // 继承并实现特性类，CalledAttribute同理
    public class CallingAttribute : CallingInterceptAttribute
    {
        public override void Calling(CallingInterceptContext callingInterceptorContext)
        {
            Console.WriteLine("Calling in " + callingInterceptorContext.Owner.ToString());
        }
    }

    public class ExceptionAttribute : ExceptionInterceptAttribute
    {
        public override void Exception(ExceptionInterceptContext exceptionInterceptorContext)
        {
            /// 处理完异常后，应把HasHandled设为true
            Console.WriteLine("An exception was thrown: " + exceptionInterceptorContext.Exception.Message);
            exceptionInterceptorContext.HasHandled = true;
        }
    }

    [Called]
    public interface ITest // 使用接口进行代理
    {
        [Calling]
        void Output();
    }

    public class TestClass : ITest
    {
        public void Output()
        {
            Console.WriteLine("output ...");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // 依赖注入方式（推荐）
            var services = new ServiceCollection()
                .UseDynamicProxyService(true)
                .AddScoped<ITest, TestClass>()
                .BuildServiceProvider();

            var test1 = services.GetRequiredService<ITest>();
            test1.Output();

            Console.WriteLine("\n----------------------------------\n");

            // 使用静态类
            var proxyProvider = ProxyFlare.Flare.UseDefaultProviders(true).GetProvider();
            var test2= proxyProvider.GetProxy<ITest, TestClass>();
            test2.Output();
            Console.ReadKey();
        }
    }
```

#### 输出
```
    // 输出
    Calling in <AspectFlare>TestClass
    output ...
    Called in <AspectFlare>TestClass

    ----------------------------------

    Calling in <AspectFlare>TestClass
    output ...
    Called in <AspectFlare>TestClass
```

