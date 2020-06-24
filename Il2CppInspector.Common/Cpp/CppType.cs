/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Outputs;
using Il2CppInspector.Outputs.UnityHeaders;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Cpp
{
    public abstract class CppType
    {
        // The name of the type
        public string Name { get; }

        // Calculate the size of the C++ type in bytes
        public abstract int GetSize();

        protected CppType(string name) => Name = name;
    }

    // A non-struct non-class type
    public class CppPrimitiveType : CppType
    {
        private readonly int size;

        public override int GetSize() => size;

        public CppPrimitiveType(string name, int size) : base(name) => this.size = size;
    }

    // A struct or class type
    public class CppComplexType : CppType
    {
        // Dictionary of byte offset in the type to each field
        public Dictionary<int, CppField> Fields { get; } = new Dictionary<int, CppField>();

        // Calculate the size by summing the size of each field's type
        public override int GetSize() => Fields.Values.Select(f => f.Type.GetSize()).Sum();

        public CppComplexType(string name) : base(name) {}

        // TODO: Add fields
    }

    // A field in a C++ type
    public struct CppField
    {
        // The name of the field
        public string Name { get; }

        // The offset of the field into the type
        public int Offset { get; }

        // The type of the field
        public CppType Type { get; }
    }

    // A collection of C++ types
    public class CppTypes
    {
        // All of the types
        private Dictionary<string, CppType> types { get; }

        public CppType this[string s] => types[s];

        // Architecture width in bytes (4 bytes for 32-bit or 8 bytes for 64-bit, to determine pointer sizes)
        public int WordSize { get; }

        private static readonly List<CppPrimitiveType> primitiveTypes = new List<CppPrimitiveType> {
            new CppPrimitiveType("uint8_t", 1),
            new CppPrimitiveType("uint16_t", 2),
            new CppPrimitiveType("uint32_t", 4),
            new CppPrimitiveType("uint64_t", 8),
            new CppPrimitiveType("int8_t", 1),
            new CppPrimitiveType("int16_t", 2),
            new CppPrimitiveType("int32_t", 4),
            new CppPrimitiveType("int64_t", 8),
            new CppPrimitiveType("char", 1)
        };

        public CppTypes(int wordSize) {
            WordSize = wordSize;
            types = primitiveTypes.ToDictionary(t => t.Name, t => (CppType) t);

            // This is all compiler-dependent, let's hope for the best!
            types.Add("uintptr_t", new CppPrimitiveType("uintptr_t", WordSize));
            types.Add("size_t", new CppPrimitiveType("size_t", WordSize));
        }

        // Parse a block of C++ source code, adding any types found
        public void AddFromDeclarationText(string text) {
            using StringReader lines = new StringReader(text);

            var rgxTypedef = new Regex(@"typedef (\S+) (\S+);");
            var rgxTypedefFnPtr = new Regex(@"typedef\s+\S+\s*\(\s*\*(\S+)\)\s*\(.*\);");

            string line;
            while ((line = lines.ReadLine()) != null) {

                // Sanitize
                line = line.Trim();
                line = line.Replace(" const ", " ");

                // typedef <retType> (*<alias>)(<args>);
                var typedef = rgxTypedefFnPtr.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[1].Captures[0].ToString();

                    Debug.WriteLine($"[TYPEDEF] {line}  --  Adding method pointer typedef to {alias}");

                    types.Add(alias, types["uintptr_t"]);
                    continue;
                }

                // typedef <targetType> <alias>;
                typedef = rgxTypedef.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[2].Captures[0].ToString();
                    var existingType = typedef.Groups[1].Captures[0].ToString();

                    // Pointers
                    if (existingType.Contains("*")) {
                        // TODO: Typedef pointers
                    }
                    // Regular aliases
                    else {
                        Debug.WriteLine($"[TYPEDEF] {line}  --  Adding typedef from {existingType} to {alias}");

                        types.Add(alias, types[existingType]);
                        continue;
                    }
                }

                Debug.WriteLine("[IGNORE ] " + line);
            }
        }

        // Generate a populated CppTypes object from an
        public static CppTypes FromUnityHeaders(UnityVersion version) {
            var cppTypes = new CppTypes(8);

            // Process Unity headers
            var headers = UnityHeader.GetHeaderForVersion(version).GetHeaderText();
            cppTypes.AddFromDeclarationText(headers);

            return cppTypes;
        }
    }
}
