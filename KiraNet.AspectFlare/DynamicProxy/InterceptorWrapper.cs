using System;
using System.Collections.Generic;
using System.Linq;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class InterceptorWrapper
    {
        public IEnumerable<ICallingInterceptor> CallingInterceptors { get; set; }
        public IEnumerable<ICalledInterceptor> CalledInterceptors { get; set; }
        public IExceptionInterceptor ExceptionInterceptor { get; set; }

        public InterceptResult CallingIntercepts(object owner, object[] parameters)
        {
            if (owner == null)
            {
                throw new System.ArgumentNullException(nameof(owner));
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

        public InterceptResult CalledIntercepts(object owner, object[] parameters, object returnValue)
        {
            if (owner == null)
            {
                throw new System.ArgumentNullException(nameof(owner));
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
                Parameters = parameters,
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

        public InterceptResult ExceptionIntercept(object owner, object[] parameters, object returnValue, Exception exception)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
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
