using System;
using System.Collections.Generic;
using System.Linq;

namespace KiraNet.AspectFlare.DynamicProxy
{
    //public class InterceptorWrapper<TResult>: InterceptorWrapper
    //{
    //    private TResult _result;
    //    public TResult Result
    //    {
    //        get
    //        {
    //            TResult result = _result;
    //            _result = default(TResult);
    //            return result;
    //        }
    //        set
    //        {
    //            _result = value;
    //        }
    //    }
    //}

    public class InterceptorWrapper
    {
        public IEnumerable<ICallingInterceptor> CallingInterceptors { get; set; }
        public IEnumerable<ICalledInterceptor> CalledInterceptors { get; set; }
        public IExceptionInterceptor ExceptionInterceptor { get; set; }

        private object _result;
        public object Result
        {
            get
            {
                object result = _result;
                _result = null;
                return result;
            }
            set
            {
                _result = value;
            }
        }

        public InterceptResult CallingIntercepts(object owner, string interceptedName, object[] parameters)
        {
            if (owner == null)
            {
                throw new System.ArgumentNullException(nameof(owner));
            }

            if (interceptedName == null)
            {
                throw new System.ArgumentNullException(nameof(interceptedName));
            }

            if (CallingInterceptors == null || !CallingInterceptors.Any())
            {
                return new InterceptResult
                {
                    HasResult = false
                };
            }

            var context = new CallingInterceptContext
            {
                Owner = owner,
                InterceptedName = interceptedName,
                Parameters = parameters,
                HasResult = false
            };

            foreach (var callingInterceptor in CallingInterceptors)
            {
                callingInterceptor.Calling(context);
                if(context.HasResult)
                {
                    return new InterceptResult
                    {
                        HasResult = true,
                        Result = context.Result
                    };
                }
            }

            return new InterceptResult
            {
                HasResult = false
            };
        }

        public InterceptResult CalledIntercepts(object owner, string interceptedName, object returnValue)
        {
            if (owner == null)
            {
                throw new System.ArgumentNullException(nameof(owner));
            }

            if (interceptedName == null)
            {
                throw new System.ArgumentNullException(nameof(interceptedName));
            }

            if (CalledInterceptors == null || !CalledInterceptors.Any())
            {
                return new InterceptResult
                {
                    HasResult = false
                };
            }

            var context = new CalledInterceptContext
            {
                Owner = owner,
                InterceptedName = interceptedName,
                ReturnValue = returnValue,
                HasResult = false
            };

            foreach (var calledInterceptor in CalledInterceptors)
            {
                calledInterceptor.Called(context);
                if (context.HasResult)
                {
                    return new InterceptResult
                    {
                        HasResult = false,
                        Result = context.Result
                    };
                }
            }

            return new InterceptResult
            {
                HasResult = false
            };
        }

        public InterceptResult ExceptionIntercept(object owner, string interceptedName, object[] parameters, object returnValue, Exception exception)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (interceptedName == null)
            {
                throw new ArgumentNullException(nameof(interceptedName));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (ExceptionInterceptor == null)
            {
                return new InterceptResult
                {
                    HasResult = false
                };
            }

            var context = new ExceptionInterceptContext
            {
                Owner = owner,
                InterceptedName = interceptedName,
                Parameters = parameters,
                ReturnValue = returnValue,
                Exception = exception,
                HasResult = false
            };

            ExceptionInterceptor.Exception(context);
            if(context.HasResult)
            {
                return new InterceptResult
                {
                    HasResult = true,
                    Result = context.Result
                };
            }
          
            return new InterceptResult
            {
                HasResult = false
            };
        }
    }
}
