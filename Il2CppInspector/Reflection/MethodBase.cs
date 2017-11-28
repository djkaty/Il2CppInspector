/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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

        public string GetModifierString() {
            // Interface methods and properties have no visible modifiers (they are always declared 'public abstract')
            if (DeclaringType.IsInterface)
                return string.Empty;

            StringBuilder modifiers = new StringBuilder();

            if (IsPrivate)
                modifiers.Append("private ");
            if (IsPublic)
                modifiers.Append("public ");
            if (IsFamily)
                modifiers.Append("protected ");
            if (IsAssembly)
                modifiers.Append("internal ");
            if (IsFamilyOrAssembly)
                modifiers.Append("protected internal ");
            if (IsFamilyAndAssembly)
                modifiers.Append("[family and assembly] ");

            if (IsAbstract)
                modifiers.Append("abstract ");
            // Methods that implement interfaces are IsVirtual && IsFinal with MethodAttributes.NewSlot (don't show 'virtual sealed' for these)
            if (IsFinal && (Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot)
                modifiers.Append("sealed override ");
            // All abstract, override and sealed methods are also virtual by nature
            if (IsVirtual && !IsAbstract && !IsFinal)
                modifiers.Append((Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot ? "virtual " : "override ");
            if (IsStatic)
                modifiers.Append("static ");
            if ((Attributes & MethodAttributes.PinvokeImpl) != 0)
                modifiers.Append("extern ");

            // Will include a trailing space
            return modifiers.ToString();
        }
    }
}