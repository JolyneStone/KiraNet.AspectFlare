using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class ImplementMethodOperator : CallMethodOperator
    {
        public override void Generate(GenerateTypeContext context)
        {
            var proxyType = context.ProxyType;
            var typeBuilder = context.TypeBuilder;
            var wrappers = context.WrappersField;
            var proxyMethods = proxyType.GetMethods(
                                        BindingFlags.Instance |
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic
                                    )
                                    .Where(
                                     x =>
                                x.IsVirtual &&
                                (x.IsPublic || x.IsFamily || !(x.IsAssembly || x.IsFamilyAndAssembly || x.IsFamilyOrAssembly)) &&
                                x.HasInterceptAttribute()
                            );

            foreach (var proxyMethod in proxyMethods)
            {
                var baseParameterInfos = proxyMethod.GetParameters();
                var parameters = baseParameterInfos
                                    .Select(x => x.ParameterType)
                                    .ToArray();

                // 定义方法
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        proxyMethod.Name,
                        proxyMethod.Attributes,
                        CallingConventions.HasThis
                    );

                SetMethodParameters(methodBuilder, proxyMethod, baseParameterInfos);

                ILGenerator methodGenerator = methodBuilder.GetILGenerator();

                if(proxyMethod.IsDefined(typeof(AsyncStateMachineAttribute))
                        && proxyMethod.ReturnType != typeof(void))
                {
                    CallMethodAsync(proxyMethod, methodGenerator, context, baseParameterInfos);
                }
                else
                {
                    CallMethodSync(proxyMethod, methodGenerator, context, baseParameterInfos);
                }

                // overrided method
                //typeBuilder.DefineMethodOverride(methodBuilder, proxyMethod);
            }
        }


        private void SetMethodParameters(MethodBuilder methodBuilder, MethodInfo method, ParameterInfo[] parameters)
        {
            if (method.IsGenericMethodDefinition)
            {
                var genericArguments = method.GetGenericArguments();
                GenericTypeParameterBuilder[] typeArguments = methodBuilder.DefineGenericParameters(
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

            for (var i = 0; i < parameters.Length; i++)
            {
                var baseParameter = parameters[i];

                var parameterBuilder = methodBuilder.DefineParameter(
                            i + 1,
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
