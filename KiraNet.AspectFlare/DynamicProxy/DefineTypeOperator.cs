using System;
using System.Linq;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class DefineTypeOperator : IGenerateOperator
    {
        public void Generate(GeneratorTypeContext context)
        {
            var proxyType = context.ProxyType;
            if (context.InterfaceType == null)
            {  
                var typeBuilder = context.ModuleBuilder.DefineType(
                        $"{proxyType.Name}_AspectFlare",
                        proxyType.Attributes
                    );

                typeBuilder.SetParent(proxyType);
                context.TypeBuilder = typeBuilder;
            }
            else
            {
                context.TypeBuilder = context.ModuleBuilder.DefineType(
                         $"<AspectFlare>{proxyType.Name}",
                         proxyType.Attributes,
                         typeof(object),
                         new Type[] { context.InterfaceType }
                     );
            }

            if(proxyType.IsGenericTypeDefinition)
            {
                GenerateGeneric(proxyType, context.TypeBuilder);
            }
        }

        private void GenerateGeneric(Type type, TypeBuilder typeBuilder)
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
