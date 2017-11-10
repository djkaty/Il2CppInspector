/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public abstract class MethodBase : MemberInfo
    {
        // Information/flags about the method
        public MethodAttributes Attributes { get; protected set; }

        // True if the type contains unresolved generic type parameters
        public bool ContainsGenericParameters => throw new NotImplementedException();

        // TODO: Custom attribute stuff

        public List<ParameterInfo> DeclaredParameters { get; } = new List<ParameterInfo>();

        public bool IsAbstract => (Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract;
        public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;
        public bool IsConstructor => throw new NotImplementedException();
        public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;
        public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;
        public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;
        public bool IsFinal => (Attributes & MethodAttributes.Final) == MethodAttributes.Final;
        public bool IsGenericMethod => throw new NotImplementedException();
        public bool IsGenericMethodDefinition => throw new NotImplementedException();
        public bool IsHideBySig => (Attributes & MethodAttributes.HideBySig) == MethodAttributes.HideBySig;
        public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
        public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
        public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) == MethodAttributes.SpecialName;
        public bool IsStatic => (Attributes & MethodAttributes.Static) == MethodAttributes.Static;
        public bool IsVirtual => (Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual;

        // TODO: GetMethodBody()

        protected MethodBase(TypeInfo declaringType) : base(declaringType) { }
    }
}