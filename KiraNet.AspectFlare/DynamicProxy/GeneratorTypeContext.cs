using System;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class GeneratorTypeContext
    {
        public int Token { get; set; } = 0;
        public ModuleBuilder ModuleBuilder { get; set; }
        public Type ProxyType { get; set; }
        public Type InterfaceType { get; set; }
        public TypeBuilder TypeBuilder { get; set; }
        public FieldBuilder Wrappers { get; set; }
        public FieldBuilder Interface { get; set; }
        public MethodBuilder InitMethod { get; set; }
    }
}
