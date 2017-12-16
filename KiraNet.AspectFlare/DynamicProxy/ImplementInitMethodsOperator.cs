using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using KiraNet.AspectFlare.Utilities;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class ImplementInitMethodsOperator : IGenerateOperator
    {
        public void Generate(GenerateTypeContext context)
        {
            var methodBuilder = context.TypeBuilder.DefineMethod(
                          "<Init_Proxy>",
                          MethodAttributes.Private |
                          MethodAttributes.Final,
                          CallingConventions.HasThis
                      );

            var methodGenerator = methodBuilder.GetILGenerator();
            var t_type = methodGenerator.DeclareLocal(typeof(Type));

            methodGenerator.Emit(OpCodes.Ldtoken, context.ProxyType);
            methodGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);
            methodGenerator.Emit(OpCodes.Stloc_0);

            methodGenerator.Emit(OpCodes.Ldarg_0);
            methodGenerator.Emit(OpCodes.Ldloc_0);
            methodGenerator.Emit(OpCodes.Newobj, ReflectionInfoProvider.InterceptorWrapperCollectionByType);
            methodGenerator.Emit(OpCodes.Stfld, context.WrappersField);

            methodGenerator.Emit(OpCodes.Ret);

            context.InitMethod = methodBuilder;
        }
    }
}
