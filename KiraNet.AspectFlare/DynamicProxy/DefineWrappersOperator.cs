using System.Reflection;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class DefineWrappersOperator : IGenerateOperator
    {
        public void Generate(GeneratorTypeContext context)
        {
            var fieldBuider = context.TypeBuilder.DefineField(
                "<_wrappers>",
                typeof(InterceptorWrapperCollection),
                FieldAttributes.Private
            );

            context.Wrappers = fieldBuider;
        }
    }
}

