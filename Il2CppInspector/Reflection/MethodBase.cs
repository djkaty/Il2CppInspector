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

        public List<ParameterInfo> DeclaredParameters => throw new NotImplementedException();

        public bool IsAbstract => (Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract;
        public bool IsAssembly => throw new NotImplementedException();
        public bool IsConstructor => throw new NotImplementedException();
        public bool IsFamily => throw new NotImplementedException();
        public bool IsFamilyAndAssembly => throw new NotImplementedException();
        public bool IsFamilyOrAssembly => throw new NotImplementedException();
        public bool IsFinal => throw new NotImplementedException();
        public bool IsGenericMethod => throw new NotImplementedException();
        public bool IsGenericMethodDefinition => throw new NotImplementedException();
        public bool IsHideBySig => throw new NotImplementedException();
        public bool IsPrivate => (Attributes & MethodAttributes.Private) == MethodAttributes.Private;
        public bool IsPublic => (Attributes & MethodAttributes.Public) == MethodAttributes.Public;
        public bool IsStatic => (Attributes & MethodAttributes.Static) == MethodAttributes.Static;
        public bool IsVirtual => (Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual;

        // TODO: GetMethodBody()
        // TODO: GetParameters()

        protected MethodBase(TypeInfo declaringType) : base(declaringType) { }
    }
}