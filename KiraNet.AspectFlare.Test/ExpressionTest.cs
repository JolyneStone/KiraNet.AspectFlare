//using System;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Reflection;
//using Xunit;

//namespace KiraNet.AspectFlare.Test
//{
//    public class ExpressionTest
//    {
//        [Fact]
//        public void Replace()
//        {
//            Expression<Func<object>> expression = () => new BarBase(1);
//            Assert.False(expression.ReturnType == typeof(FooBase));
//            NewExpression newExpression = expression.Body as NewExpression;

//            var expressionVistor = new ProxyExpressionVisitor();
//            var foo = (expressionVistor.Visit(expression) as Expression<Func<object>>).Compile().Invoke() as Bar;
//            Assert.Equal(1, foo.X);
//        }

//        [Fact]
//        public void Test()
//        {
//            var parameters = typeof(ExpressionTest).GetMethod("Te").GetParameters();
//            Assert.True(parameters.Count() == 2);

//            var parame = typeof(CallClass).GetMethod("Func").GetParameters()[0];
//            var p = parame.GetCustomAttribute<ExceptionAttribute>(true);
//        }

//        public void Te(ref int x, out int y)
//        {
//            y = 1;
//        }

//        [Fact]
//        public void FuncExpressionTest()
//        {
//            Type type = typeof(CallClass);
//            MethodInfo method = type.GetMethod("Func");
//            ParameterInfo[] parameterInfos = method.GetParameters();
//            object[] parameterValues = new object[] { 1, 2 };
//            ParameterExpression baseClass = Expression.Parameter(type, "baseClass");
//            ParameterExpression arguments = Expression.Parameter(typeof(object[]), "arguments");

//            Expression[] parameters = new Expression[parameterInfos.Length];
//            for (int i = 0; i < parameterInfos.Length; i++)
//            {
//                BinaryExpression getElementByIndex = Expression.ArrayIndex(arguments, Expression.Constant(i));
//                UnaryExpression convertToParameterType = Expression.Convert(getElementByIndex, parameterInfos[i].ParameterType);
//                parameters[i] = convertToParameterType;
//            }


//            UnaryExpression instanceCast = Expression.Convert(baseClass, type);
//            MethodCallExpression methodCall = Expression.Call(instanceCast, method, parameters);

//            Type returnType = method.ReturnType;
//            UnaryExpression convertToObjectType = Expression.Convert(methodCall, returnType);
//            var actionFunc = Expression.Lambda<Func<CallClass, object[], Exception>>(convertToObjectType, baseClass, arguments).Compile();

//            var x = new Exception();
//            Exception y = null;
//            var result = actionFunc(new CallClass(), new object[] { x, y });
//        }
//    }

//    public class ClassBaseClass
//    {
//        public virtual Exception Func(ref Exception x, out Exception y)
//        {
//            y = null;
//            return new Exception("test", x);
//        }
//    }

//    public class CallClass : ClassBaseClass
//    {
//        public override Exception Func(ref Exception x, out Exception y)
//        {
//            return base.Func(ref x, out y);
//        }

//        public Exception Funcs(Exception x)
//        {
//            Exception y;
//            return this.Func(ref x, out y);
//        }

//        public Func<Exception> GetFunc()
//        {
//            var x = new Exception();
//            Exception y;
//            return () => base.Func(ref x, out y);
//        }
//    }

//    public class ProxyExpressionVisitor : ExpressionVisitor
//    {
//        protected override Expression VisitNew(NewExpression node)
//        {
//            Assert.True(node.Type == typeof(BarBase));
//            var fooType = typeof(Bar);
//            var arguments = node.Arguments.Select(x => x.Type).ToArray();
//            ConstructorInfo fooconstructor = fooType.GetConstructors()
//                                .Where(constructor =>
//                                {
//                                    var parameters = constructor.GetParameters().Select(x => x.ParameterType).ToArray();
//                                    if (parameters.Length != arguments.Length)
//                                    {
//                                        return false;
//                                    }

//                                    if (parameters.Length == 0)
//                                    {
//                                        return true;
//                                    }

//                                    for (int i = 0; i < arguments.Length; i++)
//                                    {
//                                        if (parameters[i] != arguments[i])
//                                            return false;
//                                    }

//                                    return true;
//                                }).FirstOrDefault();
//            var newfoo = Expression.New(fooconstructor, node.Arguments);
//            return newfoo;
//        }
//    }

//    public interface IBar
//    {

//    }

//    public class BarBase
//    {
//        public BarBase(int x)
//        {
//            X = x;
//        }
//        public int X { get; }
//    }

//    public class Bar : BarBase, IBar
//    {
//        public Bar(int x) : base(x) { }
//    }
//}
