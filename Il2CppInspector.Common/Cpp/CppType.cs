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
    // Compound type
    public enum CompoundType
    {
        Struct,
        Union
    }

    // A type with no fields
    public class CppType
    {
        // The name of the type
        public string Name { get; internal set; }

        // The size of the C++ type in bytes
        public virtual int Size { get; protected set; }

        public CppType(string name, int size) {
            Name = name;
            Size = size;
        }

        public override string ToString() => Name + " // Size: " + Size;
    }

    // A struct, union or class type (type with fields)
    public class CppComplexType : CppType
    {
        // The compound type
        public CompoundType CompoundType;

        // Dictionary of byte offset in the type to each field
        // Unions and bitfields can have more than one field at the same offset
        public Dictionary<int, List<CppField>> Fields { get; internal set; } = new Dictionary<int, List<CppField>>();

        public CppComplexType(CompoundType compoundType) : base("", 0) => CompoundType = compoundType;

        // Size can't be calculated lazily (as we go along adding fields) because of forward declarations
        public override int Size =>
            CompoundType == CompoundType.Union
                // Union size is the size of the largest element in the union
                ? Fields.Values.SelectMany(f => f).Select(f => f.Size).Max()
                : Fields.Values.SelectMany(f => f).Select(f => f.Size).Sum();

        // Add a field to the type. Returns the offset of the field in the type
        public int AddField(CppField field) {
            // TODO: Use InheritanceStyleEnum to determine whether the field is embedded or a pointer
            field.Offset = CompoundType == CompoundType.Struct ? Size : 0;

            if (Fields.ContainsKey(field.Offset))
                Fields[field.Offset].Add(field);
            else
                Fields.Add(field.Offset, new List<CppField> { field });

            return Size;
        }

        // Summarize all field names and offsets
        public override string ToString() {
            var sb = new StringBuilder();

            sb.Append(CompoundType == CompoundType.Struct ? "struct " : "union ");
            sb.AppendLine(Name + (Name.Length > 0? " ":"") + "{");

            foreach (var field in Fields.Values.SelectMany(f => f))
                sb.AppendLine("  " + field);

            sb.Append($"}}; // Size: 0x{Size:x2}");

            return sb.ToString();
        }
    }

    // A field in a C++ type
    public struct CppField
    {
        // The type collection this belongs to
        public CppTypes CppTypes { get; set; }

        // The name of the field
        public string Name { get; set; }

        // The offset of the field into the type
        public int Offset { get; set; }

        // The size of the field (this will differ from the type size if the field is a pointer)
        public int Size => IsPointer? CppTypes.WordSize : Type.Size;

        // The type of the field
        // This type will be wrong (by design) for function pointers, pointers to pointers etc.
        // and we only want it to calculate offsets so we hide this
        internal CppType Type { get; set; }

        // True if the field is a pointer
        internal bool IsPointer { get; set; }

        // C++ representation of field (might be incorrect due to the above)
        public override string ToString() => $"/* 0x{Offset:x2} - 0x{Offset + Size - 1:x2} (0x{Size:x2}) */ " + Name +
                                             // nested anonymous types
                                             (Type.Name == "" ? "\n" + Type : "");
    }

    // A collection of C++ types
    public class CppTypes : IEnumerable<CppType>
    {
        // All of the types
        public Dictionary<string, CppType> Types { get; }

        public CppType this[string s] => Types[s];

        public IEnumerator<CppType> GetEnumerator() => Types.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
            new CppType("char", 1),
            new CppType("int", 4),
            new CppType("float", 4),
            new CppType("double", 8)
        };

        public CppTypes(int wordSize) {
            WordSize = wordSize;
            Types = primitiveTypes.ToDictionary(t => t.Name, t => t);

            // This is all compiler-dependent, let's hope for the best!
            Types.Add("uintptr_t", new CppType("uintptr_t", WordSize));
            Types.Add("size_t", new CppType("size_t", WordSize));
            Types.Add("void", new CppType("void", WordSize));
        }

        // Parse a block of C++ source code, adding any types found
        public void AddFromDeclarationText(string text) {
            using StringReader lines = new StringReader(text);

            var rgxExternDecl = new Regex(@"struct (\S+);");
            var rgxTypedefForwardDecl = new Regex(@"typedef struct (\S+) (\S+);");
            var rgxTypedefFnPtr = new Regex(@"typedef\s+(?:struct )?\S+\s*\(\s*\*(\S+)\)\s*\(.*\);");
            var rgxTypedefPtr = new Regex(@"typedef (\S+)\s*\*\s*(\S+);");
            var rgxTypedef = new Regex(@"typedef (\S+) (\S+);");
            var rgxFieldFnPtr = new Regex(@"\S+\s*\(\s*\*(\S+)\)\s*\(.*\);");
            var rgxFieldPtr = new Regex(@"^(?:struct )?(\S+)\s*\*\s*(\S+);");
            var rgxFieldVal = new Regex(@"^(?:struct )?(\S+)\s+(\S+);");

            var currentType = new Stack<CppComplexType>();
            bool inEnum = false;
            string line;

            // TODO: Bitfields
            // TODO: Arrays
            // TODO: Alignment directives
            // TODO: unsigned
            // TODO: enum prefix in field (Il2CppWindowsRuntimeTypeName)
            // TODO: comma-separated fields
            // TODO: #ifdef IS_32BIT
            // TODO: volatile

            while ((line = lines.ReadLine()) != null) {

                // Sanitize
                line = line.Trim();
                line = line.Replace(" const ", " ");
                line = line.Replace("const*", "*");
                if (line.StartsWith("const "))
                    line = line.Substring(6);
                line = line.Replace("**", "*"); // we don't care about pointers to pointers
                line = line.Replace("* *", "*"); // pointer to const pointer

                // External declaration
                // struct <external-type>;
                // NOTE: Unfortunately we're not going to ever know the size of this type
                var externDecl = rgxExternDecl.Match(line);
                if (externDecl.Success) {
                    var declType = externDecl.Groups[1].Captures[0].ToString();

                    Types.Add(declType, new CppComplexType(CompoundType.Struct) {Name = declType});

                    Debug.WriteLine($"[EXTERN DECL  ] {line}");
                    continue;
                }

                // Forward declaration
                // typedef struct <struct-type> <alias>
                var typedef = rgxTypedefForwardDecl.Match(line);
                if (typedef.Success) {
                    // We're lazy so we're just going to hope that the struct and alias names are the same
                    var alias = typedef.Groups[2].Captures[0].ToString();
                    var declType = typedef.Groups[1].Captures[0].ToString();

                    Debug.Assert(alias == declType);

                    // Sometimes we might get multiple forward declarations for the same type
                    if (!Types.ContainsKey(alias))
                        Types.Add(alias, new CppComplexType(CompoundType.Struct) {Name = alias});

                    Debug.WriteLine($"[FORWARD DECL ] {line}");
                    continue;
                }

                // Function pointer
                // typedef <retType> (*<alias>)(<args>);
                typedef = rgxTypedefFnPtr.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[1].Captures[0].ToString();
                    Types.Add(alias, Types["uintptr_t"]);

                    Debug.WriteLine($"[TYPEDEF FNPTR] {line}  --  Adding method pointer typedef to {alias}");
                    continue;
                }

                // Pointer alias
                // typedef <targetType>* <alias>;
                typedef = rgxTypedefPtr.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[2].Captures[0].ToString();
                    Types.Add(alias, Types["uintptr_t"]);

                    Debug.WriteLine($"[TYPEDEF PTR  ] {line}  --  Adding pointer typedef to {alias}");
                    continue;
                }

                // Alias
                // typedef <targetType> <alias>;
                typedef = rgxTypedef.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[2].Captures[0].ToString();
                    var existingType = typedef.Groups[1].Captures[0].ToString();

                    Types.Add(alias, Types[existingType]);

                    Debug.WriteLine($"[TYPEDEF ALIAS] {line}  --  Adding typedef from {existingType} to {alias}");
                    continue;
                }

                // Start of struct
                // typedef struct <optional-type-name>
                if ((line.StartsWith("typedef struct") || line.StartsWith("struct ")) && line.IndexOf(";", StringComparison.Ordinal) == -1
                    && currentType.Count == 0) {
                    currentType.Push(new CppComplexType(CompoundType.Struct));

                    if (line.StartsWith("struct "))
                        currentType.Peek().Name = line.Split(' ')[1];

                    Debug.WriteLine($"\n[STRUCT START ] {line}");
                    continue;
                }

                // Start of union
                // typedef union <optional-type-name>
                if (line.StartsWith("typedef union") && line.IndexOf(";", StringComparison.Ordinal) == -1) {
                    currentType.Push(new CppComplexType(CompoundType.Union));

                    Debug.WriteLine($"\n[UNION START  ] {line}");
                    continue;
                }

                // Start of enum
                // typedef enum <optional-type-name>
                if (line.StartsWith("typedef enum") && line.IndexOf(";", StringComparison.Ordinal) == -1) {
                    inEnum = true;

                    Debug.WriteLine($"\n[ENUM START   ] {line}");
                    continue;
                }

                // Nested complex field
                // struct <optional-type-name>
                // union <optional-type-name>
                var words = line.Split(' ');
                if ((words[0] == "union" || words[0] == "struct") && words.Length <= 2) {
                    currentType.Push(new CppComplexType(words[0] == "struct"? CompoundType.Struct : CompoundType.Union));

                    Debug.WriteLine($"[FIELD START   ] {line}");
                    continue;
                }

                // End of already named struct
                if (line == "};" && currentType.Count == 1) {
                    var ct = currentType.Pop();
                    if (!Types.ContainsKey(ct.Name))
                        Types.Add(ct.Name, ct);
                    else
                        ((CppComplexType) Types[ct.Name]).Fields = ct.Fields;

                    Debug.WriteLine($"[STRUCT END   ] {line}  --  {ct.Name}\n");
                    continue;
                }

                // End of complex field, complex type or enum
                // end of [typedef] struct/union/enum
                if (line.StartsWith("}") && line.EndsWith(";")) {
                    // We need this to avoid false positives on field matches below
                    if (currentType.Count == 0 && !inEnum) {
                        Debug.WriteLine($"[IGNORE       ] {line}");
                        continue;
                    }

                    var name = line[1..^1].Trim();

                    if (inEnum) {
                        Types.Add(name, new CppType(name, WordSize));
                        inEnum = false;

                        Debug.WriteLine($"[ENUM END     ] {line}  --  {name}\n");
                        continue;
                    }

                    var ct = currentType.Pop();

                    // End of top-level typedef, so it's a type name
                    if (currentType.Count == 0) {
                        ct.Name = name;

                        if (!Types.ContainsKey(name))
                            Types.Add(name, ct);

                        // We will have to copy the type data if the type was forward declared,
                        // because other types are already referencing it; replacing it in the
                        // collection will not replace the references to the empty version in
                        // other types
                        else {
                            ((CppComplexType) Types[name]).Fields = ct.Fields;
                        }

                        Debug.WriteLine($"[STRUCT END   ] {line}  --  {name}\n");
                    }

                    // Otherwise it's a field name in the current type
                    else {
                        var parent = currentType.Peek();
                        parent.AddField(new CppField { CppTypes = this, Name = name, Type = ct });

                        Debug.WriteLine($"[FIELD END    ] {line}  --  {ct.Name} {name}");
                    }
                    continue;
                }

                // Function pointer field
                var fieldFnPtr = rgxFieldFnPtr.Match(line);
                if (fieldFnPtr.Success) {
                    var name = fieldFnPtr.Groups[1].Captures[0].ToString();

                    var ct = currentType.Peek();
                    ct.AddField(new CppField() {CppTypes = this, Name = name, Type = Types["uintptr_t"], IsPointer = false});

                    Debug.WriteLine($"[FIELD FNPTR  ] {line}  --  {name}");
                    continue;
                }

                // Pointer or value field
                var fieldPtr = rgxFieldPtr.Match(line);
                var fieldVal = rgxFieldVal.Match(line);

                if (fieldPtr.Success || fieldVal.Success) {
                    var fieldMatch = fieldPtr.Success ? fieldPtr : fieldVal;
                    var name = fieldMatch.Groups[2].Captures[0].ToString();
                    var type = fieldMatch.Groups[1].Captures[0].ToString();

                    var ct = currentType.Peek();
                    ct.AddField(new CppField {CppTypes = this, Name = name, Type = Types[type], IsPointer = fieldPtr.Success});

                    Debug.WriteLine($"[FIELD {(fieldPtr.Success? "PTR":"VAL")}    ] {line}  --  {name}");
                    continue;
                }

                Debug.WriteLine($"[IGNORE       ] {line}");
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
