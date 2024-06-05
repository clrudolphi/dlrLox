using System.Dynamic;
using System.Linq.Expressions;

namespace LoxOnDLR.Runtime
{
    public class LoxBinaryOperationBinder : BinaryOperationBinder
    {
        public LoxBinaryOperationBinder(ExpressionType operation)
            : base(operation)
        {
        }

        public override DynamicMetaObject FallbackBinaryOperation(
                    DynamicMetaObject target, DynamicMetaObject arg,
                    DynamicMetaObject errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue || !arg.HasValue)
            {
                return Defer(target, arg);
            }
            var restrictions = target.Restrictions.Merge(arg.Restrictions)
                .Merge(BindingRestrictions.GetTypeRestriction(
                    target.Expression, target.LimitType))
                .Merge(BindingRestrictions.GetTypeRestriction(
                    arg.Expression, arg.LimitType));

            switch ((this.Operation, target.LimitType, arg.LimitType))
            {
                case (ExpressionType.Add, Type t, Type a) when t == typeof(string) && a == typeof(string):
                    return new DynamicMetaObject(
                                   RuntimeHelpers.EnsureObjectResult(
                                     Expression.Call(
                                        typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) }),
                                        Expression.Convert(target.Expression, target.LimitType),
                                        Expression.Call(arg.Expression, "ToString", null, Array.Empty<Expression>()))),
                                    restrictions
                                );
                    break;

                case (ExpressionType.Divide, Type dt, Type da) when dt == typeof(Double) && da == typeof(Double):
                case (ExpressionType.Multiply, Type mt, Type ma) when mt == typeof(Double) && ma == typeof(Double):
                case (ExpressionType.Subtract, Type st, Type sa) when st == typeof(Double) && sa == typeof(Double):
                case (ExpressionType.Add, Type at, Type aa) when at == typeof(Double) && aa == typeof(Double):
                case (ExpressionType.GreaterThan, Type gt, Type ga) when gt == typeof(Double) && ga == typeof(Double):
                case (ExpressionType.GreaterThanOrEqual, Type gte, Type gae) when gte == typeof(Double) && gae == typeof(Double):
                case (ExpressionType.LessThan, Type lt, Type la) when lt == typeof(Double) && la == typeof(Double):
                case (ExpressionType.LessThanOrEqual, Type lte, Type lae) when lte == typeof(Double) && lae == typeof(Double):
                case (ExpressionType.GreaterThan, Type gst, Type gsa) when gst == typeof(string) && gsa == typeof(string):
                case (ExpressionType.GreaterThanOrEqual, Type gste, Type gsea) when gste == typeof(string) && gsea == typeof(string):
                case (ExpressionType.LessThan, Type lts, Type lsa) when lts == typeof(string) && lsa == typeof(string):
                case (ExpressionType.LessThanOrEqual, Type lste, Type lsea) when lste == typeof(string) && lsea == typeof(string):
                case (ExpressionType.Equal, Type es, Type ea) when es == ea:
                case (ExpressionType.NotEqual, Type ne, Type na) when ne == na:
                    return new DynamicMetaObject(
                        RuntimeHelpers.EnsureObjectResult(
                            Expression.MakeBinary(
                                this.Operation,
                                Expression.Convert(target.Expression, target.LimitType),
                                Expression.Convert(arg.Expression, arg.LimitType))),
                        restrictions
                    );
                    break;

                case (ExpressionType.Divide, _, _):
                case (ExpressionType.Multiply, _, _):
                case (ExpressionType.Subtract, _, _):
                case (ExpressionType.GreaterThan, Type gtn, Type gta) when gtn == typeof(Double) || gta == typeof(Double):
                case (ExpressionType.GreaterThanOrEqual, Type gten, Type gtae) when gten == typeof(Double) || gtae == typeof(Double):
                case (ExpressionType.LessThan, Type ltn, Type lta) when ltn == typeof(Double) || lta == typeof(Double):
                case (ExpressionType.LessThanOrEqual, Type lten, Type ltae) when lten == typeof(Double) || ltae == typeof(Double):
                case (ExpressionType.Equal, Type eqn, Type eqa) when eqn == typeof(Double) || eqa == typeof(Double):
                case (ExpressionType.NotEqual, Type neq, Type nea) when neq == typeof(Double) || nea == typeof(Double):
                    return RuntimeHelpers.CreateThrow(
                        target,
                        new[] { arg },
                        restrictions,
                        typeof(LoxRuntimeException),
                        "Operands must be numbers.");
                    break;

                case (ExpressionType.Add, _, _):
                case (ExpressionType.GreaterThan, _, _):
                case (ExpressionType.GreaterThanOrEqual, _, _):
                case (ExpressionType.LessThan, _, _):
                case (ExpressionType.LessThanOrEqual, _, _):
                case (ExpressionType.Equal, _, _):
                case (ExpressionType.NotEqual, _, _):
                    return RuntimeHelpers.CreateThrow(
                        target,
                        new[] { arg },
                        restrictions,
                        typeof(LoxRuntimeException),
                        "Operands must be two numbers or two strings.");
                    break;

                default:
                    return new DynamicMetaObject(
                        RuntimeHelpers.EnsureObjectResult(
                            Expression.MakeBinary(
                                this.Operation,
                                Expression.Convert(target.Expression, target.LimitType),
                                Expression.Convert(arg.Expression, arg.LimitType))),
                        restrictions
                    );
                    break;
            }
        }
    }
}