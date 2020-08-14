/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using Il2CppInspector.Cpp;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Model
{
    // Class that represents a composite IL/C++ type
    public class AppType
    {
        // The logical group this type is part of
        // This is purely for querying types in related groups and has no bearing on the code
        public string Group { get; set; }

        // The corresponding C++ type definition which represents an instance of the object
        // This is derived from Il2CppObject
        // If the underlying .NET type is a struct (value type), this will return the boxed version
        public CppComplexType CppType { get; internal set; }

        // For an underlying .NET type which is a struct (value type), the unboxed type, otherwise null
        public CppComplexType CppValueType { get; internal set; }

        // The type in the .NET type model this object maps to
        public TypeInfo Type { get; internal set; }

        // The VA of the Il2CppClass object which defines this type (ClassName__TypeInfo)
        public ulong TypeClassAddress { get; internal set; }

        // The VA of the Il2CppType* (VA of the pointer to the Il2CppType) object which references this type (ClassName__TypeRef)
        public ulong TypeRefPtrAddress { get; internal set; }

        public AppType(TypeInfo ilType, CppComplexType cppType, CppComplexType valueType = null,
            ulong cppClassPtr = 0xffffffff_ffffffff, ulong cppTypeRefPtr = 0xffffffff_ffffffff) {
            CppType = cppType;
            Type = ilType;
            CppValueType = valueType;
            TypeClassAddress = cppClassPtr;
            TypeRefPtrAddress = cppTypeRefPtr;
        }

        // The C++ name of the type
        // TODO: Known issue here where we should be using CppDeclarationGenerator.TypeNamer to ensure uniqueness
        // Prefer Foo over Foo__Boxed; if there is no C++ type defined, just convert the IL type to a C identifier
        public string Name => CppValueType?.Name ?? CppType?.Name ?? Type.Name.ToCIdentifier();

        public override string ToString() => Type.FullName + " -> " + CppType.Name;
    }
}