/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Il2CppInspector.Cpp
{
    public abstract class CppType
    {
        // Calculate the size of the C++ type in bytes
        public abstract int GetSize();
    }

    // A non-struct non-class type
    public class CppPrimitiveType : CppType
    {
        private readonly int size;

        public override int GetSize() => size;

        public CppPrimitiveType(int size) => this.size = size;
    }

    // A struct or class type
    public class CppComplexType : CppType
    {
        // Dictionary of byte offset in the type to each field
        public Dictionary<int, CppField> Fields { get; } = new Dictionary<int, CppField>();

        // Calculate the size by summing the size of each field's type
        public override int GetSize() => Fields.Values.Select(f => f.Type.GetSize()).Sum();
    }

    // A field in a C++ type
    public struct CppField
    {
        // The name of the field
        public string Name { get; }

        // The type of the field
        public CppType Type { get; }
    }

    // A collection of C++ types
    public class CppTypes
    {
        private Dictionary<string, CppType> types { get; }

        public CppType this[string s] => types[s];

        public CppTypes() {
            types = new Dictionary<string, CppType> {
                ["uint8_t"] = new CppPrimitiveType(1),
                ["uint16_t"] = new CppPrimitiveType(2),
                ["uint32_t"] = new CppPrimitiveType(4),
                ["uint64_t"] = new CppPrimitiveType(8),
                ["int8_t"] = new CppPrimitiveType(1),
                ["int16_t"] = new CppPrimitiveType(2),
                ["int32_t"] = new CppPrimitiveType(4),
                ["int64_t"] = new CppPrimitiveType(8),
                ["char"]  = new CppPrimitiveType(1),
                ["void"] = new CppPrimitiveType(4), // or 8; pointer
            };
        }
    }
}
