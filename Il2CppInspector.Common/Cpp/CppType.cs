/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections;
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
    // A type with no fields
    public class CppType
    {
        // The name of the type
        public string Name { get; internal set; }

        // The size of the C++ type in bytes
        public int Size { get; protected set; }

        public CppType(string name, int size) {
            Name = name;
            Size = size;
        }
    }

    // A struct, union or class type (type with fields)
    public class CppComplexType : CppType
    {
        // Dictionary of byte offset in the type to each field
        // Unions and bitfields can have more than one field at the same offset
        public Dictionary<int, List<CppField>> Fields { get; } = new Dictionary<int, List<CppField>>();

        public CppComplexType() : base("", 0) {}

        // Add a field to the type. Returns the offset of the field in the type
        public int AddField(CppField field) {
            // TODO: Use InheritanceStyleEnum to determine whether the field is embedded or a pointer
            field.Offset = Size;

            if (Fields.ContainsKey(Size))
                Fields[Size].Add(field);
            else
                Fields.Add(Size, new List<CppField> { field });

            Size += field.Type.Size;
            return Size;
        }
    }

    // A field in a C++ type
    public struct CppField
    {
        // The name of the field
        public string Name { get; set; }

        // The offset of the field into the type
        public int Offset { get; set; }

        // The type of the field
        public CppType Type { get; set; }
    }

    // A collection of C++ types
    public class CppTypes
    {
        // All of the types
        private Dictionary<string, CppType> types { get; }

        public CppType this[string s] => types[s];

        // Architecture width in bytes (4 bytes for 32-bit or 8 bytes for 64-bit, to determine pointer sizes)
        public int WordSize { get; }

        private static readonly List<CppType> primitiveTypes = new List<CppType> {
            new CppType("uint8_t", 1),
            new CppType("uint16_t", 2),
            new CppType("uint32_t", 4),
            new CppType("uint64_t", 8),
            new CppType("int8_t", 1),
            new CppType("int16_t", 2),
            new CppType("int32_t", 4),
            new CppType("int64_t", 8),
            new CppType("char", 1)
        };

        public CppTypes(int wordSize) {
            WordSize = wordSize;
            types = primitiveTypes.ToDictionary(t => t.Name, t => (CppType) t);

            // This is all compiler-dependent, let's hope for the best!
            types.Add("uintptr_t", new CppType("uintptr_t", WordSize));
            types.Add("size_t", new CppType("size_t", WordSize));
        }

        // Parse a block of C++ source code, adding any types found
        public void AddFromDeclarationText(string text) {
            using StringReader lines = new StringReader(text);

            var rgxTypedef = new Regex(@"typedef (\S+) (\S+);");
            var rgxTypedefFnPtr = new Regex(@"typedef\s+\S+\s*\(\s*\*(\S+)\)\s*\(.*\);");

            var currentType = new Stack<CppComplexType>();
            string line;

            while ((line = lines.ReadLine()) != null) {

                // Sanitize
                line = line.Trim();
                line = line.Replace(" const ", " ");
                if (line.StartsWith("const "))
                    line = line.Substring(6);

                // Function pointer
                // typedef <retType> (*<alias>)(<args>);
                var typedef = rgxTypedefFnPtr.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[1].Captures[0].ToString();
                    types.Add(alias, types["uintptr_t"]);

                    Debug.WriteLine($"[TYPEDEF PTR  ] {line}  --  Adding method pointer typedef to {alias}");
                    continue;
                }

                // Alias
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
                        types.Add(alias, types[existingType]);

                        Debug.WriteLine($"[TYPEDEF ALIAS] {line}  --  Adding typedef from {existingType} to {alias}");
                        continue;
                    }
                }

                // Start of struct
                // typedef struct <optional-type-name>
                if (line.StartsWith("typedef struct") && line.IndexOf(";", StringComparison.Ordinal) == -1) {
                    currentType.Push(new CppComplexType());

                    Debug.WriteLine($"[STRUCT START ] {line}");
                    continue;
                }

                // Nested complex field
                // struct <optional-type-name>
                // union <optional-type-name>
                var words = line.Split(' ');
                if ((words[0] == "union" || words[0] == "struct") && words.Length <= 2) {
                    currentType.Push(new CppComplexType());

                    Debug.WriteLine($"[FIELD START   ] {line}");
                    continue;
                }

                // End of complex field or complex type
                // end of [typedef] struct/union
                if (line.StartsWith("}") && line.EndsWith(";") && currentType.Count > 0) {
                    var ct = currentType.Pop();
                    var name = line[1..^1].Trim();

                    // End of top-level typedef, so it's a type name
                    if (currentType.Count == 0) {
                        ct.Name = name;
                        types.Add(ct.Name, ct);

                        Debug.WriteLine($"[STRUCT END   ] {line}  --  {name}");
                    }

                    // Otherwise it's a field name in the current type
                    else {
                        var parent = currentType.Peek();
                        parent.AddField(new CppField { Name = name, Type = ct });

                        Debug.WriteLine($"[FIELD END    ] {line}  --  {ct.Name} {name}");
                    }

                    continue;
                }

                Debug.WriteLine("[IGNORE       ] " + line);
            }
        }

        // Generate a populated CppTypes object from a set of Unity headers
        public static CppTypes FromUnityHeaders(UnityVersion version) {
            var cppTypes = new CppTypes(8);

            // Process Unity headers
            var headers = UnityHeader.GetHeaderForVersion(version).GetHeaderText();
            cppTypes.AddFromDeclarationText(headers);

            return cppTypes;
        }
    }
}
