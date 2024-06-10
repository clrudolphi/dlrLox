﻿using System.Dynamic;
using System.Xml.Linq;

namespace LoxOnDLR.Runtime
{
    internal class LoxGetSuperMemberBinder : GetMemberBinder
    {
        public readonly string? StartSearchInClassName;

        public LoxGetSuperMemberBinder(string name, string? startSearchInClassName, bool ignoreCase) : base(name, ignoreCase)
        {
            this.StartSearchInClassName = startSearchInClassName;
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