/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    // Unity 5.6.2p3 -> v23
    // Unity 5.6.4f1 -> v23
    // Unity 2017.2f3 -> v24
    // Unity 2019.2.8f1 -> v24.1

    // From il2cpp-metadata.h
#pragma warning disable CS0649
    public class Il2CppGlobalMetadataHeader
    {
        // Metadata v21
        public uint sanity;
        public int version;
        public int stringLiteralOffset; // string data for managed code
        public int stringLiteralCount;
        public int stringLiteralDataOffset;
        public int stringLiteralDataCount;
        public int stringOffset; // string data for metadata
        public int stringCount;
        public int eventsOffset; // Il2CppEventDefinition
        public int eventsCount;
        public int propertiesOffset; // Il2CppPropertyDefinition
        public int propertiesCount;
        public int methodsOffset; // Il2CppMethodDefinition
        public int methodsCount;
        public int parameterDefaultValuesOffset; // Il2CppParameterDefaultValue
        public int parameterDefaultValuesCount;
        public int fieldDefaultValuesOffset; // Il2CppFieldDefaultValue
        public int fieldDefaultValuesCount;
        public int fieldAndParameterDefaultValueDataOffset; // uint8_t
        public int fieldAndParameterDefaultValueDataCount;
        public int fieldMarshaledSizesOffset; // Il2CppFieldMarshaledSize
        public int fieldMarshaledSizesCount;
        public int parametersOffset; // Il2CppParameterDefinition
        public int parametersCount;
        public int fieldsOffset; // Il2CppFieldDefinition
        public int fieldsCount;
        public int genericParametersOffset; // Il2CppGenericParameter
        public int genericParametersCount;
        public int genericParameterConstraintsOffset; // TypeIndex
        public int genericParameterConstraintsCount;
        public int genericContainersOffset; // Il2CppGenericContainer
        public int genericContainersCount;
        public int nestedTypesOffset; // TypeDefinitionIndex
        public int nestedTypesCount;
        public int interfacesOffset; // TypeIndex
        public int interfacesCount;
        public int vtableMethodsOffset; // EncodedMethodIndex
        public int vtableMethodsCount;
        public int interfaceOffsetsOffset; // Il2CppInterfaceOffsetPair
        public int interfaceOffsetsCount;
        public int typeDefinitionsOffset; // Il2CppTypeDefinition
        public int typeDefinitionsCount;

        [Version(Max = 24.0)]
        public int rgctxEntriesOffset; // Il2CppRGCTXDefinition
        [Version(Max = 24.0)]
        public int rgctxEntriesCount;

        public int imagesOffset; // Il2CppImageDefinition
        public int imagesCount;
        public int assembliesOffset; // Il2CppAssemblyDefinition
        public int assembliesCount;
        public int metadataUsageListsOffset; // Il2CppMetadataUsageList
        public int metadataUsageListsCount;
        public int metadataUsagePairsOffset; // Il2CppMetadataUsagePair
        public int metadataUsagePairsCount;
        public int fieldRefsOffset; // Il2CppFieldRef
        public int fieldRefsCount;
        public int referencedAssembliesOffset; // int
        public int referencedAssembliesCount;
        public int attributesInfoOffset; // Il2CppCustomAttributeTypeRange
        public int attributesInfoCount;
        public int attributeTypesOffset; // TypeIndex
        public int attributeTypesCount;

        // Added in metadata v22
        [Version(Min = 22)]
        public int unresolvedVirtualCallParameterTypesOffset; // TypeIndex
        [Version(Min = 22)]
        public int unresolvedVirtualCallParameterTypesCount;
        [Version(Min = 22)]
        public int unresolvedVirtualCallParameterRangesOffset; // Il2CppRange
        [Version(Min = 22)]
        public int unresolvedVirtualCallParameterRangesCount;

        // Added in metadata v23
        [Version(Min = 23)]
        public int windowsRuntimeTypeNamesOffset; // Il2CppWindowsRuntimeTypeNamePair
        [Version(Min = 23)]
        public int windowsRuntimeTypeNamesSize;

        // Added in metadata v24
        [Version(Min = 24)]
        public int exportedTypeDefinitionsOffset; // TypeDefinitionIndex
        [Version(Min = 24)]
        public int exportedTypeDefinitionsCount;
    }

    public class Il2CppImageDefinition
    {
        public int nameIndex;
        public int assemblyIndex;

        public int typeStart;
        public uint typeCount;

        [Version(Min = 24)]
        public int exportedTypeStart;
        [Version(Min = 24)]
        public uint exportedTypeCount;

        public int entryPointIndex;
        public uint token;

        [Version(Min = 24.1)]
        public int customAttributeStart;
        [Version(Min = 24.1)]
        public uint customAttributeCount;
    }
#pragma warning restore CS0649

    public class Il2CppTypeDefinition
    {
        public int nameIndex;
        public int namespaceIndex;

        // Removed in later versions of metadata v24
        [Version(Max = 24.0)]
        public int customAttributeIndex;

        public int byvalTypeIndex;
        public int byrefTypeIndex;

        public int declaringTypeIndex;
        public int parentIndex;
        public int elementTypeIndex; // we can probably remove this one. Only used for enums

        [Version(Max = 24.0)]
        public int rgctxStartIndex;
        [Version(Max = 24.0)]
        public int rgctxCount;

        public int genericContainerIndex;

        // Removed in metadata v23
        [Version(Max = 22)]
        public int delegateWrapperFromManagedToNativeIndex; // (was renamed to reversePInvokeWrapperIndex in v22)
        [Version(Max = 22)]
        public int marshalingFunctionsIndex;
        [Version(Max = 22)]
        public int ccwFunctionIndex;
        [Version(Max = 22)]
        public int guidIndex;

        public uint flags;

        public int fieldStart;
        public int methodStart;
        public int eventStart;
        public int propertyStart;
        public int nestedTypesStart;
        public int interfacesStart;
        public int vtableStart;
        public int interfaceOffsetsStart;

        public ushort method_count;
        public ushort property_count;
        public ushort field_count;
        public ushort event_count;
        public ushort nested_type_count;
        public ushort vtable_count;
        public ushort interfaces_count;
        public ushort interface_offsets_count;

        // bitfield to portably encode boolean values as single bits
        // 01 - valuetype;
        // 02 - enumtype;
        // 03 - has_finalize;
        // 04 - has_cctor;
        // 05 - is_blittable;
        // 06 - is_import; (from v22: is_import_or_windows_runtime)
        // 07-10 - One of nine possible PackingSize values (0, 1, 2, 4, 8, 16, 32, 64, or 128)
        public uint bitfield;
        public uint token;
    }

    public class Il2CppMethodDefinition
    {
        public int nameIndex;
        public int declaringType;
        public int returnType;
        public int parameterStart;

        [Version(Max = 24.0)]
        public int customAttributeIndex;

        public int genericContainerIndex;

        [Version(Max = 24.0)]
        public int methodIndex;
        [Version(Max = 24.0)]
        public int invokerIndex;
        [Version(Max = 24.0)]
        public int reversePInvokeWrapperIndex; // (was renamed from delegateWrapperIndex in v22)
        [Version(Max = 24.0)]
        public int rgctxStartIndex;
        [Version(Max = 24.0)]
        public int rgctxCount;

        public uint token;
        public ushort flags;
        public ushort iflags;
        public ushort slot;
        public ushort parameterCount;
    }

    public class Il2CppParameterDefinition
    {
        public int nameIndex;
        public uint token;

        [Version(Max = 24.0)]
        public int customAttributeIndex;

        public int typeIndex;
    }

    public class Il2CppFieldDefinition
    {
        public int nameIndex;
        public int typeIndex;

        [Version(Max = 24.0)]
        public int customAttributeIndex;

        public uint token;
    }

    public class Il2CppFieldDefaultValue
    {
        public int fieldIndex;
        public int typeIndex;
        public int dataIndex;
    }

    public class Il2CppPropertyDefinition
    {
        public int nameIndex;
        public int get;
        public int set;
        public uint attrs;

        [Version(Max = 24.0)]
        public int customAttributeIndex;

        public uint token;
    }

    public class Il2CppEventDefinition
    {
        public int nameIndex;
        public int typeIndex;
        public int add;
        public int remove;
        public int raise;

        [Version(Max = 24.0)]
        public int customAttributeIndex;

        public uint token;
    }
}
