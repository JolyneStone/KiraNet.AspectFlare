namespace KiraNet.AspectFlare.DynamicProxy
{
    public class InterceptorWrapper
    {
        public ICallingInterceptor[] CallingInterceptors { get; set; }
        public ICalledInterceptor[] CalledInterceptors { get; set; }
        public IExceptionInterceptor ExceptionInterceptor { get; set; }
    }
}
