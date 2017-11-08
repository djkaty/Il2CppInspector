/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class ConstructorInfo : MethodBase
    {
        // TODO
        public override MemberTypes MemberType => MemberTypes.Constructor | MemberTypes.Method;

        public ConstructorInfo(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) :
            base(declaringType) { }
    }

    public class PropertyInfo : MemberInfo
    {
        // TODO
        public override MemberTypes MemberType => MemberTypes.Property | MemberTypes.Method;
        public PropertyInfo(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) :
            base(declaringType) { }
    }

    public class CustomAttributeData
    {
        // TODO
    }
}
