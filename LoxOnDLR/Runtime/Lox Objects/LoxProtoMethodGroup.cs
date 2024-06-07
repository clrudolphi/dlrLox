namespace LoxOnDLR.Runtime
{
    internal class LoxProtoMethodGroup
    {
        public string Name { get; }
        internal Dictionary<int, List<LoxFunction>> _functions = new Dictionary<int, List<LoxFunction>>();
        public LoxProtoMethodGroup(string name) { Name = name; }
        public LoxProtoMethodGroup(string name, LoxFunction firstMethod) { 
            Name = name; 
            _functions.Add(firstMethod.Arity, new List<LoxFunction>() { firstMethod }); 
        }

        public void AddMethod(LoxFunction func)
        {
            if (!_functions.TryGetValue(func.Arity, out var list))
            {
                list = new List<LoxFunction>();
                _functions.Add(func.Arity, list);
            }
            list.Add(func);
        }
        public LoxMethodGroup Instantiate(LoxObject obj)
        {
            return new LoxMethodGroup(Name, _functions.SelectMany(x => x.Value.Select(y => new LoxInstanceMethod(obj, y))));
        }
    }
}