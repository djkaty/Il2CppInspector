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
using Il2CppInspector.CppUtils.UnityHeaders;

namespace Il2CppInspector.CppUtils
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

        // Even an array of 1-bit bitfirleds must use at least 1 byte each
        public override int Size => SizeBytes * 8;

        public override int SizeBytes => ElementType.SizeBytes * Length;

        public CppArrayType(CppType elementType, int length) : base(null) {
            ElementType = elementType;
            Length = length;
        }

        public override string ToString() => ElementType + "[" + Length + "]";
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
            field.Offset = CompoundType == CompoundType.Struct ? Size : 0;

            // If we just came out of a bitfield, move to the next byte if necessary
            if (field.BitfieldSize == 0 && field.Offset % 8 != 0)
                field.Offset = (field.Offset / 8) * 8 + 8;

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
            sb.Append(CompoundType == CompoundType.Struct ? "struct " : "union ");
            sb.Append(Name + (Name.Length > 0 ? " " : ""));

            if (Fields.Any()) {
                sb.AppendLine("{");
                foreach (var field in Fields.Values.SelectMany(f => f))
                    sb.AppendLine("  " + field);

                sb.Append($"}} {Name}{(Name.Length > 0 ? " " : "")}/* Size: 0x{SizeBytes:x2} */;");
            }
            // Forward declaration
            else {
                sb.Append($"{Name};");
            }

            return sb.ToString();
        }
    }

    // A field in a C++ type
    public struct CppField
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
        public override string ToString() =>
            $"/* 0x{OffsetBytes:x2} - 0x{OffsetBytes + SizeBytes - 1:x2} (0x{SizeBytes:x2}) */"
            // nested anonymous types
            + (Type is CppComplexType && Type.Name == "" ? "\n" + Type.ToString()[..^1] + " " + Name :
            // regular fields
            $" {Type.Name} {Name}" + (BitfieldSize > 0? $" : {BitfieldSize}" : ""))
            // arrays
            + (Type is CppArrayType? "[" + ((CppArrayType)Type).Length + "]" : "")
            + ";"
            // bitfields
            + (BitfieldSize > 0? $" // bits {BitfieldLSB} - {BitfieldMSB}" : "");
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
            new CppType("void", 0)
        };

        public CppTypes(int wordSize) {
            WordSize = wordSize * 8;
            Types = primitiveTypes.ToDictionary(t => t.Name, t => t);

            // This is all compiler-dependent, let's hope for the best!
            Types.Add("uintptr_t", new CppType("uintptr_t", WordSize));
            Types.Add("size_t", new CppType("size_t", WordSize));
        }

        // Parse a block of C++ source code, adding any types found
        public void AddFromDeclarationText(string text) {
            using StringReader lines = new StringReader(text);

            var fnPtr = @"\S+\s*\(\s*\*(\S+)\)\s*\(.*\)";
            var rgxExternDecl = new Regex(@"struct (\S+);");
            var rgxTypedefForwardDecl = new Regex(@"typedef struct (\S+) (\S+);");
            var rgxTypedefFnPtr = new Regex(@"typedef\s+(?:struct )?" + fnPtr + ";");
            var rgxTypedef = new Regex(@"typedef (\S+?)\s*\**\s*(\S+);");
            var rgxFieldFnPtr = new Regex(fnPtr + @";");
            var rgxField = new Regex(@"^(?:struct )?(\S+?)\s*\**\s*(\S+)(?:\s*:\s*([0-9]+))?;");

            var rgxStripKeywords = new Regex(@"\b(?:const|unsigned|volatile)\b");
            var rgxCompressPtrs = new Regex(@"\*\s+\*");

            var rgxArrayField = new Regex(@"(\S+?)\[([0-9]+)\]");

            var currentType = new Stack<CppComplexType>();
            bool inEnum = false;
            string line;

            // TODO: Alignment directives
            // TODO: enum prefix in field (Il2CppWindowsRuntimeTypeName)
            // TODO: comma-separated fields
            // TODO: #ifdef IS_32BIT
            // TODO: function pointer signatures

            while ((line = lines.ReadLine()) != null) {

                // Sanitize
                line = rgxStripKeywords.Replace(line, "");
                line = rgxCompressPtrs.Replace(line, "**");
                line = line.Trim();

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
                    var alias = typedef.Groups[1].Captures[0].ToString();
                    Types.Add(alias, Types["uintptr_t"]);

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
                        parent.AddField(new CppField { Name = name, Type = ct });

                        Debug.WriteLine($"[FIELD END    ] {line}  --  {ct.Name} {name}");
                    }
                    continue;
                }

                // Function pointer field
                var fieldFnPtr = rgxFieldFnPtr.Match(line);
                if (fieldFnPtr.Success) {
                    var name = fieldFnPtr.Groups[1].Captures[0].ToString();

                    var ct = currentType.Peek();
                    ct.AddField(new CppField {Name = name, Type = Types["uintptr_t"]});

                    Debug.WriteLine($"[FIELD FNPTR  ] {line}  --  {name}");
                    continue;
                }

                // Pointer or value field
                var field = rgxField.Match(line);

                if (field.Success) {
                    var name = field.Groups[2].Captures[0].ToString();
                    var typeName = field.Groups[1].Captures[0].ToString();

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

                    ct.AddField(new CppField {Name = name, Type = type, BitfieldSize = bitfield});

                    if (bitfield == 0)
                        Debug.WriteLine($"[FIELD {(pointers > 0? "PTR":"VAL")}    ] {line}  --  {name}");
                    else
                        Debug.WriteLine($"[BITFIELD     ] {line}  --  {name} : {bitfield}");
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
