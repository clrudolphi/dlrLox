namespace LoxOnDLR.Runtime
{
    internal class LoxProtoMethodGroup
    {
        public string Name { get; }
        internal Dictionary<int, LoxFunction> _functions = new ();
        public LoxProtoMethodGroup(string name, LoxFunction firstMethod) { 
            Name = name; 
            _functions.Add(firstMethod.Arity, firstMethod ); 
        }

        public LoxProtoMethodGroup(string name, LoxFunction firstMethod, LoxProtoMethodGroup inheritedMethodGroup)
        {
            Name = name;
            _functions =inheritedMethodGroup._functions.ToDictionary(x => x.Key, x => x.Value);
            _functions[firstMethod.Arity] = firstMethod;
        }
    }
}