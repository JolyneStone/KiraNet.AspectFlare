using System;
using System.Threading.Tasks;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public sealed class Caller
    {
        private InterceptorWrapper _wrapper;
        private MethodType _methodType;
        public Caller(InterceptorWrapper wrapper, MethodType methodType)
        {
            _wrapper = wrapper;
            _methodType = methodType;
        }

        public async void Call<TResult>(object owner, string interceptedName, Func<object> caller, object[] parameters)
        {
            if(_wrapper == null)
            {
                _wrapper.Result = caller();
                return;
            }

            InterceptResult result;
            object methodResult = null;
            try
            {
                result = _wrapper.CallingIntercepts(this, interceptedName, parameters);
                if (result.HasResult)
                {
                    _wrapper.Result = await CallResult<TResult>(result.Result);
                    return;
                }

                // 调用基类方法
                methodResult = await CallResult<TResult>(caller());

                result = _wrapper.CalledIntercepts(this, interceptedName, methodResult);
                if (result.HasResult)
                {
                    _wrapper.Result = await CallResult<TResult>(result.Result);
                    return;
                }

                _wrapper.Result = methodResult;
            }
            catch (Exception ex)
            {
                result = _wrapper.ExceptionIntercept(this, interceptedName, parameters, null, ex);
                if (!result.HasResult)
                {
                    throw ex;
                }

                _wrapper.Result = await CallResult<TResult>(result.Result);
            }
        }

        private async Task<object> CallResult<TResult>(object result)
        {
            object realResult;
            switch(_methodType)
            {
                case MethodType.Type:
                    realResult = result;
                    break;
                case MethodType.AsyncTask:
                    await (Task)result;
                    realResult = null;
                    break;
                case MethodType.AsyncTaskOfType:
                    realResult = await (Task<TResult>)result;
                    break;
                case MethodType.AsyncValueTaskOfType:
                    realResult = await (ValueTask<TResult>)result;
                    break;
                case MethodType.None:
                default:
                    realResult = null;
                    break;
            }

            return realResult;
        }
    }
}
