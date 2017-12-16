using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class GeneratorAsyncContext : GeneratorSyncContext
    {
        public GeneratorAsyncContext(GenerateTypeContext context) : base(context)
        {
        }

        public AsyncType AsyncType { get; set; }
        public FieldInfo CallerField { get; set; }
        public FieldInfo FuncField { get; set; }
        public MethodBuilder FuncMethod { get; set; }
        public TypeBuilder DisplayClass { get; set; }
        public FieldInfo[] DisplayClassFields { get; set; }
        public ConstructorInfo DisplayClassCtor { get; set; }
        public MethodInfo DisplayClassMethod { get; set; }
    }
}
