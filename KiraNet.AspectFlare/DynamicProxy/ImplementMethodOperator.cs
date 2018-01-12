using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using KiraNet.AspectFlare.DynamicProxy.Exensions;
using KiraNet.AspectFlare.Utilities;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class ImplementMethodOperator : CallMethodOperator
    {
        public override void Generate(GeneratorTypeContext context)
        {
            var proxyType = context.ProxyType;
            var typeBuilder = context.TypeBuilder;

            bool hasTypeIntercept = proxyType.HasInterceptAttribute();
            foreach (var proxyMethod in proxyType.GetMethods(
                                        BindingFlags.Instance |
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic
                                    ).Where(
                                     x =>
                                       x.IsVirtual &&
                                      (x.IsPublic || x.IsFamily)
                            )) 
            {
                if (proxyMethod.IsDefined(typeof(NonInterceptAttribute)))
                {
                    continue;
                }
                if (!proxyMethod.HasDefineInterceptAttribute() && !hasTypeIntercept)
                {
                    continue;
                }

                var baseParameterInfos = proxyMethod.GetParameters();
                var parameters = baseParameterInfos
                                    .Select(x => x.ParameterType)
                                    .ToArray();

                // 定义方法
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        proxyMethod.Name,
                        proxyMethod.Attributes ^ MethodAttributes.NewSlot,
                        CallingConventions.HasThis | CallingConventions.Standard,
                        proxyMethod.ReturnType,
                        baseParameterInfos.Select(x=>x.ParameterType).ToArray()
                    );

                methodBuilder.SetReturnType(proxyMethod.ReturnType);
                methodBuilder.SetMethodParameters(proxyMethod, baseParameterInfos);

                ILGenerator methodGenerator = methodBuilder.GetILGenerator();
                GenerateMethod(methodBuilder, proxyMethod, methodGenerator, context, baseParameterInfos);

                // TODO: interfacing
                // overrided method
                typeBuilder.DefineMethodOverride(methodBuilder, proxyMethod);
            }
        }
    }
}
