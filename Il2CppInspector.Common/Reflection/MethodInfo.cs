/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

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
        public TypeInfo ReturnType => ReturnParameter.ParameterType;

        public override bool RequiresUnsafeContext => base.RequiresUnsafeContext || ReturnType.RequiresUnsafeContext;

        // IL2CPP doesn't seem to retain return type custom attributes

        public MethodInfo(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) : base(pkg, methodIndex, declaringType) {
            // Add return parameter
            ReturnParameter = new ParameterInfo(pkg, -1, this);
        }

        public MethodInfo(MethodInfo methodDef, TypeInfo declaringType) : base(methodDef, declaringType) {
            ReturnParameter = ((MethodInfo)rootDefinition).ReturnParameter
                .SubstituteGenericArguments(this, DeclaringType.GetGenericArguments(), GetGenericArguments());
        }

        private MethodInfo(MethodInfo methodDef, TypeInfo[] typeArguments) : base(methodDef, typeArguments) {
            ReturnParameter = ((MethodInfo)rootDefinition).ReturnParameter
                .SubstituteGenericArguments(this, DeclaringType.GetGenericArguments(), GetGenericArguments());
        }

        protected override MethodBase MakeGenericMethodImpl(TypeInfo[] typeArguments) => new MethodInfo(this, typeArguments);

        public override string ToString() => ReturnType.Name + " " + Name + GetFullTypeParametersString() + "(" + string.Join(", ", 
                            DeclaredParameters.Select(x => x.ParameterType.IsByRef? x.ParameterType.Name.TrimEnd('&') + " ByRef" : x.ParameterType.Name)) + ")";
    }
}