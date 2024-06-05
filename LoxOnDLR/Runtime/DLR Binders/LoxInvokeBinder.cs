using System.Dynamic;

namespace LoxOnDLR.Runtime
{
    public class LoxInvokeBinder : InvokeBinder
    {
        public LoxInvokeBinder(CallInfo callinfo) : base(callinfo)
        {
        }

        public override DynamicMetaObject FallbackInvoke(
                DynamicMetaObject targetMO, DynamicMetaObject[] argMOs,
                DynamicMetaObject errorSuggestion)
        {
            DynamicMetaObject result;
            int parmsLength = 0;
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!targetMO.HasValue || argMOs.Any((a) => !a.HasValue))
            {
                var deferArgs = new DynamicMetaObject[argMOs.Length + 1];
                for (int i = 0; i < argMOs.Length; i++)
                {
                    deferArgs[i + 1] = argMOs[i];
                }
                deferArgs[0] = targetMO;
                return Defer(deferArgs);
            }
            if (targetMO.Value is LoxFunction)
            {
                parmsLength = ((LoxFunction)(targetMO.Value)).Arity;
            }

            return errorSuggestion ??
                RuntimeHelpers.CreateThrow(
                    targetMO, argMOs,
                    BindingRestrictions.GetTypeRestriction(targetMO.Expression,
                                                           targetMO.LimitType),
                    typeof(LoxRuntimeException),
                    $"Expected {parmsLength} arguments but got {argMOs.Length}.");
        }
    }
}