using System.Dynamic;

namespace LoxOnDLR.Runtime
{
    internal class LoxMethodGroup : DynamicObject
    {
        public string Name { get; }
        private Dictionary<int, List<LoxInstanceMethod>> _methods = new Dictionary<int, List<LoxInstanceMethod>>();

        public LoxMethodGroup(string name, IEnumerable<LoxInstanceMethod> methods)
        {
            Name = name;
            foreach (var method in methods)
            {
                if (!_methods.TryGetValue(method.Arity, out var list))
                {
                    list = new List<LoxInstanceMethod>();
                    _methods.Add(method.Arity, list);
                }

                _methods[method.Arity].Add(method);
            }
        }

        public override string ToString()
        {
            return $"<fn {Name}>";
        }

        public bool TryResolve(int arity, out LoxInstanceMethod method)
        {
            bool found = false;
            method = null;

            if (_methods.TryGetValue(arity, out var methodlist))
            {
                if (methodlist.Count >= 1)
                {
                    method = methodlist[0];
                    found = true;
                }
                else
                {
                    method = null;
                    found = method != null;
                }
            }
            return found;
        }

        public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
        {
            return TryInvoke(args, out result);
        }

        internal bool TryInvoke(object?[]? args, out object? result)
        {
            LoxInstanceMethod meth = null;
            int numArgs = args == null ? 0 : args.Length;
            bool found = TryResolve(numArgs + 1, out meth);
            if (found)
            {
                result = meth.Invoke(args);
                return true;
            }
            else
            {
                var method = _methods.First().Value.First();
                throw new LoxRuntimeException($"Expected {method.Arity - 1} arguments but got {numArgs}.");
            }
        }

    }
}