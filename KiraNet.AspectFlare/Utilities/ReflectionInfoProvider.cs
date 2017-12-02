using KiraNet.AspectFlare.DynamicProxy;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace KiraNet.AspectFlare.Utilities
{
    internal static class ReflectionInfoProvider
    {
        private static MethodInfo _getCustomAttributes;
        private static MethodInfo _getCusomAttribute;
        private static MethodInfo _ofType;
        private static MethodInfo _toArray;
        private static MethodInfo _getCurrentMethod;
        private static MethodInfo _getTypeFromHandle;
        private static MethodInfo _getInterceptorWrapperDictionary;
        public static MethodInfo GetCustomAttributes
        {
            get
            {
                if (_getCustomAttributes == null)
                {
                    _getCustomAttributes = typeof(CustomAttributeExtensions).GetMethods(
                                              BindingFlags.Public |
                                              BindingFlags.Static
                                          )
                                           .Where(x => CheckGetCustomAttributeMenthod(x, "GetCustomAttributes"))
                                          .First();
                }

                return _getCustomAttributes;
            }
        }

        public static MethodInfo GetCustomAttribute
        {
            get
            {
                if (_getCusomAttribute == null)
                {
                    _getCusomAttribute = typeof(CustomAttributeExtensions).GetMethods(
                                            BindingFlags.Public |
                                            BindingFlags.Static
                                        )
                                        .Where(x => CheckGetCustomAttributeMenthod(x, "GetCustomAttribute"))
                                        .First();
                }

                return _getCusomAttribute;
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
    }
}
