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
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Cpp.UnityHeaders;

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

        // Helper factories
        public static CppComplexType NewStruct(string name = "") => new CppComplexType(ComplexValueType.Struct) {Name = name};
        public static CppComplexType NewUnion(string name = "") => new CppComplexType(ComplexValueType.Union) {Name = name};
        public static CppEnumType NewEnum(CppType underlyingType, string name = "") => new CppEnumType(underlyingType) {Name = name};

        public virtual string ToString(string format = "") => format == "o" ? $"/* {SizeBytes:x2} - {Name} */" : $"/* {Name} */";

        public override string ToString() => ToString();
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
        public static CppFnPtrType FromSignature(CppTypes types, string text) {
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
        public string FieldToString(string name) => $"{ReturnType.Name} (*{name})({string.Join(", ", Arguments.Select(a => a.Type.Name + (a.Name.Length > 0? " " + a.Name : "")))})";

        // Output as a typedef declaration
        public override string ToString(string format = "") => "typedef " + FieldToString(Name) + ";";
    }

    // A typedef alias
    public class CppAlias : CppType
    {
        public CppType ElementType { get; }

        public override int Size => ElementType.Size;

        public override int SizeBytes => ElementType.SizeBytes;

        public CppAlias(string name, CppType elementType) : base(name) => ElementType = elementType;

        public override string ToString(string format = "") => $"typedef {ElementType.Name} {Name};";
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

        // Size can't be calculated lazily (as we go along adding fields) because of forward declarations
        public override int Size =>
            ComplexValueType == ComplexValueType.Union
                // Union size is the size of the largest element in the union
                ? Fields.Values.SelectMany(f => f).Select(f => f.Size).Max()
                // For structs we look for the last item and add the size;
                // adding all the sizes might fail because of alignment padding
                : Fields.Values.Any() ? Fields.Values.SelectMany(f => f).Select(f => f.Offset + f.Size).Max() : 0;

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

            if (Fields.ContainsKey(field.Offset))
                Fields[field.Offset].Add(field);
            else
                Fields.Add(field.Offset, new List<CppField> { field });

            return Size;
        }

        // Add a field to the type
        public int AddField(string name, CppType type, int alignmentBytes = 0, int bitfield = 0)
            => AddField(new CppField(name, type, bitfield), alignmentBytes);

        // Summarize all field names and offsets
        public override string ToString(string format = "") {
            var sb = new StringBuilder();
            
            if (Name.Length > 0)
                sb.Append("typedef ");
            sb.Append(ComplexValueType == ComplexValueType.Struct ? "struct " : "union ");
            sb.Append(Name + (Name.Length > 0 ? " " : ""));

            if (Fields.Any()) {
                sb.Append("{");
                foreach (var field in Fields.Values.SelectMany(f => f))
                    sb.Append("\n\t" + string.Join("\n\t", field.ToString(format).Split('\n')) + ";");

                sb.Append($"\n}}{(Name.Length > 0? " " + Name : "")}{(format == "o"? $" /* Size: 0x{SizeBytes:x2} */" : "")};");
            }
            // Forward declaration
            else {
                sb.Append($"{Name};");
            }

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

        public void AddField(string name, ulong value) => AddField(new CppEnumField(name, UnderlyingType, value));

        public override string ToString(string format = "") {
            var sb = new StringBuilder();
            
            sb.Append($"typedef enum {Name} : {UnderlyingType.Name}");

            if (Fields.Any()) {
                sb.Append(" {");
                foreach (var field in Fields.Values.SelectMany(f => f))
                    sb.Append("\n\t" + string.Join("\n\t", field.ToString(format).Split('\n')) + ",");

                // Chop off final comma
                sb = sb.Remove(sb.Length - 1, 1);
                sb.Append($"\n}} {Name}{(format == "o"? $" /* Size: 0x{SizeBytes:x2} */" : "")};");
            }
            // Forward declaration
            else {
                sb.Append($"{Name};");
            }

            return sb.ToString();
        }
    }
}
