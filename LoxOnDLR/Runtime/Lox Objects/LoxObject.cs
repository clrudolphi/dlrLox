using System.Dynamic;
using System.Runtime.CompilerServices;

namespace LoxOnDLR.Runtime
{
    internal class LoxObject : DynamicObject
    {
        private readonly string ClassName;
        private readonly LoxPrototype Prototype;
        private readonly Dictionary<string, object> Fields = new();
        private LoxPrototype SearchRoot;

        private Dictionary<string, LoxMethodGroup> InstanceClassMethodGroups;
        private Dictionary<string, LoxMethodGroup> InstanceSuperMethodGroups;

        public LoxObject(string className, LoxPrototype prototype)
        {
            ClassName = className;
            Prototype = prototype;
            SearchRoot = prototype;

            InstanceClassMethodGroups = prototype.ProtoMethodGroups.ToDictionary(x => x.Key, x => x.Value.Instantiate(this));
            InstanceSuperMethodGroups = prototype.SuperMethodGroups.ToDictionary(x => x.Key, x => x.Value.Instantiate(this)) ?? new();
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
            methodGroup = null;
            if (ClassName == searchRootClassName)
            {
                if (InstanceClassMethodGroups.TryGetValue(methodName, out var mg))
                {
                    methodGroup = mg;
                    return true;
                }
            }

            if (InstanceSuperMethodGroups.TryGetValue(methodName, out var smg))
            {
                methodGroup = smg;
                return true;
            }

            return false;
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