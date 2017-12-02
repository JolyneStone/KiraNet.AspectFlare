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
        private static readonly Lazy<ConcurrentDictionary<int, Type>> _typePool =
            new Lazy<ConcurrentDictionary<int, Type>>(() => new ConcurrentDictionary<int, Type>(), true);

        private static ConcurrentDictionary<int, Type> TypePool => _typePool.Value;

        private readonly ModuleBuilder _moduleBuilder;
        // private readonly int _token; // 用于防止类名或字段名重复


        public ProxyTypeGenerator()
        {
            _moduleBuilder = ProxyConfiguration.Configuration.ProxyModuleBuilder;
            //_token = this.GetHashCode();
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

            var typeBuilder = _moduleBuilder.DefineType(
                                typeName,
                                TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.Sealed);

            var fieldBuilders = DefineField(typeBuilder, metadataToken);
            var initProxyBuilder = InitProxyMethodByClass(typeBuilder, proxyType, fieldBuilders, metadataToken);
            GeneratorConstructors(proxyType, proxyType, typeBuilder, initProxyBuilder, fieldBuilders, metadataToken);

            return typeBuilder.CreateTypeInfo();
        }

        public Type GenerateProxyByInterface(Type interfaceType, Type proxyType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (proxyType == null)
            {
                throw new ArgumentNullException(nameof(proxyType));
            }

            var metadataToken = interfaceType.MetadataToken;
            var typeName = GetTypeName(proxyType, metadataToken);
            if (TypePool.TryGetValue(metadataToken, out Type implementType))
            {
                return implementType;
            }

            var typeBuilder = _moduleBuilder.DefineType(
                           typeName,
                           TypeAttributes.Public |
                           TypeAttributes.Class |
                           TypeAttributes.Sealed);

            var fieldBuilders = DefineField(typeBuilder, metadataToken);
            var initProxyBuilder = InitProxyMethodByInterface(typeBuilder, interfaceType, proxyType, fieldBuilders, metadataToken);
            GeneratorConstructors(interfaceType, proxyType, typeBuilder, initProxyBuilder, fieldBuilders, metadataToken);

            return typeBuilder.CreateTypeInfo();
        }

        private static string GetTypeName(Type proxyType, int token)
        {
            return $"{proxyType.FullName}_AspectFlare_DynamicProxy_{token}";
        }

        private static FieldBuilder[] DefineField(TypeBuilder typeBuilder, int token)
        {
            var fieldBuilders = new FieldBuilder[4];
            fieldBuilders[0] = typeBuilder.DefineField(
                   "_callingInterceptors_" + token,
                   typeof(ICallingInterceptor[]),
                   FieldAttributes.Private
               );


            fieldBuilders[1] = typeBuilder.DefineField(
                    "_calledInterceptors_" + token,
                    typeof(ICalledInterceptor[]),
                    FieldAttributes.Private
                );

            fieldBuilders[2] = typeBuilder.DefineField(
                    "_exceptionInterceptors_" + token,
                    typeof(IExceptionInterceptor),
                    FieldAttributes.Private
                );


            fieldBuilders[3] = typeBuilder.DefineField(
                "_interceptWrapperDict",
                typeof(IDictionary<int, InterceptorWrapper>),
                FieldAttributes.Private
                );

            return fieldBuilders;
        }

        private static void GeneratorConstructors(
            Type serviceType,
            Type proxyType,
            TypeBuilder typeBuilder,
            MethodBuilder initProxyBuilder,
            FieldBuilder[] fieldBuilders,
            int token)
        {
            var proxyCtors = proxyType.GetConstructors(
                                        BindingFlags.CreateInstance |
                                        BindingFlags.Instance |
                                        BindingFlags.Public
                                    )
                                    .ToArray();

            foreach (var proxyCtor in proxyCtors)
            {
                var parameters = proxyCtor.GetParameters()
                                    .Select(x => x.ParameterType)
                                    .ToArray();

                // 定义构造函数
                ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                        MethodAttributes.Public,
                        CallingConventions.HasThis,
                        parameters
                    );

                ILGenerator ctorGenerator = constructorBuilder.GetILGenerator();
                GeneratorConstructor(serviceType, proxyType, proxyCtor, ctorGenerator, initProxyBuilder, fieldBuilders, parameters);


            }
        }

        private static void GeneratorConstructor(
            Type serviceType,
            Type proxyType,
            ConstructorInfo constructor,
            ILGenerator ctorGenerator,
            MethodBuilder initProxyBuilder,
            FieldBuilder[] fieldBuilders,
            Type[] parameters)
        {
            LocalBuilder methodBase = ctorGenerator.DeclareLocal(typeof(MethodBase));


            // 初始化
            // Ldarg_0 一般是方法开始的第一个指令
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Call, initProxyBuilder);


            // var methodBase = MethodInfo.GetCurrentMethod();
            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetCurrentMethod);
            ctorGenerator.Emit(OpCodes.Stloc_0);


            // 调用基类构造函数 base(x, y)
            // 注：这里的调用位置在C#代码中无法表示，因为在C#中调用基类构造函数必须在方法参数列表之后
            CallBaseConstructor(ctorGenerator, constructor, parameters);





            ctorGenerator.Emit(OpCodes.Ret);
        }

        private static void CallBaseConstructor(ILGenerator ctorGenerator, ConstructorInfo constructor, Type[] parameters)
        {
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            switch (parameters.Length)
            {
                case 0:
                    break;
                case 1:
                    ctorGenerator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    ctorGenerator.Emit(OpCodes.Ldarg_1);
                    ctorGenerator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    ctorGenerator.Emit(OpCodes.Ldarg_1);
                    ctorGenerator.Emit(OpCodes.Ldarg_2);
                    ctorGenerator.Emit(OpCodes.Ldarg_3);
                    break;
                case int n when n > 3:
                    ctorGenerator.Emit(OpCodes.Ldarg_1);
                    ctorGenerator.Emit(OpCodes.Ldarg_2);
                    ctorGenerator.Emit(OpCodes.Ldarg_3);
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ctorGenerator.Emit(OpCodes.Ldarg_S, i + 4);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unable to generate build the {constructor.Name} constructor for agent service class");
            }

            ctorGenerator.Emit(OpCodes.Call, constructor);
        }

        // 定义用于初始化字段的私有方法
        /// <summary>
        /// 定义用于初始化类字段的私有方法
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="proxyType"></param>
        /// <param name="fieldBuilders"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static MethodBuilder InitProxyMethodByClass(TypeBuilder typeBuilder, Type proxyType, FieldBuilder[] fieldBuilders, int token)
        {
            // 定义私有方法
            var initProxy = typeBuilder.DefineMethod(
                    "InitProxy_" + token,
                    MethodAttributes.Private,
                    CallingConventions.HasThis
                );

            var initGenerator = initProxy.GetILGenerator();

            LocalBuilder baseType = initGenerator.DeclareLocal(proxyType);

            // var baseType = typeof(BaseType);
            initGenerator.Emit(OpCodes.Ldtoken, proxyType);
            initGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);

            initGenerator.Emit(OpCodes.Stloc_0);

            // 为字段赋值
            EvaluationFieldCallingInterceptor(initGenerator, fieldBuilders[0]);
            EvaluationFieldCalledInterceptor(initGenerator, fieldBuilders[1]);
            EvaluationFieldExceptionInterceptor(initGenerator, fieldBuilders[2]);
            EvaluationFieldInterceptorWrapperDict(initGenerator, fieldBuilders[3], false);

            initGenerator.Emit(OpCodes.Ret);

            return initProxy;
        }

        /// <summary>
        /// 定义用于初始化接口字段的私有方法
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="proxyType"></param>
        /// <param name="fieldBuilders"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static MethodBuilder InitProxyMethodByInterface(TypeBuilder typeBuilder, Type interfaceType, Type proxyType, FieldBuilder[] fieldBuilders, int token)
        {
            // 定义私有方法
            var initProxy = typeBuilder.DefineMethod(
                    "InitProxy_" + token,
                    MethodAttributes.Private,
                    CallingConventions.HasThis
                );

            var initGenerator = initProxy.GetILGenerator();

            LocalBuilder baseType = initGenerator.DeclareLocal(proxyType);
            LocalBuilder iType = initGenerator.DeclareLocal(interfaceType);

            // var baseType = typeof(BaseType);
            initGenerator.Emit(OpCodes.Ldtoken, proxyType);
            initGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);

            initGenerator.Emit(OpCodes.Stloc_0);

            // var interfaceType = typeof(IFoo);
            initGenerator.Emit(OpCodes.Ldtoken, iType);
            initGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetTypeFromHandle);

            initGenerator.Emit(OpCodes.Stloc_1);

            // 为字段赋值
            EvaluationFieldCallingInterceptor(initGenerator, fieldBuilders[0]);
            EvaluationFieldCalledInterceptor(initGenerator, fieldBuilders[1]);
            EvaluationFieldExceptionInterceptor(initGenerator, fieldBuilders[2]);
            EvaluationFieldInterceptorWrapperDict(initGenerator, fieldBuilders[3], true);

            initGenerator.Emit(OpCodes.Ret);

            return initProxy;
        }

        private static void EvaluationFieldCallingInterceptor(ILGenerator ctorGenerator, FieldBuilder fieldBuilder)
        {
            var callingType = typeof(ICallingInterceptor);
            //  _callingInterceptors = baseType.GetCustomAttributes<CallingInterceptAttribute>(true).OfType().ToArray();
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Ldloc_0);
            ctorGenerator.Emit(OpCodes.Ldc_I4_1);

            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetCustomAttributes.MakeGenericMethod(typeof(CallingInterceptAttribute)));
            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.OfType.MakeGenericMethod(callingType));
            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.ToArray.MakeGenericMethod(callingType));
            ctorGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        }

        private static void EvaluationFieldCalledInterceptor(ILGenerator ctorGenerator, FieldBuilder fieldBuilder)
        {
            var calledType = typeof(ICalledInterceptor);
            //  _calledInterceptors = baseType.GetCustomAttributes<CalledInterceptAttribute>(true).OfType().ToArray();
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Ldloc_0);
            ctorGenerator.Emit(OpCodes.Ldc_I4_1);

            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetCustomAttributes.MakeGenericMethod(typeof(CalledInterceptAttribute)));
            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.OfType.MakeGenericMethod(calledType));
            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.ToArray.MakeGenericMethod(calledType));
            ctorGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        }

        private static void EvaluationFieldExceptionInterceptor(ILGenerator ctorGenerator, FieldBuilder fieldBuilder)
        {
            // _exceptionInterceptors = baseType.GetCustomAttribute<ExceptionInterceptAttribute>(true).OfType().ToArray();
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Ldloc_0);
            ctorGenerator.Emit(OpCodes.Ldc_I4_1);

            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetCustomAttribute.MakeGenericMethod(typeof(ExceptionInterceptAttribute)));
            ctorGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        }

        private static void EvaluationFieldInterceptorWrapperDict(ILGenerator ctorGenerator, FieldBuilder fieldBuilder, bool hasInterfaceType)
        {
            // _interceptorWrapperDict = baseType.GetInterceptorWrapperDictionary();
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            if (hasInterfaceType)
            {
                ctorGenerator.Emit(OpCodes.Ldloc_1);
                ctorGenerator.Emit(OpCodes.Ldloc_1);
            }
            else
            {
                ctorGenerator.Emit(OpCodes.Ldloc_0);
                ctorGenerator.Emit(OpCodes.Ldnull);
            }
           
            ctorGenerator.Emit(OpCodes.Call, ReflectionInfoProvider.GetInterceptorWrapperDictionary);
            ctorGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        }
    }
}
