﻿/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

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
        public (ulong Start, ulong End)? VirtualAddress { get; }

        // Method.Invoke implementation
        public MethodInvoker Invoker { get; set; }

        // Information/flags about the method
        public MethodAttributes Attributes { get; protected set; }

        // Custom attributes for this member
        public override IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(this);

        public List<ParameterInfo> DeclaredParameters { get; } = new List<ParameterInfo>();

        public bool IsAbstract => (Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract;
        public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;
        public bool IsConstructor => MemberType == MemberTypes.Constructor;
        public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;
        public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;
        public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;
        public bool IsFinal => (Attributes & MethodAttributes.Final) == MethodAttributes.Final;
        public bool IsHideBySig => (Attributes & MethodAttributes.HideBySig) == MethodAttributes.HideBySig;
        public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
        public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
        public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) == MethodAttributes.SpecialName;
        public bool IsStatic => (Attributes & MethodAttributes.Static) == MethodAttributes.Static;
        public bool IsVirtual => (Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual;

        public virtual bool RequiresUnsafeContext => DeclaredParameters.Any(p => p.ParameterType.RequiresUnsafeContext);

        // True if the method contains unresolved generic type parameters, or if it is a non-generic method in an open ganeric type
        // See: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.containsgenericparameters?view=netframework-4.8
        public bool ContainsGenericParameters => DeclaringType.ContainsGenericParameters || genericArguments.Any(ga => ga.ContainsGenericParameters);

        // For a generic method definition: the list of generic type parameters
        // For an open generic method: a mix of generic type parameters and generic type arguments
        // For a closed generic method: the list of generic type arguments
        private readonly List<TypeInfo> genericArguments = new List<TypeInfo>();

        // See: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.getgenericarguments?view=netframework-4.8
        public List<TypeInfo> GetGenericArguments() => genericArguments;

        // This was added in .NET Core 2.1 and isn't properly documented yet
        public bool IsConstructedGenericMethod => IsGenericMethod && genericArguments.All(ga => !ga.ContainsGenericParameters);

        // See: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.isgenericmethod?view=netframework-4.8
        public bool IsGenericMethod { get; }
        public bool IsGenericMethodDefinition => genericArguments.Any() && genericArguments.All(a => a.IsGenericMethodParameter);
        
        // TODO: GetMethodBody()

        public string CSharpName =>
            // Operator overload or user-defined conversion operator
            OperatorMethodNames.ContainsKey(Name)? "operator " + OperatorMethodNames[Name]

            // Explicit interface implementation
            : (IsVirtual && IsFinal && (Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot && Name.IndexOf('.') != -1)? 
            ((Func<string>)(() => {
                // This is some shenanigans because IL2CPP does not use a consistent naming scheme for explicit interface implementation method names
                var implementingInterface = DeclaringType.ImplementedInterfaces.FirstOrDefault(i => Name.StartsWith(i.Namespace + "." + i.CSharpName + "."))
                    ?? DeclaringType.ImplementedInterfaces.FirstOrDefault(i => Name.StartsWith(i.Namespace + "." + i.GetCSharpTypeDeclarationName().Replace(" ", "") + "."));
                // TODO: There are some combinations we haven't dealt with so use this test as a safety valve
                if (implementingInterface == null)
                    return Name;
                return implementingInterface.CSharpName + Name.Substring(Name.LastIndexOf('.'));
            }))()

            // Regular method
            : Name;

        // Initialize a method from a method definition (MethodDef)
        protected MethodBase(Il2CppInspector pkg, int methodIndex, TypeInfo declaringType) : base(declaringType) {
            Definition = pkg.Methods[methodIndex];
            Index = methodIndex;
            Name = pkg.Strings[Definition.nameIndex];

            // Find method pointer
            VirtualAddress = pkg.GetMethodPointer(Assembly.ModuleDefinition, Definition);

            // Add to global method definition list
            Assembly.Model.MethodsByDefinitionIndex[Index] = this;

            // Generic method definition?
            if (Definition.genericContainerIndex >= 0) {
                IsGenericMethod = true;

                // Store the generic type parameters for later instantiation
                var container = pkg.GenericContainers[Definition.genericContainerIndex];

                genericArguments = pkg.GenericParameters.Skip((int)container.genericParameterStart).Take(container.type_argc).Select(p => new TypeInfo(this, p)).ToList();
            }

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

        // Initialize a method from a concrete generic method (MethodSpec)
        protected MethodBase(Il2CppModel model, Il2CppMethodSpec spec, TypeInfo declaringType) : base(declaringType) {
            var methodDef = model.MethodsByDefinitionIndex[spec.methodDefinitionIndex];

            Name = methodDef.Name;
            Attributes = methodDef.Attributes;

            IsGenericMethod = true;
            genericArguments = model.ResolveGenericArguments(model.Package.GenericInstances[spec.methodIndexIndex]);

            // Substitute matching generic type parameters with concrete type arguments
            foreach (var p in methodDef.DeclaredParameters) {
                if (!p.ParameterType.IsGenericMethodParameter)
                    DeclaredParameters.Add(p);
                else
                    DeclaredParameters.Add(new ParameterInfo(model, p, genericArguments[p.ParameterType.GenericParameterPosition]));
            }

            VirtualAddress = model.Package.GetGenericMethodPointer(spec);
        }

        public string GetAccessModifierString() => this switch {
            // Static constructors can not have an access level modifier
            { IsConstructor: true, IsStatic: true } => "",

            // Finalizers can not have an access level modifier
            { Name: "Finalize", IsVirtual: true, IsFamily: true } => "",

            // Explicit interface implementations do not have an access level modifier
            { IsVirtual: true, IsFinal: true, Attributes: var a } when (a & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot && Name.IndexOf('.') != -1 => "",

            { IsPrivate: true } => "private ",
            { IsPublic: true } => "public ",
            { IsFamily: true } => "protected ",
            { IsAssembly: true } => "internal ",
            { IsFamilyOrAssembly: true } => "protected internal ",
            { IsFamilyAndAssembly: true } => "private protected ",
            _ => ""
        };

        public string GetModifierString() {
            // Interface methods and properties have no visible modifiers (they are always declared 'public abstract')
            if (DeclaringType.IsInterface)
                return string.Empty;

            var modifiers = new StringBuilder(GetAccessModifierString());

            if (RequiresUnsafeContext)
                modifiers.Append("unsafe ");
            if (IsAbstract)
                modifiers.Append("abstract ");
            // Methods that implement interfaces are IsVirtual && IsFinal with MethodAttributes.NewSlot (don't show 'virtual sealed' for these)
            if (IsFinal && (Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot)
                modifiers.Append("sealed override ");
            // All abstract, override and sealed methods are also virtual by nature
            if (IsVirtual && !IsAbstract && !IsFinal && Name != "Finalize")
                modifiers.Append((Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot ? "virtual " : "override ");
            if (IsStatic)
                modifiers.Append("static ");
            if ((Attributes & MethodAttributes.PinvokeImpl) != 0)
                modifiers.Append("extern ");

            // Method hiding
            if ((DeclaringType.BaseType?.GetAllMethods().Any(m => m.GetSignatureString() == GetSignatureString() && m.IsHideBySig) ?? false)
                && (((Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot && !IsVirtual)
                    || (Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot))
                modifiers.Append("new ");

            if (Name == "op_Implicit")
                modifiers.Append("implicit ");
            if (Name == "op_Explicit")
                modifiers.Append("explicit ");

            // Will include a trailing space
            return modifiers.ToString();
        }

        // Get C# syntax-friendly list of parameters
        public string GetParametersString(Scope usingScope, bool emitPointer = false, bool commentAttributes = false)
            => string.Join(", ", DeclaredParameters.Select(p => p.GetParameterString(usingScope, emitPointer, commentAttributes)));

        public string GetTypeParametersString(Scope usingScope) => !GetGenericArguments().Any()? "" :
            "<" + string.Join(", ", GetGenericArguments().Select(p => p.GetScopedCSharpName(usingScope))) + ">";

        public string GetFullTypeParametersString() => !GetGenericArguments().Any()? "" :
            "[" + string.Join(",", GetGenericArguments().Select(p => p.Name)) + "]";

        public abstract string GetSignatureString();

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