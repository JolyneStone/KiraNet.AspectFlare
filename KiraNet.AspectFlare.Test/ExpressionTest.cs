using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace KiraNet.AspectFlare.Test
{
    public class ExpressionTest
    {
        [Fact]
        public void Replace()
        {
            Assert.True(typeof(IBar).IsAssignableFrom(typeof(Bar)));
            Expression<Func<object>> expression = () => new BarBase(1);
            Assert.False(expression.ReturnType == typeof(FooBase));
            NewExpression newExpression = expression.Body as NewExpression;

            var expressionVistor = new ProxyExpressionVisitor();
            var foo = (expressionVistor.Visit(expression) as Expression<Func<object>>).Compile().Invoke() as Bar;
            Assert.Equal(1, foo.X);
        }
    }

    public class ProxyExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitNew(NewExpression node)
        {
            Assert.True(node.Type == typeof(BarBase));
            var fooType = typeof(Bar);
            var arguments = node.Arguments.Select(x => x.Type).ToArray();
            ConstructorInfo fooconstructor = fooType.GetConstructors()
                                .Where(constructor =>
                                {
                                    var parameters = constructor.GetParameters().Select(x => x.ParameterType).ToArray();
                                    if (parameters.Length != arguments.Length)
                                    {
                                        return false;
                                    }

                                    if (parameters.Length == 0)
                                    {
                                        return true;
                                    }

                                    for (int i = 0; i < arguments.Length; i++)
                                    {
                                        if (parameters[i] != arguments[i])
                                            return false;
                                    }

                                    return true;
                                }).FirstOrDefault();
            var newfoo = Expression.New(fooconstructor, node.Arguments);
            return newfoo;
        }
    }

    public interface IBar
    {

    }

    public class BarBase
    {
        public BarBase(int x)
        {
            X = x;
        }
        public int X { get; }
    }

    public class Bar : BarBase, IBar
    {
        public Bar(int x) : base(x) { }
    }
}
