using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KiraNet.AspectFlare.Utilities;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal abstract class CallMethodOperator : IGenerateOperator
    {
        public abstract void Generate(GenerateTypeContext context);

        private AsyncMethodGenerator _asyncMethodGenerator;

        private AsyncMethodGenerator AsyncMethodGenerator
            => _asyncMethodGenerator = _asyncMethodGenerator ?? new AsyncMethodGenerator();

        protected void CallMethodSync(
            MethodBase proxyMethod,
            ILGenerator generator,
            GenerateTypeContext context,
            ParameterInfo[] parameters)
        {
            GeneratorSyncContext syncContext = new GeneratorSyncContext(context)
            {
                Method = proxyMethod,
                Generator = generator,
                Parameters = proxyMethod.GetParameters()
            };

            if (proxyMethod is MethodInfo meth)
            {
                syncContext.ReturnType = meth.ReturnType;
                if (meth.ReturnType == typeof(void))
                {
                    CallMethodByNonReturn(syncContext);
                }
                else
                {
                    CallMethodByHasReturn(syncContext);
                }
            }
            else if (proxyMethod is ConstructorInfo)
            {
                CallMethodByNonReturn(syncContext);
            }
        }

        protected void CallMethodAsync(
            MethodInfo proxyMethod,
            ILGenerator generator,
            GenerateTypeContext context,
            ParameterInfo[] parameters)
        {
            AsyncMethodGenerator.GeneratorMethod(context, generator, proxyMethod, parameters);
        }

        #region sync method

        private static void CallMethodByNonReturn(GeneratorSyncContext context)
        {
            var generator = context.Generator;
            var method = context.Method;
            var proxyType = context.ProxyType;
            var parameters = context.Parameters;
            var t_wrapper = generator.DeclareLocal(typeof(InterceptorWrapper));
            var t_parameters = generator.DeclareLocal(typeof(object[]));
            var t_ex = generator.DeclareLocal(typeof(Exception));

            // InterceptorWrapper wrapper = this._wrappers.GetWrapper(xxx);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, context.WrappersField);
            generator.Emit(OpCodes.Ldc_I4, method.MetadataToken);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
            generator.Emit(OpCodes.Stloc_0);

            // if (wrapper == null)
            Label label1 = generator.DefineLabel();
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Brtrue_S, label1);
            CallBaseMethod(method, generator, proxyType, parameters);
            generator.Emit(OpCodes.Ret);

            // object[] parameters = new object[]{ x, y };
            generator.MarkLabel(label1);
            AssignmentForParameterArrary(generator, parameters);
            //CallBaseConstructor(constructor, ctorGenerator, proxyType, parameters);

            // try
            Label tryLable = generator.BeginExceptionBlock();
            Label retLable = generator.DefineLabel();

            // wrapper.CallingIntercepts(this, ".ctor", parameters);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldstr, method.ToString());
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CallingIntercepts);


            if (context.Method is MethodInfo meth)
            {
                Label callingLable = generator.DefineLabel();
                generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult); // ?
                generator.Emit(OpCodes.Brfalse_S, callingLable);
                generator.Emit(OpCodes.Leave_S, retLable);
            }
            else
            {
                generator.Emit(OpCodes.Pop);
            }

            CallBaseMethod(method, generator, proxyType, parameters);

            // wrapper.CalledIntercepts(this, ".ctor", null);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldstr, method.ToString());
            // generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CalledIntercepts);
            generator.Emit(OpCodes.Pop);

            generator.Emit(OpCodes.Leave_S, retLable);

            // catch(Exception)
            generator.BeginCatchBlock(typeof(Exception));

            generator.Emit(OpCodes.Stloc_2);

            // wrapper.ExceptionIntercept(this, ".ctor", parameters, null, ex);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldstr, method.ToString());
            generator.Emit(OpCodes.Ldloc_1);
            // generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.ExceptionIntercept);

            Label throwLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
            generator.Emit(OpCodes.Brfalse_S, throwLabel);
            generator.Emit(OpCodes.Leave_S, retLable);

            // throw ex
            generator.MarkLabel(throwLabel);
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Throw);

            // end try-catch
            generator.EndExceptionBlock();

            generator.MarkLabel(retLable);
            generator.Emit(OpCodes.Ret);
        }


        private static void CallMethodByHasReturn(GeneratorSyncContext context)
        {
            var generator = context.Generator;
            var returnType = context.ReturnType;
            var method = context.Method;
            var proxyType = context.ProxyType;
            var parameters = context.Parameters;

            var t_wrapper = generator.DeclareLocal(typeof(InterceptorWrapper));
            var t_parameters = generator.DeclareLocal(typeof(object[]));
            var t_result = generator.DeclareLocal(typeof(InterceptResult));
            var t_returnValue1 = generator.DeclareLocal(returnType);
            var t_returnValue2 = generator.DeclareLocal(returnType);
            var t_ex = generator.DeclareLocal(typeof(Exception));

            // InterceptorWrapper wrapper = this._wrappers.GetWrapper(xxx);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, context.WrappersField);
            generator.Emit(OpCodes.Ldc_I4, method.MetadataToken);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
            generator.Emit(OpCodes.Stloc_0);

            // if (wrapper == null)
            Label label1 = generator.DefineLabel();
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Brtrue_S, label1);
            CallBaseMethod(method, generator, proxyType, parameters);
            generator.Emit(OpCodes.Ret);

            // object[] parameters = new object[]{ x, y };
            generator.MarkLabel(label1);
            AssignmentForParameterArrary(generator, parameters);

            // ReturnType returnValue= default(ReturnType);
            if (returnType.IsClass)
            {
                generator.Emit(OpCodes.Ldnull);
            }
            else
            {
                generator.Emit(OpCodes.Initobj, returnType);
            }

            generator.Emit(OpCodes.Stloc_3);

            // try
            Label tryLable = generator.BeginExceptionBlock();
            Label retLable = generator.DefineLabel();

            // var result = wrapper.CallingIntercepts(this, ".ctor", parameters);
            // if(result.HasResult)
            // {
            //      return (ReturnType)result.Result;
            // }
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldstr, method.ToString());
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CallingIntercepts);
            generator.Emit(OpCodes.Stloc_2);
            generator.Emit(OpCodes.Ldloc_2);
            Label callingLable = generator.DefineLabel();
            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
            generator.Emit(OpCodes.Brfalse_S, callingLable);
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.Result);
            if (returnType.IsClass)
            {
                generator.Emit(OpCodes.Castclass, returnType);
            }
            else
            {
                generator.Emit(OpCodes.Unbox_Any, returnType);
            }
            generator.Emit(OpCodes.Stloc_S, 4);
            generator.Emit(OpCodes.Leave_S, retLable);

            // 调用基类方法
            CallBaseMethod(method, generator, proxyType, parameters);
            generator.Emit(OpCodes.Stloc_3);

            // wrapper.CalledIntercepts(this, ".ctor", returnValue);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldstr, method.ToString());
            // generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldloc_3);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CalledIntercepts);
            generator.Emit(OpCodes.Stloc_2);
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
            Label calledRet = generator.DefineLabel();
            generator.Emit(OpCodes.Brfalse_S, calledRet);

            // return (ReturnType)result.Reslut;
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.Result);
            if (returnType.IsClass)
            {
                generator.Emit(OpCodes.Castclass, returnType);
            }
            else
            {
                generator.Emit(OpCodes.Unbox_Any, returnType);
            }

            generator.Emit(OpCodes.Stloc_S, 4);
            generator.Emit(OpCodes.Leave_S, retLable);

            // return returnValue;
            generator.MarkLabel(calledRet);
            generator.Emit(OpCodes.Ldloc_3);
            generator.Emit(OpCodes.Stloc_S, 4);
            generator.Emit(OpCodes.Leave_S, retLable);


            // catch(Exception)
            generator.BeginCatchBlock(typeof(Exception));

            generator.Emit(OpCodes.Stloc_S, 5);

            // wrapper.ExceptionIntercept(this, ".ctor", parameters, returnValue, ex);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldstr, method.ToString());
            // generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Ldloc_3);
            generator.Emit(OpCodes.Ldloc_S, 5);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.ExceptionIntercept);
            generator.Emit(OpCodes.Stloc_2);

            // if(result.HasResult)
            // {
            //      return (ReturnType)result.Result;
            // }
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
            Label throwLabel = generator.DefineLabel();

            generator.Emit(OpCodes.Brfalse_S, throwLabel);
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.Result);
            if (returnType.IsClass)
            {
                generator.Emit(OpCodes.Castclass, returnType);
            }
            else
            {
                generator.Emit(OpCodes.Unbox_Any, returnType);
            }
            generator.Emit(OpCodes.Stloc_S, 4);
            generator.Emit(OpCodes.Leave_S, retLable);

            // throw ex
            generator.MarkLabel(throwLabel);
            generator.Emit(OpCodes.Ldloc_S, 5);
            generator.Emit(OpCodes.Throw);

            // end try-catch
            generator.EndExceptionBlock();

            generator.MarkLabel(retLable);
            generator.Emit(OpCodes.Ldloc_S, 4);
            generator.Emit(OpCodes.Ret);
        }


        private static void CallBaseMethod(
          MethodBase method,
          ILGenerator generator,
          Type proxyType,
          ParameterInfo[] parameters)
        {
            generator.Emit(OpCodes.Ldarg_0);
            switch (parameters.Length)
            {
                case 0:
                    break;
                case 1:
                    generator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldarg_2);
                    generator.Emit(OpCodes.Ldarg_3);
                    break;
                case int n when n > 3:
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldarg_2);
                    generator.Emit(OpCodes.Ldarg_3);
                    for (int i = 3; i < parameters.Length; i++)
                    {
                        generator.Emit(OpCodes.Ldarg_S, i + 1);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unable to generate agent service class for {proxyType.FullName} type");
            }

            if (method is ConstructorInfo ctor)
            {
                generator.Emit(OpCodes.Call, ctor);
            }
            else if (method is MethodInfo meth)
            {
                generator.Emit(OpCodes.Call, meth);
            }
            else
            {
                throw new InvalidCastException($"{method.ToString()} unable to cast to MethodInfo or ConstructorInfo");
            }
        }

        private static void AssignmentForParameterArrary(ILGenerator generator, ParameterInfo[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Stloc_1);
                return;
            }

            switch (parameters.Length)
            {
                case 1:
                    generator.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    generator.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    generator.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    generator.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    generator.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    generator.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    generator.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    generator.Emit(OpCodes.Ldc_I4_8);
                    break;
                case int s when s > 8:
                    generator.Emit(OpCodes.Ldc_I4_S, s);
                    break;
                default:
                    throw new IndexOutOfRangeException("Parameter array index cross boundary");
            }

            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Stloc_1);


            for (int i = 0; i < parameters.Length; i++)
            {
                AssignmentForParameter(generator, parameters[i], i);
            }
        }

        private static void AssignmentForParameter(ILGenerator generator, ParameterInfo parameter, int index)
        {
            Type unboxtype = null;
            var parameterType = parameter.ParameterType;
            if (parameterType.IsByRef)
            {
                var unboxtypeName = parameterType.FullName.Substring(0, parameterType.FullName.Length - 1);
                unboxtype = Type.GetType(unboxtypeName);

                if (parameter.IsOut)
                {
                    // 如果是out参数则先对其赋值
                    generator.Emit(OpCodes.Ldarg_S, index + 1);
                    if (unboxtype.IsValueType)
                    {
                        generator.Emit(OpCodes.Initobj, unboxtype);
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldnull);
                        generator.Emit(OpCodes.Stind_Ref);
                    }
                }
            }

            generator.Emit(OpCodes.Ldloc_1);
            switch (index)
            {
                case 0:
                    generator.Emit(OpCodes.Ldc_I4_0);
                    generator.Emit(OpCodes.Ldarg_1);
                    break;
                case 1:
                    generator.Emit(OpCodes.Ldc_I4_1);
                    generator.Emit(OpCodes.Ldarg_2);
                    break;
                case 2:
                    generator.Emit(OpCodes.Ldc_I4_2);
                    generator.Emit(OpCodes.Ldarg_3);
                    break;
                case 3:
                    generator.Emit(OpCodes.Ldc_I4_3);
                    generator.Emit(OpCodes.Ldarg_S, 4);
                    break;
                case 4:
                    generator.Emit(OpCodes.Ldc_I4_4);
                    generator.Emit(OpCodes.Ldarg_S, 5);
                    break;
                case 5:
                    generator.Emit(OpCodes.Ldc_I4_5);
                    generator.Emit(OpCodes.Ldarg_S, 6);
                    break;
                case 6:
                    generator.Emit(OpCodes.Ldc_I4_6);
                    generator.Emit(OpCodes.Ldarg_S, 7);
                    break;
                case 7:
                    generator.Emit(OpCodes.Ldc_I4_7);
                    generator.Emit(OpCodes.Ldarg_S, 8);
                    break;
                case 8:
                    generator.Emit(OpCodes.Ldc_I4_8);
                    generator.Emit(OpCodes.Ldarg_S, 9);
                    break;
                case int s when s > 8:
                    generator.Emit(OpCodes.Ldc_I4_S, s);
                    generator.Emit(OpCodes.Ldarg_S, index + 1);
                    break;
                default:
                    throw new IndexOutOfRangeException("Parameter array index cross boundary");
            }

            if (unboxtype != null)
            {
                if (unboxtype.IsValueType)
                {
                    //generator.Emit(OpCodes.Ldarg_S, index + 1);
                    generator.Emit(OpCodes.Ldobj, unboxtype);
                    generator.Emit(OpCodes.Box, unboxtype);
                }
                else
                {
                    generator.Emit(OpCodes.Ldind_Ref);
                }
            }
            else if (parameterType.IsValueType)
            {
                //generator.Emit(OpCodes.Ldarg_S, index + 1);
                generator.Emit(OpCodes.Box, parameterType);
            }

            generator.Emit(OpCodes.Stelem_Ref);
        }

        #endregion
    }
}
