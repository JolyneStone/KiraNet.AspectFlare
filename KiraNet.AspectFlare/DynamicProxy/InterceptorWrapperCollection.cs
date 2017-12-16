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
        public InterceptorWrapperCollection(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _wrappers = new Dictionary<int, InterceptorWrapper>();
            var wrapper = new InterceptorWrapper<object>
            {
                CallingInterceptors = type.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>(),
                CalledInterceptors = type.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>(),
                ExceptionInterceptor = type.GetCustomAttribute<ExceptionInterceptAttribute>(true)
            };

            _wrappers.Add(0, wrapper);

            foreach (var ctor in type.GetConstructors(
                                BindingFlags.Instance |
                                BindingFlags.Public |
                                BindingFlags.NonPublic)
                            .Where(
                             x =>
                                x.IsVirtual &&
                                (x.IsPublic || x.IsFamily|| !(x.IsAssembly || x.IsFamilyAndAssembly || x.IsFamilyOrAssembly)) &&
                                x.HasInterceptAttribute()
                            ))
            {
                _wrappers.Add(ctor.MetadataToken, new InterceptorWrapper
                {
                    CallingInterceptors = ctor.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>(),
                    CalledInterceptors = ctor.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>(),
                    ExceptionInterceptor = ctor.GetCustomAttribute<ExceptionInterceptAttribute>(true)
                });
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance |
                                BindingFlags.Public |
                                BindingFlags.NonPublic)
                            .Where(
                             x =>
                                x.IsVirtual &&
                                (x.IsPublic || x.IsFamily) &&
                                x.HasInterceptAttribute()
                            ))
            {
                _wrappers.Add(method.MetadataToken, new InterceptorWrapper
                {
                    CallingInterceptors = method.GetCustomAttributes<CallingInterceptAttribute>(true).OfType<ICallingInterceptor>(),
                    CalledInterceptors = method.GetCustomAttributes<CalledInterceptAttribute>(true).OfType<ICalledInterceptor>(),
                    ExceptionInterceptor = method.GetCustomAttribute<ExceptionInterceptAttribute>(true)
                });
            }
        }

        public InterceptorWrapper GetWrapper(int token)
        {
            if (_wrappers.TryGetValue(token, out var value))
            {
                var super_wrapper = _wrappers[0];
                InterceptorWrapper wrapper;
                if (value != null)
                {
                    wrapper = new InterceptorWrapper
                    {
                        CallingInterceptors = value
                                    .CallingInterceptors.Concat(super_wrapper.CallingInterceptors),
                        CalledInterceptors = value
                                    .CalledInterceptors.Concat(super_wrapper.CalledInterceptors),
                        ExceptionInterceptor = value
                                    .ExceptionInterceptor ?? super_wrapper.ExceptionInterceptor
                    };
                }
                else
                {
                    wrapper = _wrappers[0];
                }

                return wrapper;
            }
            else
            {
                return _wrappers[0];
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
