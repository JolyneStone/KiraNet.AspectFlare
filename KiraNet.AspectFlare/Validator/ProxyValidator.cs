using System;
using System.Linq;
using System.Reflection;
using KiraNet.AspectFlare.DynamicProxy;

namespace KiraNet.AspectFlare.Validator
{
    public class ProxyValidator : IProxyValidator
    {
        private static readonly Type NonIntercept = typeof(NonInterceptAttribute);
        private static readonly Type CallingIntercept = typeof(CallingInterceptAttribute);
        private static readonly Type CalledIntercept = typeof(CalledInterceptAttribute);
        private static readonly Type ExceptionIntercept = typeof(ExceptionInterceptAttribute);
        public bool Validate(Type serviceType, Type proxyType)
        {
            if (serviceType == null ||
                serviceType.IsGenericTypeDefinition ||
                serviceType.IsDefined(typeof(NonInterceptAttribute), true))
            {
                return false;
            }

            if (proxyType == null ||
                !proxyType.IsClass ||
                !proxyType.IsValueType ||
                !proxyType.IsVisible ||
                !proxyType.IsSealed ||
                serviceType.IsDefined(typeof(NonInterceptAttribute), true))
            {
                return false;
            }

            if(!proxyType.HasInterceptAttribute())
            {
                return false;
            }

            // 受代理的类型至少有一个公有构造函数
            // 或至少有一个方法上有拦截器特性且其实现的接口中允许指定拦截器类型
            // 
            if ((!proxyType.GetConstructors(
                    BindingFlags.CreateInstance |
                    BindingFlags.Instance |
                    BindingFlags.Public)
                    .Any())
                ||
                ((!proxyType.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                    .Any(method => method.HasInterceptAttribute()))
                &&
                (serviceType.IsInterface &&
                 (!(serviceType.GetMethods()
                    .Any(method => method.HasInterceptAttribute())?
                    true:
                    serviceType.HasInterceptAttribute())))))
            {
                return false;
            }

            return true;
        }
    }
}
