using System.Linq.Expressions;

namespace LoxOnDLR.Runtime
{
    internal class LoxNativeFunction : LoxFunction
    {
        public LoxNativeFunction(string name, int arity, LambdaExpression funcExpr) : base(name, arity, funcExpr)
        {
        }

        public override string ToString()
        {
            return "<native fn>";
        }
    }
}