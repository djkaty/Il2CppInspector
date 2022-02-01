/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    // Unity 4.6.1p5 - first release, no global-metadata.dat
    // Unity 5.2.0f3 -> v15
    // Unity 5.3.0f4 -> v16
    // Unity 5.3.2f1 -> v19
    // Unity 5.3.3f1 -> v20
    // Unity 5.3.5f1 -> v21
    // Unity 5.5.0f3 -> v22
    // Unity 5.6.0f3 -> v23
    // Unity 2017.1.0f3 -> v24
    // Unity 2018.3.0f2 -> v24.1
    // Unity 2019.1.0f2 -> v24.2
    // Unity 2019.3.7f1 -> v24.3
    // Unity 2019.4.15f1 -> v24.4
    // Unity 2019.4.21f1 -> v24.5
    // Unity 2020.1.0f1 -> v24.3
    // Unity 2020.1.11f1 -> v24.4
    // Unity 2020.2.0f1 -> v27
    // Unity 2020.2.4f1 -> v27.1
    // Unity 2021.1.0f1 -> v27.2
    // https://unity3d.com/get-unity/download/archive
    // Metadata version is written at the end of Unity.IL2CPP.MetadataCacheWriter.WriteLibIl2CppMetadata or WriteMetadata (Unity.IL2CPP.dll)

    // From il2cpp-metadata.h
#pragma warning disable CS0649
    public class Il2CppGlobalMetadataHeader
    {
        public uint signature;
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

        [Version(Min = 16)]
        public int parameterDefaultValuesOffset; // Il2CppParameterDefaultValue
        [Version(Min = 16)]
        public int parameterDefaultValuesCount;

        public int fieldDefaultValuesOffset; // Il2CppFieldDefaultValue
        public int fieldDefaultValuesCount;
        public int fieldAndParameterDefaultValueDataOffset; // uint8_t
        public int fieldAndParameterDefaultValueDataCount;

        [Version(Min = 16)]
        public int fieldMarshaledSizesOffset; // Il2CppFieldMarshaledSize
        [Version(Min = 16)]
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

        [Version(Max = 24.1)]
        public int rgctxEntriesOffset; // Il2CppRGCTXDefinition
        [Version(Max = 24.1)]
        public int rgctxEntriesCount;

        [Version(Min = 16)]
        public int imagesOffset; // Il2CppImageDefinition
        [Version(Min = 16)]
        public int imagesCount;
        [Version(Min = 16)]
        public int assembliesOffset; // Il2CppAssemblyDefinition
        [Version(Min = 16)]
        public int assembliesCount;

        [Version(Min = 19, Max = 24.5)]
        public int metadataUsageListsOffset; // Il2CppMetadataUsageList
        [Version(Min = 19, Max = 24.5)]
        public int metadataUsageListsCount;
        [Version(Min = 19, Max = 24.5)]
        public int metadataUsagePairsOffset; // Il2CppMetadataUsagePair
        [Version(Min = 19, Max = 24.5)]
        public int metadataUsagePairsCount;
        [Version(Min = 19)]
        public int fieldRefsOffset; // Il2CppFieldRef
        [Version(Min = 19)]
        public int fieldRefsCount;
        [Version(Min = 20)]
        public int referencedAssembliesOffset; // int32_t
        [Version(Min = 20)]
        public int referencedAssembliesCount;

        //Removed in v29
        [Version(Min = 21, Max=27.2f)]
        public int attributesInfoOffset; // Il2CppCustomAttributeTypeRange
        [Version(Min = 21, Max=27.2f)]
        public int attributesInfoCount;
        [Version(Min = 21, Max=27.2f)]
        public int attributeTypesOffset; // TypeIndex
        [Version(Min = 21, Max=27.2f)]
        public int attributeTypesCount;
        
        //Added in v29 - new attribute data
        [Version(Min = 29f)] 
        public int attributeDataOffset; //uint8_t
        [Version(Min = 29f)] 
        public int attributeDataCount;
        [Version(Min = 29f)] 
        public int attributeDataRangeOffset; //Il2CppCustomAttributeDataRange
        [Version(Min = 29f)]
        public int attributeDataRangeCount; 

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

        // Added in metadata v27
        [Version(Min = 27)]
        public int windowsRuntimeStringsOffset; // const char*
        [Version(Min = 27)]
        public int windowsRuntimeStringsSize;

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

        [Version(Min = 19)]
        public uint token;

        [Version(Min = 24.1)]
        public int customAttributeStart;
        [Version(Min = 24.1)]
        public uint customAttributeCount;
    }
#pragma warning restore CS0649

    // Renamed from Il2CppAssembly somewhere after Unity 2017.2f3 up to Unity 2018.2.0f2
    public class Il2CppAssemblyDefinition
    {
        // They moved the position of aname in v16 from the top to the bottom of the struct
        public Il2CppAssemblyNameDefinition aname => aname_pre16 ?? aname_post16;

        [Version(Max = 15)]
        public Il2CppAssemblyNameDefinition aname_pre16;

        public int imageIndex;

        [Version(Min = 24.1)]
        public uint token;

        [Version(Max = 24.0)]
        public int customAttributeIndex;

        [Version(Min = 20)]
        public int referencedAssemblyStart;
        [Version(Min = 20)]
        public int referencedAssemblyCount;

        [Version(Min = 16)]
        public Il2CppAssemblyNameDefinition aname_post16;
    }

    // Renamed from Il2CppAssemblyName somewhere after Unity 2017.2f3 up to Unity 2018.2.0f2
    public class Il2CppAssemblyNameDefinition
    {
        // They moved the position of publicKeyToken in v16 from the middle to the bottom of the struct
        public byte[] publicKeyToken => publicKeyToken_post16;

        public int nameIndex;
        public int cultureIndex;
        [Version(Max = 24.3)]
        public int hashValueIndex;
        public int publicKeyIndex;
        [Version(Max = 15), ArrayLength(FixedSize = 8)]
        public byte[] publicKeyToken_pre16;
        public uint hash_alg;
        public int hash_len;
        public uint flags;
        public int major;
        public int minor;
        public int build;
        public int revision;
        [Version(Min = 16), ArrayLength(FixedSize = 8)]
        public byte[] publicKeyToken_post16;
    }

    public class Il2CppTypeDefinition
    {
        public int nameIndex;
        public int namespaceIndex;

        // Removed in metadata v24.1
        [Version(Max = 24.0)]
        public int customAttributeIndex;

        public int byvalTypeIndex;
        [Version(Max = 24.5)]
        public int byrefTypeIndex;

        public int declaringTypeIndex;
        public int parentIndex;
        public int elementTypeIndex; // we can probably remove this one. Only used for enums

        [Version(Max = 24.1)]
        public int rgctxStartIndex;
        [Version(Max = 24.1)]
        public int rgctxCount;

        public int genericContainerIndex;

        // Removed in metadata v23
        [Version(Max = 22)]
        public int delegateWrapperFromManagedToNativeIndex; // (was renamed to reversePInvokeWrapperIndex in v22)
        [Version(Max = 22)]
        public int marshalingFunctionsIndex;
        [Version(Min = 21, Max = 22)]
        public int ccwFunctionIndex;
        [Version(Min = 21, Max = 22)]
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

        [Version(Min = 19)]
        public uint token;
    }

    public class Il2CppMethodDefinition
    {
        public int nameIndex;

        [Version(Min = 16)]
        public int declaringType;

        public int returnType;
        public int parameterStart;

        [Version(Max = 24.0)]
        public int customAttributeIndex;

        public int genericContainerIndex;

        [Version(Max = 24.1)]
        public int methodIndex;
        [Version(Max = 24.1)]
        public int invokerIndex;
        [Version(Max = 24.1)]
        public int reversePInvokeWrapperIndex; // (was renamed from delegateWrapperIndex in v22)
        [Version(Max = 24.1)]
        public int rgctxStartIndex;
        [Version(Max = 24.1)]
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

    public class Il2CppParameterDefaultValue
    {
        public int parameterIndex;
        public int typeIndex;
        public int dataIndex;
    }

    public class Il2CppFieldDefinition
    {
        public int nameIndex;
        public int typeIndex;

        [Version(Max = 24.0)]
        public int customAttributeIndex;

        [Version(Min = 19)]
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

        [Version(Min = 19)]
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

        [Version(Min = 19)]
        public uint token;
    }

    public class Il2CppGenericContainer
    {
        /* index of the generic type definition or the generic method definition corresponding to this container */
        public int ownerIndex; // either index into Il2CppClass metadata array or Il2CppMethodDefinition array
        public int type_argc;
        /* If true, we're a generic method, otherwise a generic type definition. */
        public int is_method;
        /* Our type parameters. */
        public uint genericParameterStart; // GenericParameterIndex
    }

    public class Il2CppGenericParameter
    {
        public int ownerIndex;  /* Type or method this parameter was defined in. */ // GenericContainerIndex
        public int nameIndex; // StringIndex
        public short constraintsStart; // GenericParameterConstraintIndex
        public short constraintsCount;
        public ushort num; // Generic parameter position
        public ushort flags; // GenericParameterAttributes
    }

    public class Il2CppCustomAttributeTypeRange
    {
        [Version(Min = 24.1)]
        public uint token;

        public int start;
        public int count;
    }
    
    //Added in v29
    public class Il2CppCustomAttributeDataRange
    {
        public uint token;
        public uint startOffset;
    }

    public class Il2CppInterfaceOffsetPair
    {
        public int interfaceTypeIndex;
        public int offset;
    }

    // Removed in metadata v27
    public class Il2CppMetadataUsageList
    {
        public uint start;
        public uint count;
    }

    // Removed in metadata v27
    public class Il2CppMetadataUsagePair
    {
        public uint destinationindex;
        public uint encodedSourceIndex;
    }

    public class Il2CppStringLiteral
    {
        public int length;
        public int dataIndex;
    }

    public class Il2CppFieldRef
    {
        public int typeIndex;
        public int fieldIndex; // local offset into type fields
    }
}
