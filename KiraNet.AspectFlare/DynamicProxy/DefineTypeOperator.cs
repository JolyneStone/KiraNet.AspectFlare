using System;
using System.Reflection;

namespace KiraNet.AspectFlare.DynamicProxy
{
    internal class DefineTypeOperator : IGenerateOperator
    {
        public void Generate(GenerateTypeContext context)
        {
            var proxyType = context.ProxyType;
            if (context.InterfaceType == null)
            {
         
                var typeBuilder = context.ModuleBuilder.DefineType(
                        $"{proxyType.Name}_AspectFlare_DynamicProxy_{proxyType.MetadataToken}",
                        TypeAttributes.Public |
                        TypeAttributes.Class |
                        TypeAttributes.Sealed
                    );

                typeBuilder.SetParent(proxyType);
                context.TypeBuilder = typeBuilder;
            }
            else
            {
                context.TypeBuilder = context.ModuleBuilder.DefineType(
                         $"{proxyType.Name}_AspectFlare_DynamicProxy_{proxyType.MetadataToken}",
                         TypeAttributes.Public |
                         TypeAttributes.Class |
                         TypeAttributes.Sealed,
                         typeof(object),
                         new Type[] { context.InterfaceType }
                     );
            }
        }
    }
}
