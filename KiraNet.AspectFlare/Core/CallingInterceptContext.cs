﻿namespace KiraNet.AspectFlare
{
    public class CallingInterceptContext
    {
        public object Owner { get; set; }
        public string InterceptedName { get; set; }
        public object[] Parameters { get; set; }
        public object Result { get; set; }
    }
}