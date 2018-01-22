using System;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyDescriptor
    {
        public Type InterfaceType { get; set; }
        public Type ClassType { get; set; }
        public Type ProxyType { get; internal set; }
    }
}
