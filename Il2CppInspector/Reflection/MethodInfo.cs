/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class MethodInfo : MethodBase
    {
        // IL2CPP-specific data
        public Il2CppMethodDefinition Definition { get; }
        public int Index { get; }
        public uint VirtualAddress { get; }

        public override MemberTypes MemberType => MemberTypes.Method;

        // Info about the return parameter
        public ParameterInfo ReturnParameter { get; }

        // Return type of the method
        private readonly Il2CppType returnType;

        public TypeInfo ReturnType => Assembly.Model.GetType(returnType, MemberTypes.TypeInfo);

        // TODO: ReturnTypeCustomAttributes

        public MethodInfo(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) :
            base(declaringType) {
            Definition = pkg.Metadata.Methods[methodIndex];
            Index = methodIndex;
            VirtualAddress = pkg.Binary.MethodPointers[methodIndex];
            Name = pkg.Strings[Definition.nameIndex];

            returnType = pkg.TypeUsages[Definition.returnType];

            if ((Definition.flags & DefineConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == DefineConstants.METHOD_ATTRIBUTE_PRIVATE)
                Attributes |= MethodAttributes.Private;
            if ((Definition.flags & DefineConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == DefineConstants.METHOD_ATTRIBUTE_PUBLIC)
                Attributes |= MethodAttributes.Public;
            if ((Definition.flags & DefineConstants.METHOD_ATTRIBUTE_VIRTUAL) != 0)
                Attributes |= MethodAttributes.Virtual;
            if ((Definition.flags & DefineConstants.METHOD_ATTRIBUTE_STATIC) != 0)
                Attributes |= MethodAttributes.Static;
        }
    }
}