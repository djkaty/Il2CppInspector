/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Reflection;

namespace Il2CppInspector.Reflection {
    public class FieldInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppFieldDefinition Definition { get; }
        public int Index { get; }
        public int Offset { get; }
        
        public bool HasDefaultValue { get; }
        public object DefaultValue { get; }

        public string DefaultValueString {
            get {
                if (!HasDefaultValue)
                    return "";
                if (DefaultValue is string)
                    return $"\"{DefaultValue}\"";
                if (!(DefaultValue is char))
                    return (DefaultValue?.ToString() ?? "null");
                var cValue = (int) (char) DefaultValue;
                if (cValue < 32 || cValue > 126)
                    return $"'\\x{cValue:x4}'";
                return $"'{DefaultValue}'";
            }
        }

        // Information/flags about the field
        public FieldAttributes Attributes { get; }

        // Type of field
        private readonly Il2CppType fieldType;
        public TypeInfo FieldType => Assembly.Model.GetType(fieldType, MemberTypes.Field);

        // For the Is* definitions below, see:
        // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.fieldinfo.isfamilyandassembly?view=netframework-4.7.1#System_Reflection_FieldInfo_IsFamilyAndAssembly

        // True if the field is declared as internal
        public bool IsAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;

        // True if the field is declared as protected
        public bool IsFamily => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;

        // True if the field is declared as 'protected private' (always false)
        public bool IsFamilyAndAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem;

        // True if the field is declared as protected public
        public bool IsFamilyOrAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem;

        // True if the field is declared as readonly
        public bool IsInitOnly => (Attributes & FieldAttributes.InitOnly) == FieldAttributes.InitOnly;

        // True if the field is const
        public bool IsLiteral => (Attributes & FieldAttributes.Literal) == FieldAttributes.Literal;

        // True if the field has the NonSerialized attribute
        public bool IsNotSerialized => (Attributes & FieldAttributes.NotSerialized) == FieldAttributes.NotSerialized;

        // True if the field is extern
        public bool IsPinvokeImpl => (Attributes & FieldAttributes.PinvokeImpl) == FieldAttributes.PinvokeImpl;

        // True if the field is declared a private
        public bool IsPrivate => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;

        // True if the field is declared as public
        public bool IsPublic => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;

        // True if the field has a special name
        public bool IsSpecialName => (Attributes & FieldAttributes.SpecialName) == FieldAttributes.SpecialName;

        // True if the field is declared as static
        public bool IsStatic => (Attributes & FieldAttributes.Static) == FieldAttributes.Static;

        public override MemberTypes MemberType => MemberTypes.Field;

        public FieldInfo(Il2CppInspector pkg, int fieldIndex, TypeInfo declaringType) :
            base(declaringType) {
            Definition = pkg.Fields[fieldIndex];
            Index = fieldIndex;
            Offset = pkg.FieldOffsets[fieldIndex];
            Name = pkg.Strings[Definition.nameIndex];

            fieldType = pkg.TypeUsages[Definition.typeIndex];
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == Il2CppConstants.FIELD_ATTRIBUTE_PRIVATE)
                Attributes |= FieldAttributes.Private;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == Il2CppConstants.FIELD_ATTRIBUTE_PUBLIC)
                Attributes |= FieldAttributes.Public;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == Il2CppConstants.FIELD_ATTRIBUTE_FAM_AND_ASSEM)
                Attributes |= FieldAttributes.FamANDAssem;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == Il2CppConstants.FIELD_ATTRIBUTE_ASSEMBLY)
                Attributes |= FieldAttributes.Assembly;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == Il2CppConstants.FIELD_ATTRIBUTE_FAMILY)
                Attributes |= FieldAttributes.Family;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == Il2CppConstants.FIELD_ATTRIBUTE_FAM_OR_ASSEM)
                Attributes |= FieldAttributes.FamORAssem;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_STATIC) == Il2CppConstants.FIELD_ATTRIBUTE_STATIC)
                Attributes |= FieldAttributes.Static;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_INIT_ONLY) == Il2CppConstants.FIELD_ATTRIBUTE_INIT_ONLY)
                Attributes |= FieldAttributes.InitOnly;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_LITERAL) == Il2CppConstants.FIELD_ATTRIBUTE_LITERAL)
                Attributes |= FieldAttributes.Literal;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_NOT_SERIALIZED) == Il2CppConstants.FIELD_ATTRIBUTE_NOT_SERIALIZED)
                Attributes |= FieldAttributes.NotSerialized;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_SPECIAL_NAME) == Il2CppConstants.FIELD_ATTRIBUTE_SPECIAL_NAME)
                Attributes |= FieldAttributes.SpecialName;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_PINVOKE_IMPL) == Il2CppConstants.FIELD_ATTRIBUTE_PINVOKE_IMPL)
                Attributes |= FieldAttributes.PinvokeImpl;

            // Default initialization value if present
            if (pkg.FieldDefaultValue.TryGetValue(fieldIndex, out object variant)) {
                HasDefaultValue = true;
                DefaultValue = variant;
            }
        }
    }
}