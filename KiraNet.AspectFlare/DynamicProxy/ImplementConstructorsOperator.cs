using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using KiraNet.AspectFlare.DynamicProxy.Exensions;
using KiraNet.AspectFlare.Utilities;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class ImplementConstructorsOperator : CallMethodOperator
    {
        public override void Generate(GeneratorTypeContext context)
        {
            GenerateInit(context);
            var proxyType = context.ProxyType;
            var typeBuilder = context.TypeBuilder;
            bool hasTypeIntercept = proxyType.HasInterceptAttribute();
            foreach (var ctor in  proxyType.GetConstructors(
                                        BindingFlags.CreateInstance |
                                        BindingFlags.Instance |
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic
                                    ).Where(
                                     x =>
                                        x.IsPublic ||
                                        x.IsFamily || 
                                        !(x.IsAssembly || x.IsFamilyAndAssembly || x.IsFamilyOrAssembly)
                                    ))
            {
                var baseParameterInfos = ctor.GetParameters();
                var parameters = baseParameterInfos
                                    .Select(x => x.ParameterType)
                                    .ToArray();

                // 定义构造函数
                ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                        MethodAttributes.Public,
                        CallingConventions.HasThis,
                        parameters
                    );

                bool isIntercept = true;
                if (ctor.IsDefined(typeof(NonInterceptAttribute)))
                {
                    isIntercept = false;
                }
                if (!ctor.HasDefineInterceptAttribute() && !hasTypeIntercept)
                {
                    isIntercept = false;
                }

                constructorBuilder.SetMethodParameters(baseParameterInfos);
                ILGenerator ctorGenerator = constructorBuilder.GetILGenerator();
                GeneratorConstructor(
                        proxyType,
                        constructorBuilder,
                        ctor, 
                        ctorGenerator,
                        context,
                        baseParameterInfos,
                        isIntercept
                    );
            }
        }

        private void GeneratorConstructor(
            Type proxyType,
            ConstructorBuilder methodBuilder, 
            ConstructorInfo constructor,
            ILGenerator ctorGenerator,
            GeneratorTypeContext context,
            ParameterInfo[] parameters,
            bool isIntercept)
        {
            // 初始化
            // Ldarg_0 一般是方法开始的第一个指令
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Call, context.InitMethod);

            if (isIntercept)
            {
                // 调用基类构造函数 base(x)
                // 注：这里的调用位置在C#代码中无法表示，因为在C#中调用基类构造函数必须在方法参数列表之后
                GenerateMethod(methodBuilder, constructor, ctorGenerator, context, parameters);
            }
            else
            {
                ctorGenerator.Emit(OpCodes.Ldarg_0);
                for (var i = 0; i < parameters.Length; i++)
                {
                    ctorGenerator.Emit(OpCodes.Ldarg_S, i + 1);
                }
                ctorGenerator.Emit(OpCodes.Call, constructor);
                ctorGenerator.Emit(OpCodes.Ret);
            }
        }

        public void GenerateInit(GeneratorTypeContext context)
        {
            var initBuilder = context.TypeBuilder.DefineMethod(
                          "<Proxy>__init",
                          MethodAttributes.Private |
                          MethodAttributes.HideBySig,
                          CallingConventions.HasThis
                      );

            var initGenerator = initBuilder.GetILGenerator();
            var typeLocal = initGenerator.DeclareLocal(typeof(Type));

            initGenerator.Emit(OpCodes.Ldarg_0);
            initGenerator.Emit(OpCodes.Ldtoken, context.ProxyType);
            initGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);
            initGenerator.Emit(OpCodes.Newobj, ReflectionInfoProvider.InterceptorWrapperCollectionByType);
            initGenerator.Emit(OpCodes.Stfld, context.Wrappers);
            initGenerator.Emit(OpCodes.Ret);

            context.InitMethod = initBuilder;
        }
    }
}
