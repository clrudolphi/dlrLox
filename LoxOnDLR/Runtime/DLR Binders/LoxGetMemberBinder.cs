﻿using System.Dynamic;

namespace LoxOnDLR.Runtime
{
    public class LoxGetMemberBinder : GetMemberBinder
    {

        public LoxGetMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase)
        {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject targetMO, DynamicMetaObject? errorSuggestion)
        {
            // This will cause the TryGetMember to be called on the DynamicObject and if that fails, it will throw this exception
            return errorSuggestion ??
                RuntimeHelpers.CreateThrow(
                    targetMO, null,
                    BindingRestrictions.GetTypeRestriction(targetMO.Expression,
                                                           targetMO.LimitType),
                    typeof(LoxRuntimeException),
                    "Undefined property '" + Name + "'.");
        }
    }
}