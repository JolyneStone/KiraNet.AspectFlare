using System;

namespace KiraNet.AspectFlare
{
    [AttributeUsage(
        AttributeTargets.Struct |
        AttributeTargets.Class |
        AttributeTargets.Constructor |
        AttributeTargets.Event |
        AttributeTargets.Property |
        AttributeTargets.Field |
        AttributeTargets.Interface |
        AttributeTargets.Method,
        AllowMultiple = true,
        Inherited = true)]
    public abstract class CalledInterceptAttribute : Attribute, ICalledInterceptor
    {
        public virtual void Called(CalledInterceptContext calledInterceptorContext)
        {
        }
    }
}
