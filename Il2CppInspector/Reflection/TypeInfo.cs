/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection {
    public class TypeInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppTypeDefinition Definition { get; }
        public int Index { get; }

        // Information/flags about the type
        // Undefined if the Type represents a generic type parameter
        public TypeAttributes Attributes { get; }

        // Type that this type inherits from
        private Il2CppType baseType;
        public TypeInfo BaseType => baseType != null? Assembly.Model.GetType(baseType, MemberTypes.TypeInfo) : null;

        // True if the type contains unresolved generic type parameters
        public bool ContainsGenericParameters { get; }

        // C# colloquial name of the type (if available)
        public string CSharpName {
            get {
                var s = Namespace + "." + base.Name;
                var i = Il2CppConstants.FullNameTypeString.IndexOf(s);
                var n = (i != -1 ? Il2CppConstants.CSharpTypeString[i] : base.Name);
                if (n?.IndexOf("`", StringComparison.Ordinal) != -1)
                    n = n?.Remove(n.IndexOf("`", StringComparison.Ordinal));
                if (IsArray)
                    n = ElementType.CSharpName;
                var g = (GenericTypeParameters != null ? "<" + string.Join(", ", GenericTypeParameters.Select(x => x.CSharpName)) + ">" : "");
                return (IsPointer ? "void *" : "") + n + g + (IsArray ? "[]" : "");
            }
        }

        public List<ConstructorInfo> DeclaredConstructors { get; } = new List<ConstructorInfo>();
        public List<EventInfo> DeclaredEvents { get; } = new List<EventInfo>();
        public List<FieldInfo> DeclaredFields { get; } = new List<FieldInfo>();
        public List<MemberInfo> DeclaredMembers => throw new NotImplementedException();
        public List<MethodInfo> DeclaredMethods { get; } = new List<MethodInfo>();
        public List<TypeInfo> DeclaredNestedTypes => throw new NotImplementedException();
        public List<PropertyInfo> DeclaredProperties { get; } = new List<PropertyInfo>();

        // Method that the type is declared in if this is a type parameter of a generic method
        public MethodBase DeclaringMethod => throw new NotImplementedException();

        // Gets the type of the object encompassed or referred to by the current array, pointer or reference type
        private Il2CppType enumElementType;
        private TypeInfo elementType;
        public TypeInfo ElementType {
            get {
                if (IsEnum && elementType == null)
                    elementType = Assembly.Model.GetType(enumElementType, MemberTypes.TypeInfo);
                return elementType;
            }
        }

        // Type name including namespace
        public string FullName => (IsPointer? "void *" : "")
            + Namespace
            + (Namespace.Length > 0? "." : "")
            + base.Name
            + (GenericTypeParameters != null ? "<" + string.Join(", ", GenericTypeParameters.Select(x => x.Name)) + ">" : "")
            + (IsArray? "[]" : "");

        // TODO: Alot of other generics stuff
        
        public List<TypeInfo> GenericTypeParameters { get; }

        public bool HasElementType => ElementType != null;

        private Il2CppType[] implementedInterfaces;
        public IEnumerable<TypeInfo> ImplementedInterfaces => implementedInterfaces.Select(x => Assembly.Model.GetType(x, MemberTypes.TypeInfo));

        public bool IsAbstract => (Attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
        public bool IsArray { get; }
        public bool IsByRef => throw new NotImplementedException();
        public bool IsClass => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
        public bool IsEnum { get; }
        public bool IsGenericParameter { get; }
        public bool IsGenericType => throw new NotImplementedException();
        public bool IsGenericTypeDefinition => throw new NotImplementedException();
        public bool IsImport => (Attributes & TypeAttributes.Import) == TypeAttributes.Import;
        public bool IsInterface => (Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
        public bool IsNested { get; } // TODO: Partially implemented
        public bool IsNestedAssembly => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly;
        public bool IsNestedFamANDAssem => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem;
        public bool IsNestedFamily => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;
        public bool IsNestedFamORAssem => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem;
        public bool IsNestedPrivate => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;
        public bool IsNestedPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;
        public bool IsNotPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic;
        public bool IsPointer { get; }
        public bool IsPrimitive => Namespace == "System" && new[] { "Boolean", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "IntPtr", "UIntPtr", "Char", "Double", "Single" }.Contains(Name);
        public bool IsPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;
        public bool IsSealed => (Attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed;
        public bool IsSerializable => (Attributes & TypeAttributes.Serializable) == TypeAttributes.Serializable;
        public bool IsSpecialName => (Attributes & TypeAttributes.SpecialName) == TypeAttributes.SpecialName;
        public bool IsValueType => BaseType?.FullName == "System.ValueType";

        public override MemberTypes MemberType { get; }

        public override string Name {
            get => (IsPointer ? "void *" : "")
                + (base.Name.IndexOf("`", StringComparison.Ordinal) == -1? base.Name : base.Name.Remove(base.Name.IndexOf("`", StringComparison.Ordinal)))
                + (GenericTypeParameters != null? "<" + string.Join(", ", GenericTypeParameters.Select(x => x.Name)) + ">" : "")
                + (IsArray ? "[]" : "");
            protected set => base.Name = value;
        }

        public string Namespace { get; }

        // Number of dimensions of an array
        private readonly int arrayRank;
        public int GetArrayRank() => arrayRank;

        // TODO: Custom attribute stuff

        public string[] GetEnumNames() => IsEnum? DeclaredFields.Where(x => x.Name != "value__").Select(x => x.Name).ToArray() : throw new InvalidOperationException("Type is not an enumeration");

        public TypeInfo GetEnumUnderlyingType() => ElementType;

        public Array GetEnumValues() => IsEnum? DeclaredFields.Where(x => x.Name != "value__").Select(x => x.DefaultValue).ToArray() : throw new InvalidOperationException("Type is not an enumeration");

        // TODO: Generic stuff

        // Initialize from specified type index in metadata
        public TypeInfo(Il2CppInspector pkg, int typeIndex, Assembly owner) :
            base(owner) {
            Definition = pkg.TypeDefinitions[typeIndex];
            Index = typeIndex;
            Namespace = pkg.Strings[Definition.namespaceIndex];
            Name = pkg.Strings[Definition.nameIndex];

            if (Definition.parentIndex >= 0)
                baseType = pkg.TypeUsages[Definition.parentIndex];

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

            // Enumerations - bit 1 of bitfield indicates this (also the baseType will be System.Enum)
            if (((Definition.bitfield >> 1) & 1) == 1) {
                IsEnum = true;
                enumElementType = pkg.TypeUsages[Definition.elementTypeIndex];
            }

            // Add all implemented interfaces
            implementedInterfaces = new Il2CppType[Definition.interfaces_count];
            for (var i = 0; i < Definition.interfaces_count; i++)
                implementedInterfaces[i] = pkg.TypeUsages[pkg.InterfaceUsageIndices[Definition.interfacesStart + i]];

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
            MemberType = MemberTypes.TypeInfo;
        }

        // Initialize type from binary usage
        public TypeInfo(Il2CppReflector model, Il2CppType pType, MemberTypes memberType) : base(null) {
            var image = model.Package.BinaryImage;

            IsNested = true;
            MemberType = memberType;

            // Generic type unresolved and concrete instance types
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST) {
                var generic = image.ReadMappedObject<Il2CppGenericClass>(pType.datapoint);
                var genericTypeDef = model.GetTypeFromIndex(generic.typeDefinitionIndex);

                Namespace = genericTypeDef.Namespace;
                Name = genericTypeDef.Name;

                // TODO: Generic* properties and ContainsGenericParameters

                // Get the instantiation
                var genericInstance = image.ReadMappedObject<Il2CppGenericInst>(generic.context.class_inst);

                // Get list of pointers to type parameters (both unresolved and concrete)
                var genericTypeParameters = image.ReadMappedWordArray(genericInstance.type_argv, (int)genericInstance.type_argc);

                GenericTypeParameters = new List<TypeInfo>();
                foreach (var pArg in genericTypeParameters) {
                    var argType = image.ReadMappedObject<Il2CppType>((ulong) pArg);
                    // TODO: Detect whether unresolved or concrete (add concrete to GenericTypeArguments instead)
                    // TODO: GenericParameterPosition etc. in types we generate here
                    GenericTypeParameters.Add(model.GetType(argType)); // TODO: Fix MemberType here
                }
                Attributes |= TypeAttributes.Class;
            }

            // Array with known dimensions and bounds
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_ARRAY) {
                var descriptor = image.ReadMappedObject<Il2CppArrayType>(pType.datapoint);
                var elementType = image.ReadMappedObject<Il2CppType>(descriptor.etype);
                this.elementType = model.GetType(elementType);
                Namespace = ElementType.Namespace;
                Name = ElementType.Name;

                IsArray = true;
                arrayRank = descriptor.rank;
            }

            // Dynamically allocated array
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY) {
                var elementType = image.ReadMappedObject<Il2CppType>(pType.datapoint);
                this.elementType = model.GetType(elementType);
                Namespace = ElementType.Namespace;
                Name = ElementType.Name;

                IsArray = true;
            }

            // Unresolved generic type variable
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VAR || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_MVAR) {
                ContainsGenericParameters = true;
                Attributes |= TypeAttributes.Class;
                IsGenericParameter = true;
                Name = "T"; // TODO: Don't hardcode parameter name

                // TODO: GenericTypeParameters?
            }

            // Pointer type
            // TODO: Should set ElementType etc.
            IsPointer = (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_PTR);
        }
    }
}