using System;
using System.Collections.Generic;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public abstract class MemberInfo
    {
        // Assembly that this member is defined in
        public Assembly Assembly { get; set; }

        // Custom attributes for this member
        public IEnumerable<CustomAttributeData> CustomAttributes { get; set; }

        // Type that this type is declared in for nested types
        public Type DeclaringType { get; set; }

        // What sort of member this is, eg. method, field etc.
        public MemberTypes MemberType { get; set; }

        // Name of the member
        public string Name { get; set; }

        // TODO: GetCustomAttributes etc.
    }

    public class Type : MemberInfo
    {
        // (not code attributes)
        // Undefined if the Type represents a generic type parameter
        public TypeAttributes Attributes { get; set; }

        // Type that this type inherits from
        public Type BaseType { get; set; }

        // TODO: ContainsGenericParameters

        // Method that the type is declared in if this is a type parameter of a generic method
        public MethodBase DeclaringMethod { get; set; }

        // Gets the type of the object encompassed or referred to by the current array, pointer or reference type
        public Type ElementType { get; set; }

        // Type name including namespace
        public string FullName { get; set; }

        // TODO: Generic stuff

        public bool HasElementType { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsArray { get; set; }
        public bool IsByRef { get; set; }
        public bool IsClass { get; set; }
        public bool IsEnum { get; set; }
        public bool IsGenericParameter { get; set; }
        public bool IsGenericType { get; set; }
        public bool IsGenericTypeDefinition { get; set; }
        public bool IsInterface { get; set; }
        public bool IsNested { get; set; }
        public bool IsNestedPrivate { get; set; }
        public bool IsNestedPublic { get; set; }
        public bool IsPointer { get; set; }
        public bool IsPrimitive { get; set; }
        public bool IsPublic { get; set; }
        public bool IsSealed { get; set; }
        public bool IsSerializable { get; set; }
        public bool IsValueType { get; set; }

        public string Namespace { get; set; }

        // Number of dimensions of an array
        public int GetArrayRank() => throw new NotImplementedException();

        public List<ConstructorInfo> Constructors { get; set; }

        public List<Type> Inerfaces { get; set; }

        public List<MemberInfo> Members { get; set; }

        public List<MethodInfo> Methods { get; set; }

        public List<FieldInfo> Fields { get; set; }

        public List<Type> NestedTypes { get; set; }

        public List<PropertyInfo> Properties { get; set; }

        // TODO: Custom attribute stuff

        public string[] GetEnumNames() => throw new NotImplementedException();

        public Type GetEnumUnderlyingType() => throw new NotImplementedException();

        public Array GetEnumValues() => throw new NotImplementedException();

        // TODO: Event stuff

        // TODO: Generic stuff
    }

    public abstract class MethodBase : MemberInfo
    {
        // (not code attributes)
        public MethodAttributes Attributes { get; set; }

        // TODO: ContainsGenericParameters
    }

    public class ConstructorInfo : MethodBase
    {
        // TODO
    }

    public class MethodInfo : MethodBase
    {
        // TODO
    }

    public class FieldInfo : MemberInfo
    {
        // TODO
    }

    public class PropertyInfo : MemberInfo
    {
        // TODO
    }

    public class CustomAttributeData
    {
        // TODO
    }
}
