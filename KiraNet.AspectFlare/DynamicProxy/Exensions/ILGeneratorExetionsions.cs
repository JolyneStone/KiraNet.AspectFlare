using System;
using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal static class ILGeneratorExetionsions
    {
        //internal static void EmitConstructor(this ILGenerator ctorGenerator, ConstructorInfo constructor, Type[] parameters)
        //{
        //    ctorGenerator.Emit(OpCodes.Ldarg_0);

        //    //LocalBuilder methodBase = ctorGenerator.DeclareLocal(typeof(MethodBase));
        //    //ctorGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod(
        //    //    "GetCurrentMethod",
        //    //    BindingFlags.Instance |
        //    //    BindingFlags.Public |
        //    //    BindingFlags.Static));

        //    //ctorGenerator.Emit(OpCodes.Stloc_0);
        //    //// Console.WriteLine(methodBase);
        //    //ctorGenerator.Emit(OpCodes.Ldloc_0);
        //    //ctorGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine",
        //    //    BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static,
        //    //    null,
        //    //    new Type[] { typeof(object) },
        //    //    null
        //    // ));

        //    switch (parameters.Length)
        //    {
        //        case 0:
        //            break;
        //        case 1:
        //            ctorGenerator.Emit(OpCodes.Ldarg_1);
        //            break;
        //        case 2:
        //            ctorGenerator.Emit(OpCodes.Ldarg_1);
        //            ctorGenerator.Emit(OpCodes.Ldarg_2);
        //            break;
        //        case 3:
        //            ctorGenerator.Emit(OpCodes.Ldarg_1);
        //            ctorGenerator.Emit(OpCodes.Ldarg_2);
        //            ctorGenerator.Emit(OpCodes.Ldarg_3);
        //            break;
        //        case int n when n > 3:
        //            ctorGenerator.Emit(OpCodes.Ldarg_1);
        //            ctorGenerator.Emit(OpCodes.Ldarg_2);
        //            ctorGenerator.Emit(OpCodes.Ldarg_3);
        //            for (int i = 0; i < parameters.Length; i++)
        //            {
        //                ctorGenerator.Emit(OpCodes.Ldarg_S, i + 4);
        //            }
        //            break;
        //        default:
        //            throw new InvalidOperationException($"Unable to generate build the {constructor.Name} constructor for agent service class");
        //    }

        //    ctorGenerator.Emit(OpCodes.Call, constructor);
        //    ctorGenerator.Emit(OpCodes.Ret);
        //}
    }
}
