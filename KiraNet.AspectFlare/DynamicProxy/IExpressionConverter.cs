using System;
using System.Linq.Expressions;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IExpressionConverter<TOther, TProxy>
            where TOther : class
            where TProxy : class
    {
        bool TryConvert(Expression<Func<TOther, TProxy>> lambdaExpression, Type rawType, Type implementType, out Expression<Func<TOther, TProxy>> convertLambdaExpression);
    }
}
