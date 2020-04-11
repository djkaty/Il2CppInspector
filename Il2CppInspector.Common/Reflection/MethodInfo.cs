/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

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
        private readonly TypeRef returnTypeReference;
        public TypeInfo ReturnType => returnTypeReference.Value;

        public override bool RequiresUnsafeContext => base.RequiresUnsafeContext || ReturnType.RequiresUnsafeContext;

        // IL2CPP doesn't seem to retain return type custom attributes

        public MethodInfo(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) : base(pkg, methodIndex, declaringType) {
            // Add return parameter
            returnTypeReference = TypeRef.FromReferenceIndex(Assembly.Model, Definition.returnType);
            ReturnParameter = new ParameterInfo(pkg, -1, this);
        }

        public MethodInfo(Il2CppModel model, Il2CppMethodSpec spec, TypeInfo declaringType) : base(model, spec, declaringType) {
            var methodDef = model.MethodsByDefinitionIndex[spec.methodDefinitionIndex];
            // Add return parameter (TODO substitute type)
            returnTypeReference = TypeRef.FromReferenceIndex(Assembly.Model, methodDef.Definition.returnType);
            ReturnParameter = ((MethodInfo) methodDef).ReturnParameter;
        }

        public override string ToString() => ReturnType.Name + " " + Name + GetFullTypeParametersString() + "(" + string.Join(", ", 
                            DeclaredParameters.Select(x => x.ParameterType.IsByRef? x.ParameterType.Name.TrimEnd('&') + " ByRef" : x.ParameterType.Name)) + ")";

        public override string GetSignatureString() => ReturnParameter.GetSignatureString() + " " + Name + GetFullTypeParametersString()
                                              + "(" + string.Join(",", DeclaredParameters.Select(x => x.GetSignatureString())) + ")";
    }
}