/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppInspector.Cpp.UnityHeaders;

namespace Il2CppInspector.Cpp
{
    // A collection of C++ types
    public class CppTypes : IEnumerable<CppType>
    {
        // All of the types
        public Dictionary<string, CppType> Types { get; }

        public CppType this[string s] => Types[s];

        public IEnumerator<CppType> GetEnumerator() => Types.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Architecture width in bits (32/64) - to determine pointer sizes
        public int WordSize { get; }

        private static readonly List<CppType> primitiveTypes = new List<CppType> {
            new CppType("uint8_t", 8),
            new CppType("uint16_t", 16),
            new CppType("uint32_t", 32),
            new CppType("uint64_t", 64),
            new CppType("int8_t", 8),
            new CppType("int16_t", 16),
            new CppType("int32_t", 32),
            new CppType("int64_t", 64),
            new CppType("char", 8),
            new CppType("int", 32),
            new CppType("float", 32),
            new CppType("double", 64),
            new CppType("bool", 8),
            new CppType("void", 0)
        };

        public CppTypes(int wordSize) {
            if (wordSize != 32 && wordSize != 64)
                throw new ArgumentOutOfRangeException("Architecture word size must be 32 or 64-bit to generate C++ data");

            WordSize = wordSize;
            Types = primitiveTypes.ToDictionary(t => t.Name, t => t);

            // This is all compiler-dependent, let's hope for the best!
            Add(new CppType("long", WordSize));
            Add(new CppType("intptr_t", WordSize));
            Add(new CppType("uintptr_t", WordSize));
            Add(new CppType("size_t", WordSize));
        }

        #region Code parser
        // Parse a block of C++ source code, adding any types found
        public void AddFromDeclarationText(string text) {
            using StringReader lines = new StringReader(text);

            var rgxExternDecl = new Regex(@"struct (\S+);");
            var rgxTypedefForwardDecl = new Regex(@"typedef struct (\S+) (\S+);");
            var rgxTypedefFnPtr = new Regex(@"typedef\s+(?:struct )?" + CppFnPtrType.Regex + ";");
            var rgxTypedef = new Regex(@"typedef (\S+?)\s*\**\s*(\S+);");
            var rgxFieldFnPtr = new Regex(CppFnPtrType.Regex + @";");
            var rgxField = new Regex(@"^(?:struct |enum )?(\S+?)\s*\**\s*((?:\S|\s*,\s*)+)(?:\s*:\s*([0-9]+))?;");
            var rgxEnumValue = new Regex(@"^\s*([A-Za-z0-9_]+)(?:\s*=\s*(.+?))?,?\s*$");

            var rgxStripKeywords = new Regex(@"\b(?:const|unsigned|volatile)\b");
            var rgxCompressPtrs = new Regex(@"\*\s+\*");

            var rgxArrayField = new Regex(@"(\S+?)\[([0-9]+)\]");

            var rgxAlignment = new Regex(@"__attribute__\(\(aligned\(([0-9]+)\)\)\)");
            var rgxIsBitDirective = new Regex(@"#ifdef\s+IS_(32|64)BIT");
            var rgxSingleLineComment = new Regex(@"/\*.*?\*/");

            var currentType = new Stack<CppComplexType>();
            bool falseIfBlock = false;
            bool inComment = false;
            bool inMethod = false;
            var nextEnumValue = 0ul;
            string line;

            while ((line = lines.ReadLine()) != null) {

                // Remove comments
                if (line.Contains("//"))
                    line = line.Substring(0, line.IndexOf("//", StringComparison.Ordinal));

                // End of multi-line comment?
                if (line.Contains("*/") && inComment) {
                    inComment = false;
                    line = line.Substring(line.IndexOf("*/", StringComparison.Ordinal) + 2);
                }

                if (inComment) {
                    Debug.WriteLine($"[COMMENT      ] {line}");
                    continue;
                }

                // Remove all single-line comments
                line = rgxSingleLineComment.Replace(line, "");

                // Start of multi-line comment?
                if (line.Contains("/*") && !inComment) {
                    inComment = true;
                    line = line.Substring(0, line.IndexOf("/*"));
                }

                // Ignore global variables
                if (line.StartsWith("const ") && currentType.Count == 0) {
                    Debug.WriteLine($"[GLOBAL       ] {line}");
                    continue;
                }

                // Ignore methods
                // Note: This is a very lazy way of processing early version IL2CPP headers
                if (line != "}" && inMethod) {
                    Debug.WriteLine($"[METHOD       ] {line}");
                    continue;
                }

                if (line == "}" && inMethod) {
                    inMethod = false;

                    Debug.WriteLine($"[METHOD END   ] {line}");
                    continue;
                }

                if (line.StartsWith("static inline ")) {
                    inMethod = true;

                    Debug.WriteLine($"[METHOD START ] {line}");
                    continue;
                }

                // Remove keywords we don't care about
                line = rgxStripKeywords.Replace(line, "");

                // Remove whitespace in multiple indirections
                line = rgxCompressPtrs.Replace(line, "**");

                // Process __attribute((aligned(x)))
                var alignment = 0;
                var alignmentMatch = rgxAlignment.Match(line);
                if (alignmentMatch.Success) {
                    alignment = int.Parse(alignmentMatch.Groups[1].Captures[0].ToString());
                    line = rgxAlignment.Replace(line, "");
                }

                line = line.Trim();

                // Ignore blank lines
                if (line.Length == 0)
                    continue;

                // Process #ifs before anything else
                // Doesn't handle nesting but we probably don't need to (use a Stack if we do)
                var ifdef = rgxIsBitDirective.Match(line);
                if (ifdef.Success) {
                    var bits = int.Parse(ifdef.Groups[1].Captures[0].ToString());
                    if (bits != WordSize)
                        falseIfBlock = true;

                    Debug.WriteLine($"[IF           ] {line}");
                    continue;
                }
                if (line == "#else") {
                    falseIfBlock = !falseIfBlock;

                    Debug.WriteLine($"[ELSE         ] {line}");
                    continue;
                }
                if (line == "#endif") {
                    falseIfBlock = false;

                    Debug.WriteLine($"[ENDIF        ] {line}");
                    continue;
                }

                if (falseIfBlock) {
                    Debug.WriteLine($"[FALSE        ] {line}");
                    continue;
                }

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
                    var alias = typedef.Groups[2].Captures[0].ToString();
                    var declType = typedef.Groups[1].Captures[0].ToString();

                    // Sometimes we might get multiple forward declarations for the same type
                    if (!Types.ContainsKey(declType))
                        Types.Add(declType, new CppComplexType(CompoundType.Struct) {Name = declType});

                    // Sometimes the alias might be the same name as the type (this is usually the case)
                    if (!Types.ContainsKey(alias))
                        Types.Add(alias, Types[declType].AsAlias(alias));

                    Debug.WriteLine($"[FORWARD DECL ] {line}");
                    continue;
                }

                // Function pointer
                // typedef <retType> (*<alias>)(<args>);
                typedef = rgxTypedefFnPtr.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[2].Captures[0].ToString();

                    var fnPtrType = CppFnPtrType.FromSignature(this, line);
                    fnPtrType.Name = alias;

                    Types.Add(alias, fnPtrType);

                    Debug.WriteLine($"[TYPEDEF FNPTR] {line}  --  Adding method pointer typedef to {alias}");
                    continue;
                }

                // Alias
                // typedef <targetType>[*..] <alias>;
                typedef = rgxTypedef.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[2].Captures[0].ToString();
                    var existingType = typedef.Groups[1].Captures[0].ToString();

                    // Potential multiple indirection
                    var type = Types[existingType];
                    var pointers = line.Count(c => c == '*');
                    for (int i = 0; i < pointers; i++)
                        type = type.AsPointer(WordSize);

                    Types.Add(alias, type.AsAlias(alias));

                    Debug.WriteLine($"[TYPEDEF {(pointers > 0? "PTR":"VAL")}  ] {line}  --  Adding typedef from {type.Name} to {alias}");
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
                    currentType.Push(new CppComplexType(CompoundType.Enum));
                    nextEnumValue = 0;

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
                    var name = line[1..^1].Trim();
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
                        parent.AddField(new CppField { Name = name, Type = ct });

                        Debug.WriteLine($"[FIELD END    ] {line}  --  {ct.Name} {name}");
                    }
                    continue;
                }

                // Function pointer field
                var fieldFnPtr = rgxFieldFnPtr.Match(line);
                if (fieldFnPtr.Success) {
                    var fnPtrType = CppFnPtrType.FromSignature(this, line);
                    
                    var name = fieldFnPtr.Groups[2].Captures[0].ToString();

                    var ct = currentType.Peek();
                    ct.AddField(new CppField {Name = name, Type = fnPtrType}, alignment);

                    Debug.WriteLine($"[FIELD FNPTR  ] {line}  --  {name}");
                    continue;
                }

                // Pointer or value field
                var field = rgxField.Match(line);

                if (field.Success) {
                    var names = field.Groups[2].Captures[0].ToString();
                    var typeName = field.Groups[1].Captures[0].ToString();

                    // Multiple fields can be separated by commas
                    foreach (var fieldName in names.Split(',')) {
                        string name = fieldName.Trim();

                        // Array
                        var array = rgxArrayField.Match(name);
                        int arraySize = 0;
                        if (array.Success && array.Groups[2].Captures.Count > 0) {
                            arraySize = int.Parse(array.Groups[2].Captures[0].ToString());
                            name = array.Groups[1].Captures[0].ToString();
                        }

                        // Bitfield
                        int bitfield = 0;
                        if (field.Groups[3].Captures.Count > 0)
                            bitfield = int.Parse(field.Groups[3].Captures[0].ToString());

                        // Potential multiple indirection
                        var type = Types[typeName];
                        var pointers = line.Count(c => c == '*');
                        for (int i = 0; i < pointers; i++)
                            type = type.AsPointer(WordSize);

                        var ct = currentType.Peek();

                        if (arraySize > 0)
                            type = type.AsArray(arraySize);

                        ct.AddField(new CppField {Name = name, Type = type, BitfieldSize = bitfield}, alignment);

                        if (bitfield == 0)
                            Debug.WriteLine($"[FIELD {(pointers > 0 ? "PTR" : "VAL")}    ] {line}  --  {name}");
                        else
                            Debug.WriteLine($"[BITFIELD     ] {line}  --  {name} : {bitfield}");
                    }
                    continue;
                }

                // Enum value field
                var enumValue = rgxEnumValue.Match(line);
                if (enumValue.Success) {
                    var name = enumValue.Groups[1].Captures[0].ToString();

                    var value = nextEnumValue++;
                    if (enumValue.Groups[2].Captures.Count > 0) {
                        // Convert the text to a ulong even if it's hexadecimal with a 0x prefix
                        var valueText = enumValue.Groups[2].Captures[0].ToString();
                        var conv = new System.ComponentModel.UInt64Converter();

                        // Handle bit shift operator
                        var values = valueText.Split("<<").Select(t => (ulong) conv.ConvertFromInvariantString(t.Trim())).ToArray();
                        value = values.Length == 1 ? values[0] : values[0] << (int)values[1];
                        nextEnumValue = value + 1;
                    }

                    var ct = currentType.Peek();
                    ct.AddField(new CppEnumField {Name = name, Type = WordSize == 32 ? Types["uint32_t"] : Types["uint64_t"], Value = value});

                    Debug.WriteLine($"[ENUM VALUE   ] {line}  --  {name} = {value}");
                    continue;
                }

                // Make sure we're not ignoring anything we shouldn't
                Debug.WriteLine($"[IGNORE       ] {line}");

                // Block opens
                if (line == "{")
                    continue;

                // Global variables
                if (line.StartsWith("static"))
                    continue;

                // Pragma directives
                if (line.StartsWith("#pragma"))
                    continue;

                // Imports
                if (line.StartsWith("extern"))
                    continue;

                throw new InvalidOperationException("Could not understand C++ code: " + line);
            }
        }
        #endregion

        // Get a type from its name, handling pointer types
        public CppType GetType(string typeName) {
            var baseName = typeName.Replace("*", "");
            var indirectionCount = typeName.Length - baseName.Length;

            var type = Types[baseName.Trim()];
            for (int i = 0; i < indirectionCount; i++)
                type = type.AsPointer(WordSize);

            return type;
        }

        // Add a type externally
        public void Add(CppType type) => Types.Add(type.Name, type);

        // Generate a populated CppTypes object from a set of Unity headers
        public static CppTypes FromUnityVersion(UnityVersion version, int wordSize = 32)
            => FromUnityHeaders(UnityHeader.GetHeaderForVersion(version), wordSize);

        public static CppTypes FromUnityHeaders(UnityHeader header, int wordSize = 32) {
            var cppTypes = new CppTypes(wordSize);

            // Add junk from config files we haven't included
            cppTypes.Add(new CppType("Il2CppIManagedObjectHolder"));
            cppTypes.Add(new CppType("Il2CppIUnknown"));

            // Process Unity headers
            var headers = header.GetHeaderText();
            cppTypes.AddFromDeclarationText(headers);

            return cppTypes;
        }
    }
}
