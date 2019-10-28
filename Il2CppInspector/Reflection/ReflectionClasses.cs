/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class ConstructorInfo : MethodBase
    {
        // IL names of constructor and static constructor
        public static readonly string ConstructorName = ".ctor";

        public static readonly string TypeConstructorName = ".cctor";

        // TODO
        public override MemberTypes MemberType => MemberTypes.Constructor;

        public ConstructorInfo(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) : base(pkg, methodIndex, declaringType) { }
    }

    public class CustomAttributeData
    {
        // TODO
    }
}
