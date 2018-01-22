using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using KiraNet.AspectFlare.DynamicProxy.Exensions;
using KiraNet.AspectFlare.Utilities;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class ImplementConstructorsOperator : GenerateMethodOperator
    {
        public override void Generate(GeneratorTypeContext context)
        {
            GenerateInit(context);
            var classType = context.ClassType;
            var typeBuilder = context.TypeBuilder;
            bool hasTypeIntercept = classType.HasInterceptAttribute();
            foreach (var ctor in classType.GetConstructors(
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

            if (context.Interface != null)
            {
                ctorGenerator.Emit(OpCodes.Ldarg_0);
                ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.ObjectConstructor);

                ctorGenerator.Emit(OpCodes.Ldarg_0);
                for (var i = 1; i <= parameters.Length; i++)
                {
                    ctorGenerator.Emit(OpCodes.Ldarg_S, i);
                }

                ctorGenerator.Emit(OpCodes.Newobj, constructor);
                ctorGenerator.Emit(OpCodes.Stfld, context.Interface);
            }
            else
            {
                if (isIntercept)
                {
                    context.MethodHandles.Add(constructor.MethodHandle);

                    // 调用基类构造函数 base(x)
                    // 注：这里的调用位置在C#代码中无法表示，因为在C#中调用基类构造函数必须在方法参数列表之后
                    GenerateMethod(methodBuilder, constructor, null, ctorGenerator, context, parameters);
                }
                else
                {
                    ctorGenerator.Emit(OpCodes.Ldarg_0);
                    for (var i = 1; i <= parameters.Length; i++)
                    {
                        ctorGenerator.Emit(OpCodes.Ldarg_S, i);
                    }

                    ctorGenerator.Emit(OpCodes.Call, constructor);
                }
            }

            ctorGenerator.Emit(OpCodes.Ret);
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
            var hasInterface = context.InterfaceType != null;
            initGenerator.Emit(OpCodes.Ldarg_0);
            if (hasInterface)
            {
                initGenerator.Emit(OpCodes.Ldtoken, context.InterfaceType);
                initGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);
            }
            initGenerator.Emit(OpCodes.Ldtoken, context.ClassType);
            initGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);
            initGenerator.Emit(OpCodes.Ldtoken, context.TypeBuilder);
            initGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);
            initGenerator.Emit(OpCodes.Newobj, hasInterface ?
                ReflectionInfoProvider.InterceptorWrapperCollectionByInterface :
                ReflectionInfoProvider.InterceptorWrapperCollectionByClass
            );
            initGenerator.Emit(OpCodes.Stfld, context.Wrappers);
            initGenerator.Emit(OpCodes.Ret);

            context.InitMethod = initBuilder;
        }
    }
}
