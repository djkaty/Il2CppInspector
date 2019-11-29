/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppInspector.Reflection {
    public class FieldInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppFieldDefinition Definition { get; }
        public int Index { get; }
        public long Offset { get; }
        public ulong DefaultValueMetadataAddress { get; }

        // Custom attributes for this member
        public override IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(this);

        public bool HasDefaultValue => (Attributes & FieldAttributes.HasDefault) != 0;
        public object DefaultValue { get; }

        public string DefaultValueString => HasDefaultValue ? DefaultValue.ToCSharpValue(FieldType) : "";

        // Information/flags about the field
        public FieldAttributes Attributes { get; }

        // Type of field
        private readonly int fieldTypeUsage;
        public TypeInfo FieldType => Assembly.Model.GetTypeFromUsage(fieldTypeUsage, MemberTypes.Field);

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

            fieldTypeUsage = Definition.typeIndex;
            var fieldType = pkg.TypeUsages[fieldTypeUsage];

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
            if ((fieldType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_HAS_DEFAULT) != 0)
                Attributes |= FieldAttributes.HasDefault;

            // Default initialization value if present
            if (pkg.FieldDefaultValue.TryGetValue(fieldIndex, out (ulong address, object variant) value)) {
                DefaultValue = value.variant;
                DefaultValueMetadataAddress = value.address;
            }
        }

        public string GetAccessModifierString() => this switch {
            { IsPrivate: true } => "private ",
            { IsPublic: true } => "public ",
            { IsFamily: true } => "protected ",
            { IsAssembly: true } => "internal ",
            { IsFamilyOrAssembly: true } => "protected internal ",
            { IsFamilyAndAssembly: true } => "private protected ",
            _ => ""
        };

        public string GetModifierString() {
            var modifiers = new StringBuilder(GetAccessModifierString());

            if (FieldType.RequiresUnsafeContext || GetCustomAttributes("System.Runtime.CompilerServices.FixedBufferAttribute").Any())
                modifiers.Append("unsafe ");
            if (IsLiteral)
                modifiers.Append("const ");
            // All const fields are also static by implication
            else if (IsStatic)
                modifiers.Append("static ");
            if (IsInitOnly)
                modifiers.Append("readonly ");
            if (IsPinvokeImpl)
                modifiers.Append("extern ");
            if (GetCustomAttributes("System.Runtime.CompilerServices.FixedBufferAttribute").Any())
                modifiers.Append("fixed ");
            return modifiers.ToString();
        }
    }
}