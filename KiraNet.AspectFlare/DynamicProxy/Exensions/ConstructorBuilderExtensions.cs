using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy.Exensions
{
    internal static class ConstructorBuilderExtensions
    {
        public static void SetMethodParameters(this ConstructorBuilder ctorBuilder, ParameterInfo[] parameters)
        {
            //if (method.IsGenericMethodDefinition)
            //{
            //    var genericArguments = method.GetGenericArguments();
            //    GenericTypeParameterBuilder[] typeArguments = ctorBuilder.DefineGenericParameters(
            //                                                        genericArguments.Select(x => x.Name).ToArray()
            //                                                );
            //    for (var i = 0; i < genericArguments.Length; i++)
            //    {
            //        var typeArgument = typeArguments[i];
            //        var genericArgument = genericArguments[i];
            //        typeArgument.SetGenericParameterAttributes(genericArguments[i].GenericParameterAttributes);
            //        typeArgument.SetBaseTypeConstraint(genericArgument.BaseType);
            //        typeArgument.SetInterfaceConstraints(genericArgument.GetInterfaces());
            //    }
            //}

            for (var i = 0; i < parameters.Length; i++)
            {
                var baseParameter = parameters[i];
                var parameterBuilder = ctorBuilder.DefineParameter(
                            i + 1,   // TODO
                            baseParameter.Attributes,
                            baseParameter.Name
                        );

                if (baseParameter.HasDefaultValue && baseParameter.TryGetDefaultValue(out object defaultValue))
                {
                    parameterBuilder.SetConstant(defaultValue);
                }
            }
        }
    }
}
