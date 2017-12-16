using System;

namespace KiraNet.AspectFlare
{
    public class ExceptionInterceptContext
    {
        public object Owner { get; set; }
        public string InterceptedName { get; set; }
        public object[] Parameters { get; set; }
        public object ReturnValue { get; set; }
        public Exception Exception { get; set; }
        public bool HasResult { get; set; }
        public object Result { get; set; }
    }
}
