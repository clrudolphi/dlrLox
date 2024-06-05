using System.Dynamic;
using System.Linq.Expressions;

namespace LoxOnDLR.Runtime
{
    public class LoxFunction : DynamicObject
    {
        public int Arity { get; private set; }
        public string Name { get; private set; }

        public Delegate Func
        {
            get; set;
        }

        public LoxFunction(string name, int arity, LambdaExpression funcExpr)
        {
            Arity = arity;
            Name = name;
            Func = funcExpr.Compile();
        }

        public LoxFunction(string name, int arity, Delegate func)
        {
            Arity = arity;
            Name = name;
            Func = func;
        }

        public virtual object Invoke(object[] args)
        {
            return Func.DynamicInvoke(args);
        }

        public override string ToString()
        {
            return $"<fn {Name}>";
        }

        public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object result)
        {
            bool success = false;
            result = null;
            int numArgs = args == null ? 0 : args.Length;
            if (numArgs == Arity)
            {
                result = Invoke(args);
                success = true;
            }

            return success;
        }
    }
}