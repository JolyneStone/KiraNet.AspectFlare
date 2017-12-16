using System;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class GenerateTypeContext
    {
        public ModuleBuilder ModuleBuilder { get; set; }
        public Type ProxyType { get; set; }
        public Type InterfaceType { get; set; }
        public TypeBuilder TypeBuilder { get; set; }
        public FieldBuilder WrappersField { get; set; }
        public FieldBuilder InterfaceField { get; set; }
        public MethodBuilder InitMethod { get; set; }
    }
}
