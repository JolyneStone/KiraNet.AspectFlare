using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class ImplementConstructorsOperator : CallMethodOperator
    {
        public override void Generate(GenerateTypeContext context)
        {
            var proxyType = context.ProxyType;
            var typeBuilder = context.TypeBuilder;
            var proxyCtors = proxyType.GetConstructors(
                                        BindingFlags.CreateInstance |
                                        BindingFlags.Instance |
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic
                                    );

            foreach (var proxyCtor in proxyCtors)
            {
                var baseParameterInfos = proxyCtor.GetParameters();
                var parameters = baseParameterInfos
                                    .Select(x => x.ParameterType)
                                    .ToArray();

                // 定义构造函数
                ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                        MethodAttributes.Public,
                        CallingConventions.HasThis,
                        parameters
                    );


                SetConstructorParameters(constructorBuilder, baseParameterInfos);

                ILGenerator ctorGenerator = constructorBuilder.GetILGenerator();
                GeneratorConstructor(
                        proxyType, 
                        proxyCtor, 
                        ctorGenerator,
                        context,
                        baseParameterInfos
                    );
            }
        }

        private void GeneratorConstructor(
            Type proxyType,
            ConstructorInfo constructor,
            ILGenerator ctorGenerator,
            GenerateTypeContext context,
            ParameterInfo[] parameters)
        {
            // 初始化
            // Ldarg_0 一般是方法开始的第一个指令
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Call, context.InitMethod);


            // 调用基类构造函数 base(x, y)
            // 注：这里的调用位置在C#代码中无法表示，因为在C#中调用基类构造函数必须在方法参数列表之后
            // CallBaseMethod(ctorGenerator, constructor, parameters);
            CallMethodSync(constructor, ctorGenerator, context, parameters);
        }

        private void SetConstructorParameters(ConstructorBuilder constructorBuilder, ParameterInfo[] parameters)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                var baseParameter = parameters[i];

                var parameterBuilder = constructorBuilder.DefineParameter(
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
