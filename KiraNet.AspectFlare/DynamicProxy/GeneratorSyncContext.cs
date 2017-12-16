using System;
using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class GeneratorSyncContext: GenerateTypeContext
    {
        public GeneratorSyncContext(GenerateTypeContext context)
        {
            this.TypeBuilder = context.TypeBuilder;
            this.WrappersField = context.WrappersField;
            this.ProxyType = context.ProxyType;
            this.InterfaceType = context.InterfaceType;
            this.InterfaceField = context.InterfaceField;
            this.InitMethod = context.InitMethod;
        }

        public MethodBase Method { get; set; }
        public ILGenerator Generator { get; set; }
        public Type ReturnType { get; set; }

        public ParameterInfo[] Parameters { get; set; }
    }
}
