using KiraNet.AspectFlare.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyTypeGenerator : IProxyTypeGenerator
    {
        private readonly static ConcurrentDictionary<int, Type> TypePool = new ConcurrentDictionary<int, Type>();

        private readonly ModuleBuilder _moduleBuilder;
        // private readonly int _token; // 用于防止类名或字段名重复

        private IGenerateOperator _defineTypeOperator;
        private IGenerateOperator _defineGenericTypeArgumentsOperator;
        private IGenerateOperator _defineWrappersOperator;
        private IGenerateOperator _implementInitMethodOperator;
        private IGenerateOperator _implementConstructorsOperator;
        private IGenerateOperator _implementMethodsOperator;

        private IGenerateOperator DefineTypeOperator
        {
            get
            {
                if(_defineTypeOperator==null)
                {
                    _defineTypeOperator = new DefineTypeOperator();
                }

                return _defineTypeOperator;
            }
        }

        private IGenerateOperator DefineWrappersOperator
        {
            get
            {
                if (_defineWrappersOperator == null)
                {
                    _defineWrappersOperator = new DefineWrappersOperator();
                }

                return _defineWrappersOperator;
            }
        }

        private IGenerateOperator DefineGenericTypeArgumentsOperator
        {
            get
            {
                if (_defineGenericTypeArgumentsOperator == null)
                {
                    _defineGenericTypeArgumentsOperator = new DefineGenericTypeArgumentsOperator();
                }

                return _defineGenericTypeArgumentsOperator;
            }
        }

        private IGenerateOperator ImplementInitMethodOperator
        {
            get
            {
                if(_implementInitMethodOperator == null)
                {
                    _implementInitMethodOperator = new ImplementInitMethodsOperator();
                }

                return _implementInitMethodOperator;
            }
        }

        private IGenerateOperator ImplementConstructorsOperator
        {
            get
            {
                if(_implementConstructorsOperator == null)
                {
                    _implementConstructorsOperator = new ImplementConstructorsOperator();
                }

                return _implementConstructorsOperator;
            }
        }

        private IGenerateOperator ImplementMethodsOperator
        {
            get
            {
                if(_implementMethodsOperator == null)
                {
                    _implementMethodsOperator = new ImplementMethodOperator();
                }

                return _implementMethodsOperator;
            }
        }

        public ProxyTypeGenerator()
        {
            _moduleBuilder = ProxyConfiguration.Configuration.ProxyModuleBuilder;
            _defineTypeOperator = new DefineTypeOperator();
        }

        public Type GenerateProxyByClass(Type proxyType)
        {
            if (proxyType == null)
            {
                throw new ArgumentNullException(nameof(proxyType));
            }

            // MetadataToken是在运行时的一张Metadata表中的标识，因此对于已经Build的程序集，其Metadata表已经固定
            var metadataToken = proxyType.MetadataToken;
            var typeName = GetTypeName(proxyType, metadataToken);
            if (TypePool.TryGetValue(metadataToken, out Type implementType))
            {
                return implementType;
            }

            var context = new GenerateTypeContext
            {
                ModuleBuilder = _moduleBuilder,
                ProxyType = proxyType
            };

            DefineTypeOperator.Generate(context);

            if(proxyType.IsGenericTypeDefinition)
            {
                DefineGenericTypeArgumentsOperator.Generate(context);
            }

            DefineWrappersOperator.Generate(context);

            ImplementInitMethodOperator.Generate(context);
            ImplementConstructorsOperator.Generate(context);
            ImplementMethodsOperator.Generate(context);

            return context.TypeBuilder.CreateTypeInfo();
        }

        public Type GenerateProxyByInterface(Type interfaceType, Type proxyType)
        {
            throw new NotImplementedException();
        }

        private static string GetTypeName(Type proxyType, int token)
        {
            return $"{proxyType.Name}_AspectFlare_DynamicProxy_{token}";
        }

        private static void GeneratorGenericTypeArguments(Type proxyType, TypeBuilder typeBuilder)
        {
            if (proxyType.IsGenericTypeDefinition)
            {
                var genericArguments = proxyType.GetGenericArguments();
                GenericTypeParameterBuilder[] typeArguments = typeBuilder.DefineGenericParameters(
                                                                    genericArguments.Select(x => x.Name).ToArray()
                                                            );
                for (var i = 0; i < genericArguments.Length; i++)
                {
                    var typeArgument = typeArguments[i];
                    var genericArgument = genericArguments[i];
                    typeArgument.SetGenericParameterAttributes(genericArguments[i].GenericParameterAttributes);
                    typeArgument.SetBaseTypeConstraint(genericArgument.BaseType);
                    typeArgument.SetInterfaceConstraints(genericArgument.GetInterfaces());
                }
            }
        }

        private static void GeneratorConstructors(
            Type proxyType,
            TypeBuilder typeBuilder,
            MethodInfo init,
            FieldInfo wrappers)
        {
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
                GeneratorConstructor(proxyType, proxyCtor, ctorGenerator, init, wrappers, baseParameterInfos);
            }
        }

        private static void GeneratorConstructor(
            Type proxyType,
            ConstructorInfo constructor,
            ILGenerator ctorGenerator,
            MethodInfo init,
            FieldInfo wrapper,
            ParameterInfo[] parameters)
        {
            // 初始化
            // Ldarg_0 一般是方法开始的第一个指令
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Call, init);


            // 调用基类构造函数 base(x, y)
            // 注：这里的调用位置在C#代码中无法表示，因为在C#中调用基类构造函数必须在方法参数列表之后
            // CallBaseMethod(ctorGenerator, constructor, parameters);
            CallMethodByNoReturn(proxyType, constructor, ctorGenerator, wrapper, parameters);
        }

        private static FieldBuilder GeneratorField(TypeBuilder typeBuilder)
        {
            var fieldBuider = typeBuilder.DefineField("<_wrappers>", typeof(InterceptorWrapperCollection), FieldAttributes.Private);
            return fieldBuider;
        }

        private static MethodBuilder GeneratorInitMethod(TypeBuilder typeBuilder, FieldInfo wrappers, Type proxyType)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                                    "<Init_Proxy>",
                                    MethodAttributes.Private |
                                    MethodAttributes.Final,
                                    CallingConventions.HasThis
                                );

            var methodGenerator = methodBuilder.GetILGenerator();
            var t_type = methodGenerator.DeclareLocal(typeof(Type));

            methodGenerator.Emit(OpCodes.Ldtoken, proxyType);
            methodGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);
            methodGenerator.Emit(OpCodes.Stloc_0);

            methodGenerator.Emit(OpCodes.Ldarg_0);
            methodGenerator.Emit(OpCodes.Ldloc_0);
            methodGenerator.Emit(OpCodes.Newobj, ReflectionInfoProvider.InterceptorWrapperCollectionByType);
            methodGenerator.Emit(OpCodes.Stfld, wrappers);

            methodGenerator.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        private static void CallMethodByNoReturn(
            Type proxyType,
            MethodBase method,
            ILGenerator generator,
            FieldInfo wrappers,
            ParameterInfo[] parameters)
        {

            var t_wrapper = generator.DeclareLocal(typeof(InterceptorWrapper));
            var t_parameters = generator.DeclareLocal(typeof(object[]));
            var t_ex = generator.DeclareLocal(typeof(Exception));

            // InterceptorWrapper wrapper = this._wrappers.GetWrapper(xxx);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, wrappers);
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


            if (method is MethodInfo meth)
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
            generator.Emit(OpCodes.Pop);

            generator.Emit(OpCodes.Leave_S, retLable);

            // end try-catch
            generator.EndExceptionBlock();

            generator.MarkLabel(retLable);
            generator.Emit(OpCodes.Ret);
        }

        private static void CallMethodHasReturn(Type proxyType,
            MethodInfo method,
            ILGenerator generator,
            FieldInfo wrappers,
            ParameterInfo[] parameters)
        {
            var returnType = method.ReturnType;
            var t_wrapper = generator.DeclareLocal(typeof(InterceptorWrapper));
            var t_parameters = generator.DeclareLocal(typeof(object[]));
            var t_result = generator.DeclareLocal(typeof(InterceptResult));
            var t_returnValue1 = generator.DeclareLocal(returnType);
            var t_returnValue2 = generator.DeclareLocal(returnType);
            var t_ex = generator.DeclareLocal(typeof(Exception));

            // InterceptorWrapper wrapper = this._wrappers.GetWrapper(xxx);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, wrappers);
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
            if(returnType.IsClass)
            {
                generator.Emit(OpCodes.Castclass, returnType);
            }
            else
            {
                generator.Emit(OpCodes.Unbox_Any, returnType);
            }
            generator.Emit(OpCodes.Stloc_S, 4); // ?
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
            generator.Emit(OpCodes.Ldloc_1);
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
            Label exceptionRet = generator.DefineLabel();
            generator.Emit(OpCodes.Brfalse_S, exceptionRet);
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
            generator.MarkLabel(exceptionRet);
            generator.Emit(OpCodes.Ldloc_3);
            generator.Emit(OpCodes.Stloc_S, 4);
            generator.Emit(OpCodes.Leave_S, retLable);

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
                    throw new InvalidOperationException($"Unable to generate agent service class for {proxyType.FullName} class");
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

        private static void GeneratorClassMethods(Type proxyType, TypeBuilder typeBuilder, FieldInfo wrappers)
        {
            var proxyMethods = proxyType.GetMethods(
                                        BindingFlags.Instance |
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic
                                    )
                                    .Where(
                                     x =>
                                x.IsVirtual &&
                                (x.IsPublic || x.IsFamily || !(x.IsAssembly || x.IsFamilyAndAssembly || x.IsFamilyOrAssembly)) &&
                                x.HasInterceptAttribute()
                            );

            foreach (var proxyMethod in proxyMethods)
            {
                var baseParameterInfos = proxyMethod.GetParameters();
                var parameters = baseParameterInfos
                                    .Select(x => x.ParameterType)
                                    .ToArray();

                // 定义方法
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                        proxyMethod.Name,
                        proxyMethod.Attributes,
                        CallingConventions.HasThis
                    );

                SetGenericMethodArguments(methodBuilder, proxyMethod);
                SetMethodParameters(methodBuilder, baseParameterInfos);

                ILGenerator methodGenerator = methodBuilder.GetILGenerator();
                GeneratorMethod(proxyType, proxyMethod, methodGenerator, wrappers, baseParameterInfos);

                // overrided method
                typeBuilder.DefineMethodOverride(methodBuilder, proxyMethod);
            }
        }

        private static void GeneratorMethod(
            Type proxyType,
            MethodInfo proxyMethod,
            ILGenerator methodGenerator,
            FieldInfo wrappers,
            ParameterInfo[] parameters)
        {
            if(proxyMethod.ReturnType == typeof(void))
            {
                CallMethodByNoReturn(proxyType, proxyMethod, methodGenerator, wrappers, parameters);
            }
            else
            {
                CallMethodHasReturn(proxyType, proxyMethod, methodGenerator, wrappers, parameters);
            }
        }

        private static void SetGenericMethodArguments(MethodBuilder methodBuilder, MethodInfo method)
        {
            if (method.IsGenericMethodDefinition)
            {
                var genericArguments = method.GetGenericArguments();
                GenericTypeParameterBuilder[] typeArguments = methodBuilder.DefineGenericParameters(
                                                                    genericArguments.Select(x => x.Name).ToArray()
                                                            );
                for (var i = 0; i < genericArguments.Length; i++)
                {
                    var typeArgument = typeArguments[i];
                    var genericArgument = genericArguments[i];
                    typeArgument.SetGenericParameterAttributes(genericArguments[i].GenericParameterAttributes);
                    typeArgument.SetBaseTypeConstraint(genericArgument.BaseType);
                    typeArgument.SetInterfaceConstraints(genericArgument.GetInterfaces());
                }
            }
        }

        private static void SetMethodParameters(MethodBuilder methodBuilder, MethodInfo method)
        {
            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var baseParameter = parameters[i];
                var parameterBuilder = methodBuilder.DefineParameter(
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

        private static void SetConstructorParameters(ConstructorBuilder constructorBuilder, ParameterInfo[] parameters)
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

        private static void SetMethodParameters(MethodBuilder methodBuilder, ParameterInfo[] parameters)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                var baseParameter = parameters[i];

                var parameterBuilder = methodBuilder.DefineParameter(
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

    
        private static IEnumerable<MethodInfo> GetInterceptedMethodByClass(Type proxyType)
        {
            return proxyType.GetMethods(
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.Instance
                ).Where(
                    x =>
                        x.IsVirtual &&
                        (x.IsPublic || x.IsFamily) &&
                        x.HasInterceptAttribute()
                );
        }
    }
}
