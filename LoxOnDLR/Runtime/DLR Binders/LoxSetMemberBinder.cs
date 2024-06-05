using System.Dynamic;

namespace LoxOnDLR.Runtime
{
    // LoxSetMemberBinder is used for general dotted expressions for setting
    // members.
    //
    public class LoxSetMemberBinder : SetMemberBinder
    {
        public LoxSetMemberBinder(string name)
            : base(name, true)
        {
        }

        public override DynamicMetaObject FallbackSetMember(
                DynamicMetaObject targetMO, DynamicMetaObject value,
                DynamicMetaObject errorSuggestion)
        {
            DynamicMetaObject result;
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!targetMO.HasValue) return Defer(targetMO);
            return errorSuggestion ??
                RuntimeHelpers.CreateThrow(
                    targetMO, null,
                    BindingRestrictions.GetTypeRestriction(targetMO.Expression,
                                                           targetMO.LimitType),
                    typeof(LoxRuntimeException),
                     $"Can't set member '{Name}'.");
        }
    }
}