/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppInspector.Reflection {
    public class TypeInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppTypeDefinition Definition { get; }
        public int Index { get; } = -1;

        // Information/flags about the type
        // Undefined if the Type represents a generic type parameter
        public TypeAttributes Attributes { get; }

        // Type that this type inherits from
        private readonly int baseTypeUsage = -1;

        public TypeInfo BaseType => IsPointer? null :
            baseTypeUsage != -1?
                Assembly.Model.GetTypeFromUsage(baseTypeUsage, MemberTypes.TypeInfo)
                : IsArray? Assembly.Model.TypesByDefinitionIndex.First(t => t.FullName == "System.Array")
                : Namespace != "System" || BaseName != "Object" ? Assembly.Model.TypesByDefinitionIndex.First(t => t.FullName == "System.Object")
                : null;

        // True if the type contains unresolved generic type parameters
        public bool ContainsGenericParameters { get; }

        public string BaseName => base.Name;

        // Get rid of generic backticks
        public string UnmangledBaseName => base.Name.IndexOf("`", StringComparison.Ordinal) == -1 ? base.Name : base.Name.Remove(base.Name.IndexOf("`", StringComparison.Ordinal));

        // C# colloquial name of the type (if available)
        public string CSharpName {
            get {
                var s = Namespace + "." + base.Name;
                var i = Il2CppConstants.FullNameTypeString.IndexOf(s);
                var n = (i != -1 ? Il2CppConstants.CSharpTypeString[i] : base.Name);
                if (n?.IndexOf("`", StringComparison.Ordinal) != -1)
                    n = n?.Remove(n.IndexOf("`", StringComparison.Ordinal));
                var g = (GenericTypeParameters != null ? "<" + string.Join(", ", GenericTypeParameters.Select(x => x.CSharpName)) + ">" : "");
                g = (GenericTypeArguments != null ? "<" + string.Join(", ", GenericTypeArguments.Select(x => x.CSharpName)) + ">" : g);
                n += g;
                if (s == "System.Nullable`1" && GenericTypeArguments.Any())
                    n = GenericTypeArguments[0].CSharpName + "?";
                if (HasElementType)
                    n = ElementType.CSharpName;
                return n + (IsArray ? "[" + new string(',', GetArrayRank() - 1) + "]" : "") + (IsPointer ? "*" : "");
            }
        }

        // C# name as it would be written in a type declaration
        public string CSharpTypeDeclarationName =>
                    (HasElementType?
                        ElementType.CSharpTypeDeclarationName :
                        base.Name.IndexOf("`", StringComparison.Ordinal) == -1 ? base.Name : base.Name.Remove(base.Name.IndexOf("`", StringComparison.Ordinal))
                        + (GenericTypeParameters != null ? "<" + string.Join(", ", GenericTypeParameters.Select(x => x.Name)) + ">" : "")
                        + (GenericTypeArguments != null ? "<" + string.Join(", ", GenericTypeArguments.Select(x => x.Name)) + ">" : ""))
                   + (IsArray ? "[" + new string(',', GetArrayRank() - 1) + "]" : "")
                   + (IsPointer ? "*" : "");

        // Custom attributes for this member
        public override IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(this);

        public List<ConstructorInfo> DeclaredConstructors { get; } = new List<ConstructorInfo>();
        public List<EventInfo> DeclaredEvents { get; } = new List<EventInfo>();
        public List<FieldInfo> DeclaredFields { get; } = new List<FieldInfo>();
        public List<MemberInfo> DeclaredMembers => throw new NotImplementedException();
        public List<MethodInfo> DeclaredMethods { get; } = new List<MethodInfo>();

        private int[] declaredNestedTypes;
        public IEnumerable<TypeInfo> DeclaredNestedTypes => declaredNestedTypes.Select(x => Assembly.Model.TypesByDefinitionIndex[x]);

        public List<PropertyInfo> DeclaredProperties { get; } = new List<PropertyInfo>();

        // Get a field by its name
        public FieldInfo GetField(string name) => DeclaredFields.FirstOrDefault(f => f.Name == name);

        // Get a method by its name
        public MethodInfo GetMethod(string name) => DeclaredMethods.FirstOrDefault(m => m.Name == name);

        // Get all methods with same name (overloads)
        public MethodInfo[] GetMethods(string name) => DeclaredMethods.Where(m => m.Name == Name).ToArray();

        // Get methods including inherited methods
        public MethodInfo[] GetAllMethods() {
            var methods = new List<IEnumerable<MethodInfo>>();

            for (var type = this; type != null; type = type.BaseType)
                methods.Add(type.DeclaredMethods);

            return methods.SelectMany(m => m).ToArray();
        }

        // Get a property by its name
        public PropertyInfo GetProperty(string name) => DeclaredProperties.FirstOrDefault(p => p.Name == name);

        // Method that the type is declared in if this is a type parameter of a generic method
        // TODO: Make a unit test from this: https://docs.microsoft.com/en-us/dotnet/api/system.type.declaringmethod?view=netframework-4.8
        public MethodBase DeclaringMethod;
        
        // IsGenericTypeParameter and IsGenericMethodParameter from https://github.com/dotnet/corefx/issues/23883
        public bool IsGenericTypeParameter => IsGenericParameter && DeclaringMethod == null;
        public bool IsGenericMethodParameter => IsGenericParameter && DeclaringMethod != null;

        // Gets the type of the object encompassed or referred to by the current array, pointer or reference type
        public TypeInfo ElementType { get; }

        // Type name including namespace
        public string FullName =>
            IsGenericParameter? null :
                (HasElementType? ElementType.FullName : 
                    (DeclaringType != null? DeclaringType.FullName + "+" : Namespace + (Namespace.Length > 0? "." : ""))
                    + base.Name
                    + (GenericTypeParameters != null ? "[" + string.Join(",", GenericTypeParameters.Select(x => x.FullName ?? x.Name)) + "]" : "")
                    + (GenericTypeArguments != null ? "[" + string.Join(",", GenericTypeArguments.Select(x => x.FullName ?? x.Name)) + "]" : ""))
                + (IsArray? "[" + new string(',', GetArrayRank() - 1) + "]" : "")
                + (IsPointer? "*" : "");

        public List<TypeInfo> GenericTypeParameters { get; }

        public List<TypeInfo> GenericTypeArguments { get; }

        // True if an array, pointer or reference, otherwise false
        // See: https://docs.microsoft.com/en-us/dotnet/api/system.type.haselementtype?view=netframework-4.8
        public bool HasElementType => ElementType != null;

        private readonly int[] implementedInterfaceUsages;
        public IEnumerable<TypeInfo> ImplementedInterfaces => implementedInterfaceUsages.Select(x => Assembly.Model.GetTypeFromUsage(x, MemberTypes.TypeInfo));

        public bool IsAbstract => (Attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
        public bool IsArray { get; }
        public bool IsByRef => throw new NotImplementedException();
        public bool IsClass => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
        public bool IsEnum => enumUnderlyingTypeUsage != -1;
        public bool IsGenericParameter { get; }
        public bool IsGenericType { get; }
        public bool IsGenericTypeDefinition { get; }
        public bool IsImport => (Attributes & TypeAttributes.Import) == TypeAttributes.Import;
        public bool IsInterface => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
        public bool IsNested => (MemberType & MemberTypes.NestedType) == MemberTypes.NestedType;
        public bool IsNestedAssembly => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly;
        public bool IsNestedFamANDAssem => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem;
        public bool IsNestedFamily => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;
        public bool IsNestedFamORAssem => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem;
        public bool IsNestedPrivate => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;
        public bool IsNestedPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;
        public bool IsNotPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic;
        public bool IsPointer { get; }
        // Primitive types table: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table (we exclude Object and String)
        public bool IsPrimitive => Namespace == "System" && new[] { "Boolean", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "IntPtr", "UIntPtr", "Char", "Decimal", "Double", "Single" }.Contains(Name);
        public bool IsPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;
        public bool IsSealed => (Attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed;
        public bool IsSerializable => (Attributes & TypeAttributes.Serializable) == TypeAttributes.Serializable;
        public bool IsSpecialName => (Attributes & TypeAttributes.SpecialName) == TypeAttributes.SpecialName;
        public bool IsValueType => BaseType?.FullName == "System.ValueType";

        // Helper function for determining if using this type as a field, parameter etc. requires that field or method to be declared as unsafe
        public bool RequiresUnsafeContext => IsPointer || (HasElementType && ElementType.RequiresUnsafeContext);

        // May get overridden by Il2CppType-based constructor below
        public override MemberTypes MemberType { get; } = MemberTypes.TypeInfo;

        private string @namespace;
        public string Namespace {
            get => !string.IsNullOrEmpty(@namespace) ? @namespace : DeclaringType?.Namespace ?? "";
            set => @namespace = value;
        }

        // Number of dimensions of an array
        private readonly int arrayRank;
        public int GetArrayRank() => arrayRank;

        public string[] GetEnumNames() => IsEnum? DeclaredFields.Where(x => x.Name != "value__").Select(x => x.Name).ToArray() : throw new InvalidOperationException("Type is not an enumeration");

        // The underlying type of an enumeration (int by default)
        private readonly int enumUnderlyingTypeUsage = -1;
        private TypeInfo enumUnderlyingType;

        public TypeInfo GetEnumUnderlyingType() {
            if (!IsEnum)
                return null;
            enumUnderlyingType ??= Assembly.Model.GetTypeFromUsage(enumUnderlyingTypeUsage, MemberTypes.TypeInfo);
            return enumUnderlyingType;
        }

        public Array GetEnumValues() => IsEnum? DeclaredFields.Where(x => x.Name != "value__").Select(x => x.DefaultValue).ToArray() : throw new InvalidOperationException("Type is not an enumeration");

        // Initialize from specified type index in metadata

        // Top-level types
        public TypeInfo(int typeIndex, Assembly owner) : base(owner) {
            var pkg = Assembly.Model.Package;

            Definition = pkg.TypeDefinitions[typeIndex];
            Index = typeIndex;
            Namespace = pkg.Strings[Definition.namespaceIndex];
            Name = pkg.Strings[Definition.nameIndex];

            // Derived type?
            if (Definition.parentIndex >= 0)
                baseTypeUsage = Definition.parentIndex;

            // Nested type?
            if (Definition.declaringTypeIndex >= 0) {
                declaringTypeDefinitionIndex = (int) pkg.TypeUsages[Definition.declaringTypeIndex].datapoint;
                MemberType |= MemberTypes.NestedType;
            }

            // Generic type definition?
            if (Definition.genericContainerIndex >= 0) {
                IsGenericType = true;
                IsGenericParameter = false;
                IsGenericTypeDefinition = true; // All of our generic type parameters are unresolved
                ContainsGenericParameters = true;

                // Store the generic type parameters for later instantiation
                var container = pkg.GenericContainers[Definition.genericContainerIndex];

                GenericTypeParameters = pkg.GenericParameters.Skip((int) container.genericParameterStart).Take(container.type_argc).Select(p => new TypeInfo(this, p)).ToList();

                // TODO: Constraints
            }

            // Add to global type definition list
            Assembly.Model.TypesByDefinitionIndex[Index] = this;

            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_SERIALIZABLE) != 0)
                Attributes |= TypeAttributes.Serializable;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_PUBLIC)
                Attributes |= TypeAttributes.Public;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NOT_PUBLIC)
                Attributes |= TypeAttributes.NotPublic;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_PUBLIC)
                Attributes |= TypeAttributes.NestedPublic;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_PRIVATE)
                Attributes |= TypeAttributes.NestedPrivate;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_ASSEMBLY)
                Attributes |= TypeAttributes.NestedAssembly;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_FAMILY)
                Attributes |= TypeAttributes.NestedFamily;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM)
                Attributes |= TypeAttributes.NestedFamANDAssem;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == Il2CppConstants.TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM)
                Attributes |= TypeAttributes.NestedFamORAssem;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_ABSTRACT) != 0)
                Attributes |= TypeAttributes.Abstract;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_SEALED) != 0)
                Attributes |= TypeAttributes.Sealed;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_SPECIAL_NAME) != 0)
                Attributes |= TypeAttributes.SpecialName;
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_IMPORT) != 0)
                Attributes |= TypeAttributes.Import;

            // TypeAttributes.Class == 0 so we only care about setting TypeAttributes.Interface (it's a non-interface class by default)
            if ((Definition.flags & Il2CppConstants.TYPE_ATTRIBUTE_INTERFACE) != 0)
                Attributes |= TypeAttributes.Interface;

            // Enumerations - bit 1 of bitfield indicates this (also the baseTypeUsage will be System.Enum)
            if (((Definition.bitfield >> 1) & 1) == 1)
                enumUnderlyingTypeUsage = Definition.elementTypeIndex;

            // Add all implemented interfaces
            implementedInterfaceUsages = new int[Definition.interfaces_count];
            for (var i = 0; i < Definition.interfaces_count; i++)
                implementedInterfaceUsages[i] = pkg.InterfaceUsageIndices[Definition.interfacesStart + i];

            // Add all nested types
            declaredNestedTypes = new int[Definition.nested_type_count];
            for (var n = 0; n < Definition.nested_type_count; n++)
                declaredNestedTypes[n] = pkg.NestedTypeIndices[Definition.nestedTypesStart + n];

            // Add all fields
            for (var f = Definition.fieldStart; f < Definition.fieldStart + Definition.field_count; f++)
                DeclaredFields.Add(new FieldInfo(pkg, f, this));

            // Add all methods
            for (var m = Definition.methodStart; m < Definition.methodStart + Definition.method_count; m++) {
                var method = new MethodInfo(pkg, m, this);
                if (method.Name == ConstructorInfo.ConstructorName || method.Name == ConstructorInfo.TypeConstructorName)
                    DeclaredConstructors.Add(new ConstructorInfo(pkg, m, this));
                else
                    DeclaredMethods.Add(method);
            }

            // Add all properties
            for (var p = Definition.propertyStart; p < Definition.propertyStart + Definition.property_count; p++)
                DeclaredProperties.Add(new PropertyInfo(pkg, p, this));

            // Add all events
            for (var e = Definition.eventStart; e < Definition.eventStart + Definition.event_count; e++)
                DeclaredEvents.Add(new EventInfo(pkg, e, this));
        }

        // Initialize type from binary usage
        // Much of the following is adapted from il2cpp::vm::Class::FromIl2CppType
        public TypeInfo(Il2CppModel model, Il2CppType pType, MemberTypes memberType) {
            var image = model.Package.BinaryImage;

            // Generic type unresolved and concrete instance types
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST) {
                var generic = image.ReadMappedObject<Il2CppGenericClass>(pType.datapoint); // Il2CppGenericClass *
                var genericTypeDef = model.TypesByDefinitionIndex[generic.typeDefinitionIndex];

                Definition = model.Package.TypeDefinitions[generic.typeDefinitionIndex];
                Index = (int) generic.typeDefinitionIndex;

                Assembly = genericTypeDef.Assembly;
                Namespace = genericTypeDef.Namespace;
                Name = genericTypeDef.BaseName;
                Attributes |= TypeAttributes.Class;

                // Derived type?
                if (genericTypeDef.Definition.parentIndex >= 0)
                    baseTypeUsage = genericTypeDef.Definition.parentIndex;

                // Nested type?
                if (genericTypeDef.Definition.declaringTypeIndex >= 0) {
                    declaringTypeDefinitionIndex = (int)model.Package.TypeUsages[genericTypeDef.Definition.declaringTypeIndex].datapoint;
                    MemberType = memberType | MemberTypes.NestedType;
                }

                IsGenericType = true;
                IsGenericParameter = false;
                IsGenericTypeDefinition = false; // This is a use of a generic type definition
                ContainsGenericParameters = true;

                // Get the instantiation
                var genericInstance = image.ReadMappedObject<Il2CppGenericInst>(generic.context.class_inst);

                // Get list of pointers to type parameters (both unresolved and concrete)
                var genericTypeArguments = image.ReadMappedWordArray(genericInstance.type_argv, (int)genericInstance.type_argc);

                GenericTypeArguments = new List<TypeInfo>();

                foreach (var pArg in genericTypeArguments) {
                    var argType = model.GetTypeFromVirtualAddress((ulong) pArg);
                    // TODO: Detect whether unresolved or concrete (add concrete to GenericTypeArguments instead)
                    // TODO: GenericParameterPosition etc. in types we generate here
                    // TODO: Assembly etc.
                    GenericTypeArguments.Add(argType); // TODO: Fix MemberType here
                }
            }

            // TODO: Set DeclaringType for the two below

            // Array with known dimensions and bounds
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_ARRAY) {
                var descriptor = image.ReadMappedObject<Il2CppArrayType>(pType.datapoint);
                ElementType = model.GetTypeFromVirtualAddress(descriptor.etype);

                Assembly = ElementType.Assembly;
                Definition = ElementType.Definition;
                Index = ElementType.Index;
                Namespace = ElementType.Namespace;
                Name = ElementType.Name;
                ContainsGenericParameters = ElementType.ContainsGenericParameters;

                IsArray = true;
                arrayRank = descriptor.rank;
            }

            // Dynamically allocated array or pointer type
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_PTR) {
                ElementType = model.GetTypeFromVirtualAddress(pType.datapoint);

                Assembly = ElementType.Assembly;
                Definition = ElementType.Definition;
                Index = ElementType.Index;
                Namespace = ElementType.Namespace;
                Name = ElementType.Name;
                ContainsGenericParameters = ElementType.ContainsGenericParameters;

                IsPointer = (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_PTR);
                IsArray = !IsPointer;

                // Heap arrays always have one dimension
                arrayRank = 1;
            }

            // Generic type parameter
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VAR || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_MVAR) {
                var paramType = model.Package.GenericParameters[pType.datapoint]; // genericParameterIndex
                var container = model.Package.GenericContainers[paramType.ownerIndex];

                var ownerType = model.TypesByDefinitionIndex[
                        container.is_method == 1
                        ? model.Package.Methods[container.ownerIndex].declaringType
                        : container.ownerIndex];

                Assembly = ownerType.Assembly;
                Namespace = "";
                Name = model.Package.Strings[paramType.nameIndex];
                Attributes |= TypeAttributes.Class;

                // Derived type?
                if (ownerType.Definition.parentIndex >= 0)
                    baseTypeUsage = ownerType.Definition.parentIndex;

                // Nested type always - sets DeclaringType used below
                declaringTypeDefinitionIndex = ownerType.Index;
                MemberType = memberType | MemberTypes.NestedType;

                // All generic method type parameters have a declared method
                if (container.is_method == 1)
                    DeclaringMethod = model.MethodsByDefinitionIndex[container.ownerIndex];

                IsGenericParameter = true;
                ContainsGenericParameters = true;
                IsGenericType = false;
                IsGenericTypeDefinition = false;
            }
        }

        // Initialize a type that is a generic parameter of a generic type
        // See: https://docs.microsoft.com/en-us/dotnet/api/system.type.isgenerictype?view=netframework-4.8
        public TypeInfo(TypeInfo declaringType, Il2CppGenericParameter param) : base(declaringType) {
            // Same visibility attributes as declaring type
            Attributes = declaringType.Attributes;

            // Same namespace as delcaring type
            Namespace = declaringType.Namespace;

            // Base type of object
            // TODO: This may change under constraints

            // Name of parameter
            Name = declaringType.Assembly.Model.Package.Strings[param.nameIndex];

            IsGenericParameter = true;
            IsGenericType = false;
            IsGenericTypeDefinition = false;
            ContainsGenericParameters = true;
        }

        // Initialize a type that is a generic parameter of a generic method
        public TypeInfo(MethodBase declaringMethod, Il2CppGenericParameter param) : this(declaringMethod.DeclaringType, param) {
            DeclaringMethod = declaringMethod;
        }

        // Display name of object
        public override string Name => IsGenericParameter ? base.Name :
            (HasElementType? ElementType.Name :
                (DeclaringType != null ? DeclaringType.Name + "+" : "")
                + base.Name
                + (GenericTypeParameters != null ? "[" + string.Join(",", GenericTypeParameters.Select(x => x.Namespace != Namespace? x.FullName ?? x.Name : x.Name)) + "]" : "")
                + (GenericTypeArguments != null ? "[" + string.Join(",", GenericTypeArguments.Select(x => x.Namespace != Namespace? x.FullName ?? x.Name : x.Name)) + "]" : ""))
            + (IsArray ? "[" + new string(',', GetArrayRank() - 1) + "]" : "")
            + (IsPointer ? "*" : "");

        public string GetAccessModifierString() => this switch {
            { IsPublic: true } => "public ",
            { IsNotPublic: true } => "internal ",

            { IsNestedPublic: true } => "public ",
            { IsNestedPrivate: true } => "private ",
            { IsNestedFamily: true } => "protected ",
            { IsNestedAssembly: true } => "internal ",
            { IsNestedFamORAssem: true } => "protected internal ",
            { IsNestedFamANDAssem: true } => "private protected ",
            _ => throw new InvalidOperationException("Unknown type access modifier")
        };

        public string GetModifierString() {
            var modifiers = new StringBuilder(GetAccessModifierString());

            // An abstract sealed class is a static class
            if (IsAbstract && IsSealed)
                modifiers.Append("static ");
            else {
                if (IsAbstract && !IsInterface)
                    modifiers.Append("abstract ");
                if (IsSealed && !IsValueType && !IsEnum)
                    modifiers.Append("sealed ");
            }
            if (IsInterface)
                modifiers.Append("interface ");
            else if (IsValueType)
                modifiers.Append("struct ");
            else if (IsEnum)
                modifiers.Append("enum ");
            else
                modifiers.Append("class ");

            return modifiers.ToString();
        }

        public override string ToString() => Name;
    }
}