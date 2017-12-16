using System.Reflection;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IMethodGenerator
    {
        void GeneratorMethod(
            GenerateTypeContext context,
            ILGenerator methodGenerator,
            MethodBase method,
            ParameterInfo[] parameters);
    }
}
