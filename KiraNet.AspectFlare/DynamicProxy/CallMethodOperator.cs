using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KiraNet.AspectFlare.DynamicProxy.Exensions;
using KiraNet.AspectFlare.Utilities;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal abstract class CallMethodOperator : IGenerateOperator
    {
        public abstract void Generate(GeneratorTypeContext context);
        protected void GenerateMethod(
                MethodBase methodBuilder,
                MethodBase proxyMethod,
                ILGenerator generator,
                GeneratorTypeContext typeContext,
                ParameterInfo[] parameters
            )
        {
            var context = GetContext(
                    methodBuilder,
                    proxyMethod,
                    generator,
                    typeContext,
                    parameters
                );

            GenerateCallerMethod(context);
            GenerateDisplay(context);
            GenerateMethodBody(context);
        }

        private static GeneratorContext GetContext(
                MethodBase builder,
                MethodBase proxyMethod,
                ILGenerator generator,
                GeneratorTypeContext typeContext,
                ParameterInfo[] parameters
            )
        {
            var context = new GeneratorContext(typeContext)
            {
                Generator = generator,
                Parameters = proxyMethod.GetParameters()
            };

            Type fieldType;
            if (proxyMethod is MethodInfo meth)
            {
                context.Method = meth;
                var returnType = meth.ReturnType;
                if (returnType == typeof(void))
                {
                    context.ReturnType = returnType;
                    context.CallerType = CallerType.Void;
                    fieldType = typeof(VoidCaller);
                }
                else if (!meth.IsDefined(typeof(StateMachineAttribute)))
                {
                    context.ReturnType = returnType;
                    context.CallerType = CallerType.Return;
                    fieldType = typeof(ReturnCaller<>).MakeGenericType(returnType);
                }
                else if (returnType == typeof(Task))
                {
                    context.ReturnType = null;
                    context.CallerType = CallerType.Task;
                    fieldType = typeof(TaskCaller);
                }
                else if (returnType.IsGenericType)
                {
                    var type = returnType.GetGenericTypeDefinition();
                    context.ReturnType = returnType.GetGenericArguments()[0];
                    if (type == typeof(Task<>))
                    {
                        context.CallerType = CallerType.TaskOfT;
                        fieldType = typeof(TaskCaller<>).MakeGenericType(context.ReturnType);
                    }
                    else if (type == typeof(ValueTask<>))
                    {
                        context.CallerType = CallerType.ValueTaskOfT;
                        fieldType = typeof(ValueTaskCaller<>).MakeGenericType(context.ReturnType);
                    }
                    else
                    {
                        throw new InvalidOperationException("function return value error!");
                    }
                }
                else
                {
                    throw new InvalidOperationException("function return value error!");
                }

                context.Caller = context.TypeBuilder.DefineField($"<>_caller{context.Token}", fieldType, FieldAttributes.Private);
                context.MethodBuilder = (MethodBuilder)builder;
            }
            else if (proxyMethod is ConstructorInfo ctor)
            {
                context.Constructor = ctor;
                context.ReturnType = null;
                context.CallerType = CallerType.Ctor;
                context.Caller = context.TypeBuilder.DefineField($"<>_caller{context.Token}", typeof(VoidCaller), FieldAttributes.Private);
                context.ConstructorBuilder = (ConstructorBuilder)builder;
            }

            return context;
        }

        private static void GenerateMethodBody(GeneratorContext context)
        {
            var generator = context.MethodBuilder?.GetILGenerator() ??
                                context.ConstructorBuilder?.GetILGenerator();

            DefineLocals(context);
            NewDisplay(context, generator);
            NewCaller(context, generator);
            NewObjects(context, generator);
            CallMethod(context, generator);
        }

        private static void DefineLocals(GeneratorContext context)
        {
            var generator = context.Generator;
            var localCase = context.Parameters.Length > 0;
            var locals = new LocalBuilder[localCase ? 4 : 2];
            context.CallType = context.CallerType == CallerType.Ctor || context.CallerType == CallerType.Void ?
                    typeof(Action) :
                    typeof(Func<>).MakeGenericType(context.Method.ReturnType
                );
            if (localCase)
            {
                // 4
                locals[0] = generator.DeclareLocal(context.DisplayTypeBuilder);
                locals[1] = generator.DeclareLocal(context.CallType);
                locals[2] = generator.DeclareLocal(typeof(object[]));
            }
            else
            {
                // 2                
                locals[0] = generator.DeclareLocal(context.CallType);
            }

            locals[locals.Length - 1] = generator.DeclareLocal(typeof(InterceptorWrapper));
            context.Locals = locals;
        }

        private static void NewDisplay(GeneratorContext context, ILGenerator generator)
        {
            if (context.Parameters.Length == 0)
                return;

            var parameters = context.Parameters;
            var fields = context.DisplayFields;

            // var display = new DisplayClass(){ <>__this = this };
            generator.Emit(OpCodes.Newobj, context.DisplayConstructor);
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Stfld, context.DisplayFields[0]);

            //for (var i = 0; i < parameters.Length; i++)
            //{
            //    if (!parameters[i].IsOut && !parameters[i].ParameterType.IsByRef)
            //    {
            //        generator.Emit(OpCodes.Ldloc_0);
            //        generator.Emit(OpCodes.Ldarg_S, i + 1);
            //        generator.Emit(OpCodes.Stfld, fields[i + 1]);
            //    }
            //}
        }

        private static void NewCaller(GeneratorContext context, ILGenerator generator)
        {
            var callerType = context.CallerType;
            var label = generator.DefineLabel();

            // if (this._caller == null)
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, context.Caller);
            generator.Emit(OpCodes.Brtrue_S, label);

            // InterceptorWrapper wrapper = this._wrappers.GetWrapper(int);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, context.Wrappers);
            generator.Emit(OpCodes.Ldc_I4, callerType == CallerType.Ctor ?
                context.Constructor.MetadataToken :
                context.Method.MetadataToken
            );

            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
            if (context.Parameters.Length == 0)
            {
                generator.Emit(OpCodes.Stloc_1);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldloc_1);
            }
            else
            {
                generator.Emit(OpCodes.Stloc_3);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldloc_3);
            }

            // this._caller = new Caller(wrapper);
            Type type;
            switch (callerType)
            {
                case CallerType.Ctor:
                case CallerType.Void:
                    type = typeof(VoidCaller);
                    break;
                case CallerType.Return:
                    type = typeof(ReturnCaller<>)
                        .MakeGenericType(context.ReturnType);
                    break;
                case CallerType.Task:
                    type = typeof(TaskCaller);
                    break;
                case CallerType.TaskOfT:
                    type = typeof(TaskCaller<>)
                        .MakeGenericType(context.ReturnType);
                    break;
                case CallerType.ValueTaskOfT:
                    type = typeof(ValueTaskCaller<>)
                        .MakeGenericType(context.ReturnType);
                    break;
                default:
                    throw new InvalidOperationException("function return value error!");
            }

            generator.Emit(OpCodes.Newobj, type.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(InterceptorWrapper) },
                    null
                ));

            generator.Emit(OpCodes.Stfld, context.Caller);
            generator.MarkLabel(label);
        }

        private static void NewObjects(GeneratorContext context, ILGenerator generator)
        {
            if (context.Parameters.Length == 0)
            {
                return;
            }

            var parameters = context.Parameters;
            var displayFields = context.DisplayFields;

            //for(var i = 0; i < parameters.Length; i++)
            //{
            //    var displayField = displayFields[i + 1];
            //    generator.Emit(OpCodes.Ldloc_0);
            //    if (parameters[i].IsOut)
            //    {
            //        if (displayFields[i].FieldType.IsValueType)
            //        {
            //            generator.Emit(OpCodes.Ldflda, displayField);
            //            generator.Emit(OpCodes.Initobj, displayField.FieldType);
            //        }
            //        else
            //        {
            //            generator.Emit(OpCodes.Ldnull);
            //            generator.Emit(OpCodes.Stfld, displayField);
            //        }
            //    }
            //    else if (parameters[i].ParameterType.IsByRef)
            //    {
            //        generator.Emit(OpCodes.Ldarg, i + 1);
            //        if (displayFields[i].FieldType.IsValueType)
            //        {
            //            generator.Emit(OpCodes.Ldobj, displayField.FieldType);
            //        }
            //        else
            //        {
            //            generator.Emit(OpCodes.Ldind_Ref);
            //        }

            //        generator.Emit(OpCodes.Stfld, displayField);
            //    }
            //}

            generator.Emit(OpCodes.Ldc_I4, parameters.Length);
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Stloc_1);

            for (var i = 1; i <= parameters.Length; i++)
            {
                var parameter = parameters[i - 1];
                var displayField = displayFields[i];
                generator.Emit(OpCodes.Ldloc_0);
                if (parameter.IsOut)
                {
                    if (displayField.FieldType.IsValueType)
                    {
                        generator.Emit(OpCodes.Ldflda, displayField);
                        generator.Emit(OpCodes.Initobj, displayField.FieldType);
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldnull);
                        generator.Emit(OpCodes.Stfld, displayField);
                    }
                }
                else if (parameter.ParameterType.IsByRef)
                {
                    generator.Emit(OpCodes.Ldarg_S, i);
                    if (displayField.FieldType.IsValueType)
                    {
                        generator.Emit(OpCodes.Ldobj, displayField.FieldType);
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldind_Ref);
                    }

                    generator.Emit(OpCodes.Stfld, displayField);
                }
                else
                {
                    generator.Emit(OpCodes.Ldarg_S, i);
                    generator.Emit(OpCodes.Stfld, displayField);
                }

                generator.Emit(OpCodes.Ldloc_1);
                generator.Emit(OpCodes.Ldc_I4, i - 1);
                if (parameter.IsOut)
                {
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ldfld, displayField);
                    if (displayField.FieldType.IsValueType)
                    {
                        generator.Emit(OpCodes.Box, displayField.FieldType);
                    }
                }
                else if (parameter.ParameterType.IsByRef)
                {
                    generator.Emit(OpCodes.Ldarg_S, i);
                    if (displayField.FieldType.IsValueType)
                    {
                        generator.Emit(OpCodes.Ldobj, displayField.FieldType);
                        generator.Emit(OpCodes.Box, displayField.FieldType);
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldind_Ref);
                    }
                }
                else
                {
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ldfld, displayField);
                    if (displayField.FieldType.IsValueType)
                    {
                        generator.Emit(OpCodes.Box, displayField.FieldType);
                    }
                }

                generator.Emit(OpCodes.Stelem_Ref);
            }
        }

        private static void CallMethod(GeneratorContext context, ILGenerator generator)
        {
            var callerType = context.CallerType;
            var parameters = context.Parameters;
            if (parameters.Length > 0)
            {
                // Action or Func<T> call = () => base.Method(xx);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldftn, context.CallerMethod);
                generator.Emit(OpCodes.Newobj, context.CallType.GetConstructors()[0]);
                generator.Emit(OpCodes.Stloc_2);

                // T result = this._caller.Call(this, call, parameters);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, context.Caller);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldloc_2);
                generator.Emit(OpCodes.Ldloc_1);
                generator.Emit(OpCodes.Callvirt, context.Caller.FieldType.GetMethod(
                    "Call",
                    BindingFlags.Public | BindingFlags.Instance
                ));

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].IsOut)
                    {
                        var field = context.DisplayFields[i + 1];
                        generator.Emit(OpCodes.Ldarg_S, i + 1);
                        generator.Emit(OpCodes.Ldloc_0);
                        generator.Emit(OpCodes.Ldfld, field);
                        if (parameters[i].ParameterType.IsValueType)
                        {
                            generator.Emit(OpCodes.Stobj, field.FieldType);
                        }
                        else
                        {
                            generator.Emit(OpCodes.Stind_Ref);
                        }
                    }
                }
            }
            else
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldftn, context.CallerMethod);
                generator.Emit(OpCodes.Newobj, context.CallType.GetConstructors()[0]);
                generator.Emit(OpCodes.Stloc_0);

                // this._caller.Call(this, call, null);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, context.Caller);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Callvirt, context.Caller.FieldType.GetMethod(
                    "Call",
                    BindingFlags.Public | BindingFlags.Instance
                ));
            }

            generator.Emit(OpCodes.Ret);
        }

        private static void GenerateCallerMethod(GeneratorContext context)
        {
            MethodBase baseMethod;
            Type returnType;
            if (context.CallerType == CallerType.Ctor)
            {
                baseMethod = context.Constructor;
                returnType = null;
            }
            else
            {
                baseMethod = context.Method;
                returnType = context.Method.ReturnType;
            }

            var method = context.TypeBuilder.DefineMethod(
                    $"<{baseMethod.Name}>_{context.Token}",
                    MethodAttributes.Private | MethodAttributes.HideBySig,
                    CallingConventions.HasThis | CallingConventions.Standard,
                    returnType,
                    context.Parameters.Select(x => x.ParameterType).ToArray()
                );

            method.SetMethodParameters(baseMethod, context.Parameters);

            var generator = method.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            if (context.Parameters.Length > 0)
            {
                for (var i = 0; i < context.Parameters.Length; i++)
                {
                    generator.Emit(OpCodes.Ldarg_S, i + 1);
                }
            }

            if (context.CallerType == CallerType.Ctor)
            {
                generator.Emit(OpCodes.Call, context.Constructor);
            }
            else
            {
                generator.Emit(OpCodes.Call, context.Method);
            }

            generator.Emit(OpCodes.Ret);
            context.CallerMethod = method;
        }

        private static void GenerateDisplay(GeneratorContext context)
        {
            if (context.Parameters.Length == 0)
            {
                return;
            }

            context.DisplayTypeBuilder = context.TypeBuilder.DefineNestedType(
                $"<display>_proxy{context.Token}",
                TypeAttributes.NestedPrivate |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.Sealed |
                TypeAttributes.BeforeFieldInit
            );

            DefineDisplayFields(context);
            GenerateDisplayCtor(context);
            GenerateDisplayMethod(context);

            context.DisplayTypeBuilder.CreateTypeInfo();
        }

        private static void DefineDisplayFields(GeneratorContext context)
        {
            var parameters = context.Parameters;
            var fields = new FieldInfo[parameters.Length + 1];
            var displayBuilder = context.DisplayTypeBuilder;

            fields[0] = displayBuilder.DefineField(
                "<>__this",
                context.TypeBuilder,
                FieldAttributes.Public
            );

            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                {
                    var name = parameters[i].ParameterType.FullName;
                    fields[i + 1] = displayBuilder.DefineField(
                        parameters[i].Name,
                        Type.GetType(name.Substring(0, name.Length - 1)),
                        FieldAttributes.Public
                    );
                }
                else
                {
                    fields[i + 1] = displayBuilder.DefineField(
                        parameters[i].Name,
                        parameters[i].ParameterType,
                        FieldAttributes.Public
                    );
                }
            }

            context.DisplayFields = fields;
        }

        private static void GenerateDisplayCtor(GeneratorContext context)
        {
            var ctor = context.DisplayTypeBuilder.DefineConstructor(
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.HasThis | CallingConventions.Standard,
                null
            );

            var generator = ctor.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, ReflectionInfoProvider.ObjectConstructor);
            generator.Emit(OpCodes.Ret);

            context.DisplayConstructor = ctor;
        }

        private static void GenerateDisplayMethod(GeneratorContext context)
        {
            var method = context.DisplayTypeBuilder.DefineMethod(
                $"<{context.TypeBuilder.Name}>__{context.Token}",
                MethodAttributes.Assembly | MethodAttributes.HideBySig,
                CallingConventions.HasThis | CallingConventions.Standard,
                context.Method?.ReturnType,
                null
            );

            var fields = context.DisplayFields;
            var parameters = context.Parameters;
            var generator = method.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, fields[0]);
            for (var i = 0; i < parameters.Length; i++)
            {
                generator.Emit(OpCodes.Ldarg_0);
                if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                {
                    generator.Emit(OpCodes.Ldflda, fields[i + 1]);
                }
                else
                {
                    generator.Emit(OpCodes.Ldfld, fields[i + 1]);
                }
            }

            generator.Emit(OpCodes.Call, context.CallerMethod);
            generator.Emit(OpCodes.Ret);
            context.DisplayMethod = method;
        }

        //protected void CallMethodSync(
        //    MethodBase methodBuilder,
        //    MethodBase proxyMethod,
        //    ILGenerator generator,
        //    GeneratorTypeContext context,
        //    ParameterInfo[] parameters)
        //{
        //    GeneratorContext syncContext = new GeneratorContext(context)
        //    {
        //        Method = proxyMethod,
        //        Generator = generator,
        //        Parameters = proxyMethod.GetParameters()
        //    };

        //    if (methodBuilder is MethodBuilder mb)
        //    {
        //        syncContext.MethodBuilder = mb;
        //    }
        //    else if (methodBuilder is ConstructorBuilder cb)
        //    {
        //        syncContext.ConstructorBuilder = cb;
        //    }

        //    if (proxyMethod is MethodInfo meth)
        //    {
        //        syncContext.ReturnType = meth.ReturnType;
        //        if (meth.ReturnType == typeof(void))
        //        {
        //            CallMethodByNonReturn(syncContext);
        //        }
        //        else
        //        {
        //            CallMethodByHasReturn(syncContext);
        //        }

        //        context.TypeBuilder.DefineMethodOverride(syncContext.MethodBuilder, meth);
        //    }
        //    else if (proxyMethod is ConstructorInfo)
        //    {
        //        CallMethodByNonReturn(syncContext);
        //    }
        //}

        //protected void CallMethodAsync(
        //    MethodBuilder methodBuilder,
        //    MethodInfo proxyMethod,
        //    ILGenerator generator,
        //    GeneratorTypeContext context,
        //    ParameterInfo[] parameters)
        //{
        //    AsyncMethodGenerator.GenerateMethod(context, generator, methodBuilder, proxyMethod, parameters);
        //}

        //#region sync method

        //private static void CallMethodByNonReturn(GeneratorContext context)
        //{
        //    var generator = context.Generator;
        //    var method = context.Method;
        //    var proxyType = context.ProxyType;
        //    var parameters = context.Parameters;
        //    var t_wrapper = generator.DeclareLocal(typeof(InterceptorWrapper));
        //    var t_parameters = generator.DeclareLocal(typeof(object[]));
        //    var t_ex = generator.DeclareLocal(typeof(Exception));

        //    // InterceptorWrapper wrapper = this._wrappers.GetWrapper(xxx);
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldfld, context.WrappersField);
        //    generator.Emit(OpCodes.Ldc_I4, method.MetadataToken);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
        //    generator.Emit(OpCodes.Stloc_0);

        //    // if (wrapper == null)
        //    Label label1 = generator.DefineLabel();
        //    generator.Emit(OpCodes.Ldloc_0);
        //    generator.Emit(OpCodes.Brtrue_S, label1);
        //    CallBaseMethod(method, generator, proxyType, parameters);
        //    generator.Emit(OpCodes.Ret);

        //    // object[] parameters = new object[]{ x, y };
        //    generator.MarkLabel(label1);
        //    AssignmentForParameterArrary(generator, parameters);
        //    //CallBaseConstructor(constructor, ctorGenerator, proxyType, parameters);

        //    // try
        //    Label tryLable = generator.BeginExceptionBlock();
        //    Label retLable = generator.DefineLabel();

        //    // wrapper.CallingIntercepts(this, ".ctor", parameters);
        //    generator.Emit(OpCodes.Ldloc_0);
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldstr, method.ToString());
        //    generator.Emit(OpCodes.Ldloc_1);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CallingIntercepts);


        //    if (context.Method is MethodInfo meth)
        //    {
        //        Label callingLable = generator.DefineLabel();
        //        generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult); // ?
        //        generator.Emit(OpCodes.Brfalse_S, callingLable);
        //        generator.Emit(OpCodes.Leave_S, retLable);
        //    }
        //    else
        //    {
        //        generator.Emit(OpCodes.Pop);
        //    }

        //    CallBaseMethod(method, generator, proxyType, parameters);

        //    // wrapper.CalledIntercepts(this, ".ctor", null);
        //    generator.Emit(OpCodes.Ldloc_0);
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldstr, method.ToString());
        //    // generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldnull);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CalledIntercepts);
        //    generator.Emit(OpCodes.Pop);

        //    generator.Emit(OpCodes.Leave_S, retLable);

        //    // catch(Exception)
        //    generator.BeginCatchBlock(typeof(Exception));

        //    generator.Emit(OpCodes.Stloc_2);

        //    // wrapper.ExceptionIntercept(this, ".ctor", parameters, null, ex);
        //    generator.Emit(OpCodes.Ldloc_0);
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldstr, method.ToString());
        //    generator.Emit(OpCodes.Ldloc_1);
        //    // generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldnull);
        //    generator.Emit(OpCodes.Ldloc_2);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.ExceptionIntercept);

        //    Label throwLabel = generator.DefineLabel();
        //    generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
        //    generator.Emit(OpCodes.Brfalse_S, throwLabel);
        //    generator.Emit(OpCodes.Leave_S, retLable);

        //    // throw ex
        //    generator.MarkLabel(throwLabel);
        //    generator.Emit(OpCodes.Ldloc_2);
        //    generator.Emit(OpCodes.Throw);

        //    // end try-catch
        //    generator.EndExceptionBlock();

        //    generator.MarkLabel(retLable);
        //    generator.Emit(OpCodes.Ret);
        //}


        //private static void CallMethodByHasReturn(GeneratorContext context)
        //{
        //    var generator = context.Generator;
        //    var returnType = context.ReturnType;
        //    var method = context.Method;
        //    var proxyType = context.ProxyType;
        //    var parameters = context.Parameters;

        //    var t_wrapper = generator.DeclareLocal(typeof(InterceptorWrapper));
        //    var t_parameters = generator.DeclareLocal(typeof(object[]));
        //    var t_result = generator.DeclareLocal(typeof(InterceptResult));
        //    var t_returnValue1 = generator.DeclareLocal(returnType);
        //    var t_returnValue2 = generator.DeclareLocal(returnType);
        //    var t_ex = generator.DeclareLocal(typeof(Exception));

        //    // InterceptorWrapper wrapper = this._wrappers.GetWrapper(xxx);
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldfld, context.WrappersField);
        //    generator.Emit(OpCodes.Ldc_I4, method.MetadataToken);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
        //    generator.Emit(OpCodes.Stloc_0);

        //    // if (wrapper == null)
        //    Label label1 = generator.DefineLabel();
        //    generator.Emit(OpCodes.Ldloc_0);
        //    generator.Emit(OpCodes.Brtrue_S, label1);
        //    CallBaseMethod(method, generator, proxyType, parameters);
        //    generator.Emit(OpCodes.Ret);

        //    // object[] parameters = new object[]{ x, y };
        //    generator.MarkLabel(label1);
        //    AssignmentForParameterArrary(generator, parameters);

        //    // ReturnType returnValue= default(ReturnType);
        //    if (returnType.IsClass)
        //    {
        //        generator.Emit(OpCodes.Ldnull);
        //    }
        //    else
        //    {
        //        generator.Emit(OpCodes.Initobj, returnType);
        //    }

        //    generator.Emit(OpCodes.Stloc_3);

        //    // try
        //    Label tryLable = generator.BeginExceptionBlock();
        //    Label retLable = generator.DefineLabel();

        //    // var result = wrapper.CallingIntercepts(this, ".ctor", parameters);
        //    // if(result.HasResult)
        //    // {
        //    //      return (ReturnType)result.Result;
        //    // }
        //    generator.Emit(OpCodes.Ldloc_0);
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldstr, method.ToString());
        //    generator.Emit(OpCodes.Ldloc_1);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CallingIntercepts);
        //    generator.Emit(OpCodes.Stloc_2);
        //    generator.Emit(OpCodes.Ldloc_2);
        //    Label callingLable = generator.DefineLabel();
        //    generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
        //    generator.Emit(OpCodes.Brfalse_S, callingLable);
        //    generator.Emit(OpCodes.Ldloc_2);
        //    generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.Result);
        //    if (returnType.IsClass)
        //    {
        //        generator.Emit(OpCodes.Castclass, returnType);
        //    }
        //    else
        //    {
        //        generator.Emit(OpCodes.Unbox_Any, returnType);
        //    }
        //    generator.Emit(OpCodes.Stloc_S, 4);
        //    generator.Emit(OpCodes.Leave_S, retLable);

        //    // 调用基类方法
        //    CallBaseMethod(method, generator, proxyType, parameters);
        //    generator.Emit(OpCodes.Stloc_3);

        //    // wrapper.CalledIntercepts(this, ".ctor", returnValue);
        //    generator.Emit(OpCodes.Ldloc_0);
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldstr, method.ToString());
        //    // generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldloc_3);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CalledIntercepts);
        //    generator.Emit(OpCodes.Stloc_2);
        //    generator.Emit(OpCodes.Ldloc_2);
        //    generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
        //    Label calledRet = generator.DefineLabel();
        //    generator.Emit(OpCodes.Brfalse_S, calledRet);

        //    // return (ReturnType)result.Reslut;
        //    generator.Emit(OpCodes.Ldloc_2);
        //    generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.Result);
        //    if (returnType.IsClass)
        //    {
        //        generator.Emit(OpCodes.Castclass, returnType);
        //    }
        //    else
        //    {
        //        generator.Emit(OpCodes.Unbox_Any, returnType);
        //    }

        //    generator.Emit(OpCodes.Stloc_S, 4);
        //    generator.Emit(OpCodes.Leave_S, retLable);

        //    // return returnValue;
        //    generator.MarkLabel(calledRet);
        //    generator.Emit(OpCodes.Ldloc_3);
        //    generator.Emit(OpCodes.Stloc_S, 4);
        //    generator.Emit(OpCodes.Leave_S, retLable);


        //    // catch(Exception)
        //    generator.BeginCatchBlock(typeof(Exception));

        //    generator.Emit(OpCodes.Stloc_S, 5);

        //    // wrapper.ExceptionIntercept(this, ".ctor", parameters, returnValue, ex);
        //    generator.Emit(OpCodes.Ldloc_0);
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldstr, method.ToString());
        //    // generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldloc_1);
        //    generator.Emit(OpCodes.Ldloc_3);
        //    generator.Emit(OpCodes.Ldloc_S, 5);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.ExceptionIntercept);
        //    generator.Emit(OpCodes.Stloc_2);

        //    // if(result.HasResult)
        //    // {
        //    //      return (ReturnType)result.Result;
        //    // }
        //    generator.Emit(OpCodes.Ldloc_2);
        //    generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
        //    Label throwLabel = generator.DefineLabel();

        //    generator.Emit(OpCodes.Brfalse_S, throwLabel);
        //    generator.Emit(OpCodes.Ldloc_2);
        //    generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.Result);
        //    if (returnType.IsClass)
        //    {
        //        generator.Emit(OpCodes.Castclass, returnType);
        //    }
        //    else
        //    {
        //        generator.Emit(OpCodes.Unbox_Any, returnType);
        //    }
        //    generator.Emit(OpCodes.Stloc_S, 4);
        //    generator.Emit(OpCodes.Leave_S, retLable);

        //    // throw ex
        //    generator.MarkLabel(throwLabel);
        //    generator.Emit(OpCodes.Ldloc_S, 5);
        //    generator.Emit(OpCodes.Throw);

        //    // end try-catch
        //    generator.EndExceptionBlock();

        //    generator.MarkLabel(retLable);
        //    generator.Emit(OpCodes.Ldloc_S, 4);
        //    generator.Emit(OpCodes.Ret);
        //}


        //private static void CallBaseMethod(
        //  MethodBase method,
        //  ILGenerator generator,
        //  Type proxyType,
        //  ParameterInfo[] parameters)
        //{
        //    generator.Emit(OpCodes.Ldarg_0);
        //    switch (parameters.Length)
        //    {
        //        case 0:
        //            break;
        //        case 1:
        //            generator.Emit(OpCodes.Ldarg_1);
        //            break;
        //        case 2:
        //            generator.Emit(OpCodes.Ldarg_1);
        //            generator.Emit(OpCodes.Ldarg_2);
        //            break;
        //        case 3:
        //            generator.Emit(OpCodes.Ldarg_1);
        //            generator.Emit(OpCodes.Ldarg_2);
        //            generator.Emit(OpCodes.Ldarg_3);
        //            break;
        //        case int n when n > 3:
        //            generator.Emit(OpCodes.Ldarg_1);
        //            generator.Emit(OpCodes.Ldarg_2);
        //            generator.Emit(OpCodes.Ldarg_3);
        //            for (int i = 3; i < parameters.Length; i++)
        //            {
        //                generator.Emit(OpCodes.Ldarg_S, i + 1);
        //            }
        //            break;
        //        default:
        //            throw new InvalidOperationException($"Unable to generate agent service class for {proxyType.FullName} type");
        //    }

        //    if (method is ConstructorInfo ctor)
        //    {
        //        generator.Emit(OpCodes.Call, ctor);
        //    }
        //    else if (method is MethodInfo meth)
        //    {
        //        generator.Emit(OpCodes.Call, meth);
        //    }
        //    else
        //    {
        //        throw new InvalidCastException($"{method.ToString()} unable to cast to MethodInfo or ConstructorInfo");
        //    }
        //}

        //private static void AssignmentForParameterArrary(ILGenerator generator, ParameterInfo[] parameters)
        //{
        //    if (parameters == null || parameters.Length == 0)
        //    {
        //        generator.Emit(OpCodes.Ldnull);
        //        generator.Emit(OpCodes.Stloc_1);
        //        return;
        //    }

        //    switch (parameters.Length)
        //    {
        //        case 1:
        //            generator.Emit(OpCodes.Ldc_I4_1);
        //            break;
        //        case 2:
        //            generator.Emit(OpCodes.Ldc_I4_2);
        //            break;
        //        case 3:
        //            generator.Emit(OpCodes.Ldc_I4_3);
        //            break;
        //        case 4:
        //            generator.Emit(OpCodes.Ldc_I4_4);
        //            break;
        //        case 5:
        //            generator.Emit(OpCodes.Ldc_I4_5);
        //            break;
        //        case 6:
        //            generator.Emit(OpCodes.Ldc_I4_6);
        //            break;
        //        case 7:
        //            generator.Emit(OpCodes.Ldc_I4_7);
        //            break;
        //        case 8:
        //            generator.Emit(OpCodes.Ldc_I4_8);
        //            break;
        //        case int s when s > 8:
        //            generator.Emit(OpCodes.Ldc_I4_S, s);
        //            break;
        //        default:
        //            throw new IndexOutOfRangeException("Parameter array index cross boundary");
        //    }

        //    generator.Emit(OpCodes.Newarr, typeof(object));
        //    generator.Emit(OpCodes.Stloc_1);


        //    for (int i = 0; i < parameters.Length; i++)
        //    {
        //        AssignmentForParameter(generator, parameters[i], i);
        //    }
        //}

        //private static void AssignmentForParameter(ILGenerator generator, ParameterInfo parameter, int index)
        //{
        //    Type unboxtype = null;
        //    var parameterType = parameter.ParameterType;
        //    if (parameterType.IsByRef)
        //    {
        //        var unboxtypeName = parameterType.FullName.Substring(0, parameterType.FullName.Length - 1);
        //        unboxtype = Type.GetType(unboxtypeName);

        //        if (parameter.IsOut)
        //        {
        //            // 如果是out参数则先对其赋值
        //            generator.Emit(OpCodes.Ldarg_S, index + 1);
        //            if (unboxtype.IsValueType)
        //            {
        //                generator.Emit(OpCodes.Initobj, unboxtype);
        //            }
        //            else
        //            {
        //                generator.Emit(OpCodes.Ldnull);
        //                generator.Emit(OpCodes.Stind_Ref);
        //            }
        //        }
        //    }

        //    generator.Emit(OpCodes.Ldloc_1);
        //    switch (index)
        //    {
        //        case 0:
        //            generator.Emit(OpCodes.Ldc_I4_0);
        //            generator.Emit(OpCodes.Ldarg_1);
        //            break;
        //        case 1:
        //            generator.Emit(OpCodes.Ldc_I4_1);
        //            generator.Emit(OpCodes.Ldarg_2);
        //            break;
        //        case 2:
        //            generator.Emit(OpCodes.Ldc_I4_2);
        //            generator.Emit(OpCodes.Ldarg_3);
        //            break;
        //        case 3:
        //            generator.Emit(OpCodes.Ldc_I4_3);
        //            generator.Emit(OpCodes.Ldarg_S, 4);
        //            break;
        //        case 4:
        //            generator.Emit(OpCodes.Ldc_I4_4);
        //            generator.Emit(OpCodes.Ldarg_S, 5);
        //            break;
        //        case 5:
        //            generator.Emit(OpCodes.Ldc_I4_5);
        //            generator.Emit(OpCodes.Ldarg_S, 6);
        //            break;
        //        case 6:
        //            generator.Emit(OpCodes.Ldc_I4_6);
        //            generator.Emit(OpCodes.Ldarg_S, 7);
        //            break;
        //        case 7:
        //            generator.Emit(OpCodes.Ldc_I4_7);
        //            generator.Emit(OpCodes.Ldarg_S, 8);
        //            break;
        //        case 8:
        //            generator.Emit(OpCodes.Ldc_I4_8);
        //            generator.Emit(OpCodes.Ldarg_S, 9);
        //            break;
        //        case int s when s > 8:
        //            generator.Emit(OpCodes.Ldc_I4_S, s);
        //            generator.Emit(OpCodes.Ldarg_S, index + 1);
        //            break;
        //        default:
        //            throw new IndexOutOfRangeException("Parameter array index cross boundary");
        //    }

        //    if (unboxtype != null)
        //    {
        //        if (unboxtype.IsValueType)
        //        {
        //            //generator.Emit(OpCodes.Ldarg_S, index + 1);
        //            generator.Emit(OpCodes.Ldobj, unboxtype);
        //            generator.Emit(OpCodes.Box, unboxtype);
        //        }
        //        else
        //        {
        //            generator.Emit(OpCodes.Ldind_Ref);
        //        }
        //    }
        //    else if (parameterType.IsValueType)
        //    {
        //        //generator.Emit(OpCodes.Ldarg_S, index + 1);
        //        generator.Emit(OpCodes.Box, parameterType);
        //    }

        //    generator.Emit(OpCodes.Stelem_Ref);
        //}

        //#endregion
    }
}
