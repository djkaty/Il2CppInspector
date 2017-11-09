/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

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
        public string DefaultValueString => !HasDefaultValue ? "" : (DefaultValue is string? $"\"{DefaultValue}\"" : (DefaultValue?.ToString() ?? "null"));

        // Information/flags about the field
        public FieldAttributes Attributes { get; }

        // Type of field
        private readonly Il2CppType fieldType;
        public TypeInfo FieldType => Assembly.Model.GetType(fieldType, MemberTypes.Field);

        // For the Is* definitions below, see:
        // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.fieldinfo.isfamilyandassembly?view=netframework-4.7.1#System_Reflection_FieldInfo_IsFamilyAndAssembly

        // True if the field is declared as internal
        public bool IsAssembly => throw new NotImplementedException();

        // True if the field is declared as protected
        public bool IsFamily => throw new NotImplementedException();

        // True if the field is declared as 'protected private' (always false)
        public bool IsFamilyAndAssembly => false;

        // True if the field is declared as protected public
        public bool IsFamilyOrAssembly => throw new NotImplementedException();

        // True if the field is declared as readonly
        public bool IsInitOnly => (Attributes & FieldAttributes.InitOnly) == FieldAttributes.InitOnly;

        // True if the field is declared a private
        public bool IsPrivate => (Attributes & FieldAttributes.Private) == FieldAttributes.Private;

        // True if the field is declared as public
        public bool IsPublic => (Attributes & FieldAttributes.Public) == FieldAttributes.Public;

        // True if the field is declared as static
        public bool IsStatic => (Attributes & FieldAttributes.Static) == FieldAttributes.Static;

        public override MemberTypes MemberType => MemberTypes.Field;

        public FieldInfo(Il2CppInspector pkg, int fieldIndex, TypeInfo declaringType) :
            base(declaringType) {
            Definition = pkg.Metadata.Fields[fieldIndex];
            Index = fieldIndex;
            Offset = pkg.FieldOffsets[fieldIndex];
            Name = pkg.Strings[Definition.nameIndex];

            fieldType = pkg.TypeUsages[Definition.typeIndex];
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_PRIVATE) == Il2CppConstants.FIELD_ATTRIBUTE_PRIVATE)
                Attributes |= FieldAttributes.Private;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_PUBLIC) == Il2CppConstants.FIELD_ATTRIBUTE_PUBLIC)
                Attributes |= FieldAttributes.Public;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_STATIC) == Il2CppConstants.FIELD_ATTRIBUTE_STATIC)
                Attributes |= FieldAttributes.Static;
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_INIT_ONLY) ==
                Il2CppConstants.FIELD_ATTRIBUTE_INIT_ONLY)
                Attributes |= FieldAttributes.InitOnly;

            // Default initialization value if present
            if (pkg.FieldDefaultValue.TryGetValue(fieldIndex, out object variant)) {
                HasDefaultValue = true;
                DefaultValue = variant;
            }
        }
    }
}