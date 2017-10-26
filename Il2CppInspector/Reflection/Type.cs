using System;
using System.Collections.Generic;
using System.Reflection;

namespace Il2CppInspector.Reflection {
    public class Type : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppTypeDefinition Definition { get; }
        public int Index { get; }

        // (not code attributes)
        // Undefined if the Type represents a generic type parameter
        public TypeAttributes Attributes { get; } // TODO

        // Type that this type inherits from
        public Type BaseType { get; } // TODO

        // TODO: ContainsGenericParameters

        // Method that the type is declared in if this is a type parameter of a generic method
        public MethodBase DeclaringMethod { get; } // TODO

        // Gets the type of the object encompassed or referred to by the current array, pointer or reference type
        public Type ElementType { get; } // TODO

        // Type name including namespace
        public string FullName => Namespace + "." + Name;

        // TODO: Generic stuff

        public bool HasElementType { get; } // TODO
        public bool IsAbstract { get; }
        public bool IsArray { get; } // TODO
        public bool IsByRef { get; } // TODO
        public bool IsClass { get; }
        public bool IsEnum { get; } // TODO
        public bool IsGenericParameter { get; } // TODO
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

        public string Namespace { get; }

        // Number of dimensions of an array
        public int GetArrayRank() => throw new NotImplementedException();

        public List<ConstructorInfo> Constructors { get; } // TODO

        public List<Type> Inerfaces { get; } // TODO

        public List<MemberInfo> Members { get; } // TODO

        public List<MethodInfo> Methods { get; } // TODO

        public List<FieldInfo> Fields { get; } // TODO

        public List<Type> NestedTypes { get; } // TODO

        public List<PropertyInfo> Properties { get; } // TODO

        // TODO: Custom attribute stuff

        public string[] GetEnumNames() => throw new NotImplementedException();

        public Type GetEnumUnderlyingType() => throw new NotImplementedException();

        public Array GetEnumValues() => throw new NotImplementedException();

        // TODO: Event stuff

        // TODO: Generic stuff

        // Initialize from specified type index in package
        public Type(Il2CppInspector pkg, int typeIndex, Assembly owner) {
            Assembly = owner;
            Definition = pkg.Metadata.Types[typeIndex];
            Index = typeIndex;
            Name = pkg.Metadata.Strings[Definition.nameIndex];
            Namespace = pkg.Metadata.Strings[Definition.namespaceIndex];

            IsSerializable = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_SERIALIZABLE) != 0;
            IsPublic = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == DefineConstants.TYPE_ATTRIBUTE_PUBLIC;
            IsAbstract = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_ABSTRACT) != 0;
            IsSealed = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_SEALED) != 0;
            IsInterface = (Definition.flags & DefineConstants.TYPE_ATTRIBUTE_INTERFACE) != 0;
            IsClass = !IsInterface;
        }
    }
}