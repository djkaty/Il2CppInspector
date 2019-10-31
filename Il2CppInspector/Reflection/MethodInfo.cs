/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class MethodInfo : MethodBase
    {
        public override MemberTypes MemberType => MemberTypes.Method;

        // Info about the return parameter
        public ParameterInfo ReturnParameter { get; }

        // Return type of the method
        private readonly int returnTypeUsage;
        public TypeInfo ReturnType => Assembly.Model.GetTypeFromUsage(returnTypeUsage, MemberTypes.TypeInfo);

        // TODO: ReturnTypeCustomAttributes

        public MethodInfo(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) : base(pkg, methodIndex, declaringType) {
            // Add return parameter
            returnTypeUsage = Definition.returnType;
            ReturnParameter = new ParameterInfo(pkg, -1, this);
        }

        public override string ToString() => ReturnType.Name + " " + Name + "(" + string.Join(", ", DeclaredParameters.Select(x => x.ParameterType.Name)) + ")";
    }
}