/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// NOTE: The types in this file should not be created directly. Always create types using the CppTypeCollection API!

namespace Il2CppInspector.Cpp
{
    // Value type with fields
    public enum ComplexValueType
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

        // The logical group this type is part of
        // This is purely for querying types in related groups and has no bearing on the code
        public string Group { get; set; }

        // The size of the C++ type in bits
        public virtual int Size { get; set; }

        // The alignment of the type
        public int AlignmentBytes { get; set; }

        // The size of the C++ type in bytes
        public virtual int SizeBytes => (Size / 8) + (Size % 8 > 0 ? 1 : 0);

        public CppType(string name = null, int size = 0, int alignmentBytes = 0) {
            Name = name;
            Size = size;
            AlignmentBytes = alignmentBytes;
        }

        // Generate pointer to this type
        public CppPointerType AsPointer(int WordSize) => new CppPointerType(WordSize, this);

        // Generate array of this type
        public CppArrayType AsArray(int Length) => new CppArrayType(this, Length);

        // Generate typedef to this type
        public CppAlias AsAlias(string Name) => new CppAlias(Name, this);

        // Return the type as a field
        public virtual string ToFieldString(string fieldName) => Name + " " + fieldName;

        public virtual string ToString(string format = "") => format == "o" ? $"/* {SizeBytes:x2} - {Name} */" : "";

        public override string ToString() => ToString();
    }

    // A pointer type
    public class CppPointerType : CppType
    {
        public override string Name => ElementType.Name + " *";

        public CppType ElementType { get; }

        public CppPointerType(int WordSize, CppType elementType) : base(null, WordSize) => ElementType = elementType;

        // Return the type as a field
        public override string ToFieldString(string fieldName) => ElementType.ToFieldString("*" + fieldName);

        public override string ToString(string format = "") => ToFieldString("");
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

        public CppArrayType(CppType elementType, int length) : base() {
            ElementType = elementType;
            Length = length;
        }

        // Return the type as a field
        public override string ToFieldString(string fieldName) => ElementType.ToFieldString(fieldName) + "[" + Length + "]";

        public override string ToString(string format = "") => ElementType + "[" + Length + "]";
    }

    // A function pointer type
    public class CppFnPtrType : CppType
    {
        // Function return type
        public CppType ReturnType { get; }

        // Function argument names and types by position (some may have no names)
        public List<(string Name, CppType Type)> Arguments { get; }

        // Regex which matches a function pointer
        public const string Regex = @"(\S+)\s*\(\s*\*(\S+)\s*\)\s*\(\s*(.*?)\s*\)";

        public CppFnPtrType(int WordSize, CppType returnType, List<(string Name, CppType Type)> arguments) : base(null, WordSize) {
            ReturnType = returnType;
            Arguments = arguments;
        }

        // Generate a CppFnPtrType from a text signature (typedef or field)
        public static CppFnPtrType FromSignature(CppTypeCollection types, string text) {
            if (text.StartsWith("typedef "))
                text = text.Substring(8);

            var typedef = System.Text.RegularExpressions.Regex.Match(text, Regex);

            var returnType = types.GetType(typedef.Groups[1].Captures[0].ToString());
            var argumentText = typedef.Groups[3].Captures[0].ToString().Split(',');
            if (argumentText.Length == 1 && argumentText[0] == "")
                argumentText = new string[0];
            var argumentNames = argumentText.Select(
                a => a.IndexOf("*") != -1 ? a.Substring(a.LastIndexOf("*") + 1).Trim() :
                a.IndexOf(" ") != -1 ? a.Substring(a.LastIndexOf(" ") + 1) : "");

            var arguments = argumentNames.Zip(argumentText, (name, argument) =>
                    (name, types.GetType(argument.Substring(0, argument.Length - name.Length)))).ToList();

            return new CppFnPtrType(types.WordSize, returnType, arguments);
        }

        // Output as a named field in a type
        public override string ToFieldString(string name) => $"{ReturnType.Name} (*{name})({string.Join(", ", Arguments.Select(a => a.Type.Name + (a.Name.Length > 0? " " + a.Name : "")))})";

        // Output as a typedef declaration
        public override string ToString(string format = "") => "typedef " + ToFieldString(Name) + ";\n";

        // Output as a function signature
        public string ToSignatureString() => $"{ReturnType.Name} {Name}({string.Join(", ", Arguments.Select(a => a.Type.Name + (a.Name.Length > 0? " " + a.Name : "")))})";
    }

    // A named alias for another type
    // These are not stored in the type collection but generated on-the-fly for fields by GetType()
    public class CppAlias : CppType
    {
        public CppType ElementType { get; }

        public override int Size => ElementType.Size;

        public override int SizeBytes => ElementType.SizeBytes;

        public CppAlias(string name, CppType elementType) : base(name) => ElementType = elementType;

        public override string ToString(string format = "") => $"typedef {ElementType.ToFieldString(Name)};";
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
                    var type = field.Type;
                    while (type is CppAlias aliasType)
                        type = aliasType.ElementType;

                    if (type is CppComplexType ct) {
                        var baseOffset = field.Offset;
                        var fields = ct.Flattened.Fields.Select(kl => new {
                            Key = kl.Key + baseOffset,
                            Value = kl.Value.Select(f => new CppField(f.Name, f.Type, f.BitfieldSize) { Offset = f.Offset + baseOffset }).ToList()
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
        public ComplexValueType ComplexValueType;

        // Dictionary of byte offset in the type to each field
        // Unions and bitfields can have more than one field at the same offset
        public SortedDictionary<int, List<CppField>> Fields { get; internal set; } = new SortedDictionary<int, List<CppField>>();

        public CppComplexType(ComplexValueType complexValueType) : base("", 0) => ComplexValueType = complexValueType;

        // Add a field to the type. Returns the offset of the field in the type
        public int AddField(CppField field, int alignmentBytes = 0) {
            // Unions and enums always have an offset of zero
            field.Offset = ComplexValueType == ComplexValueType.Struct ? Size : 0;

            // If we just came out of a bitfield, move to the next byte if necessary
            if (field.BitfieldSize == 0 && field.Offset % 8 != 0)
                field.Offset = (field.Offset / 8) * 8 + 8;

            // Respect alignment directives
            if (alignmentBytes > 0 && field.OffsetBytes % alignmentBytes != 0)
                field.Offset += (alignmentBytes - field.OffsetBytes % alignmentBytes) * 8;

            if (field.Type.AlignmentBytes > 0 && field.OffsetBytes % field.Type.AlignmentBytes != 0)
                field.Offset += (field.Type.AlignmentBytes - field.OffsetBytes % field.Type.AlignmentBytes) * 8;

            if (Fields.ContainsKey(field.Offset))
                Fields[field.Offset].Add(field);
            else
                Fields.Add(field.Offset, new List<CppField> { field });

            // Update type size. This lazy evaluation only works if there are no value type forward declarations in the type
            // Union size is the size of the largest element in the union
            if (ComplexValueType == ComplexValueType.Union)
                if (field.Size > Size)
                    Size = field.Size;

            // For structs we look for the last item and add the size; adding the sizes without offsets might fail because of alignment padding
            if (ComplexValueType == ComplexValueType.Struct)
                Size = field.Offset + field.Size;

            return Size;
        }

        // Add a field to the type
        public int AddField(string name, CppType type, int alignmentBytes = 0, int bitfield = 0, bool isConst = false)
            => AddField(new CppField(name, type, bitfield, isConst), alignmentBytes);

        // Return the type as a field
        public override string ToFieldString(string fieldName) => (ComplexValueType == ComplexValueType.Struct ? "struct " : "union ") + Name + " " + fieldName;

        // Summarize all field names and offsets
        public override string ToString(string format = "") {
            var sb = new StringBuilder();
            
            sb.Append(ComplexValueType == ComplexValueType.Struct ? "struct " : "union ");

            if (AlignmentBytes != 0)
                sb.Append($"__declspec(align({AlignmentBytes})) ");

            sb.Append(Name + (Name.Length > 0 ? " " : ""));

            sb.Append("{");
            foreach (var field in Fields.Values.SelectMany(f => f))
                sb.Append("\n    " + string.Join("\n    ", field.ToString(format).Split('\n')) + ";");

            sb.Append($"\n}}{(format == "o"? $" /* Size: 0x{SizeBytes:x2} */" : "")};");

            sb.Append("\n");
            return sb.ToString();
        }
    }

    // Enumeration type
    public class CppEnumType : CppComplexType
    {
        // The underlying type of the enum
        public CppType UnderlyingType { get; }

        public override int Size => UnderlyingType.Size;

        public CppEnumType(CppType underlyingType) : base(ComplexValueType.Enum) => UnderlyingType = underlyingType;

        public void AddField(string name, object value) => AddField(new CppEnumField(name, UnderlyingType, value));

        // Return the type as a field
        public override string ToFieldString(string fieldName) => "enum " + Name + " " + fieldName;

        public override string ToString(string format = "") {
            var sb = new StringBuilder();
            
            sb.Append($"enum {Name} : {UnderlyingType.Name} {{");

            foreach (var field in Fields.Values.SelectMany(f => f))
                sb.Append("\n    " + string.Join("\n    ", field.ToString(format).Split('\n')) + ",");

            sb.AppendLine($"\n}}{(format == "o"? $" /* Size: 0x{SizeBytes:x2} */" : "")};");
            return sb.ToString();
        }
    }
}
