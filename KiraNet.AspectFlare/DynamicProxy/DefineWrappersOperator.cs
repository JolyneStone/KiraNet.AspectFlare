using System.Reflection;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class DefineWrappersOperator : IGenerateOperator
    {
        public void Generate(GenerateTypeContext context)
        {
            var fieldBuider = context.TypeBuilder.DefineField(
                "<_wrappers>",
                typeof(InterceptorWrapperCollection),
                FieldAttributes.Private
            );

            context.WrappersField = fieldBuider;
        }
    }
}

