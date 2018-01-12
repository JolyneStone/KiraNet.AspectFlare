//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Reflection.Emit;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Tasks;
//using KiraNet.AspectFlare.DynamicProxy;

//namespace KiraNet.AspectFlare.Utilities
//{
//    public class TS
//    {
//        public virtual void T(ref int x, out Exception y)
//        {
//            y = null;
//        }
//    }

//    public class TSS : TS
//    {
//        public override void T(ref int x, out Exception y)
//        {
//            int z = x;
//            y = null;
//            Exception s = y;
//            Action action = () => base.T(ref z, out s);
//        }
//    }


//    public class Test
//    {
//        private static int _token = 0;
//        public static void GenerateMethod(
//                    GeneratorTypeContext context,
//                    ILGenerator methodGenerator,
//                    MethodBuilder methodBuilder,
//                    MethodBase method,
//                    ParameterInfo[] parameters)
//        {
//            var meth = method as MethodInfo;
//            if (meth == null)
//            {
//                throw new InvalidCastException($"the {method} is not a legal type");
//            }

//            Type returnGenericType = meth.ReturnType;
//            Type returnGenericArgumentType;
//            AsyncType asyncType;
//            if (returnGenericType == typeof(Task))
//            {
//                returnGenericArgumentType = null;
//                asyncType = AsyncType.Task;
//            }
//            else if (returnGenericType.IsGenericType)
//            {
//                returnGenericArgumentType = returnGenericType.GetGenericArguments()[0];
//                asyncType = returnGenericType.GetGenericTypeDefinition() == typeof(Task<>) ?
//                            AsyncType.TaskOfT :
//                            AsyncType.ValueTaskOfT;
//            }
//            else
//            {
//                throw new InvalidOperationException("async method return type is no valid");
//            }

//            var asyncContext = new GeneratorAsyncContext(context)
//            {
//                Method = meth,
//                Parameters = parameters,
//                ReturnType = returnGenericType,
//                AsyncReturnType = returnGenericArgumentType,
//                AsyncType = asyncType,
//                Generator = methodGenerator,
//                MethodBuilder = methodBuilder
//            };

//            asyncContext.AsyncMethod = GeneratorAsyncMethod(asyncContext, _token);
//            var stateMachineName = $"<{method.Name}>pd_{_token}";
//            asyncContext.StructBuilder = context.TypeBuilder.DefineNestedType(
//                                            stateMachineName,
//                                            TypeAttributes.NestedPrivate |
//                                            TypeAttributes.Sealed |
//                                            TypeAttributes.BeforeFieldInit,
//                                            typeof(ValueType),
//                                            new Type[] { typeof(IAsyncStateMachine) }
//                                        );

//            //asyncContext.StructBuilder.SetCustomAttribute(
//            //        new CustomAttributeBuilder(ReflectionInfoProvider.CompilerGeneratedAttributeConstructor, Type.EmptyTypes)
//            //    );

//            StructCtor(asyncContext);
//            DefineFields(asyncContext);
//            asyncContext.SetStateMachine = SetStateMachine(asyncContext);
//            asyncContext.MoveNext = MoveNext(asyncContext);
//            GenerateMethod(asyncContext);

//            context.TypeBuilder.DefineMethodOverride(methodBuilder, meth);
//            asyncContext.StructBuilder.CreateTypeInfo();
//        }

//        public static byte[] BakeByteArray(ILGenerator generator)
//        {
//            // BakeByteArray is an internal function designed to be called by MethodBuilder to do
//            // all of the fixups and return a new byte array representing the byte stream with labels resolved, etc.

//            int newSize;
//            int updateAddr;
//            byte[] newBytes;

//            //Calculate the size of the new array.
//            newSize =(int)GetField("m_length").GetValue(generator);

//            //Allocate space for the new array.
//            newBytes = new byte[newSize];

//            //Copy the data from the old array
//            Buffer.BlockCopy((Array)GetField("m_ILStream").GetValue(generator), 0, newBytes, 0, newSize);

//            //Do the fixups.
//            //This involves iterating over all of the labels and
//            //replacing them with their proper values.
//            Array m_fixupData = (Array)GetField("m_fixupData").GetValue(generator);
//            for (int i = 0; i < (int)GetField("m_fixupCount").GetValue(generator); i++)
//            {
//                object mm = m_fixupData.GetValue(i);
//                Label m_fixupLabel =(Label)mm.GetType().GetField("m_fixupLabel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mm);
//                int m_fixupPos = (int)mm.GetType().GetField("m_fixupPos", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mm);
//                int m_fixupInstSize = (int)mm.GetType().GetField("m_fixupInstSize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mm);
//                updateAddr = (int)typeof(ILGenerator).GetMethod("GetLabelPos", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(generator, new object[] { m_fixupLabel }) - (m_fixupPos + m_fixupInstSize);

//                //Handle single byte instructions
//                //Throw an exception if they're trying to store a jump in a single byte instruction that doesn't fit.
//                if (m_fixupInstSize == 1)
//                {
//                    //Verify that our one-byte arg will fit into a Signed Byte.
//                    if (updateAddr < SByte.MinValue || updateAddr > SByte.MaxValue)
//                    {
//                        throw new NotSupportedException();
//                    }

//                    //Place the one-byte arg
//                    if (updateAddr < 0)
//                    {
//                        newBytes[m_fixupPos] = (byte)(256 + updateAddr);
//                    }
//                    else
//                    {
//                        newBytes[m_fixupPos] = (byte)updateAddr;
//                    }
//                }
//                else
//                {
//                    //Place the four-byte arg
//                    //PutInteger4InArray(updateAddr, m_fixupData[i].m_fixupPos, newBytes);
//                }
//            }
//            return newBytes;

//            FieldInfo GetField(string name)
//            {
//                return typeof(ILGenerator).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
//            }
//        }

//        private static void GenerateMethod(GeneratorAsyncContext context)
//        {
//            context.MethodBuilder.SetCustomAttribute(
//                    new CustomAttributeBuilder(
//                        ReflectionInfoProvider.AsyncStateMachineAttributeConstructor,
//                        new Type[] { context.StructBuilder })
//                );

//            var generator = context.Generator;
//            var asyncStruct = generator.DeclareLocal(context.StructBuilder);
//            var builder = generator.DeclareLocal(context._Builder.FieldType);

//            // <AsyncMethod>d__.<>__this = this;
//            generator.Emit(OpCodes.Ldloca_S, 0);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Stfld, context._This);

//            var parameters = context._ParameterFields;
//            if (parameters != null)
//            {
//                for (var i = 0; i < parameters.Length; i++)
//                {
//                    generator.Emit(OpCodes.Ldloca_S, 0);
//                    generator.Emit(OpCodes.Ldarg_S, i + 1);
//                    generator.Emit(OpCodes.Stfld, parameters[i]);
//                }
//            }

//            generator.Emit(OpCodes.Ldloca_S, 0);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod(
//                    "Create",
//                    BindingFlags.Public | BindingFlags.Static
//                ));
//            generator.Emit(OpCodes.Stfld, context._Builder);
//            generator.Emit(OpCodes.Ldloca_S, 0);
//            generator.Emit(OpCodes.Ldc_I4_M1);
//            generator.Emit(OpCodes.Stfld, context._State);
//            generator.Emit(OpCodes.Ldloc_0);
//            generator.Emit(OpCodes.Ldfld, context._Builder);
//            generator.Emit(OpCodes.Stloc_1);
//            generator.Emit(OpCodes.Ldloca_S, 1);
//            generator.Emit(OpCodes.Ldloca_S, 0);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod(
//                    "Start",
//                    BindingFlags.Public | BindingFlags.Instance
//                ).MakeGenericMethod(context.StructBuilder));
//            generator.Emit(OpCodes.Ldloca_S, 0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod(
//                    "get_Task",
//                    BindingFlags.Public | BindingFlags.Instance
//                ));

//            generator.Emit(OpCodes.Ret);
//        }

//        private static void DefineFields(GeneratorAsyncContext context)
//        {
//            var typeBuilder = context.StructBuilder;
//            context._State = typeBuilder.DefineField(
//                "<>1__state",
//                typeof(int),
//                FieldAttributes.Public
//            );
//            context._Builder = typeBuilder.DefineField(
//                "<>t__builder",
//                typeof(AsyncTaskMethodBuilder),
//                FieldAttributes.Public
//            );
//            context._This = typeBuilder.DefineField(
//                "<>4__this",
//                context.TypeBuilder,
//                FieldAttributes.Public
//            );
//            //context._Wrapper = typeBuilder.DefineField(
//            //    "<wrappe>5__1",
//            //    typeof(InterceptorWrapper),
//            //    FieldAttributes.Private
//            //);
//            context._U = typeBuilder.DefineField(
//                "<>u__1",
//                typeof(TaskAwaiter),
//                FieldAttributes.Private
//            );
//            //var typeBuilder = context.StructBuilder;
//            //var asyncType = context.AsyncType;
//            //var syncReturnType = context.AsyncReturnType;
//            //Type builderType;
//            //Type awaitType;

//            //switch (asyncType)
//            //{
//            //    case AsyncType.Task:
//            //        builderType = typeof(AsyncTaskMethodBuilder);
//            //        awaitType = typeof(TaskAwaiter);
//            //        break;
//            //    case AsyncType.TaskOfT:
//            //        builderType = typeof(AsyncTaskMethodBuilder<>)
//            //            .MakeGenericType(syncReturnType);
//            //        awaitType = typeof(TaskAwaiter<>)
//            //            .MakeGenericType(syncReturnType);
//            //        break;
//            //    case AsyncType.ValueTaskOfT:
//            //        builderType = typeof(AsyncValueTaskMethodBuilder<>)
//            //            .MakeGenericType(syncReturnType);
//            //        awaitType = typeof(ValueTaskAwaiter<>)
//            //            .MakeGenericType(syncReturnType);
//            //        break;
//            //    default:
//            //        throw new InvalidOperationException("async method return type is no valid");
//            //}

//            //context._State = typeBuilder.DefineField(
//            //    "<>__state",
//            //    typeof(int),
//            //    FieldAttributes.Public
//            //);

//            //context._Builder = typeBuilder.DefineField(
//            //        "<>t__builder",
//            //        builderType,
//            //        FieldAttributes.Public
//            //);

//            //context._This = typeBuilder.DefineField(
//            //    "<>__this",
//            //    context.TypeBuilder,
//            //    FieldAttributes.Public
//            //);

//            //var parameters = context.Parameters;
//            //if (parameters != null && parameters.Length > 0)
//            //{
//            //    var list = new List<FieldInfo>(parameters.Length);
//            //    foreach (var parameter in parameters)
//            //    {
//            //        list.Add(typeBuilder.DefineField(
//            //                parameter.Name,
//            //                parameter.ParameterType,
//            //                FieldAttributes.Public
//            //            ));
//            //    }

//            //    context._ParameterFields = list.ToArray();
//            //}

//            //context._Wrapper = typeBuilder.DefineField(
//            //      "<wrapper>__1",
//            //      typeof(InterceptorWrapper),
//            //      FieldAttributes.Private
//            //  );

//            //context._Parameters = typeBuilder.DefineField(
//            //      "<parameters>__2",
//            //      typeof(object[]),
//            //      FieldAttributes.Private
//            //  );

//            //if (context.AsyncType != AsyncType.Task)
//            //{
//            //    context._ReturnValue = typeBuilder.DefineField(
//            //            "<returnValue>__3",
//            //            syncReturnType,
//            //            FieldAttributes.Private
//            //      );
//            //}

//            //context._U = typeBuilder.DefineField(
//            //        "<>u__1",
//            //        awaitType,
//            //        FieldAttributes.Private
//            //    );
//        }

//        private static ConstructorInfo StructCtor(GeneratorAsyncContext context)
//        {
//            var ctor = context.StructBuilder.DefineConstructor(
//                MethodAttributes.HideBySig | 
//                MethodAttributes.SpecialName | 
//                MethodAttributes.RTSpecialName, 
//                CallingConventions.HasThis | CallingConventions.Standard, 
//                null);

//            var generator = ctor.GetILGenerator();
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Call, typeof(object).GetConstructor(
//                BindingFlags.Public| BindingFlags.Instance | BindingFlags.CreateInstance, null, Type.EmptyTypes, null)
//            );
//            generator.Emit(OpCodes.Ret);
//            return ctor;
//        }

//        private static MethodInfo GeneratorAsyncMethod(GeneratorAsyncContext context, int token)
//        {
//            var asyncMethodBuilder = context.TypeBuilder.DefineMethod(
//                "<>n__" + token,
//                MethodAttributes.Private | MethodAttributes.HideBySig,
//                CallingConventions.HasThis,
//                context.ReturnType,
//                Type.EmptyTypes
//            );

//            asyncMethodBuilder.SetCustomAttribute(
//                    new CustomAttributeBuilder(ReflectionInfoProvider.CompilerGeneratedAttributeConstructor, Type.EmptyTypes)
//                );
//            asyncMethodBuilder.SetCustomAttribute(
//                    new CustomAttributeBuilder(ReflectionInfoProvider.DebuggerHiddenAttributeConstructor, Type.EmptyTypes)
//                );
//            asyncMethodBuilder.SetParameters(
//                    context.Parameters.Select(p => p.ParameterType).ToArray()
//                );

//            var generator = asyncMethodBuilder.GetILGenerator();

//            generator.Emit(OpCodes.Ldarg_0);
//            for (int i = 0; i < context.Parameters.Length;)
//            {
//                generator.Emit(OpCodes.Ldarga_S, ++i);
//            }

//            generator.Emit(OpCodes.Call, context.Method);
//            generator.Emit(OpCodes.Ret);

//            return asyncMethodBuilder;
//        }

//        private static MethodInfo SetStateMachine(GeneratorAsyncContext context)
//        {
//            var asyncStateMachineType = typeof(IAsyncStateMachine);
//            var returnGerenicType = context.ReturnType;
//            var returnGenericArgumentType = context.AsyncReturnType;
//            var setStateMachineMethod = context.StructBuilder.DefineMethod(
//                        "SetStateMachine",
//                        MethodAttributes.Final |
//                        MethodAttributes.Private |
//                        MethodAttributes.HideBySig |
//                        MethodAttributes.NewSlot |
//                        MethodAttributes.Virtual,
//                        CallingConventions.HasThis | CallingConventions.Standard,
//                        null,
//                        new Type[] { asyncStateMachineType }
//                    ); ;

//            var generator = setStateMachineMethod.GetILGenerator();
//            //generator.Emit(OpCodes.Ldarg_0);
//            //generator.Emit(OpCodes.Ldflda, context._Builder);
//            //generator.Emit(OpCodes.Ldarg_1);
//            //generator.Emit(
//            //        OpCodes.Call,
//            //        context._Builder.FieldType.GetMethod(
//            //                "SetStateMachine",
//            //                BindingFlags.Public | BindingFlags.Instance,
//            //                null,
//            //                new Type[] { asyncStateMachineType },
//            //                null
//            //        )
//            //);

//            generator.Emit(OpCodes.Ret);
//            context.StructBuilder.DefineMethodOverride(setStateMachineMethod, ReflectionInfoProvider.SetStateMachine);
//            return setStateMachineMethod;
//        }

//        private static MethodInfo MoveNext(GeneratorAsyncContext context)
//        {
//            var moveNext = context.StructBuilder.DefineMethod(
//                        "MoveNext",
//                        MethodAttributes.Private |
//                        MethodAttributes.Final |
//                        MethodAttributes.NewSlot |
//                        MethodAttributes.Virtual |
//                        MethodAttributes.HideBySig,
//                        CallingConventions.HasThis | CallingConventions.Standard,
//                        null,
//                        null
//            );

//            var generator = moveNext.GetILGenerator();
//            Init0(context, generator);
//            context.StructBuilder.DefineMethodOverride(moveNext, ReflectionInfoProvider.MoveNext);
//            // BakeByteArray(generator);
//            return moveNext;
//        }

//        private static LocalBuilder[] DefineLocalVariables(GeneratorAsyncContext context, ILGenerator generator)
//        {
//            //return  new List<LocalBuilder>(5)
//            //{
//            //    generator.DeclareLocal(typeof(int)),
//            //    generator.DeclareLocal(context.TypeBuilder),
//            //    //generator.DeclareLocal(typeof(object[])),
//            //    generator.DeclareLocal(context._U.FieldType),
//            //    generator.DeclareLocal(typeof(Exception))
//            //}.ToArray();
//            return new List<LocalBuilder>(5)
//            {
//                generator.DeclareLocal(typeof(int)),
//                generator.DeclareLocal(context._U.FieldType),
//                //generator.DeclareLocal(typeof(object[])),
//                generator.DeclareLocal(context.StructBuilder),
//                generator.DeclareLocal(typeof(Exception))
//            }.ToArray();

//            //List<LocalBuilder> list = new List<LocalBuilder>(9)
//            //{
//            //    generator.DeclareLocal(typeof(int)),
//            //    generator.DeclareLocal(context.TypeBuilder)
//            //};

//            //var asyncType = context.AsyncType;
//            //var asyncReturnType = context.AsyncReturnType;
//            //switch (asyncType)
//            //{
//            //    case AsyncType.Task:
//            //        list.Add(generator.DeclareLocal(context._U.FieldType));
//            //        break;

//            //    case AsyncType.TaskOfT:
//            //        list.Add(generator.DeclareLocal(asyncReturnType));
//            //        list.Add(generator.DeclareLocal(typeof(InterceptResult)));
//            //        list.Add(generator.DeclareLocal(context._U.FieldType));
//            //        break;

//            //    case AsyncType.ValueTaskOfT:
//            //        list.Add(generator.DeclareLocal(asyncReturnType));
//            //        list.Add(generator.DeclareLocal(typeof(InterceptResult)));
//            //        list.Add(generator.DeclareLocal(context._U.FieldType));
//            //        list.Add(generator.DeclareLocal(context.ReturnType));
//            //        break;

//            //    default:
//            //        throw new InvalidOperationException("async method return type is no valid");
//            //}

//            //var exceptionType = typeof(Exception);
//            //if (asyncReturnType != null && asyncReturnType != exceptionType)
//            //{
//            //    list.Add(generator.DeclareLocal(asyncReturnType));
//            //}

//            //list.Add(generator.DeclareLocal(exceptionType));
//            //list.Add(generator.DeclareLocal(exceptionType));
//            //return list.ToArray();
//        }

//        private static void Init3(GeneratorAsyncContext context, ILGenerator generator)
//        {
//            var variables = DefineLocalVariables(context, generator);
//            Label[] labels = new Label[10];
//            for (var i = 0; i < 10; i++)
//            {
//                labels[i] = generator.DefineLabel();
//            }

//            // int num = this.<>1__state;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._State);
//            // awaiter = ((T)t).Test1().GetAwaiter();
//            generator.Emit(OpCodes.Stloc_0);
//            // T1 t = this.<>4__this;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._This);
//            generator.Emit(OpCodes.Stloc_1);

//            var try1 = generator.BeginExceptionBlock();
//            generator.Emit(OpCodes.Ldloc_0);
//            generator.Emit(OpCodes.Brfalse_S, labels[0]);
//            generator.Emit(OpCodes.Ldloc_0);
//            generator.Emit(OpCodes.Ldc_I4_1);
//            generator.Emit(OpCodes.Beq, labels[1]);

//            // this.<wrapper>5__1 = t._wrappers.GetWrapper(10000);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Ldfld, context._Wrapper);
//            generator.Emit(OpCodes.Ldc_I4, context.Method.MetadataToken);
//            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
//            generator.Emit(OpCodes.Stfld, context._Wrapper);

//            // if (this.<wrapper>5__1 == null)
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._Wrapper);
//            generator.Emit(OpCodes.Brtrue_S, labels[2]);

//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Call, context.AsyncMethod);
//            generator.Emit(OpCodes.Callvirt, typeof(Task).GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Stloc_3);
//            // if (!awaiter.IsCompleted)
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("get_IsCompleted", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Brtrue_S, labels[3]);

//            // num = (this.<>1__state = 0);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_0);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>u__1 = awaiter;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_3);
//            generator.Emit(OpCodes.Stfld, context._U);
//            // this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter, <Type>d__1>(ref awaiter, ref this);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("AwaitUnsafeOnCompleted", BindingFlags.Instance | BindingFlags.Public)
//                .MakeGenericMethod(context._U.FieldType, context.StructBuilder));
//            generator.Emit(OpCodes.Leave, labels[4]);

//            // awaiter = this.<>u__1;
//            generator.MarkLabel(labels[0]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._U);
//            generator.Emit(OpCodes.Stloc_3);
//            // this.<>u__1 = default(TaskAwaiter);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._U);
//            generator.Emit(OpCodes.Initobj, context._U.FieldType);
//            // num = (this.<>1__state = -1);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_M1);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);

//            // awaiter.GetResult();
//            generator.MarkLabel(labels[3]);
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("GetResult", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Leave, labels[5]);

//            // parameters = null;
//            generator.MarkLabel(labels[2]);
//            generator.Emit(OpCodes.Ldnull);
//            generator.Emit(OpCodes.Stloc_2);
//            generator.MarkLabel(labels[1]);
//            generator.Emit(OpCodes.Nop);

//            var try2 = generator.BeginExceptionBlock();
//            // if (num != 1)
//            generator.Emit(OpCodes.Ldloc_0);
//            generator.Emit(OpCodes.Ldc_I4_1);
//            generator.Emit(OpCodes.Beq_S, labels[6]);

//            // if (!this.<wrapper>5__1.CallingIntercepts(t, ".ctor", parameters).HasResult)
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._Wrapper);
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Ldstr, context.Method.Name);
//            generator.Emit(OpCodes.Ldloc_2);
//            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CallingIntercepts);
//            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
//            generator.Emit(OpCodes.Brfalse_S, labels[7]);
//            generator.Emit(OpCodes.Leave, labels[5]); // 此处需要改成长指令

//            generator.MarkLabel(labels[7]);
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Call, context.AsyncMethod);
//            generator.Emit(OpCodes.Callvirt, typeof(Task).GetMethod("GetAwaiter", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Stloc_3);
//            // if (!awaiter.IsCompleted)
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("get_IsCompleted", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Brtrue_S, labels[8]);

//            // num = (this.<>1__state = 1);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_1);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>u__1 = awaiter;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_3);
//            generator.Emit(OpCodes.Stfld, context._U);
//            // this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter, <Type>d__1>(ref awaiter, ref this);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("AwaitUnsafeOnCompleted", BindingFlags.Public | BindingFlags.Instance)
//                .MakeGenericMethod(context._U.FieldType, context.StructBuilder));
//            generator.Emit(OpCodes.Leave_S, labels[4]);

//            // awaiter = this.<>u__1;
//            generator.MarkLabel(labels[6]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._U);
//            generator.Emit(OpCodes.Stloc_3);
//            // this.<>u__1 = default(TaskAwaiter);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._U);
//            generator.Emit(OpCodes.Initobj, context._U.FieldType);
//            // num = (this.<>1__state = -1);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_M1);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);

//            // awaiter.GetResult();
//            generator.MarkLabel(labels[8]);
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("GetResult", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Leave_S, labels[9]);

//            generator.BeginCatchBlock(typeof(Exception));
//            generator.ThrowException(typeof(Exception));
//            generator.EndExceptionBlock();

//            generator.MarkLabel(labels[9]);
//            generator.Emit(OpCodes.Leave_S, labels[5]);

//            generator.BeginCatchBlock(typeof(Exception));
//            // this.<>1__state = -2;
//            generator.Emit(OpCodes.Stloc_S, 4);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_S, -2);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>t__builder.SetException(exception);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloc_S, 4);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("SetException", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Leave_S, labels[4]);
//            generator.EndExceptionBlock();

//            // this.<>1__state = -2;
//            generator.MarkLabel(labels[5]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_S, -2);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>t__builder.SetResult();
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("SetResult", BindingFlags.Public | BindingFlags.Instance));
//            generator.MarkLabel(labels[4]);
//            generator.Emit(OpCodes.Ret);
//        }

//        private static void Init2(GeneratorAsyncContext context, ILGenerator generator)
//        {
//            var variables = DefineLocalVariables(context, generator);
//            Label[] labels = new Label[8];
//            for (var i = 0; i < 8; i++)
//            {
//                labels[i] = generator.DefineLabel();
//            }

//            // int num = this.<>1__state;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._State);
//            // awaiter = ((T)t).Test1().GetAwaiter();
//            generator.Emit(OpCodes.Stloc_0);
//            // T1 t = this.<>4__this;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._This);
//            generator.Emit(OpCodes.Stloc_1);

//            var try1 = generator.BeginExceptionBlock();
//            generator.Emit(OpCodes.Ldloc_0);
//            generator.Emit(OpCodes.Brfalse_S, labels[0]);
//            generator.Emit(OpCodes.Ldloc_0);
//            generator.Emit(OpCodes.Ldc_I4_1);
//            generator.Emit(OpCodes.Beq, labels[1]);

//            // this.<wrapper>5__1 = t._wrappers.GetWrapper(10000);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Ldfld, context._Wrapper);
//            generator.Emit(OpCodes.Ldc_I4, context.Method.MetadataToken);
//            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.GetWrapper);
//            generator.Emit(OpCodes.Stfld, context._Wrapper);

//            // if (this.<wrapper>5__1 == null)
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._Wrapper);
//            generator.Emit(OpCodes.Brtrue_S, labels[2]);

//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Call, context.AsyncMethod);
//            generator.Emit(OpCodes.Callvirt, typeof(Task).GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Stloc_3);
//            // if (!awaiter.IsCompleted)
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("get_IsCompleted", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Brtrue_S, labels[3]);

//            // num = (this.<>1__state = 0);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_0);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>u__1 = awaiter;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_3);
//            generator.Emit(OpCodes.Stfld, context._U);
//            // this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter, <Type>d__1>(ref awaiter, ref this);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("AwaitUnsafeOnCompleted", BindingFlags.Instance | BindingFlags.Public)
//                .MakeGenericMethod(context._U.FieldType, context.StructBuilder));
//            generator.Emit(OpCodes.Leave, labels[4]);

//            // awaiter = this.<>u__1;
//            generator.MarkLabel(labels[0]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._U);
//            generator.Emit(OpCodes.Stloc_3);
//            // this.<>u__1 = default(TaskAwaiter);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._U);
//            generator.Emit(OpCodes.Initobj, context._U.FieldType);
//            // num = (this.<>1__state = -1);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_M1);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);

//            // awaiter.GetResult();
//            generator.MarkLabel(labels[3]);
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("GetResult", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Leave, labels[5]);

//            // parameters = null;
//            generator.MarkLabel(labels[2]);
//            generator.Emit(OpCodes.Ldnull);
//            generator.Emit(OpCodes.Stloc_2);
//            //generator.MarkLabel(labels[1]);
//            //generator.Emit(OpCodes.Nop);

//            //var try2 = generator.BeginExceptionBlock();
//            //// if (num != 1)
//            //generator.Emit(OpCodes.Ldloc_0);
//            //generator.Emit(OpCodes.Ldc_I4_1);
//            //generator.Emit(OpCodes.Beq_S, labels[6]);

//            // if (!this.<wrapper>5__1.CallingIntercepts(t, ".ctor", parameters).HasResult)
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._Wrapper);
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Ldstr, context.Method.Name);
//            generator.Emit(OpCodes.Ldloc_2);
//            generator.Emit(OpCodes.Callvirt, ReflectionInfoProvider.CallingIntercepts);
//            generator.Emit(OpCodes.Ldfld, ReflectionInfoProvider.HasResult);
//            generator.Emit(OpCodes.Brfalse_S, labels[6]);
//            generator.Emit(OpCodes.Leave, labels[5]); // 此处需要改成长指令

//            generator.MarkLabel(labels[6]);
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Call, context.AsyncMethod);
//            generator.Emit(OpCodes.Callvirt, typeof(Task).GetMethod("GetAwaiter", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Stloc_3);
//            // if (!awaiter.IsCompleted)
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("get_IsCompleted", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Brtrue_S, labels[7]);

//            // num = (this.<>1__state = 1);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_1);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>u__1 = awaiter;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_3);
//            generator.Emit(OpCodes.Stfld, context._U);
//            // this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter, <Type>d__1>(ref awaiter, ref this);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("AwaitUnsafeOnCompleted", BindingFlags.Public | BindingFlags.Instance)
//                .MakeGenericMethod(context._U.FieldType, context.StructBuilder));
//            generator.Emit(OpCodes.Leave_S, labels[4]);

//            // awaiter = this.<>u__1;
//            generator.MarkLabel(labels[1]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._U);
//            generator.Emit(OpCodes.Stloc_3);
//            // this.<>u__1 = default(TaskAwaiter);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._U);
//            generator.Emit(OpCodes.Initobj, context._U.FieldType);
//            // num = (this.<>1__state = -1);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_M1);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);

//            // awaiter.GetResult();
//            generator.MarkLabel(labels[7]);
//            generator.Emit(OpCodes.Ldloca_S, 3);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("GetResult", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Leave_S, labels[5]);

//            //generator.BeginCatchBlock(typeof(Exception));
//            //generator.ThrowException(typeof(Exception));
//            //generator.EndExceptionBlock();

//            //generator.MarkLabel(labels[9]);
//            //generator.Emit(OpCodes.Leave_S, labels[5]);

//            generator.BeginCatchBlock(typeof(Exception));
//            // this.<>1__state = -2;
//            generator.Emit(OpCodes.Stloc_S, 4);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_S, -2);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>t__builder.SetException(exception);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloc_S, 4);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("SetException", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Leave_S, labels[4]);
//            generator.EndExceptionBlock();

//            // this.<>1__state = -2;
//            generator.MarkLabel(labels[5]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_S, -2);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>t__builder.SetResult();
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("SetResult", BindingFlags.Public | BindingFlags.Instance));
//            generator.MarkLabel(labels[4]);
//            generator.Emit(OpCodes.Ret);
//        }

//        private static void Init1(GeneratorAsyncContext context, ILGenerator generator)
//        {
//            var variables = DefineLocalVariables(context, generator);
//            Label[] labels = new Label[4];
//            for (var i = 0; i < 4; i++)
//            {
//                labels[i] = generator.DefineLabel();
//            }

//            // int num=this.<>__state
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._State);
//            // awaiter = ((T)t).Method().GetAwaiter();
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._This);
//            generator.Emit(OpCodes.Stloc_1);

//            var tryLabel = generator.BeginExceptionBlock();
//            // if (num != 0)
//            generator.Emit(OpCodes.Ldloc_0);
//            generator.Emit(OpCodes.Brfalse_S, labels[0]); // 0046
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Call, context.AsyncMethod);
//            generator.Emit(OpCodes.Callvirt, typeof(Task).GetMethod("GetAwaiter", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Stloc_2);
//            // if (!awaiter.IsCompleted)
//            generator.Emit(OpCodes.Ldloca_S, 2);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("get_IsCompleted", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Brtrue_S, labels[1]); // 0062

//            // num = (this.<>1__state = 0);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_0);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>u__1 = awaiter;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_2);
//            generator.Emit(OpCodes.Stfld, context._U);
//            // this.<>t__builder.AwaitUnsafeOnCompleted<TaskAwaiter, <Test1>d__1>(ref awaiter, ref this);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloca_S, 2);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("AwaitUnsafeOnCompleted")
//                .MakeGenericMethod(context._U.FieldType, context.StructBuilder));
//            generator.Emit(OpCodes.Leave_S, labels[2]); // 0095

//            // awaiter = this.<>u__1;
//            generator.MarkLabel(labels[0]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._U);
//            generator.Emit(OpCodes.Stloc_2);
//            // this.<>u__1 = default(TaskAwaiter);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._U);
//            generator.Emit(OpCodes.Initobj, context._U.FieldType);
//            // num = (this.<>1__state = -1);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_M1);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);

//            // awaiter.GetResult();
//            generator.MarkLabel(labels[1]);
//            generator.Emit(OpCodes.Ldloca_S, 2);
//            generator.Emit(OpCodes.Call, context._U.FieldType.GetMethod("GetResult", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Leave_S, labels[3]);

//            generator.BeginCatchBlock(typeof(Exception));
//            generator.Emit(OpCodes.Stloc_3);
//            // this.<>1__state = -2;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_S, -2);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>t__builder.SetException(exception);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloc_3);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("SetException", BindingFlags.Instance | BindingFlags.Public));
//            generator.Emit(OpCodes.Leave_S, labels[2]);

//            generator.EndExceptionBlock();

//            // this.<>1__state = -2;
//            generator.MarkLabel(labels[3]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_S, -2);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>t__builder.SetResult();
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("SetResult", BindingFlags.Instance | BindingFlags.Public));

//            generator.MarkLabel(labels[2]);
//            generator.Emit(OpCodes.Ret);
//        }

//        private static void S(ref int x)
//        {

//        }

//        private static void Init0(GeneratorAsyncContext context, ILGenerator generator)
//        {
//            int x = 1;
//            Action action = () => S(ref x); 
//            var variables = DefineLocalVariables(context, generator);
//            var labels = new Label[6];
//            for(var i = 0; i< 6; i++)
//            {
//                labels[i] = generator.DefineLabel();
//            }

//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._State);
//            generator.Emit(OpCodes.Stloc_0);

//            var try1 = generator.BeginExceptionBlock();
//            // if(num != 0)
//            generator.Emit(OpCodes.Ldloc_0);
//            generator.Emit(OpCodes.Brfalse_S, labels[0]);
//            generator.Emit(OpCodes.Br_S, labels[1]);
//            generator.MarkLabel(labels[0]);
//            generator.Emit(OpCodes.Br_S, labels[2]);
//            generator.MarkLabel(labels[1]);
//            generator.Emit(OpCodes.Nop);

//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._This);
//            generator.Emit(OpCodes.Call, context.AsyncMethod);
//            context.GetAwaiter = context.ReturnType.GetMethod("GetAwaiter", BindingFlags.Public | BindingFlags.Instance);
//            generator.Emit(OpCodes.Callvirt, context.GetAwaiter);
//            generator.Emit(OpCodes.Stloc_1);

//            // if(!awaiter.IsConpleted)
//            generator.Emit(OpCodes.Ldloca_S, 1);
//            context.Get_IsCompleted = context._U.FieldType.GetMethod("get_IsCompleted", BindingFlags.Public | BindingFlags.Instance);
//            generator.Emit(OpCodes.Call, context.Get_IsCompleted);
//            generator.Emit(OpCodes.Brtrue_S, labels[3]);

//            // num = (this.<>_state = 0);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_0);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);

//            // this.<>u__1 = awaiter;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Stfld, context._U);
//            // <T>d__1 = <T>d__ = this;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldloc_1);
//            generator.Emit(OpCodes.Stloc_2);
//            // this.<>t__builder.AwaitUnsafeOnCompleted(ref awaiter, ref <T>d__);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloca_S, 1);
//            generator.Emit(OpCodes.Ldloca_S, 2);
//            context.AwaitUnsafeOnCompleted = context._Builder.FieldType.GetMethod("AwaitUnsafeOnCompleted", BindingFlags.Public | BindingFlags.Instance)
//                .MakeGenericMethod(context._U.FieldType, context.StructBuilder);
//            generator.Emit(OpCodes.Call, context.AwaitUnsafeOnCompleted);
//            generator.Emit(OpCodes.Leave_S, labels[4]);

//            // awaiter = this.<>u__1;
//            generator.MarkLabel(labels[2]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldfld, context._U);
//            generator.Emit(OpCodes.Stloc_1);
//            // this.<>u__1 = default(TaskAwaiter);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._U);
//            generator.Emit(OpCodes.Initobj, context._U.FieldType);
//            // num = (this.<>__state = -1);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_M1);
//            generator.Emit(OpCodes.Dup);
//            generator.Emit(OpCodes.Stloc_0);
//            generator.Emit(OpCodes.Stfld, context._State);

//            // awaiter.GetResult();
//            generator.MarkLabel(labels[3]);
//            generator.Emit(OpCodes.Ldloca_S, 1);
//            context.GetResult = context._U.FieldType.GetMethod("GetResult", BindingFlags.Public | BindingFlags.Instance);
//            generator.Emit(OpCodes.Call, context.GetResult);
//            generator.Emit(OpCodes.Leave_S, labels[5]);

//            generator.BeginCatchBlock(typeof(Exception));
//            generator.Emit(OpCodes.Stloc_3);
//            // this.<>__state = -2;
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_S, -2);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>t__builder.SetException(exception);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Ldloc_3);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("SetException", BindingFlags.Public | BindingFlags.Instance));
//            generator.Emit(OpCodes.Leave_S, labels[4]);
//            generator.EndExceptionBlock();

//            // this.<>__state = -2;
//            generator.MarkLabel(labels[5]);
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldc_I4_S, -2);
//            generator.Emit(OpCodes.Stfld, context._State);
//            // this.<>t__builder.SetResult();
//            generator.Emit(OpCodes.Ldarg_0);
//            generator.Emit(OpCodes.Ldflda, context._Builder);
//            generator.Emit(OpCodes.Call, context._Builder.FieldType.GetMethod("SetResult", BindingFlags.Public | BindingFlags.Instance));
//            generator.MarkLabel(labels[4]);
//            generator.Emit(OpCodes.Ret);
//        }
//    }
//}