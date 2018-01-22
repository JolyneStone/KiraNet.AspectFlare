using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class InterceptorWrapperCollection : IEnumerable<KeyValuePair<int, InterceptorWrapper>>
    {
        private readonly Dictionary<int, InterceptorWrapper> _wrappers;

        //public InterceptorWrapperCollection(Type interfaceType, Type proxyType) : this(proxyType)
        //{
        //    if (interfaceType == null)
        //    {
        //        throw new ArgumentNullException(nameof(interfaceType));
        //    }

        //    if (_wrappers.ContainsKey(0))
        //    {
        //        _wrappers[0].CallingInterceptors.Concat(interfaceType.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>());
        //        _wrappers[0].CalledInterceptors.Concat(interfaceType.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICalledInterceptor>());
        //        if (_wrappers[0].ExceptionInterceptor == null)
        //        {
        //            _wrappers[0].ExceptionInterceptor = interfaceType.GetCustomAttribute<ExceptionInterceptAttribute>(true);
        //        }
        //    }

        //    bool hasTypeIntercept = interfaceType.HasInterceptAttribute();
        //    foreach (var method in proxyType.GetMethods(
        //                        BindingFlags.Instance |
        //                        BindingFlags.Public |
        //                        BindingFlags.NonPublic)
        //                    .Where(
        //                     x =>
        //                        x.IsVirtual && (x.IsPublic || x.IsPrivate)
        //                    ))
        //    {
        //        if (method.IsDefined(typeof(NonInterceptAttribute)))
        //        {
        //            continue;
        //        }

        //        if (!method.HasDefineInterceptAttribute() && !hasTypeIntercept)
        //        {
        //            continue;
        //        }

        //        _wrappers.Add(method.MetadataToken, new InterceptorWrapper
        //        {
        //            CallingInterceptors = method.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>(),
        //            CalledInterceptors = method.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>(),
        //            ExceptionInterceptor = method.GetCustomAttribute<ExceptionInterceptAttribute>(true)
        //        });
        //    }
        //}

        //public InterceptorWrapperCollection(Type proxyType)
        //{
        //    if (proxyType == null)
        //    {
        //        throw new ArgumentNullException(nameof(proxyType));
        //    }

        //    _wrappers = new Dictionary<int, InterceptorWrapper>();
        //    var wrapper = new InterceptorWrapper
        //    {
        //        CallingInterceptors = proxyType.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>(),
        //        CalledInterceptors = proxyType.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>(),
        //        ExceptionInterceptor = proxyType.GetCustomAttribute<ExceptionInterceptAttribute>(true)
        //    };

        //    _wrappers.Add(0, wrapper);

        //    bool hasTypeIntercept = proxyType.HasInterceptAttribute();
        //    foreach (var ctor in proxyType.GetConstructors(
        //                        BindingFlags.Instance |
        //                        BindingFlags.Public |
        //                        BindingFlags.NonPublic)
        //                    .Where(
        //                     x =>
        //                        (x.IsPublic || x.IsFamily || !(x.IsAssembly || x.IsFamilyAndAssembly || x.IsFamilyOrAssembly))
        //                    ))
        //    {
        //        if (ctor.IsDefined(typeof(NonInterceptAttribute)))
        //        {
        //            continue;
        //        }

        //        if (!ctor.HasDefineInterceptAttribute() && !hasTypeIntercept)
        //        {
        //            continue;
        //        }

        //        _wrappers.Add(ctor.MetadataToken, new InterceptorWrapper
        //        {
        //            CallingInterceptors = ctor.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>(),
        //            CalledInterceptors = ctor.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>(),
        //            ExceptionInterceptor = ctor.GetCustomAttribute<ExceptionInterceptAttribute>(true)
        //        });
        //    }

        //    foreach (var method in proxyType.GetMethods(
        //                        BindingFlags.Instance |
        //                        BindingFlags.Public |
        //                        BindingFlags.NonPublic)
        //                    .Where(
        //                     x =>
        //                        x.IsVirtual &&
        //                        (x.IsPublic || x.IsFamily)
        //                    ))
        //    {
        //        if (method.IsDefined(typeof(NonInterceptAttribute)))
        //        {
        //            continue;
        //        }
        //        if (!method.HasDefineInterceptAttribute() && !hasTypeIntercept)
        //        {
        //            continue;
        //        }

        //        _wrappers.Add(method.MetadataToken, new InterceptorWrapper
        //        {
        //            CallingInterceptors = method.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>(),
        //            CalledInterceptors = method.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>(),
        //            ExceptionInterceptor = method.GetCustomAttribute<ExceptionInterceptAttribute>(true)
        //        });
        //    }
        //}

        public InterceptorWrapperCollection(Type interfaceType, Type classType, Type proxyType)
            : this(classType, proxyType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (_wrappers.ContainsKey(0))
            {
                _wrappers[0].CallingInterceptors.AddRange(interfaceType.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>());
                _wrappers[0].CalledInterceptors.AddRange(interfaceType.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICalledInterceptor>());
                if (_wrappers[0].ExceptionInterceptor == null)
                {
                    _wrappers[0].ExceptionInterceptor = interfaceType.GetCustomAttribute<ExceptionInterceptAttribute>(true);
                }
            }
        }

        public InterceptorWrapperCollection(Type classType, Type proxyType)
        {
            if (classType == null)
            {
                throw new ArgumentNullException(nameof(classType));
            }

            _wrappers = new Dictionary<int, InterceptorWrapper>();
            var wrapper = new InterceptorWrapper
            {
                CallingInterceptors = classType.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>().ToList(),
                CalledInterceptors = classType.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>().ToList(),
                ExceptionInterceptor = classType.GetCustomAttribute<ExceptionInterceptAttribute>(true)
            };

            _wrappers.Add(0, wrapper);

            foreach (var methodHandle in HandleCollection.GetHandles(proxyType.MetadataToken))
            {
                var method = MethodBase.GetMethodFromHandle(methodHandle);
                _wrappers.Add(method.MetadataToken, new InterceptorWrapper
                {
                    CallingInterceptors = method.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>().ToList(),
                    CalledInterceptors = method.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>().ToList(),
                    ExceptionInterceptor = method.GetCustomAttribute<ExceptionInterceptAttribute>(true)
                });
            }
        }

        public InterceptorWrapper GetWrapper(int interfaceToken, int proxyToken)
        {
            var wrapper = new InterceptorWrapper();
            InitWrapper(proxyToken, wrapper);
            InitWrapper(interfaceToken, wrapper);
            InitWrapper(0, wrapper);
            return wrapper;
        }

        public InterceptorWrapper GetWrapper(int proxyToken)
        {
            var wrapper = new InterceptorWrapper();
            InitWrapper(proxyToken, wrapper);
            InitWrapper(0, wrapper);
            return wrapper;
        }

        private void InitWrapper(int token, InterceptorWrapper wrapper)
        {
            if (_wrappers.TryGetValue(token, out var value))
            {
                if (value != null)
                {
                    if (wrapper.CallingInterceptors == null)
                    {
                        wrapper.CallingInterceptors = new List<ICallingInterceptor>();
                    }

                    wrapper.CallingInterceptors.AddRange(value.CallingInterceptors);

                    if (wrapper.CalledInterceptors == null)
                    {
                        wrapper.CalledInterceptors = new List<ICalledInterceptor>();
                    }

                    wrapper.CalledInterceptors.AddRange(value.CalledInterceptors);

                    if (wrapper.ExceptionInterceptor == null)
                    {
                        wrapper.ExceptionInterceptor = value.ExceptionInterceptor;
                    }
                }
            }
        }

        public IEnumerator<KeyValuePair<int, InterceptorWrapper>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<int, InterceptorWrapper>>)_wrappers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<int, InterceptorWrapper>>)_wrappers).GetEnumerator();
        }
    }
}
