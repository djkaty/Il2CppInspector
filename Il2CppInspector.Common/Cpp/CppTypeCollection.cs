/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppInspector.Cpp.UnityHeaders;

namespace Il2CppInspector.Cpp
{
    // A collection of C++ types
    public class CppTypeCollection : IEnumerable<CppType>
    {
        // All of the types
        public Dictionary<string, CppType> Types { get; }

        // All of the literal typedef aliases
        public Dictionary<string, CppType> TypedefAliases { get; } = new Dictionary<string, CppType>();

        public CppType this[string s] => Types.ContainsKey(s)? Types[s] :
                                         TypedefAliases.ContainsKey(s)? TypedefAliases[s].AsAlias(s) : null;

        public IEnumerator<CppType> GetEnumerator() => Types.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Architecture width in bits (32/64) - to determine pointer sizes
        public int WordSize { get; }

        private Dictionary<string, ComplexValueType> complexTypeMap = new Dictionary<string, ComplexValueType> {
            ["struct"] = ComplexValueType.Struct,
            ["union"] = ComplexValueType.Union,
            ["enum"] = ComplexValueType.Enum
        };

        // The group that the next added type(s) will be placed in
        private string currentGroup = string.Empty;
        public void SetGroup(string group) => currentGroup = group;

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

        public CppTypeCollection(int wordSize) {
            if (wordSize != 32 && wordSize != 64)
                throw new ArgumentOutOfRangeException("Architecture word size must be 32 or 64-bit to generate C++ data");

            WordSize = wordSize;
            Types = primitiveTypes.ToDictionary(t => t.Name, t => t);

            // This is all compiler-dependent, let's hope for the best!
            Add(new CppType("long", WordSize));
            Add(new CppType("intptr_t", WordSize));
            Add(new CppType("uintptr_t", WordSize));
            Add(new CppType("size_t", WordSize));

            foreach (var type in Types.Values)
                type.Group = "primitive";
        }

        #region Code parser
        // Parse a block of C++ source code, adding any types found
        public void AddFromDeclarationText(string text) {
            using StringReader lines = new StringReader(text);

            var rgxForwardDecl = new Regex(@"(struct|union)\s+(\S+);");
            var rgxTypedefAlias = new Regex(@"typedef\s+(?:(struct|union)\s+)?(\S+)\s+(\S+);");
            var rgxTypedefFnPtr = new Regex(@"typedef\s+(?:struct\s+)?" + CppFnPtrType.Regex + ";");
            var rgxDefinition = new Regex(@"^(typedef\s+)?(struct|union|enum)");
            var rgxFieldFnPtr = new Regex(CppFnPtrType.Regex + @";");
            var rgxField = new Regex(@"^(?:struct\s+|enum\s+)?(\S+?\s*\**)\s*((?:\S|\s*,\s*)+)(?:\s*:\s*([0-9]+))?;");
            var rgxEnumValue = new Regex(@"^\s*([A-Za-z0-9_]+)(?:\s*=\s*(.+?))?,?\s*$");
            var rgxIsConst = new Regex(@"\bconst\b");

            var rgxStripKeywords = new Regex(@"\b(?:const|unsigned|volatile)\b");
            var rgxCompressPtrs = new Regex(@"\*\s+\*");

            var rgxArrayField = new Regex(@"(\S+?)\[([0-9]+)\]");

            var rgxAlignment = new Regex(@"__attribute__\(\(aligned\(([0-9]+)\)\)\)\s+");
            var rgxIsBitDirective = new Regex(@"#ifdef\s+IS_(32|64)BIT");
            var rgxSingleLineComment = new Regex(@"/\*.*?\*/");

            var currentType = new Stack<CppComplexType>();
            bool falseIfBlock = false;
            bool inComment = false;
            bool inMethod = false;
            bool inTypedef = false;
            var nextEnumValue = 0ul;
            string rawLine, line;

            while ((rawLine = line = lines.ReadLine()) != null) {

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

                // Ignore defines
                if (line.StartsWith("#define"))
                    continue;

                // Process #ifdefs before anything else
                // Doesn't handle nesting but we probably don't need to (use a Stack if we do)
                var ifdef = rgxIsBitDirective.Match(line);
                if (ifdef.Success) {
                    var bits = int.Parse(ifdef.Groups[1].Captures[0].ToString());
                    if (bits != WordSize)
                        falseIfBlock = true;

                    Debug.WriteLine($"[IF           ] {line}");
                    continue;
                }
                // Other #ifdef
                if (line.StartsWith("#ifdef") || line.StartsWith("#if ")) {
                    falseIfBlock = true;

                    Debug.WriteLine($"[IF           ] {line}");
                    continue;
                }
                if (line.StartsWith("#ifndef")) {
                    falseIfBlock = false;

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

                // Forward declaration
                // <struct|union> <external-type>;
                var externDecl = rgxForwardDecl.Match(line);
                if (externDecl.Success) {
                    var complexType = complexTypeMap[externDecl.Groups[1].Captures[0].ToString()];
                    var declType = externDecl.Groups[2].Captures[0].ToString();

                    switch (complexType) {
                        case ComplexValueType.Struct: Struct(declType); break;
                        case ComplexValueType.Union: Union(declType); break;
                    }

                    Debug.WriteLine($"[FORWARD DECL ] {line}");
                    continue;
                }

                // Typedef alias
                // typedef <struct|union> <existing-type> <alias>
                var typedef = rgxTypedefAlias.Match(line);
                if (typedef.Success) {
                    //var complexType = complexTypeMap[typedef.Groups[1].Captures[0].ToString()];
                    var existingType = typedef.Groups[2].Captures[0].ToString();
                    var alias = typedef.Groups[3].Captures[0].ToString();

                    // Sometimes we might get multiple forward declarations for the same type
                    // Potential multiple indirection
                    var type = GetType(existingType);

                    // C++ allows the same typedef to be defined more than once
                    TypedefAliases.TryAdd(alias, type);

                    var pointers = line.Count(c => c == '*');
                    Debug.WriteLine($"[TYPEDEF {(pointers > 0 ? "PTR" : "VAL")}  ] {line}  --  Adding typedef from {type.Name} to {alias}");
                    continue;
                }

                // Function pointer alias
                // typedef <retType> (*<alias>)(<args>);
                typedef = rgxTypedefFnPtr.Match(line);
                if (typedef.Success) {
                    var alias = typedef.Groups[2].Captures[0].ToString();

                    var fnPtrType = CppFnPtrType.FromSignature(this, line);
                    fnPtrType.Group = currentGroup;

                    TypedefAliases.Add(alias, fnPtrType);

                    Debug.WriteLine($"[TYPEDEF FNPTR] {line}  --  Adding method pointer typedef to {alias}");
                    continue;
                }

                // Start of struct/union/enum
                // [typedef] <struct|union|enum> [optional-tag-name]
                var definition = rgxDefinition.Match(line);
                if (definition.Success && line.IndexOf(";", StringComparison.Ordinal) == -1 && currentType.Count == 0) {
                    // Must have a name if not a typedef, might have a name if it is
                    var split = line.Split(' ');

                    if (split[0] == "typedef")
                        split = split.Skip(1).ToArray();

                    var name = split.Length > 1 && split[1] != "{" ? split[1] : "";

                    currentType.Push(complexTypeMap[split[0]] switch {
                        ComplexValueType.Struct => Struct(name),
                        ComplexValueType.Union => Union(name),
                        ComplexValueType.Enum => NewDefaultEnum(name),
                        _ => throw new InvalidOperationException("Unknown complex type")
                    });

                    // Remember we have to set an alias later
                    inTypedef = line.StartsWith("typedef ");

                    // Reset next enum value if needed
                    nextEnumValue = 0;

                    Debug.WriteLine($"\n[COMPLEX START] {line}");
                    continue;
                }

                // Nested complex field
                // struct <optional-type-name>
                // union <optional-type-name>
                var words = line.Split(' ');
                if ((words[0] == "union" || words[0] == "struct") && words.Length <= 2) {
                    currentType.Push(words[0] == "struct" ? Struct() : Union());

                    Debug.WriteLine($"[FIELD START   ] {line}");
                    continue;
                }

                // End of already named (non-typedef) struct
                if (line == "};" && currentType.Count == 1) {
                    var ct = currentType.Pop();
                    if (!Types.ContainsKey(ct.Name))
                        Add(ct);
                    else
                        ((CppComplexType) Types[ct.Name]).Fields = ct.Fields;

                    Debug.WriteLine($"[STRUCT END   ] {line}  --  {ct.Name}\n");
                    continue;
                }

                // End of complex field, complex type or enum
                // end of [typedef] struct/union/enum (in which case inTypedef == true)
                if (line.StartsWith("}") && line.EndsWith(";")) {
                    var fieldNameOrTypedefAlias = line[1..^1].Trim();
                    var ct = currentType.Pop();

                    // End of top-level definition, so it's a complete type, not a field
                    if (currentType.Count == 0) {

                        if (inTypedef)
                            TypedefAliases.TryAdd(fieldNameOrTypedefAlias, ct);

                        // If the type doesn't have a name because it's a tagless typedef, give it the same name as the alias
                        if (inTypedef && string.IsNullOrEmpty(ct.Name))
                            ct.Name = fieldNameOrTypedefAlias;

                        // Add the type to the collection if we haven't already when it was created
                        if (!Types.ContainsKey(ct.Name))
                            Add(ct);

                        // We will have to copy the type data if the type was forward declared,
                        // because other types are already referencing it; replacing it in the
                        // collection will not replace the references to the empty version in
                        // other types
                        else {
                            ((CppComplexType) Types[ct.Name]).Fields = ct.Fields;
                        }

                        Debug.WriteLine($"[COMPLEX END  ] {line}  --  {ct.Name}\n");
                    }

                    // Otherwise it's a field name in the current type
                    else {
                        var parent = currentType.Peek();
                        parent.AddField(fieldNameOrTypedefAlias, ct, alignment);

                        Debug.WriteLine($"[FIELD END    ] {line}  --  {ct.Name} {fieldNameOrTypedefAlias}");
                    }
                    continue;
                }

                // Function pointer field
                var fieldFnPtr = rgxFieldFnPtr.Match(line);
                if (fieldFnPtr.Success) {
                    var fnPtrType = CppFnPtrType.FromSignature(this, line);
                    fnPtrType.Group = currentGroup;
                    
                    var name = fieldFnPtr.Groups[2].Captures[0].ToString();

                    var ct = currentType.Peek();
                    ct.AddField(name, fnPtrType, alignment);

                    Debug.WriteLine($"[FIELD FNPTR  ] {line}  --  {name}");
                    continue;
                }

                // Pointer or value field
                var field = rgxField.Match(line);

                if (field.Success) {
                    var names = field.Groups[2].Captures[0].ToString();
                    var typeName = field.Groups[1].Captures[0].ToString().Trim();
                    var isConst = rgxIsConst.Match(rawLine).Success;

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

                        // Potential multiple indirection or use of alias
                        var type = GetType(typeName);

                        var ct = currentType.Peek();

                        if (arraySize > 0)
                            type = type.AsArray(arraySize);

                        ct.AddField(name, type, alignment, bitfield, isConst);

                        if (bitfield == 0) {
                            var pointers = line.Count(c => c == '*');
                            Debug.WriteLine($"[FIELD {(pointers > 0 ? "PTR" : "VAL")}    ] {line}  --  {name}");
                        }
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
                        var conv = new UInt64Converter();

                        // Handle bit shift operator
                        var values = valueText.Split("<<").Select(t => (ulong) conv.ConvertFromInvariantString(t.Trim())).ToArray();
                        value = values.Length == 1 ? values[0] : values[0] << (int)values[1];
                        nextEnumValue = value + 1;
                    }

                    var ct = currentType.Peek();
                    ((CppEnumType) ct).AddField(name, value);

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

        // Get a type from its name, handling typedef aliases and pointer types
        public CppType GetType(string typeName, bool returnUnaliased = false) {

            // Separate type name from pointers
            var baseName = typeName.Replace("*", "");
            var indirectionCount = typeName.Length - baseName.Length;
            baseName = baseName.Trim();

            CppType type;

            // Typedef alias
            if (TypedefAliases.TryGetValue(baseName, out CppType aliasType))
                type = returnUnaliased? aliasType : aliasType.AsAlias(baseName);

            // Non-aliased type
            else {
                // Allow auto-generation of forward declarations
                // This will break type generation unless the ultimate wanted type is a pointer
                // Note this can still be the case with indirectionCount == 0 if .AsPointer() is called afterwards
                if (!Types.ContainsKey(baseName))
                    Struct(baseName);

                type = Types[baseName];
            }

            // Resolve pointer indirections
            for (int i = 0; i < indirectionCount; i++)
                type = type.AsPointer(WordSize);

            return type;
        }

        // Get a type, casting it to CppComplexType; deliberately throws exception if the type is not a CppComplexType
        public CppComplexType GetComplexType(string typeName) => (CppComplexType) GetType(typeName, returnUnaliased: true);

        // Get all of the types in a logical group
        public IEnumerable<CppType> GetTypeGroup(string groupName) => Types.Values.Where(t => t.Group == groupName);

        // Get all of the typedefs in a logical group
        public IEnumerable<CppType> GetTypedefGroup(string groupName) => TypedefAliases.Values.Where(t => t.Group == groupName);

        // Add a type
        private void Add(CppType type) {
            type.Group = currentGroup;
            Types.Add(type.Name, type);
        }

        // Add a field to a type specifying the field type and/or declaring type name as a string
        // Convenient when the field type is a pointer, or to avoid referencing this.Types or this.WordSize externally
        public int AddField(CppComplexType declaringType, string fieldName, string typeName, bool isConst = false)
            => declaringType.AddField(fieldName, GetType(typeName), isConst: isConst);

        // Helper factories
        // If the type is named, it gets added to the dictionary; otherwise it must be added manually
        // If the type already exists, it is fetched, otherwise it is created
        public CppComplexType Struct(string name = "", int alignmentBytes = 0) {
            if (!string.IsNullOrEmpty(name) && Types.TryGetValue(name, out var cppType))
                return (CppComplexType) cppType;
            var type = new CppComplexType(ComplexValueType.Struct) {Name = name, Group = currentGroup, AlignmentBytes = alignmentBytes};
            if (!string.IsNullOrEmpty(name))
                Add(type);
            return type;
        }

        public CppComplexType Union(string name = "", int alignmentBytes = 0) {
            if (!string.IsNullOrEmpty(name) && Types.TryGetValue(name, out var cppType))
                return (CppComplexType) cppType;
            var type = new CppComplexType(ComplexValueType.Union) {Name = name, Group = currentGroup, AlignmentBytes = alignmentBytes};
            if (!string.IsNullOrEmpty(name))
                Add(type);
            return type;
        }

        public CppEnumType Enum(CppType underlyingType, string name = "") {
            var type = new CppEnumType(underlyingType) {Name = name, Group = currentGroup};
            if (!string.IsNullOrEmpty(name))
                Add(type);
            return type;
        }

        // Create an empty enum with the default underlying type for the architecture (32 or 64-bit)
        public CppEnumType NewDefaultEnum(string name = "") => Enum(Types["long"], name);

        // Generate a populated CppTypeCollection object from a set of Unity headers
        // The CppDeclarationGenerator is used to ensure that the Unity header type names are not used again afterwards
        // Omit this parameter only when fetching headers for inspection without a model
        public static CppTypeCollection FromUnityVersion(UnityVersion version, CppDeclarationGenerator declGen = null)
            => FromUnityHeaders(UnityHeaders.UnityHeaders.GetHeadersForVersion(version), declGen);

        public static CppTypeCollection FromUnityHeaders(UnityHeaders.UnityHeaders header, CppDeclarationGenerator declGen = null) {
            var wordSize = declGen?.WordSize ?? 64;
            var cppTypes = new CppTypeCollection(wordSize);

            // Process Unity headers
            cppTypes.SetGroup("il2cpp");
            
            // Add junk from config files we haven't included
            cppTypes.TypedefAliases.Add("Il2CppIManagedObjectHolder", cppTypes["void"].AsPointer(wordSize));
            cppTypes.TypedefAliases.Add("Il2CppIUnknown", cppTypes["void"].AsPointer(wordSize));

            var headers = header.GetTypeHeaderText(wordSize);
            cppTypes.AddFromDeclarationText(headers);

            // Don't allow any of the header type names to be re-used; ignore primitive types
            foreach (var type in cppTypes.GetTypeGroup("il2cpp"))
                declGen?.TypeNamespace.ReserveName(type.Name);

            foreach (var typedef in cppTypes.GetTypedefGroup("il2cpp"))
                declGen?.GlobalsNamespace.ReserveName(typedef.Name);

            cppTypes.SetGroup("il2cpp-api");

            var apis = header.GetAPIHeaderTypedefText();
            cppTypes.AddFromDeclarationText(apis);

            foreach (var type in cppTypes.GetTypeGroup("il2cpp-api"))
                declGen?.TypeNamespace.ReserveName(type.Name);

            foreach (var typedef in cppTypes.GetTypedefGroup("il2cpp-api"))
                declGen?.GlobalsNamespace.ReserveName(typedef.Name);

            cppTypes.SetGroup("");

            return cppTypes;
        }
    }
}
