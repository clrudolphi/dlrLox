using System.Dynamic;

namespace LoxOnDLR.Runtime
{
    internal class LoxMethodGroup : DynamicObject
    {
        public string Name { get; }
        private LoxProtoMethodGroup _methods;
        private LoxObject Instance { get; set; }

        public LoxMethodGroup(LoxObject obj,string name, LoxProtoMethodGroup methods)
        {
            Instance = obj;
            Name = name;
            _methods = methods;
        }

        public override string ToString()
        {
            return $"<fn {Name}>";
        }

        public bool TryResolve(int arity, out LoxFunction method)
        {
            return _methods._functions.TryGetValue(arity, out method);
        }

        public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
        {
            return TryInvoke(args, out result);
        }

        internal bool TryInvoke(object?[]? args, out object? result)
        {
            //guard:: the Instance object must not be null
            if (Instance == null)
            {
                throw new LoxRuntimeException("Cannot invoke method on null instance");
            }
            int numArgs = args?.Length ?? 0;

            LoxFunction meth = null;
            bool found = TryResolve(numArgs + 1 , out meth);
            if (found)
            {
                // prepend the instance
                object[] argsCopy = new object[numArgs + 1];
                argsCopy[0] = Instance;
                args?.CopyTo(argsCopy, 1);

                result = meth.Invoke(argsCopy);
                return true;
            }
            else
            {
                var methodArity = _methods._functions.Keys.First();
                throw new LoxRuntimeException($"Expected {methodArity - 1} arguments but got {numArgs}.");
            }
        }

    }
}