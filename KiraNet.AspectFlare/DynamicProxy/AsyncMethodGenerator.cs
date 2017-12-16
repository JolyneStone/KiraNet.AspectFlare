using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KiraNet.AspectFlare.Utilities;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal sealed class AsyncMethodGenerator : IMethodGenerator
    {
        private int _token = 0;

        public void GeneratorMethod(
            GenerateTypeContext context,
            ILGenerator methodGenerator,
            MethodBase method,
            ParameterInfo[] parameters)
        {
            //GeneratorStateMachine(typeBuilder, method, context, ++_token);

            var meth = method as MethodInfo;
            if (meth == null)
            {
                throw new InvalidCastException($"the {method} is not a legal type");
            }

            Type returnGenericType = meth.ReturnType;
            GeneratorAsyncContext asyncContext = new GeneratorAsyncContext(context)
            {
                Method = method,
                Generator = methodGenerator,
                ReturnType = returnGenericType,
                Parameters = method.GetParameters()
            };


            Type returnGenericArgumentType;
            if (returnGenericType == typeof(Task))
            {
                returnGenericArgumentType = null;
                asyncContext.AsyncType = AsyncType.Task;
            }
            else if (returnGenericType.IsGenericType)
            {
                returnGenericArgumentType = returnGenericType.GetGenericArguments()[0];
                asyncContext.AsyncType = returnGenericType.GetGenericTypeDefinition() == typeof(Task<>) ?
                            AsyncType.TaskOfT :
                            AsyncType.ValueTaskOfT;
            }
            else
            {
                asyncContext.AsyncType = AsyncType.None;
            }

            _token++;
            GeneratorFields(asyncContext);
            GeneratorFuncMethod(asyncContext);
            GeneratorDisplayClassMethods(asyncContext);
        }

        private void GeneratorFields(GeneratorAsyncContext context)
        {
            var typeBuilder = context.TypeBuilder;
            context.CallerField = typeBuilder.DefineField(
                    "<>_caller_" + _token,
                    typeof(Caller),
                    FieldAttributes.Private
                );

            context.FuncField = typeBuilder.DefineField(
                    "<>_func_" + _token,
                    typeof(Func<>).MakeGenericType(context.ReturnType),
                    FieldAttributes.Private
                );
        }

        private void GeneratorFuncMethod(GeneratorAsyncContext context)
        {
            var funcMethod = context.TypeBuilder.DefineMethod(
                    "<>n__" + _token,
                    MethodAttributes.Private | MethodAttributes.HideBySig,
                    CallingConventions.HasThis
                );

            funcMethod.SetCustomAttribute(new CustomAttributeBuilder(ReflectionInfoProvider.CompilerGeneratedAttributeConstructor, null));
            funcMethod.SetCustomAttribute(new CustomAttributeBuilder(ReflectionInfoProvider.DebuggerHiddenAttributeConstructor, null));

            var parameters = context.Parameters;
            var generator = funcMethod.GetILGenerator();

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
                    throw new InvalidOperationException($"Unable to generate agent service class for {context.ProxyType.FullName} type");
            }

            if (context.Method is MethodInfo meth)
            {
                generator.Emit(OpCodes.Call, meth);
            }
            else
            {
                throw new InvalidCastException($"{context.Method.ToString()} unable to cast to MethodInfo or ConstructorInfo");
            }

            generator.Emit(OpCodes.Ret);

            context.FuncMethod = funcMethod;
        }

        private void DefineDisplayClass(GeneratorAsyncContext context)
        {
            var displayClass = context.TypeBuilder.DefineNestedType(
                    "<>c__DisplayClass_" + _token,
                    TypeAttributes.NestedPrivate |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.Sealed |
                    TypeAttributes.BeforeFieldInit,
                    typeof(object)
                );

            displayClass.SetCustomAttribute(new CustomAttributeBuilder(ReflectionInfoProvider.CompilerGeneratedAttributeConstructor, null));

            var parameters = context.Parameters;
            var fields = new FieldInfo[parameters.Length + 1];

            fields[0] = displayClass.DefineField(
                    "<>__this",
                    context.ProxyType,
                    FieldAttributes.Public
                );

            for (int i = 1; i < fields.Length; i++)
            {
                fields[i] = displayClass.DefineField(
                    parameters[i - 1].Name,
                    parameters[i - 1].ParameterType,
                    FieldAttributes.Public
                );
            }

            context.DisplayClassFields = fields;
        }

        private void GeneratorDisplayClassMethods(GeneratorAsyncContext context)
        {
            var ctor = context.DisplayClass.DefineDefaultConstructor(
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName
            );

            var ctorGenerator = ctor.GetILGenerator();
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(BindingFlags.Public, null, null, null));
            ctorGenerator.Emit(OpCodes.Ret);

            context.DisplayClassCtor = ctor;

            var method = context.DisplayClass.DefineMethod(
                    $"<{context.Method.Name}>b__0",
                    MethodAttributes.Assembly | MethodAttributes.HideBySig,
                    CallingConventions.HasThis,
                    context.ReturnType,
                    context.Parameters.Select(x => x.ParameterType).ToArray()
                );

            var methGenerator = method.GetILGenerator();
            for (var i = 0; i < context.DisplayClassFields.Length; i++)
            {
                methGenerator.Emit(OpCodes.Ldarg_0);
                methGenerator.Emit(OpCodes.Ldfld, context.DisplayClassFields[i]);
            }

            methGenerator.Emit(OpCodes.Call, context.FuncMethod);
            methGenerator.Emit(OpCodes.Ret);

            context.DisplayClassMethod = method;
        }

        private void GeneratorMethod(GeneratorAsyncContext context)
        {
            var displayFields = context.DisplayClassFields;
            var generator = context.Generator;
            generator.DeclareLocal(context.DisplayClass);
            Label[] labels = new Label[]
            {
                generator.DefineLabel(),
                generator.DefineLabel()
            };

            generator.Emit(OpCodes.Newobj, context.DisplayClassCtor);
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Stfld, displayFields[0]);

            // return this.<>_caller.Call(this, "", this.<>_async, new object[]{ xx, yy })
            for (var i = 1; i < displayFields.Length; i++)
            {
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldarg_S, i);
                generator.Emit(OpCodes.Stfld, displayFields[i]);
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, context.CallerField);
            generator.Emit(OpCodes.Brtrue_S, labels[0]);

            // this.<>_caller = new AsyncCaller(this._wrappers.GetWrapper(xxx));
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, context.WrappersField);
            generator.Emit(OpCodes.Ldc_I4, context.Method.MetadataToken);
            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
            generator.Emit(OpCodes.Newobj, ReflectionInfoProvider.AsyncCallerConstructor);
            generator.Emit(OpCodes.Stfld, context.CallerField);

            // if(this.<>_async == null)
            generator.MarkLabel(labels[0]);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, context.FuncField);
            generator.Emit(OpCodes.Brtrue_S, labels[1]);

            // this.<>async = () => base.Method(xx, yy)
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldftn, context.DisplayClassMethod);

            Type funcType;
            switch(context.AsyncType)
            {
                case AsyncType.TaskOfT:
                    funcType = typeof(Func<>).MakeGenericType(context.ReturnType);

                    generator.Emit(OpCodes.Newobj, funcType);
                    generator.Emit(OpCodes.Stfld, funcType.GetConstructor(
                            BindingFlags.Public | BindingFlags.Instance,
                            null,
                            null,
                            null
                        ));

                    break;
            }

            generator.MarkLabel(labels[1]);
            generator.Emit(OpCodes.Ldarg_0);



        }

        #region 废弃

        //private static void GeneratorStateMachine(TypeBuilder typeBuilder, MethodInfo method, GenerateTypeContext context, int token)
        //{
        //    ParameterInfo[] parameters = method.GetParameters();
        //    Type returnGenericType = method.ReturnType;
        //    Type returnGenericArgumentType;
        //    AsyncType asyncType;
        //    if (returnGenericType == typeof(Task))
        //    {
        //        returnGenericArgumentType = null;
        //        asyncType = AsyncType.Task;
        //    }
        //    else if (returnGenericType.IsGenericType)
        //    {
        //        returnGenericArgumentType = returnGenericType.GetGenericArguments()[0];
        //        asyncType = returnGenericType.GetGenericTypeDefinition() == typeof(Task<>) ?
        //                    AsyncType.TaskOfT :
        //                    AsyncType.ValueTaskOfT;
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("async method return type is no valid");
        //    }

        //    var asyncHelperMethodBuilder = GeneratorAsyncHelperMethod(typeBuilder, method, parameters, asyncType, token);


        //    var stateMachineName = $"<{method.Name}>d_{token}";
        //    var stateMachineTypeBuilder = typeBuilder.DefineNestedType(
        //                                    stateMachineName,
        //                                    TypeAttributes.NestedPrivate | TypeAttributes.Class | TypeAttributes.Sealed,
        //                                    typeof(ValueType),
        //                                    new Type[] { typeof(IAsyncStateMachine) }
        //                                );


        //    stateMachineTypeBuilder.SetCustomAttribute(
        //            new CustomAttributeBuilder(ReflectionInfoProvider.CompilerGeneratedAttributeConstructor, null)
        //        );

        //    var fields = DefineFields(stateMachineTypeBuilder, parameters, asyncType, returnGenericArgumentType);

        //}


        //private static FieldInfo[] DefineFields(
        //    TypeBuilder typeBuilder,
        //    ParameterInfo[] parameters,
        //    AsyncType asyncType,
        //    Type returnGenericArgumentType)
        //{
        //    var fields = new List<FieldBuilder>(15)
        //    {
        //        typeBuilder.DefineField(
        //            "<>__state",
        //            typeof(int),
        //            FieldAttributes.Public)
        //    };

        //    switch (asyncType)
        //    {
        //        case AsyncType.Task:
        //            fields.Add(typeBuilder.DefineField(
        //                     "<>t__builder",
        //                     typeof(AsyncTaskMethodBuilder),
        //                     FieldAttributes.Public
        //                ));

        //            break;

        //        case AsyncType.TaskOfT:
        //            fields.Add(typeBuilder.DefineField(
        //                   "<>t__builder",
        //                   typeof(AsyncTaskMethodBuilder<>).MakeGenericType(returnGenericArgumentType),
        //                   FieldAttributes.Public
        //               ));

        //            break;

        //        case AsyncType.ValueTaskOfT:
        //            fields.Add(typeBuilder.DefineField(
        //                "<>t__builder",
        //                typeof(AsyncValueTaskMethodBuilder<>).MakeGenericType(returnGenericArgumentType),
        //                FieldAttributes.Public
        //            ));

        //            break;

        //        default:
        //            throw new InvalidOperationException("async method return type is no valid");
        //    }


        //    fields.Add(typeBuilder.DefineField(
        //            "<>__this",
        //            typeBuilder,
        //            FieldAttributes.Public
        //        ));

        //    if (parameters != null && parameters.Length != 0)
        //    {
        //        foreach (var parameter in parameters)
        //        {
        //            fields.Add(typeBuilder.DefineField(
        //                    parameter.Name,
        //                    parameter.ParameterType,
        //                    FieldAttributes.Public
        //                ));
        //        }
        //    }


        //    fields.Add(typeBuilder.DefineField(
        //          "<wrapper>__1",
        //          typeof(InterceptorWrapper),
        //          FieldAttributes.Private
        //      ));

        //    fields.Add(typeBuilder.DefineField(
        //            "<returnValue>__2",
        //            returnGenericArgumentType,
        //            FieldAttributes.Private
        //        ));

        //    fields.Add(typeBuilder.DefineField(
        //          "<parameters>__3",
        //          typeof(object[]),
        //          FieldAttributes.Private
        //      ));

        //    fields.Add(typeBuilder.DefineField(
        //          "<ex>__4",
        //          typeof(InterceptorWrapper),
        //          FieldAttributes.Private
        //      ));


        //    switch (asyncType)
        //    {
        //        case AsyncType.Task:
        //            fields.Add(typeBuilder.DefineField(
        //                    "<>u__1",
        //                    typeof(TaskAwaiter),
        //                    FieldAttributes.Private
        //                ));

        //            break;

        //        case AsyncType.TaskOfT:
        //            fields.Add(typeBuilder.DefineField(
        //                      "<>u__1",
        //                      typeof(TaskAwaiter<>).MakeGenericType(returnGenericArgumentType),
        //                      FieldAttributes.Private
        //                  ));

        //            break;

        //        case AsyncType.ValueTaskOfT:
        //            fields.Add(typeBuilder.DefineField(
        //                     "<>u__1",
        //                     typeof(ValueTaskAwaiter<>).MakeGenericType(returnGenericArgumentType),
        //                     FieldAttributes.Private
        //                 ));

        //            break;

        //        default:
        //            throw new InvalidOperationException("async method return type is no valid");

        //    }


        //    fields.Add(typeBuilder.DefineField(
        //            "<>__wrap1",
        //            typeof(object),
        //            FieldAttributes.Private
        //        ));

        //    fields.Add(typeBuilder.DefineField(
        //            "<>__wrap2",
        //            typeof(int),
        //            FieldAttributes.Private
        //        ));

        //    return fields.ToArray();
        //}

        //private static MethodInfo GeneratorAsyncHelperMethod(
        //    TypeBuilder typeBuilder,
        //    MethodInfo method,
        //    ParameterInfo[] parameters,
        //    AsyncType asyncType,
        //    int token)
        //{
        //    var helperMethodBuilder = typeBuilder.DefineMethod(
        //            "<>n__" + token,
        //            MethodAttributes.Private | MethodAttributes.HideBySig,
        //            CallingConventions.HasThis,
        //            method.ReturnType,
        //            Type.EmptyTypes
        //        );

        //    helperMethodBuilder.SetCustomAttribute(new CustomAttributeBuilder(ReflectionInfoProvider.CompilerGeneratedAttributeConstructor, null));
        //    helperMethodBuilder.SetCustomAttribute(new CustomAttributeBuilder(ReflectionInfoProvider.DebuggerHiddenAttributeConstructor, null));

        //    helperMethodBuilder.SetParameters(
        //            parameters.Select(p => p.ParameterType).ToArray()
        //        );

        //    var generator = helperMethodBuilder.GetILGenerator();

        //    generator.Emit(OpCodes.Ldarg_0);
        //    for (int i = 0; i < parameters.Length;)
        //    {
        //        generator.Emit(OpCodes.Ldarga_S, ++i);
        //    }

        //    generator.Emit(OpCodes.Call, method);
        //    generator.Emit(OpCodes.Ret);

        //    return helperMethodBuilder;
        //}

        //private static MethodInfo MoveNext(
        //        TypeBuilder typeBuilder,
        //        MethodInfo method,
        //        MethodInfo helperMethod,
        //        GenerateTypeContext context,
        //        FieldInfo[] fields,
        //        AsyncType asyncType,
        //        Type returnGenericArgumentType
        //    )
        //{
        //    var fieldsLength = fields.Length;
        //    var moveNext = typeBuilder.DefineMethod(
        //               "IAsyncStateMachine.MoveNext",
        //                MethodAttributes.Final |
        //                MethodAttributes.Private |
        //                MethodAttributes.HideBySig |
        //                MethodAttributes.NewSlot |
        //                MethodAttributes.Virtual,
        //                CallingConventions.HasThis,
        //                null,
        //                Type.EmptyTypes
        //            );

        //    var parameters = method.GetParameters();
        //    var generator = moveNext.GetILGenerator();

        //    DefineLocalVariables(generator, typeBuilder, method, asyncType, returnGenericArgumentType);

        //    Init(generator, fields[0], fields[1]);

        //    // try
        //    var tryLabel = generator.BeginExceptionBlock();

        //    // switch(num)
        //    var switchLabels = Switch(generator, asyncType);

        //    // getWrappers
        //    Label noWrappersRetLabel = generator.DefineLabel(); // 00ba object[] parameters = new object[];
        //    GetWrapper(generator, context, method, helperMethod, parameters, fields, asyncType, noWrappersRetLabel);




        //    throw new NotImplementedException();
        //}

        //private static void GetWrapper(
        //    ILGenerator generator,
        //    GenerateTypeContext context,
        //    MethodInfo baseMethod,
        //    MethodInfo helperMethod,
        //    ParameterInfo[] parameters,
        //    FieldInfo[] fields,
        //    AsyncType asyncType,
        //    Label label)
        //{
        //    var index = fields.Length - 8;
        //    // this.<wrapper>__1 = @class._wrappers.GetWrapper(int)
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldloc_1);
        //    generator.Emit(OpCodes.Ldfld, context.WrappersField);
        //    generator.Emit(OpCodes.Ldc_I4, baseMethod.MetadataToken);
        //    generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
        //    generator.Emit(OpCodes.Stfld, fields[index]);

        //    // if(this.<wrapper__1> == null)
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldfld, fields[index]);
        //    Label noWrappersRetLabel = generator.DefineLabel(); // 00ba object[] parameters = new object[];
        //    generator.Emit(OpCodes.Brtrue_S, noWrappersRetLabel);

        //    generator.Emit(OpCodes.Ldloc_1);
        //    for (var i = 0; i < parameters.Length; i++)
        //    {
        //        generator.Emit(OpCodes.Ldarg_0);
        //        generator.Emit(OpCodes.Ldfld, fields[3 + i]);
        //    }

        //    generator.Emit(OpCodes.Call, helperMethod);

        //    // await = valueTask.GetAwaiter() or task.GetAwaiter();
        //    switch(asyncType)
        //    {
        //        //case AsyncType.ValueTaskOfT:
        //        //    generator.Emit(OpCodes.Stloc_S, 5);
        //        //    generator.Emit(OpCodes.Ldloca_S, 5);
        //        //    generator.Emit(OpCodes.Call, typeof(ValueTask<>).MakeGenericType())
        //    }
        //}

        //private static void Init(ILGenerator generator, FieldInfo _state, FieldInfo _this)
        //{
        //    // int num=this.<>__state
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldfld, _state);
        //    generator.Emit(OpCodes.Stloc_0);

        //    // thisType @class = this.<>__this
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldfld, _this);
        //    generator.Emit(OpCodes.Stloc_1);
        //}

        //private static Label[] Switch(ILGenerator generator, AsyncType asyncType)
        //{
        //    generator.Emit(OpCodes.Ldloc_0);
        //    Label[] switchLabels;
        //    if (asyncType == AsyncType.Task)
        //    {
        //        switchLabels = new Label[6];
        //    }
        //    else
        //    {
        //        switchLabels = new Label[5];
        //    }

        //    generator.Emit(OpCodes.Switch, switchLabels);
        //    return switchLabels;
        //}

        //private static void DefineLocalVariables(
        //    ILGenerator generator,
        //    Type thisType,
        //    MethodInfo method,
        //    AsyncType asyncType,
        //    Type returnGenericArgumentType)
        //{
        //    generator.DeclareLocal(typeof(int));
        //    generator.DeclareLocal(thisType);
        //    if (asyncType != AsyncType.Task)
        //    {
        //        generator.DeclareLocal(returnGenericArgumentType);
        //    }
        //    generator.DeclareLocal(typeof(InterceptResult));

        //    switch (asyncType)
        //    {
        //        case AsyncType.Task:
        //            generator.DeclareLocal(typeof(TaskAwaiter));
        //            break;

        //        case AsyncType.TaskOfT:
        //            generator.DeclareLocal(typeof(TaskAwaiter).MakeGenericType(returnGenericArgumentType));
        //            break;

        //        case AsyncType.ValueTaskOfT:
        //            generator.DeclareLocal(typeof(ValueTaskAwaiter<>).MakeGenericType(returnGenericArgumentType));
        //            generator.DeclareLocal(method.ReturnType);
        //            break;

        //        default:
        //            throw new InvalidOperationException("async method return type is no valid");
        //    }

        //    var exceptionType = typeof(Exception);
        //    if (returnGenericArgumentType != exceptionType)
        //    {
        //        generator.DeclareLocal(returnGenericArgumentType);
        //    }

        //    generator.DeclareLocal(exceptionType);
        //    generator.DeclareLocal(typeof(int));
        //    generator.DeclareLocal(exceptionType);
        //}

        //private static MethodInfo SetStateMachine(
        //    TypeBuilder typeBuilder,
        //    FieldInfo[] fields,
        //    Type returnGerenicType,
        //    Type returnGenericArgumentType)
        //{
        //    var setStateMachineMethod = typeBuilder.DefineMethod(
        //                "IAsyncStateMachine.SetStateMachine",
        //                MethodAttributes.Final |
        //                MethodAttributes.Private |
        //                MethodAttributes.HideBySig |
        //                MethodAttributes.NewSlot |
        //                MethodAttributes.Virtual,
        //                CallingConventions.HasThis,
        //                null,
        //                new Type[] { typeof(IAsyncStateMachine) }
        //            ); ;

        //    var generator = setStateMachineMethod.GetILGenerator();
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldflda, fields[1]);
        //    generator.Emit(OpCodes.Ldarg_1);

        //    if (returnGenericArgumentType == null)
        //    {
        //        generator.Emit(
        //                OpCodes.Call,
        //                ReflectionInfoProvider.SetStateMachineByTaskBuilder
        //            );
        //    }
        //    if (returnGerenicType == typeof(Task<>) && returnGenericArgumentType != null)
        //    {
        //        generator.Emit(
        //                OpCodes.Call,
        //                ReflectionInfoProvider.SetStateMachineByTaskBuilderOfT.MakeGenericMethod(returnGenericArgumentType)
        //            );
        //    }
        //    else
        //    {
        //        generator.Emit(
        //                OpCodes.Call,
        //                ReflectionInfoProvider.SetStateMachineByValueTaskBuilderOfT.MakeGenericMethod(returnGenericArgumentType)
        //            );
        //    }

        //    generator.Emit(OpCodes.Ret);
        //    typeBuilder.DefineMethodOverride(setStateMachineMethod, ReflectionInfoProvider.SetStateMachine);

        //    return setStateMachineMethod;
        //}

        #endregion
    }
}
