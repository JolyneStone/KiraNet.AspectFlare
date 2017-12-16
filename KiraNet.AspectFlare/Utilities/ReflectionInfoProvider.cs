using KiraNet.AspectFlare.DynamicProxy;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace KiraNet.AspectFlare.Utilities
{
    internal static class ReflectionInfoProvider
    {
        private static MethodInfo _getCustomAttributesCalling;
        private static MethodInfo _getCustomAttributesCalled;
        private static MethodInfo _getCustomAttributesException;
        private static MethodInfo _getCustomAttributeCalling;
        private static MethodInfo _getCustomAttributeCalled;
        private static MethodInfo _getCustomAttributeException;
        private static MethodInfo _ofType;
        private static MethodInfo _toArray;
        private static MethodInfo _getCurrentMethod;
        private static MethodInfo _getTypeFromHandle;
        private static MethodInfo _getInterceptorWrapperDictionary;
        private static ConstructorInfo _interceptorWrapperCollectionByType;
        private static MethodInfo _getWrapper;
        private static MethodInfo _callingIntercepts;
        private static MethodInfo _calledIntercepts;
        private static MethodInfo _exceptionIntercept;
        private static FieldInfo _hasResult;
        private static FieldInfo _result;
        private static ConstructorInfo _compilerGeneratedAttributeConstructor;
        private static ConstructorInfo _debuggerHiddenAttributeConstructor;
        private static MethodInfo _setStateMachine;
        private static MethodInfo _setStateMachineByTaskBuilder;
        private static MethodInfo _setStateMachineByTaskBuilderOfT;
        private static MethodInfo _setStateMachineByValueTaskBuilderOfT;
        private static ConstructorInfo _asyncCallerConstructor;


        private static bool CheckGetCustomAttributeMenthod(MethodInfo method, string methodName)
        {
            if (method.IsGenericMethodDefinition && method.Name == methodName)
            {
                var p = method.GetParameters();
                if (p.Length == 2 &&
                    p[0].ParameterType == typeof(MemberInfo) &&
                    p[1].ParameterType == typeof(bool))
                {
                    return true;
                }
            }

            return false;
        }

        public static MethodInfo GetCustomAttributesCalling
        {
            get
            {
                if (_getCustomAttributesCalling == null)
                {
                    _getCustomAttributesCalling = typeof(CustomAttributeExtensions).GetMethods(
                                              BindingFlags.Public |
                                              BindingFlags.Static
                                          )
                                           .Where(x => CheckGetCustomAttributeMenthod(x, "GetCustomAttributes"))
                                          .First()
                                          .MakeGenericMethod(typeof(CallingInterceptAttribute));
                }

                return _getCustomAttributesCalling;
            }
        }

        public static MethodInfo GetCustomAttributesCalled
        {
            get
            {
                if (_getCustomAttributesCalled == null)
                {
                    _getCustomAttributesCalled = typeof(CustomAttributeExtensions).GetMethods(
                                              BindingFlags.Public |
                                              BindingFlags.Static
                                          )
                                           .Where(x => CheckGetCustomAttributeMenthod(x, "GetCustomAttributes"))
                                          .First()
                                          .MakeGenericMethod(typeof(CalledInterceptAttribute));
                }

                return _getCustomAttributesCalled;
            }
        }

        public static MethodInfo GetCustomAttributesException
        {
            get
            {
                if (_getCustomAttributesException == null)
                {
                    _getCustomAttributesCalling = typeof(CustomAttributeExtensions).GetMethods(
                                              BindingFlags.Public |
                                              BindingFlags.Static
                                          )
                                           .Where(x => CheckGetCustomAttributeMenthod(x, "GetCustomAttributes"))
                                          .First()
                                          .MakeGenericMethod(typeof(ExceptionInterceptAttribute));
                }

                return _getCustomAttributesException;
            }
        }



        public static MethodInfo GetCustomAttributeCalling
        {
            get
            {
                if (_getCustomAttributeCalling == null)
                {
                    _getCustomAttributeCalling = typeof(CustomAttributeExtensions).GetMethods(
                                              BindingFlags.Public |
                                              BindingFlags.Static
                                          )
                                           .Where(x => CheckGetCustomAttributeMenthod(x, "GetCustomAttribute"))
                                          .First()
                                          .MakeGenericMethod(typeof(CallingInterceptAttribute));
                }

                return _getCustomAttributeCalling;
            }
        }

        public static MethodInfo GetCustomAttributeCalled
        {
            get
            {
                if (_getCustomAttributeCalled == null)
                {
                    _getCustomAttributeCalled = typeof(CustomAttributeExtensions).GetMethods(
                                              BindingFlags.Public |
                                              BindingFlags.Static
                                          )
                                           .Where(x => CheckGetCustomAttributeMenthod(x, "GetCustomAttribute"))
                                          .First()
                                          .MakeGenericMethod(typeof(CalledInterceptAttribute));
                }

                return _getCustomAttributeCalled;
            }
        }

        public static MethodInfo GetCustomAttributeException
        {
            get
            {
                if (_getCustomAttributeException == null)
                {
                    _getCustomAttributeCalling = typeof(CustomAttributeExtensions).GetMethods(
                                              BindingFlags.Public |
                                              BindingFlags.Static
                                          )
                                           .Where(x => CheckGetCustomAttributeMenthod(x, "GetCustomAttribute"))
                                          .First()
                                          .MakeGenericMethod(typeof(ExceptionInterceptAttribute));
                }

                return _getCustomAttributeException;
            }
        }


        public static MethodInfo OfType
        {
            get
            {
                if (_ofType == null)
                {
                    _ofType = typeof(Enumerable).GetMethod(
                                "OfType",
                                BindingFlags.Static |
                                BindingFlags.Public,
                                null,
                                new Type[] { typeof(IEnumerable) },
                                null
                            );
                }

                return _ofType;
            }
        }

        public static MethodInfo ToArray
        {
            get
            {
                if (_toArray == null)
                {
                    _toArray = typeof(Enumerable).GetMethod(
                                "ToArray",
                                BindingFlags.Static |
                                BindingFlags.Public
                            );
                }

                return _toArray;
            }
        }

        public static MethodInfo GetCurrentMethod
        {
            get
            {
                if (_getCurrentMethod == null)
                {
                    _getCurrentMethod = typeof(MethodBase).GetMethod(
                                            "GetCurrentMethod",
                                            BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.Static
                                        );
                }

                return _getCurrentMethod;
            }
        }

        public static MethodInfo GetTypeFromHandle
        {
            get
            {
                if (_getTypeFromHandle == null)
                {
                    _getTypeFromHandle = typeof(Type).GetMethod(
                                            "GetTypeFromHandle",
                                            BindingFlags.Public |
                                            BindingFlags.Instance |
                                            BindingFlags.Static
                                        );
                }

                return _getTypeFromHandle;
            }
        }

        public static MethodInfo GetInterceptorWrapperDictionary
        {
            get
            {
                if (_getInterceptorWrapperDictionary == null)
                {
                    _getInterceptorWrapperDictionary
                        = typeof(ReflectionExtensions).GetMethod(
                                    "GetInterceptorWrapperDictionary",
                                    BindingFlags.Public |
                                    BindingFlags.Static,
                                    null,
                                    new Type[] { typeof(Type), typeof(Type) },
                                    null
                                );
                }

                return _getInterceptorWrapperDictionary;
            }
        }

        public static ConstructorInfo InterceptorWrapperCollectionByType
        {
            get
            {
                if (_interceptorWrapperCollectionByType == null)
                {
                    _interceptorWrapperCollectionByType = typeof(InterceptorWrapperCollection)
                                .GetConstructor(
                                    BindingFlags.Public |
                                    BindingFlags.Instance,
                                    null,
                                    new Type[] { typeof(Type) },
                                    null
                                );
                }

                return _interceptorWrapperCollectionByType;
            }
        }

        public static MethodInfo GetWrapper
        {
            get
            {
                if (_getWrapper == null)
                {
                    _getWrapper = typeof(InterceptorWrapperCollection)
                                .GetMethod(
                                    "GetWrapper",
                                    BindingFlags.Instance |
                                    BindingFlags.Public,
                                    null,
                                    new Type[] { typeof(int) },
                                    null
                                );
                }

                return _getWrapper;
            }
        }

        public static MethodInfo CallingIntercepts
        {
            get
            {
                if (_callingIntercepts == null)
                {
                    _callingIntercepts = typeof(InterceptorWrapper)
                                .GetMethod(
                                    "CallingIntercepts",
                                    BindingFlags.Instance |
                                    BindingFlags.Public
                                );
                }

                return _callingIntercepts;
            }
        }

        public static MethodInfo CalledIntercepts
        {
            get
            {
                if (_calledIntercepts == null)
                {
                    _calledIntercepts = typeof(InterceptorWrapper)
                                .GetMethod(
                                    "CalledIntercepts",
                                    BindingFlags.Instance |
                                    BindingFlags.Public
                                );
                }

                return _calledIntercepts;
            }
        }

        public static MethodInfo ExceptionIntercept
        {
            get
            {
                if (_exceptionIntercept == null)
                {
                    _exceptionIntercept = typeof(InterceptorWrapper)
                                .GetMethod(
                                    "ExceptionIntercept",
                                    BindingFlags.Instance |
                                    BindingFlags.Public
                                );
                }

                return _exceptionIntercept;
            }
        }

        public static FieldInfo HasResult
        {
            get
            {
                if (_hasResult == null)
                {
                    _hasResult = typeof(InterceptResult)
                                    .GetField(
                                        "HasResult",
                                        BindingFlags.Instance |
                                        BindingFlags.Public
                                    );
                }

                return _hasResult;
            }
        }

        public static FieldInfo Result
        {
            get
            {
                if (_result == null)
                {
                    _result = typeof(InterceptResult)
                                .GetField(
                                    "Result",
                                    BindingFlags.Instance |
                                    BindingFlags.Public
                                );
                }

                return _result;
            }
        }

        public static ConstructorInfo CompilerGeneratedAttributeConstructor
        {
            get
            {
                if (_compilerGeneratedAttributeConstructor == null)
                {
                    _compilerGeneratedAttributeConstructor = typeof(CompilerGeneratedAttribute).GetConstructor(
                                    BindingFlags.Public | BindingFlags.Instance,
                                    null,
                                    Type.EmptyTypes,
                                    null
                                );
                }

                return _compilerGeneratedAttributeConstructor;
            }
        }

        public static ConstructorInfo DebuggerHiddenAttributeConstructor
        {
            get
            {
                if (_debuggerHiddenAttributeConstructor == null)
                {
                    _debuggerHiddenAttributeConstructor = typeof(DebuggerHiddenAttribute).GetConstructor(
                            BindingFlags.Public | BindingFlags.Instance,
                            null,
                            Type.EmptyTypes,
                            null
                        );
                }

                return _debuggerHiddenAttributeConstructor;
            }
        }

        public static MethodInfo SetStateMachine
        {
            get
            {
                if (_setStateMachine == null)
                {
                    _setStateMachine = typeof(IAsyncStateMachine).GetMethod(
                                "SetStateMachine",
                                new Type[] { typeof(IAsyncStateMachine) }
                            );
                }

                return _setStateMachine;
            }
        }

        public static MethodInfo SetStateMachineByTaskBuilder
        {
            get
            {
                if (_setStateMachineByTaskBuilder == null)
                {
                    _setStateMachineByTaskBuilder = typeof(AsyncTaskMethodBuilder).GetMethod(
                                "SetStateMachine",
                                BindingFlags.Public | BindingFlags.Instance,
                                null,
                                new Type[] { typeof(IAsyncStateMachine) },
                                null
                            );
                }

                return _setStateMachineByTaskBuilder;
            }
        }

        public static MethodInfo SetStateMachineByTaskBuilderOfT
        {
            get
            {
                if (_setStateMachineByTaskBuilderOfT == null)
                {
                    _setStateMachineByTaskBuilderOfT = typeof(AsyncTaskMethodBuilder<>).GetMethod(
                                "SetStateMachine",
                                BindingFlags.Public | BindingFlags.Instance,
                                null,
                                new Type[] { typeof(IAsyncStateMachine) },
                                null
                            );
                }

                return _setStateMachineByTaskBuilderOfT;
            }
        }

        public static MethodInfo SetStateMachineByValueTaskBuilderOfT
        {
            get
            {
                if (_setStateMachineByValueTaskBuilderOfT == null)
                {
                    _setStateMachineByValueTaskBuilderOfT = typeof(AsyncValueTaskMethodBuilder<>).GetMethod(
                                "SetStateMachine",
                                BindingFlags.Public | BindingFlags.Instance,
                                null,
                                new Type[] { typeof(IAsyncStateMachine) },
                                null
                            );
                }

                return _setStateMachineByValueTaskBuilderOfT;
            }
        }

        public static ConstructorInfo AsyncCallerConstructor
        {
            get
            {
                if (_asyncCallerConstructor == null)
                {
                    _asyncCallerConstructor = typeof(Caller).GetConstructor(
                            BindingFlags.Public | BindingFlags.Instance,
                            null,
                            CallingConventions.HasThis,
                            new Type[] { typeof(InterceptorWrapper) },
                            null
                        );
                }

                return _asyncCallerConstructor;
            }
        }
    }
}
