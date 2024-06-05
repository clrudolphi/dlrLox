using System.Dynamic;
using System.Linq.Expressions;

namespace LoxOnDLR.Runtime.DLR_Binders
{
    internal static class LoxBinders
    {
        private static Dictionary<ExpressionType, LoxBinaryOperationBinder> _binOpBinders = new();
        private static Dictionary<string, LoxGetMemberBinder> _getBinders = new();
        private static Dictionary<string, LoxSetMemberBinder> _setBinders = new();
        private static Dictionary<CallInfo, LoxInvokeBinder> _invokeBinders = new();

        public static LoxBinaryOperationBinder GetBinaryOperationBinder(ExpressionType operation)
        {
            if (!_binOpBinders.ContainsKey(operation))
            {
                _binOpBinders[operation] = new LoxBinaryOperationBinder(operation);
            }
            var binder = _binOpBinders[operation];
            return binder;
        }

        public static LoxGetMemberBinder GetGetMemberBinder(string name, string? startSearchInClassName, bool ignoreCase)
        {
            var key = name + "/" + startSearchInClassName;
            if (!_getBinders.ContainsKey(key))
            {
                _getBinders[key] = new LoxGetMemberBinder(name, startSearchInClassName, ignoreCase);
            }
            var binder = _getBinders[key];
            return binder;
        }

        public static LoxSetMemberBinder GetSetMemberBinder(string name)
        {
            if (!_setBinders.ContainsKey(name))
            {
                _setBinders[name] = new LoxSetMemberBinder(name);
            }
            var binder = _setBinders[name];
            return binder;
        }

        public static LoxInvokeBinder GetInvokeBinder(CallInfo callinfo)
        {
            if (!_invokeBinders.ContainsKey(callinfo))
            {
                _invokeBinders[callinfo] = new LoxInvokeBinder(callinfo);
            }
            return _invokeBinders[callinfo];
        }
    }
}