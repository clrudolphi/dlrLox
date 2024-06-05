using System.Dynamic;

namespace LoxOnDLR.Runtime
{
    internal class LoxObject : DynamicObject
    {
        private readonly string ClassName;
        private readonly LoxPrototype Prototype;
        private readonly Dictionary<string, object> Fields = new();
        private LoxPrototype SearchRoot;

        public LoxObject(string className, LoxPrototype prototype)
        {
            ClassName = className;
            Prototype = prototype;
            SearchRoot = prototype;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var methodName = binder.Name;
            var searchRootClassName = (binder as LoxGetMemberBinder)?.StartSearchInClassName ?? ClassName;

            // First check fields as fields may shadow methods
            if (Fields.TryGetValue(binder.Name, out result)) { return true; }

            // using the prototype's heirarchy, find all methods with the given name at or after the searchRoot className is found
            // make them into LoxInstanceMethods and attach them to a LoxMethodGroup
            // return the resulting LoxMethodGroup

            if (TryGetMethodGroup(methodName, searchRootClassName, out var methodGroup))
            {
                result = methodGroup;
                return true;
            }

            throw new LoxRuntimeException($"Undefined property '{binder.Name}'.");
        }

        internal bool TryGetMethodGroup(string methodName, string searchRootClassName, out object methodGroup)
        {
            List<LoxInstanceMethod> lmg = new();
            bool foundRootClass = false;
            foreach (var proto in Prototype.ProtoClassHeirarchy())
            {
                if (proto.ClassName == searchRootClassName)
                {
                    foundRootClass = true;
                }
                if (foundRootClass)
                {
                    if (proto.ProtoMethods.TryGetValue(methodName, out var lf))
                    {
                        var li = new LoxInstanceMethod(this, lf);
                        lmg.Add(li);
                    }
                }
            }
            if (lmg.Count > 0)
            {
                methodGroup = new LoxMethodGroup(methodName, lmg);
                return true;
            }
            else
            {
                methodGroup = null;
                return false;
            }
        }

        // Note: can't climb up the chain of base classes for Setting members; can only set a member in the current class
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            Fields[binder.Name] = value;
            return true;
        }

        public override string ToString()
        { return ClassName + " instance"; }
    }
}