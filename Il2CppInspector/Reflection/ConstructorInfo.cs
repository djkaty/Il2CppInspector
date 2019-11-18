/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class ConstructorInfo : MethodBase
    {
        // IL names of constructor and static constructor
        public static readonly string ConstructorName = ".ctor";

        public static readonly string TypeConstructorName = ".cctor";

        public override MemberTypes MemberType => MemberTypes.Constructor;

        public ConstructorInfo(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) : base(pkg, methodIndex, declaringType) { }

        public override string ToString() => DeclaringType.Name + "(" + string.Join(", ", DeclaredParameters.Select(x => x.ParameterType.Name)) + ")";

        public override string GetSignatureString() => Name + GetFullTypeParametersString()
                                                       + "(" + string.Join(",", DeclaredParameters.Select(x => x.GetSignatureString())) + ")";
    }
}