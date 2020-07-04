/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector.Cpp
{
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
        public CppType Type { get; set; }

        // C++ representation of field
        public virtual string ToString(string format = "") {
            var offset = format == "o" ? $"/* 0x{OffsetBytes:x2} - 0x{OffsetBytes + SizeBytes - 1:x2} (0x{SizeBytes:x2}) */" : "";

            var field = Type switch {
                // nested anonymous types
                CppComplexType t when string.IsNullOrEmpty(t.Name) => (format == "o"? "\n" : "") + t.ToString(format)[..^1] + (Name.Length > 0? " " + Name : ""),
                // function pointers
                CppFnPtrType t when string.IsNullOrEmpty(t.Name) => (format == "o"? " " : "") + t.FieldToString(Name),
                // regular fields
                _ => $"{(format == "o"? " ":"")}{Type.Name} {Name}" + (BitfieldSize > 0? $" : {BitfieldSize}" : "")
            };

            var suffix = "";

            // arrays
            if (Type is CppArrayType a)
                suffix += "[" + a.Length + "]";

            // bitfields
            if (BitfieldSize > 0 && format == "o")
                suffix += $" /* bits {BitfieldLSB} - {BitfieldMSB} */";

            return offset + field + suffix;
        }
        public override string ToString() => ToString();
    }

    // An enum key and value pair
    public class CppEnumField : CppField
    {
        // The value of this key name
        public ulong Value { get; set; }

        public override string ToString(string format = "") => Name + " = " + Value;
    }
}
