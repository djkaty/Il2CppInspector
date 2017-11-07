/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Reflection;

namespace Il2CppInspector.Reflection {
    public class FieldInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppFieldDefinition Definition { get; }
        public int Index { get; }
        
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
        public bool IsAssembly { get; } // TODO

        // True if the field is declared as protected
        public bool IsFamily { get; } // TODO

        // True if the field is declared as 'protected private' (always false)
        public bool IsFamilyAndAssembly => false;

        // True if the field is declared as protected public
        public bool IsFamilyOrAssembly { get; } // TODO

        // True if the field is declared as readonly
        public bool IsInitOnly => (Attributes & FieldAttributes.InitOnly) == FieldAttributes.InitOnly;

        // True if the field is declared a private
        public bool IsPrivate => (Attributes & FieldAttributes.Private) == FieldAttributes.Private;

        // True if the field is declared as public
        public bool IsPublic => (Attributes & FieldAttributes.Public) == FieldAttributes.Public;

        // True if the field is declared as static
        public bool IsStatic => (Attributes & FieldAttributes.Static) == FieldAttributes.Static;

        public override MemberTypes MemberType { get; }

        public FieldInfo(Il2CppInspector pkg, int fieldIndex, TypeInfo declaringType) :
            base(declaringType) {
            Definition = pkg.Metadata.Fields[fieldIndex];
            Index = fieldIndex;
            Name = pkg.Strings[pkg.Metadata.Fields[fieldIndex].nameIndex];

            fieldType = pkg.TypeUsages[Definition.typeIndex];
            if ((fieldType.attrs & DefineConstants.FIELD_ATTRIBUTE_PRIVATE) == DefineConstants.FIELD_ATTRIBUTE_PRIVATE)
                Attributes |= FieldAttributes.Private;
            if ((fieldType.attrs & DefineConstants.FIELD_ATTRIBUTE_PUBLIC) == DefineConstants.FIELD_ATTRIBUTE_PUBLIC)
                Attributes |= FieldAttributes.Public;
            if ((fieldType.attrs & DefineConstants.FIELD_ATTRIBUTE_STATIC) == DefineConstants.FIELD_ATTRIBUTE_STATIC)
                Attributes |= FieldAttributes.Static;
            if ((fieldType.attrs & DefineConstants.FIELD_ATTRIBUTE_INIT_ONLY) ==
                DefineConstants.FIELD_ATTRIBUTE_INIT_ONLY)
                Attributes |= FieldAttributes.InitOnly;

            // Default initialization value if present
            if (pkg.FieldDefaultValue.TryGetValue(fieldIndex, out object variant)) {
                HasDefaultValue = true;
                DefaultValue = variant;
            }
        }
    }
}