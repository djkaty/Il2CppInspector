/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

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
        public TypeAttributes Attributes { get; } // TODO

        // Type that this type inherits from
        public TypeInfo BaseType { get; } // TODO

        // True if the type contains unresolved generic type parameters
        public bool ContainsGenericParameters { get; }

        // C# colloquial name of the type (if available)
        public string CSharpName {
            get {
                var s = Namespace + "." + base.Name;
                var i = DefineConstants.FullNameTypeString.IndexOf(s);
                var n = (i != -1 ? DefineConstants.CSharpTypeString[i] : base.Name);
                if (IsArray)
                    n = ElementType.CSharpName;
                var g = (GenericTypeParameters != null ? "<" + string.Join(", ", GenericTypeParameters.Select(x => x.CSharpName)) + ">" : "");
                return (IsPointer ? "void *" : "") + n + g + (IsArray ? "[]" : "");
            }
        }

        public List<ConstructorInfo> DeclaredConstructors { get; } // TODO
        public List<EventInfo> DeclaredEvents { get; } // TODO
        public List<FieldInfo> DeclaredFields { get; } = new List<FieldInfo>();
        public List<MemberInfo> DeclaredMembers { get; } // TODO
        public List<MethodInfo> DeclaredMethods { get; } // TODO
        public List<TypeInfo> DeclaredNestedTypes { get; } // TODO
        public List<PropertyInfo> DeclaredProperties { get; } // TODO

        // Method that the type is declared in if this is a type parameter of a generic method
        public MethodBase DeclaringMethod { get; } // TODO

        // Gets the type of the object encompassed or referred to by the current array, pointer or reference type
        public TypeInfo ElementType { get; }

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
        public bool IsAbstract { get; }
        public bool IsArray { get; }
        public bool IsByRef { get; } // TODO
        public bool IsClass { get; }
        public bool IsEnum { get; } // TODO
        public bool IsGenericParameter { get; }
        public bool IsGenericType { get; } // TODO
        public bool IsGenericTypeDefinition { get; } // TODO
        public bool IsInterface { get; }
        public bool IsNested { get; } // TODO
        public bool IsNestedPrivate { get; } // TODO
        public bool IsNestedPublic { get; } // TODO
        public bool IsPointer { get; } // TODO
        public bool IsPrimitive { get; } // TODO
        public bool IsPublic { get; }
        public bool IsSealed { get; }
        public bool IsSerializable { get; }
        public bool IsValueType { get; } // TODO

        public override MemberTypes MemberType { get; }

        public override string Name {
            get => (IsPointer ? "void *" : "")
                + base.Name
                + (GenericTypeParameters != null? "<" + string.Join(", ", GenericTypeParameters.Select(x => x.Name)) + ">" : "")
                + (IsArray ? "[]" : "");
            protected set => base.Name = value;
        }

        public string Namespace { get; }

        // Number of dimensions of an array
        private readonly int arrayRank;
        public int GetArrayRank() => arrayRank;

        // TODO: Custom attribute stuff

        public string[] GetEnumNames() => throw new NotImplementedException();

        public TypeInfo GetEnumUnderlyingType() => throw new NotImplementedException();

        public Array GetEnumValues() => throw new NotImplementedException();

        // TODO: Event stuff

        // TODO: Generic stuff

        // Initialize from specified type index in metadata
        public TypeInfo(Il2CppInspector pkg, int typeIndex, Assembly owner) :
            base(owner) {
            Definition = pkg.TypeDefinitions[typeIndex];
            Index = typeIndex;
            Namespace = pkg.Strings[Definition.namespaceIndex];
            Name = pkg.Strings[pkg.TypeDefinitions[typeIndex].nameIndex];

            IsSerializable = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_SERIALIZABLE) != 0;
            IsPublic = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == DefineConstants.TYPE_ATTRIBUTE_PUBLIC;
            IsAbstract = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_ABSTRACT) != 0;
            IsSealed = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_SEALED) != 0;
            IsInterface = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_INTERFACE) != 0;
            IsClass = !IsInterface;

            for (var f = Definition.fieldStart; f < Definition.fieldStart + Definition.field_count; f++)
                DeclaredFields.Add(new FieldInfo(pkg, f, this));

            MemberType = MemberTypes.TypeInfo;
        }

        // Initialize type from binary usage
        public TypeInfo(Il2CppReflector model, Il2CppType pType, MemberTypes memberType) : base(null) {
            var image = model.Package.Binary.Image;

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
                var genericTypeParameters = image.ReadMappedArray<uint>(genericInstance.type_argv, (int)genericInstance.type_argc);

                GenericTypeParameters = new List<TypeInfo>();
                foreach (var pArg in genericTypeParameters) {
                    var argType = image.ReadMappedObject<Il2CppType>(pArg);
                    // TODO: Detect whether unresolved or concrete (add concrete to GenericTypeArguments instead)
                    // TODO: GenericParameterPosition etc. in types we generate here
                    GenericTypeParameters.Add(model.GetType(argType)); // TODO: Fix MemberType here
                }

                IsClass = true;
                IsInterface = !IsClass;
            }

            // Array with known dimensions and bounds
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_ARRAY) {
                var descriptor = image.ReadMappedObject<Il2CppArrayType>(pType.datapoint);
                var elementType = image.ReadMappedObject<Il2CppType>(descriptor.etype);
                ElementType = model.GetType(elementType);
                Namespace = ElementType.Namespace;
                Name = ElementType.Name;

                IsArray = true;
                arrayRank = descriptor.rank;
            }

            // Dynamically allocated array
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY) {
                var elementType = image.ReadMappedObject<Il2CppType>(pType.datapoint);
                ElementType = model.GetType(elementType);
                Namespace = ElementType.Namespace;
                Name = ElementType.Name;

                IsArray = true;
            }

            // Unresolved generic type variable
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VAR) {
                ContainsGenericParameters = true;
                IsClass = true;
                IsGenericParameter = true;
                Name = "T"; // TODO: Don't hardcode parameter name

                // TODO: GenericTypeParameters?
            }

            // Pointer type
            IsPointer = (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_PTR);
        }
    }
}