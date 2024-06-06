namespace LoxOnDLR.Runtime
{
    //--------------------------------------------
    internal class LoxInstanceMethod : LoxFunction
    {
        public LoxObject Instance { get; private set; }

        //public LoxInstanceMethod(string name, int arity, LambdaExpression funcExpr) : base(name, arity, funcExpr)
        //{
        //}
        public LoxInstanceMethod(LoxObject obj, LoxFunction func) : base(func.Name, func.Arity, func.Func)
        {
            Instance = obj;
        }

        //public LoxInstanceMethod SetInstance(LoxObject obj)
        //{
        //    var instantiated = new LoxInstanceMethod(Name, Arity, FuncExpr);
        //    instantiated.Instance = obj;
        //    return instantiated;
        //}

        public override object Invoke(object[] args)
        {
            // prepend the instance
            int numArgs = args.Length;
            object[] argsCopy = new object[numArgs + 1];
            argsCopy[0] = Instance;
            args.CopyTo(argsCopy, 1);
            
            return base.Invoke(argsCopy);
        }

        //public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object result)
        //{
        //    result = null;
        //    bool success = false;

        //    int numArgs = args == null ? 0 : args.Length;
        //    if (numArgs == Arity)
        //    {
        //        result = Invoke(args);
        //        success = true;
        //    }

        //    return success;
        //}
    }
}