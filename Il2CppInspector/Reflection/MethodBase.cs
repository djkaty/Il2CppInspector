/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public abstract class MethodBase : MemberInfo
    {
        // (not code attributes)
        public MethodAttributes Attributes { get; set; }

        // TODO: ContainsGenericParameters

        protected MethodBase(TypeInfo declaringType) : base(declaringType) { }
    }
}