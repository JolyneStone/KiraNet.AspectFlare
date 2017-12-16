using System.Reflection;
using System.Threading.Tasks;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IInterceptorInvoker
    {
        object InvokeIntercept(object proxy, MethodBase method, object[] parameters);
        Task<object> InvokeInterceptAsync(object proxy, MethodBase method, object[] parameters);
    }
}
