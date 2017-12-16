using System;
using System.Linq;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class DefineGenericTypeArgumentsOperator : IGenerateOperator
    {
        public void Generate(GenerateTypeContext context)
        {
            Generate(context.ProxyType, context.TypeBuilder);
            // TODO: 对于泛型接口还需要测试
        }

        private void Generate(Type type, TypeBuilder typeBuilder)
        {
            var genericArguments = type.GetGenericArguments();
            GenericTypeParameterBuilder[] typeArguments = typeBuilder.DefineGenericParameters(
                                                                genericArguments.Select(x => x.Name).ToArray()
                                                        );
            for (var i = 0; i < genericArguments.Length; i++)
            {
                var typeArgument = typeArguments[i];
                var genericArgument = genericArguments[i];
                typeArgument.SetGenericParameterAttributes(genericArguments[i].GenericParameterAttributes);
                typeArgument.SetBaseTypeConstraint(genericArgument.BaseType);
                typeArgument.SetInterfaceConstraints(genericArgument.GetInterfaces());
            }
        }
    }
}
