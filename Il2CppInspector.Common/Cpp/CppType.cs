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
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Cpp.UnityHeaders;

namespace Il2CppInspector.Cpp
{
    // Compound type
    public enum CompoundType
    {
        Struct,
        Union,
        Enum
    }

    // A type with no fields
    public class CppType
    {
        // The name of the type
        public virtual string Name { get; set; }

        // The size of the C++ type in bits
        public virtual int Size { get; set; }

        // The size of the C++ type in bytes
        public virtual int SizeBytes => (Size / 8) + (Size % 8 > 0 ? 1 : 0);

        public CppType(string name = null, int size = 0) {
            Name = name;
            Size = size;
        }

        // Generate pointer to this type
        public CppPointerType AsPointer(int WordSize) => new CppPointerType(WordSize, this);

        // Generate array of this type
        public CppArrayType AsArray(int Length) => new CppArrayType(this, Length);

        // Generate typedef to this type
        public CppAlias AsAlias(string Name) => new CppAlias(Name, this);

        public override string ToString() => $"/* {SizeBytes:x2} - {Name} */";
    }

    // A pointer type
    public class CppPointerType : CppType
    {
        public override string Name => ElementType.Name + "*";

        public CppType ElementType { get; }

        public CppPointerType(int WordSize, CppType elementType) : base(null, WordSize) => ElementType = elementType;
    }

    // An array type
    public class CppArrayType : CppType
    {
        public override string Name => ElementType.Name;

        public int Length { get; }

        public CppType ElementType { get; }

        // Even an array of 1-bit bitfields must use at least 1 byte each
        public override int Size => SizeBytes * 8;

        public override int SizeBytes => ElementType.SizeBytes * Length;

        public CppArrayType(CppType elementType, int length) : base(null) {
            ElementType = elementType;
            Length = length;
        }

        public override string ToString() => ElementType + "[" + Length + "]";
    }

    // A function pointer type
    public class CppFnPtrType : CppType
    {
        // For display purposes only
        // We could figure out the actual CppTypes for these but I'm not sure there is any advantage to it
        public string ReturnType { get; }
        public string Arguments { get; }

        public CppFnPtrType(int WordSize, string returnType, string arguments) : base(null, WordSize) {
            ReturnType = returnType;
            Arguments = arguments;
        }

        public override string ToString() => $"typedef {ReturnType} (*{Name})({Arguments});";
    }

    // A typedef alias
    public class CppAlias : CppType
    {
        public CppType ElementType { get; }

        public override int Size => ElementType.Size;

        public override int SizeBytes => ElementType.SizeBytes;

        public CppAlias(string name, CppType elementType) : base(name) => ElementType = elementType;

        public override string ToString() => $"typedef {ElementType.Name} {Name};";
    }

    // A struct, union, enum or class type (type with fields)
    public class CppComplexType : CppType, IEnumerable<CppField>
    {
        // Various enumerators
        public List<CppField> this[int byteOffset] => Fields[byteOffset * 8];

        public CppField this[string fieldName] => Fields.Values.SelectMany(f => f).FirstOrDefault(f => f.Name == fieldName);

        public IEnumerator<CppField> GetEnumerator() => Fields.Values.SelectMany(f => f).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Collection which flattens all nested fields, calculating their direct bit offsets from the start of the type
        // Unions can still cause some offsets to have multiple values
        public class FlattenedFieldsCollection : IEnumerable<CppField>
        {
            public SortedDictionary<int, List<CppField>> Fields;

            public FlattenedFieldsCollection(CppComplexType t) => Fields = getFlattenedFields(t);

            private SortedDictionary<int, List<CppField>> getFlattenedFields(CppComplexType t) {
                var flattened = new SortedDictionary<int, List<CppField>>();

                foreach (var field in t.Fields.Values.SelectMany(f => f)) {
                    if (field.Type is CppComplexType ct) {
                        var baseOffset = field.Offset;
                        var fields = ct.Flattened.Fields.Select(kl => new {
                            Key = kl.Key + baseOffset,
                            Value = kl.Value.Select(f => new CppField { Name = f.Name, Type = f.Type, BitfieldSize = f.BitfieldSize, Offset = f.Offset + baseOffset }).ToList()
                        }).ToDictionary(kv => kv.Key, kv => kv.Value);

                        flattened = new SortedDictionary<int, List<CppField>>(flattened.Union(fields).ToDictionary(kv => kv.Key, kv => kv.Value));
                    } else {
                        if (flattened.ContainsKey(field.Offset))
                            flattened[field.Offset].Add(field);
                        else
                            flattened.Add(field.Offset, new List<CppField> { field });
                    }
                }
                return flattened;
            }

            public List<CppField> this[int byteOffset] => Fields[byteOffset * 8];

            public CppField this[string fieldName] => Fields.Values.SelectMany(f => f).FirstOrDefault(f => f.Name == fieldName);

            public IEnumerator<CppField> GetEnumerator() => Fields.Values.SelectMany(f => f).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private FlattenedFieldsCollection flattenedFields;

        public FlattenedFieldsCollection Flattened {
            get {
                if (flattenedFields == null)
                    flattenedFields = new FlattenedFieldsCollection(this);
                return flattenedFields;
            }
        }

        // The compound type
        public CompoundType CompoundType;

        // Dictionary of byte offset in the type to each field
        // Unions and bitfields can have more than one field at the same offset
        public SortedDictionary<int, List<CppField>> Fields { get; internal set; } = new SortedDictionary<int, List<CppField>>();

        public CppComplexType(CompoundType compoundType) : base("", 0) => CompoundType = compoundType;

        // Size can't be calculated lazily (as we go along adding fields) because of forward declarations
        public override int Size =>
            CompoundType == CompoundType.Union
                // Union size is the size of the largest element in the union
                ? Fields.Values.SelectMany(f => f).Select(f => f.Size).Max()
                // For structs we look for the last item and add the size;
                // adding all the sizes might fail because of alignment padding
                : Fields.Values.Any() ? Fields.Values.SelectMany(f => f).Select(f => f.Offset + f.Size).Max() : 0;

        // Add a field to the type. Returns the offset of the field in the type
        public int AddField(CppField field, int alignmentBytes = 0) {
            // Unions and enums always have an offset of zero
            field.Offset = CompoundType == CompoundType.Struct ? Size : 0;

            // If we just came out of a bitfield, move to the next byte if necessary
            if (field.BitfieldSize == 0 && field.Offset % 8 != 0)
                field.Offset = (field.Offset / 8) * 8 + 8;

            // Respect alignment directives
            if (alignmentBytes > 0 && field.OffsetBytes % alignmentBytes != 0)
                field.Offset += (alignmentBytes - field.OffsetBytes % alignmentBytes) * 8;

            if (Fields.ContainsKey(field.Offset))
                Fields[field.Offset].Add(field);
            else
                Fields.Add(field.Offset, new List<CppField> { field });

            return Size;
        }

        // Summarize all field names and offsets
        public override string ToString() {
            var sb = new StringBuilder();
            
            if (Name.Length > 0)
                sb.Append("typedef ");
            sb.Append(CompoundType == CompoundType.Struct ? "struct " : CompoundType == CompoundType.Enum? "enum " : "union ");
            sb.Append(Name + (Name.Length > 0 ? " " : ""));

            var delimiter = CompoundType == CompoundType.Enum ? "," : ";";

            if (Fields.Any()) {
                sb.Append("{");
                foreach (var field in Fields.Values.SelectMany(f => f))
                    sb.Append("\n\t" + string.Join("\n\t", field.ToString().Split('\n')) + delimiter);

                // Chop off final comma
                if (CompoundType == CompoundType.Enum)
                    sb = sb.Remove(sb.Length - 1, 1);

                sb.Append($"\n}} {Name}{(Name.Length > 0 ? " " : "")}/* Size: 0x{SizeBytes:x2} */;");
            }
            // Forward declaration
            else {
                sb.Append($"{Name};");
            }

            return sb.ToString();
        }
    }

    // A field in a C++ type
    public class CppField
    {
        // The name of the field
        public string Name { get; set; }

        // The offset of the field into the type
        public int Offset { get; set; }

        // The offset of the field into the type in bytes
        public int OffsetBytes => Offset / 8;

        // The size of the field
        public int Size => (BitfieldSize > 0 ? BitfieldSize : Type.Size);

        public int SizeBytes => (Size / 8) + (Size % 8 > 0 ? 1 : 0);

        // The size of the field in bits
        public int BitfieldSize { get; set; }

        // The LSB of the bitfield
        public int BitfieldLSB => Offset % 8;

        // The MSB of the bitfield
        public int BitfieldMSB => BitfieldLSB + Size - 1;

        // The type of the field
        // This type will be wrong (by design) for function pointers, pointers to pointers etc.
        // and we only want it to calculate offsets so we hide this
        internal CppType Type { get; set; }

        // C++ representation of field (might be incorrect due to the above)
        public override string ToString() {
            var offset = $"/* 0x{OffsetBytes:x2} - 0x{OffsetBytes + SizeBytes - 1:x2} (0x{SizeBytes:x2}) */";

            var field = Type switch {
                // nested anonymous types
                CppComplexType t when string.IsNullOrEmpty(t.Name) => "\n" + t.ToString()[..^1] + " " + Name,
                // function pointers
                CppFnPtrType t when string.IsNullOrEmpty(t.Name) => $" {t.ReturnType} (*{Name})({t.Arguments})",
                // regular fields
                _ => $" {Type.Name} {Name}" + (BitfieldSize > 0? $" : {BitfieldSize}" : "")
            };

            var suffix = "";

            // arrays
            if (Type is CppArrayType a)
                suffix += "[" + a.Length + "]";

            // bitfields
            if (BitfieldSize > 0)
                suffix += $" /* bits {BitfieldLSB} - {BitfieldMSB} */";

            return offset + field + suffix;
        }
    }

    // An enum key and value pair
    public class CppEnumField : CppField
    {
        // The value of this key name
        public ulong Value { get; set; }

        public override string ToString() => Name + " = " + Value;
    }

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
            Types.Add("uintptr_t", new CppType("uintptr_t", WordSize));
            Types.Add("size_t", new CppType("size_t", WordSize));
        }

        // Parse a block of C++ source code, adding any types found
        public void AddFromDeclarationText(string text) {
            using StringReader lines = new StringReader(text);

            var fnPtr = @"(\S+)\s*\(\s*\*(\S+)\s*\)\s*\(\s*(.*?)\s*\)";
            var rgxExternDecl = new Regex(@"struct (\S+);");
            var rgxTypedefForwardDecl = new Regex(@"typedef struct (\S+) (\S+);");
            var rgxTypedefFnPtr = new Regex(@"typedef\s+(?:struct )?" + fnPtr + ";");
            var rgxTypedef = new Regex(@"typedef (\S+?)\s*\**\s*(\S+);");
            var rgxFieldFnPtr = new Regex(fnPtr + @";");
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
                    var returnType = typedef.Groups[1].Captures[0].ToString();
                    var arguments = typedef.Groups[3].Captures[0].ToString();
                    var alias = typedef.Groups[2].Captures[0].ToString();
                    Types.Add(alias, new CppFnPtrType(WordSize, returnType, arguments) {Name = alias});

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
                    // We need this to avoid false positives on field matches below
                    /*if (currentType.Count == 0 && !inEnum) {
                        Debug.WriteLine($"[IGNORE       ] {line}");
                        continue;
                    }*/

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
                    var returnType = fieldFnPtr.Groups[1].Captures[0].ToString();
                    var arguments = fieldFnPtr.Groups[3].Captures[0].ToString();
                    var name = fieldFnPtr.Groups[2].Captures[0].ToString();

                    var ct = currentType.Peek();
                    ct.AddField(new CppField {Name = name, Type = new CppFnPtrType(WordSize, returnType, arguments)}, alignment);

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

        // Generate a populated CppTypes object from a set of Unity headers
        public static CppTypes FromUnityVersion(UnityVersion version, int wordSize = 32)
            => FromUnityHeaders(UnityHeader.GetHeaderForVersion(version), wordSize);

        public static CppTypes FromUnityHeaders(UnityHeader header, int wordSize = 32) {
            var cppTypes = new CppTypes(wordSize);

            // Process Unity headers
            var headers = header.GetHeaderText();
            cppTypes.AddFromDeclarationText(headers);

            return cppTypes;
        }
    }
}
