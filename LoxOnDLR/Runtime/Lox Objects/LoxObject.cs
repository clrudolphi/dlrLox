using System.Dynamic;
using System.Runtime.CompilerServices;

namespace LoxOnDLR.Runtime
{
    internal class LoxObject : DynamicObject
    {
        private readonly string ClassName;
        private readonly LoxPrototype Prototype;
        private readonly Dictionary<string, object> Fields = new();
        private Dictionary<MethodKey, LoxMethodGroup> cachedMethodGroups = new();

        public LoxObject(string className, LoxPrototype prototype)
        {
            ClassName = className;
            Prototype = prototype;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var methodName = binder.Name;

            if (binder is LoxGetSuperMemberBinder)
            {
                var searchRootClassName = (binder as LoxGetSuperMemberBinder)?.StartSearchInClassName ?? ClassName;
                // using the prototype's heirarchy, find all methods with the given name at or after the searchRoot className is found
                // make them into LoxInstanceMethods and attach them to a LoxMethodGroup
                // return the resulting LoxMethodGroup

                if (TryGetMethodGroup(methodName, searchRootClassName, out var methodGroup))
                {
                    result = methodGroup;
                    return true;
                }
                result = null;
                return false;
            }

            // First check fields as fields may shadow methods
            if (Fields.TryGetValue(binder.Name, out result))
            {
                 return true; 
            }


            if (Prototype.ProtoMethodGroups.TryGetValue(methodName, out var lpmg))
            {
                result = new LoxMethodGroup(this, methodName, lpmg[Prototype.Depth]);
                return true;
            }
            result = null;

            return false;
        }

        internal bool TryGetMethodGroup(string methodName, string searchRootClassName, out object methodGroup)
        {
            methodGroup = null;
            methodGroup = Prototype.FindMethodGroup(methodName, searchRootClassName);
            if (methodGroup == null) { return false; }
            methodGroup = new LoxMethodGroup(this, methodName, methodGroup as LoxProtoMethodGroup);

            return methodGroup != null;
        }

        // Note: can't climb up the chain of base classes for Setting members; can only set a member in the current class
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            Fields[binder.Name] = value;
            return true;
        }

        public override string ToString()
        { return ClassName + " instance"; }

        internal void InvokeInitFunction(object[] initargs)
        {
            var initFound = TryGetMethodGroup("init", this.ClassName, out var mg);
            if (initFound != null)
            {
                var initMethodGroup = mg as LoxMethodGroup;
                if (initMethodGroup != null)
                {
                    initMethodGroup.TryInvoke(initargs, out var initResult);
                }
            }
        }
    }
}