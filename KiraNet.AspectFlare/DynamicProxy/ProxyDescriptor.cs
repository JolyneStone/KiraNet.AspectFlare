using System;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ProxyType { get; set; }
        public Type ProxyImplementType { get; internal set; }
        public object Proxy { get; internal set; }
    }
}
