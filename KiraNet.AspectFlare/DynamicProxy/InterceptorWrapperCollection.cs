using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KiraNet.AspectFlare.Utilities;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class InterceptorWrapperCollection : IEnumerable<KeyValuePair<int, InterceptorWrapper>>
    {
        private readonly Dictionary<int, InterceptorWrapper> _wrappers;

        public InterceptorWrapperCollection(Type interfaceType, Type classType, Type proxyType)
            : this(classType, proxyType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (_wrappers.ContainsKey(0))
            {
                _wrappers[0].CallingInterceptors.AddRange(interfaceType
                    .GetCustomAttributes<CallingInterceptAttribute>(true)
                    .OfType<ICallingInterceptor>()
                    .OrderByDescending(x => x.Order));

                _wrappers[0].CalledInterceptors.AddRange(interfaceType
                    .GetCustomAttributes<CallingInterceptAttribute>(true)
                    .OfType<ICalledInterceptor>()
                    .OrderByDescending(x => x.Order));

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

            var globalInterceptors = GlobalInterceptorCollection.GlobalInterceptors;
            var globalWrapper = new InterceptorWrapper
            {
                CallingInterceptors = globalInterceptors.GetCallingInterceptors(),
                CalledInterceptors = globalInterceptors.GetCalledInterceptors(),
                ExceptionInterceptor = globalInterceptors.GetExceptionInterceptor()
            };

            _wrappers.Add(-1, globalWrapper);

            var typeWrapper = new InterceptorWrapper
            {
                CallingInterceptors = classType
                    .GetCustomAttributes<CallingInterceptAttribute>(true)
                    .OfType<ICallingInterceptor>()
                    .OrderByDescending(x => x.Order)
                    .ToList(),

                CalledInterceptors = classType
                .GetCustomAttributes<CalledInterceptAttribute>(true)
                .OfType<ICalledInterceptor>()
                .OrderByDescending(x => x.Order)
                .ToList(),

                ExceptionInterceptor = classType.GetCustomAttribute<ExceptionInterceptAttribute>(true)
            };

            _wrappers.Add(0, typeWrapper);

            foreach (var methodHandle in HandleCollection.GetHandles(proxyType.MetadataToken))
            {
                var method = MethodBase.GetMethodFromHandle(methodHandle);
                _wrappers.Add(method.MetadataToken, new InterceptorWrapper
                {
                    CallingInterceptors = method
                        .GetCustomAttributes<CallingInterceptAttribute>(true)
                        .OfType<ICallingInterceptor>()
                        .OrderByDescending(x=>x.Order)
                        .ToList(),

                    CalledInterceptors = method
                        .GetCustomAttributes<CalledInterceptAttribute>(true)
                        .OfType<ICalledInterceptor>()
                        .OrderByDescending(x=>x.Order)
                        .ToList(),

                    ExceptionInterceptor = method.GetCustomAttribute<ExceptionInterceptAttribute>(true)
                });
            }
        }

        public InterceptorWrapper GetWrapper(int interfaceToken, int proxyToken)
        {
            var wrapper = new InterceptorWrapper();
            AppendWrapper(proxyToken, wrapper);
            AppendWrapper(interfaceToken, wrapper);
            AppendWrapper(0, wrapper);
            AppendWrapper(-1, wrapper);
            return wrapper;
        }

        public InterceptorWrapper GetWrapper(int proxyToken)
        {
            var wrapper = new InterceptorWrapper();
            AppendWrapper(proxyToken, wrapper);
            AppendWrapper(0, wrapper);
            AppendWrapper(-1, wrapper);
            return wrapper;
        }

        private void AppendWrapper(int token, InterceptorWrapper wrapper)
        {
            if (_wrappers.TryGetValue(token, out var value))
            {
                if (value != null)
                {
                    if (wrapper.CallingInterceptors == null)
                    {
                        wrapper.CallingInterceptors = new List<ICallingInterceptor>();
                    }

                    wrapper.CallingInterceptors.Concat(value.CallingInterceptors);

                    if (wrapper.CalledInterceptors == null)
                    {
                        wrapper.CalledInterceptors = new List<ICalledInterceptor>();
                    }

                    wrapper.CalledInterceptors.Concat(value.CalledInterceptors);

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
