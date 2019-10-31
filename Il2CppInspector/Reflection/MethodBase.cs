/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppInspector.Reflection
{
    public abstract class MethodBase : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppMethodDefinition Definition { get; }
        public int Index { get; }
        public ulong VirtualAddress { get; }

        // Information/flags about the method
        public MethodAttributes Attributes { get; protected set; }

        // True if the type contains unresolved generic type parameters
        public bool ContainsGenericParameters => throw new NotImplementedException();

        // TODO: Custom attribute stuff

        public List<ParameterInfo> DeclaredParameters { get; } = new List<ParameterInfo>();

        public bool IsAbstract => (Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract;
        public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;
        public bool IsConstructor => MemberType == MemberTypes.Constructor;
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

        public string CSharpName =>
            // Operator overload or user-defined conversion operator
            OperatorMethodNames.ContainsKey(Name)? "operator " + OperatorMethodNames[Name] : Name;

        protected MethodBase(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) : base(declaringType) {
            Definition = pkg.Methods[methodIndex];
            Index = methodIndex;
            Name = pkg.Strings[Definition.nameIndex];

            // Find method pointer
            VirtualAddress = pkg.GetMethodPointer(Assembly.Module, Definition);

            // Set method attributes
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == Il2CppConstants.METHOD_ATTRIBUTE_PRIVATE)
                Attributes |= MethodAttributes.Private;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == Il2CppConstants.METHOD_ATTRIBUTE_PUBLIC)
                Attributes |= MethodAttributes.Public;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == Il2CppConstants.METHOD_ATTRIBUTE_FAM_AND_ASSEM)
                Attributes |= MethodAttributes.FamANDAssem;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == Il2CppConstants.METHOD_ATTRIBUTE_ASSEM)
                Attributes |= MethodAttributes.Assembly;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == Il2CppConstants.METHOD_ATTRIBUTE_FAMILY)
                Attributes |= MethodAttributes.Family;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == Il2CppConstants.METHOD_ATTRIBUTE_FAM_OR_ASSEM)
                Attributes |= MethodAttributes.FamORAssem;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_VIRTUAL) != 0)
                Attributes |= MethodAttributes.Virtual;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_ABSTRACT) != 0)
                Attributes |= MethodAttributes.Abstract;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_STATIC) != 0)
                Attributes |= MethodAttributes.Static;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_FINAL) != 0)
                Attributes |= MethodAttributes.Final;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_HIDE_BY_SIG) != 0)
                Attributes |= MethodAttributes.HideBySig;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK) == Il2CppConstants.METHOD_ATTRIBUTE_NEW_SLOT)
                Attributes |= MethodAttributes.NewSlot;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_PINVOKE_IMPL) != 0)
                Attributes |= MethodAttributes.PinvokeImpl;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_SPECIAL_NAME) != 0)
                Attributes |= MethodAttributes.SpecialName;
            if ((Definition.flags & Il2CppConstants.METHOD_ATTRIBUTE_UNMANAGED_EXPORT) != 0)
                Attributes |= MethodAttributes.UnmanagedExport;

            // Add arguments
            for (var p = Definition.parameterStart; p < Definition.parameterStart + Definition.parameterCount; p++)
                DeclaredParameters.Add(new ParameterInfo(pkg, p, this));
        }

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

            if (Name == "op_Implicit")
                modifiers.Append("implicit ");
            if (Name == "op_Explicit")
                modifiers.Append("explicit ");

            // Will include a trailing space
            return modifiers.ToString();
        }

        // Get C# syntax-friendly list of parameters
        public string GetParametersString() =>
            string.Join(", ", DeclaredParameters.Select(p => $"{p.GetModifierString()}{p.ParameterType.CSharpName} {p.Name}"));

        // List of operator overload metadata names
        // https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/operator-overloads
        public static Dictionary<string, string> OperatorMethodNames = new Dictionary<string, string> {
            ["op_Implicit"] = "",
            ["op_Explicit"] = "",
            ["op_Addition"] = "+",
            ["op_Subtraction"] = "-",
            ["op_Multiply"] = "*",
            ["op_Division"] = "/",
            ["op_Modulus"] = "%",
            ["op_ExclusiveOr"] = "^",
            ["op_BitwiseAnd"] = "&",
            ["op_BitwiseOr"] = "|",
            ["op_LogicalAnd"] = "&&",
            ["op_LogicalOr"] = "||",
            ["op_Assign"] = "=",
            ["op_LeftShift"] = "<<",
            ["op_RightShift"] = ">>",
            ["op_SignedLeftShift"] = "", // Listed as N/A in the documentation
            ["op_SignedRightShift"] = "", // Listed as N/A in the documentation
            ["op_Equality"] = "==",
            ["op_Inequality"] = "!=",
            ["op_GreaterThan"] = ">",
            ["op_LessThan"] = "<",
            ["op_GreaterThanOrEqual"] = ">=",
            ["op_LessThanOrEqual"] = "<=",
            ["op_MultiplicationAssignment"] = "*=",
            ["op_SubtractionAssignment"] = "-=",
            ["op_ExclusiveOrAssignment"] = "^=",
            ["op_LeftShiftAssignment"] = "<<=", // Doesn't seem to be any right shift assignment`in documentation
            ["op_ModulusAssignment"] = "%=",
            ["op_AdditionAssignment"] = "+=",
            ["op_BitwiseAndAssignment"] = "&=",
            ["op_BitwiseOrAssignment"] = "|=",
            ["op_Comma"] = ",",
            ["op_DivisionAssignment"] = "*/=",
            ["op_Decrement"] = "--",
            ["op_Increment"] = "++",
            ["op_UnaryNegation"] = "-",
            ["op_UnaryPlus"] = "+",
            ["op_OnesComplement"] = "~"
        };
    }
}