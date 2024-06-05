using System.Linq.Expressions;

namespace LoxOnDLR.Runtime
{
    //--------------------------------------------
    // The reason we need this class is for the ToString override so that the Class Name is printed rather than the name of the function.
    internal class LoxClassConstructorFunction : LoxFunction
    {
        public LoxClassConstructorFunction(string name, int arity, LambdaExpression funcExpr) : base(name, arity, funcExpr)
        {
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}