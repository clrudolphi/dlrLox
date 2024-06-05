using Lox;
using LoxOnDLR.Infrastructure;
using LoxOnDLR.Runtime;
using System.Linq.Expressions;

namespace LoxOnDLR.Pipeline
{
    internal class GeneratorHelpers
    {
        public static Expression TruthyTest(Expression expr)
        {
            return Expression.Condition(
                        Expression.TypeIs(expr, typeof(bool)),
                        Expression.Convert(expr, typeof(bool)),
                        Expression.And(
                            Expression.NotEqual(
                               expr,
                               Expression.Constant(null, typeof(object))),
                             Expression.NotEqual(
                               expr,
                               Expression.Constant(Nothing.Instance))
                        ));
        }

        public static Expression Checked(Expression expr, int soureLine)
        {
            var lre = Expression.Variable(typeof(LoxRuntimeException), "lre");
            return Expression.TryCatch(
                expr,
                Expression.Catch(lre,
                        Expression.Block(
                            Expression.Throw(Expression.Call(lre, typeof(LoxRuntimeException).GetMethod("AtLine"), Expression.Constant(soureLine))),
                            Expression.Constant(null)
                        )
                )
            );
        }

        internal static IEnumerable<Expression> GenerateBuiltInFunctions(SymbolTableScope scope)
        {
            var assignClockFunctionToLabel = GenerateClockNativeFunction(scope);
            var builtIns = new List<Expression>
                {
                    assignClockFunctionToLabel
                };
            return builtIns;
        }

        private static Expression GenerateClockNativeFunction(SymbolTableScope scope)
        {
            var _clockFunction = Expression.Parameter(typeof(LoxNativeFunction), "_Clock");
            scope.Names.Add("clock", _clockFunction);
            var clockReturnLabel = Expression.Label(typeof(double), "ClockReturn");
            var clockFunctionBody = Expression.Block(
                new Expression[] {  Expression.Return(clockReturnLabel,
                                            Expression.Call(null,typeof(LoxRuntimeHelpers).GetMethod("Clock", Type.EmptyTypes ), Array.Empty<Expression>())),
                                    Expression.Label(clockReturnLabel, Expression.Constant(0.0)) }
                );
            var clockLambdaExpr = Expression.Lambda(
                clockFunctionBody,
                "clock",
                new ParameterExpression[] { });
            var clockNativeFuncExpr = Expression.New(typeof(LoxNativeFunction).GetConstructor(new Type[] { typeof(string), typeof(int), typeof(LambdaExpression) }),
                                                    new Expression[] { Expression.Constant("clock"), Expression.Constant(0), clockLambdaExpr });
            var assignClockFunctionToLabel = Expression.Assign(_clockFunction, clockNativeFuncExpr);
            return assignClockFunctionToLabel;
        }
    }
}