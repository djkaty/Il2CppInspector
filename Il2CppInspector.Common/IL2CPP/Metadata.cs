/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public class Metadata : BinaryObjectReader
    {
        public Il2CppGlobalMetadataHeader Header;

        public Il2CppAssemblyDefinition[] Assemblies { get; }
        public Il2CppImageDefinition[] Images { get; }
        public Il2CppTypeDefinition[] Types { get; }
        public Il2CppMethodDefinition[] Methods { get; }
        public Il2CppParameterDefinition[] Params { get; }
        public Il2CppFieldDefinition[] Fields { get; }
        public Il2CppFieldDefaultValue[] FieldDefaultValues { get; }
        public Il2CppParameterDefaultValue[] ParameterDefaultValues { get; }
        public Il2CppPropertyDefinition[] Properties { get; }
        public Il2CppEventDefinition[] Events { get; }
        public Il2CppGenericContainer[] GenericContainers { get; }
        public Il2CppGenericParameter[] GenericParameters { get; }
        public Il2CppCustomAttributeTypeRange[] AttributeTypeRanges { get; }
        public Il2CppInterfaceOffsetPair[] InterfaceOffsets { get; }
        public Il2CppMetadataUsageList[] MetadataUsageLists { get; }
        public Il2CppMetadataUsagePair[] MetadataUsagePairs { get; }
        public Il2CppFieldRef[] FieldRefs { get; }

        public int[] InterfaceUsageIndices { get; }
        public int[] NestedTypeIndices { get; }
        public int[] AttributeTypeIndices { get; }
        public int[] GenericConstraintIndices { get; }
        public uint[] VTableMethodIndices { get; }
        public string[] StringLiterals { get; }

        public Dictionary<int, string> Strings { get; } = new Dictionary<int, string>();

        public Metadata(Stream stream) : base(stream)
        {
            // Read magic bytes
            if (ReadUInt32() != 0xFAB11BAF) {
                throw new InvalidOperationException("The supplied metadata file is not valid.");
            }

            // Set object versioning for Bin2Object from metadata version
            Version = ReadInt32();

            // Rewind and read metadata header in full
            Header = ReadObject<Il2CppGlobalMetadataHeader>(0);
            if (Version < 16 || Version > 27)
            {
                throw new InvalidOperationException($"The supplied metadata file is not of a supported version ({Header.version}).");
            }

            // Sanity checking
            // Unity.IL2CPP.MetadataCacheWriter.WriteLibIl2CppMetadata always writes the metadata information in the same order it appears in the header,
            // with each block always coming directly after the previous block, 4-byte aligned. We can use this to check the integrity of the data and
            // detect sub-versions.

            // For metadata v24, the header can either be either 0x110 (24.0, 24.1) or 0x108 (24.2) bytes long. Since 'stringLiteralOffset' is the first thing
            // in the header after the sanity and version fields, and since it will always point directly to the first byte after the end of the header,
            // we can use this value to determine the actual header length and therefore narrow down the metadata version to 24.0/24.1 or 24.2.

            var realHeaderLength = Header.stringLiteralOffset;

            if (realHeaderLength != Sizeof(typeof(Il2CppGlobalMetadataHeader))) {
                if (Version == 24.0) {
                    Version = 24.2;
                    Header = ReadObject<Il2CppGlobalMetadataHeader>(0);
                }
            }

            if (realHeaderLength != Sizeof(typeof(Il2CppGlobalMetadataHeader))) {
                throw new InvalidOperationException("Could not verify the integrity of the metadata file or accurately identify the metadata sub-version");
            }
            
            // Load all the relevant metadata using offsets provided in the header
            if (Version >= 16)
                Images = ReadArray<Il2CppImageDefinition>(Header.imagesOffset, Header.imagesCount / Sizeof(typeof(Il2CppImageDefinition)));

            // As an additional sanity check, all images in the metadata should have Mono.Cecil.MetadataToken == 1
            // In metadata v24.1, two extra fields were added which will cause the below test to fail.
            // In that case, we can then adjust the version number and reload
            // Tokens were introduced in v19 - we don't bother testing earlier versions
            if (Version >= 19 && Images.Any(x => x.token != 1))
                if (Version == 24.0) {
                    Version = 24.1;

                    // No need to re-read the header, it's the same for both sub-versions
                    Images = ReadArray<Il2CppImageDefinition>(Header.imagesOffset, Header.imagesCount / Sizeof(typeof(Il2CppImageDefinition)));

                    if (Images.Any(x => x.token != 1))
                        throw new InvalidOperationException("Could not verify the integrity of the metadata file image list");
                }

            Types = ReadArray<Il2CppTypeDefinition>(Header.typeDefinitionsOffset, Header.typeDefinitionsCount / Sizeof(typeof(Il2CppTypeDefinition)));
            Methods = ReadArray<Il2CppMethodDefinition>(Header.methodsOffset, Header.methodsCount / Sizeof(typeof(Il2CppMethodDefinition)));
            Params = ReadArray<Il2CppParameterDefinition>(Header.parametersOffset, Header.parametersCount / Sizeof(typeof(Il2CppParameterDefinition)));
            Fields = ReadArray<Il2CppFieldDefinition>(Header.fieldsOffset, Header.fieldsCount / Sizeof(typeof(Il2CppFieldDefinition)));
            FieldDefaultValues = ReadArray<Il2CppFieldDefaultValue>(Header.fieldDefaultValuesOffset, Header.fieldDefaultValuesCount / Sizeof(typeof(Il2CppFieldDefaultValue)));
            Properties = ReadArray<Il2CppPropertyDefinition>(Header.propertiesOffset, Header.propertiesCount / Sizeof(typeof(Il2CppPropertyDefinition)));
            Events = ReadArray<Il2CppEventDefinition>(Header.eventsOffset, Header.eventsCount / Sizeof(typeof(Il2CppEventDefinition)));
            InterfaceUsageIndices = ReadArray<int>(Header.interfacesOffset, Header.interfacesCount / sizeof(int));
            NestedTypeIndices = ReadArray<int>(Header.nestedTypesOffset, Header.nestedTypesCount / sizeof(int));
            GenericContainers = ReadArray<Il2CppGenericContainer>(Header.genericContainersOffset, Header.genericContainersCount / Sizeof(typeof(Il2CppGenericContainer)));
            GenericParameters = ReadArray<Il2CppGenericParameter>(Header.genericParametersOffset, Header.genericParametersCount / Sizeof(typeof(Il2CppGenericParameter)));
            GenericConstraintIndices = ReadArray<int>(Header.genericParameterConstraintsOffset, Header.genericParameterConstraintsCount / sizeof(int));
            InterfaceOffsets = ReadArray<Il2CppInterfaceOffsetPair>(Header.interfaceOffsetsOffset, Header.interfaceOffsetsCount / Sizeof(typeof(Il2CppInterfaceOffsetPair)));
            VTableMethodIndices = ReadArray<uint>(Header.vtableMethodsOffset, Header.vtableMethodsCount / sizeof(uint));

            if (Version >= 16) {
                Assemblies = ReadArray<Il2CppAssemblyDefinition>(Header.assembliesOffset, Header.assembliesCount / Sizeof(typeof(Il2CppAssemblyDefinition)));
                ParameterDefaultValues = ReadArray<Il2CppParameterDefaultValue>(Header.parameterDefaultValuesOffset, Header.parameterDefaultValuesCount / Sizeof(typeof(Il2CppParameterDefaultValue)));
            }
            if (Version >= 19 && Version < 27) {
                MetadataUsageLists = ReadArray<Il2CppMetadataUsageList>(Header.metadataUsageListsOffset, Header.metadataUsageListsCount / Sizeof(typeof(Il2CppMetadataUsageList)));
                MetadataUsagePairs = ReadArray<Il2CppMetadataUsagePair>(Header.metadataUsagePairsOffset, Header.metadataUsagePairsCount / Sizeof(typeof(Il2CppMetadataUsagePair)));
            }
            if (Version >= 19) {
                FieldRefs = ReadArray<Il2CppFieldRef>(Header.fieldRefsOffset, Header.fieldRefsCount / Sizeof(typeof(Il2CppFieldRef)));
            }
            if (Version >= 21) {
                AttributeTypeIndices = ReadArray<int>(Header.attributeTypesOffset, Header.attributeTypesCount / sizeof(int));
                AttributeTypeRanges = ReadArray<Il2CppCustomAttributeTypeRange>(Header.attributesInfoOffset, Header.attributesInfoCount / Sizeof(typeof(Il2CppCustomAttributeTypeRange)));
            }

            // Get all metadata string literals
            Position = Header.stringOffset;
            while (Position < Header.stringOffset + Header.stringCount)
                Strings.Add((int)Position - Header.stringOffset, ReadNullTerminatedString());

            // Get all managed code string literals
            var stringLiteralList = ReadArray<Il2CppStringLiteral>(Header.stringLiteralOffset, Header.stringLiteralCount / Sizeof(typeof(Il2CppStringLiteral)));

            StringLiterals = new string[stringLiteralList.Length];
            for (var i = 0; i < stringLiteralList.Length; i++)
                StringLiterals[i] = ReadFixedLengthString(Header.stringLiteralDataOffset + stringLiteralList[i].dataIndex, stringLiteralList[i].length);
        }

        private int Sizeof(Type type) => Sizeof(type, Version);

        public static int Sizeof(Type type, double metadataVersion, int longSizeBytes = 8) {

            int size = 0;
            foreach (var i in type.GetTypeInfo().GetFields())
            {
                // Only process fields for our selected object versioning
                var versionAttr = i.GetCustomAttribute<VersionAttribute>(false);
                if (versionAttr != null) {
                    if (versionAttr.Min != -1 && versionAttr.Min > metadataVersion)
                        continue;
                    if (versionAttr.Max != -1 && versionAttr.Max < metadataVersion)
                        continue;
                }

                if (i.FieldType == typeof(long) || i.FieldType == typeof(ulong))
                    size += longSizeBytes;
                else if (i.FieldType == typeof(int) || i.FieldType == typeof(uint))
                    size += 4;
                else if (i.FieldType == typeof(short) || i.FieldType == typeof(ushort))
                    size += 2;

                // Fixed-length array
                else if (i.FieldType.IsArray) {
                    var attr = i.GetCustomAttribute<ArrayLengthAttribute>(false) ??
                               throw new InvalidOperationException("Array field " + i.Name + " must have ArrayLength attribute");
                    size += attr.FixedSize;
                }

                // Embedded object
                else
                    size += Sizeof(i.FieldType, metadataVersion);
            }
            return size;
        }
    }
}
