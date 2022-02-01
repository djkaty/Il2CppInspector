/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

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
    public class Metadata : BinaryObjectStream
    {
        public Il2CppGlobalMetadataHeader Header { get; set; }

        public Il2CppAssemblyDefinition[] Assemblies { get; set; }
        public Il2CppImageDefinition[] Images { get; set; }
        public Il2CppTypeDefinition[] Types { get; set; }
        public Il2CppMethodDefinition[] Methods { get; set; }
        public Il2CppParameterDefinition[] Params { get; set; }
        public Il2CppFieldDefinition[] Fields { get; set; }
        public Il2CppFieldDefaultValue[] FieldDefaultValues { get; set; }
        public Il2CppParameterDefaultValue[] ParameterDefaultValues { get; set; }
        public Il2CppPropertyDefinition[] Properties { get; set; }
        public Il2CppEventDefinition[] Events { get; set; }
        public Il2CppGenericContainer[] GenericContainers { get; set; }
        public Il2CppGenericParameter[] GenericParameters { get; set; }
        public Il2CppCustomAttributeTypeRange[] AttributeTypeRanges { get; set; } //Removed in v29
        public Il2CppInterfaceOffsetPair[] InterfaceOffsets { get; set; }
        public Il2CppMetadataUsageList[] MetadataUsageLists { get; set; }
        public Il2CppMetadataUsagePair[] MetadataUsagePairs { get; set; }
        public Il2CppFieldRef[] FieldRefs { get; set; }
        public Il2CppCustomAttributeDataRange[] AttributeDataRanges { get; set; } //Added in v29

        public int[] InterfaceUsageIndices { get; set; }
        public int[] NestedTypeIndices { get; set; }
        public int[] AttributeTypeIndices { get; set; } //Removed in v29
        public int[] GenericConstraintIndices { get; set; }
        public uint[] VTableMethodIndices { get; set; }
        public string[] StringLiterals { get; set; }

        public Dictionary<int, string> Strings { get; private set; } = new Dictionary<int, string>();

        // Set if something in the metadata has been modified / decrypted
        public bool IsModified { get; private set; } = false;

        // Status update callback
        private EventHandler<string> OnStatusUpdate { get; set; }
        private void StatusUpdate(string status) => OnStatusUpdate?.Invoke(this, status);

        // Initialize metadata object from a stream
        public static Metadata FromStream(MemoryStream stream, EventHandler<string> statusCallback = null) {
            // TODO: This should really be placed before the Metadata object is created,
            // but for now this ensures it is called regardless of which client is in use
            PluginHooks.LoadPipelineStarting();

            var metadata = new Metadata(statusCallback);
            stream.Position = 0;
            stream.CopyTo(metadata);
            metadata.Position = 0;
            metadata.Initialize();
            return metadata;
        }

        private Metadata(EventHandler<string> statusCallback = null) : base() => OnStatusUpdate = statusCallback;

        private void Initialize()
        {
            // Pre-processing hook
            var pluginResult = PluginHooks.PreProcessMetadata(this);
            IsModified = pluginResult.IsStreamModified;

            StatusUpdate("Processing metadata");

            // Read metadata header
            Header = ReadObject<Il2CppGlobalMetadataHeader>(0);

            // Check for correct magic bytes
            if (Header.signature != Il2CppConstants.MetadataSignature) {
                throw new InvalidOperationException("The supplied metadata file is not valid.");
            }

            // Set object versioning for Bin2Object from metadata version
            Version = Header.version;

            if (Version < 16 || Version > 29) {
                throw new InvalidOperationException($"The supplied metadata file is not of a supported version ({Header.version}).");
            }

            // Rewind and read metadata header with the correct version settings
            Header = ReadObject<Il2CppGlobalMetadataHeader>(0);

            // Sanity checking
            // Unity.IL2CPP.MetadataCacheWriter.WriteLibIl2CppMetadata always writes the metadata information in the same order it appears in the header,
            // with each block always coming directly after the previous block, 4-byte aligned. We can use this to check the integrity of the data and
            // detect sub-versions.

            // For metadata v24.0, the header can either be either 0x110 (24.0, 24.1) or 0x108 (24.2) bytes long. Since 'stringLiteralOffset' is the first thing
            // in the header after the sanity and version fields, and since it will always point directly to the first byte after the end of the header,
            // we can use this value to determine the actual header length and therefore narrow down the metadata version to 24.0/24.1 or 24.2.

            if (!pluginResult.SkipValidation) {
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
                // In v24.4 hashValueIndex was removed from Il2CppAssemblyNameDefinition, which is a field in Il2CppAssemblyDefinition
                // The number of images and assemblies should be the same. If they are not, we deduce that we are using v24.4
                // Note the version comparison matches both 24.2 and 24.3 here since 24.3 is tested for during binary loading
                var assemblyCount = Header.assembliesCount / Sizeof(typeof(Il2CppAssemblyDefinition));
                if (Version == 24.2 && assemblyCount < Images.Length)
                    Version = 24.4;

                Assemblies = ReadArray<Il2CppAssemblyDefinition>(Header.assembliesOffset, Images.Length);
                ParameterDefaultValues = ReadArray<Il2CppParameterDefaultValue>(Header.parameterDefaultValuesOffset, Header.parameterDefaultValuesCount / Sizeof(typeof(Il2CppParameterDefaultValue)));
            }
            if (Version >= 19 && Version < 27) {
                MetadataUsageLists = ReadArray<Il2CppMetadataUsageList>(Header.metadataUsageListsOffset, Header.metadataUsageListsCount / Sizeof(typeof(Il2CppMetadataUsageList)));
                MetadataUsagePairs = ReadArray<Il2CppMetadataUsagePair>(Header.metadataUsagePairsOffset, Header.metadataUsagePairsCount / Sizeof(typeof(Il2CppMetadataUsagePair)));
            }
            if (Version >= 19) {
                FieldRefs = ReadArray<Il2CppFieldRef>(Header.fieldRefsOffset, Header.fieldRefsCount / Sizeof(typeof(Il2CppFieldRef)));
            }
            if (Version >= 21 && Version < 29) {
                AttributeTypeIndices = ReadArray<int>(Header.attributeTypesOffset, Header.attributeTypesCount / sizeof(int));
                AttributeTypeRanges = ReadArray<Il2CppCustomAttributeTypeRange>(Header.attributesInfoOffset, Header.attributesInfoCount / Sizeof(typeof(Il2CppCustomAttributeTypeRange)));
            }
            if (Version >= 29) {
                AttributeDataRanges = ReadArray<Il2CppCustomAttributeDataRange>(Header.attributeDataRangeOffset, Header.attributeDataRangeCount / Sizeof(typeof(Il2CppCustomAttributeDataRange)));
            }

            // Get all metadata strings
            var pluginGetStringsResult = PluginHooks.GetStrings(this);
            if (pluginGetStringsResult.IsDataModified && !pluginGetStringsResult.IsInvalid)
                Strings = pluginGetStringsResult.Strings;

            else {
                Position = Header.stringOffset;

                while (Position < Header.stringOffset + Header.stringCount)
                    Strings.Add((int) Position - Header.stringOffset, ReadNullTerminatedString());
            }

            // Get all string literals
            var pluginGetStringLiteralsResult = PluginHooks.GetStringLiterals(this);
            if (pluginGetStringLiteralsResult.IsDataModified)
                StringLiterals = pluginGetStringLiteralsResult.StringLiterals.ToArray();

            else {
                var stringLiteralList = ReadArray<Il2CppStringLiteral>(Header.stringLiteralOffset, Header.stringLiteralCount / Sizeof(typeof(Il2CppStringLiteral)));

                StringLiterals = new string[stringLiteralList.Length];
                for (var i = 0; i < stringLiteralList.Length; i++)
                    StringLiterals[i] = ReadFixedLengthString(Header.stringLiteralDataOffset + stringLiteralList[i].dataIndex, stringLiteralList[i].length);
            }

            // Post-processing hook
            IsModified |= PluginHooks.PostProcessMetadata(this).IsStreamModified;
        }

        // Save metadata to file, overwriting if necessary
        public void SaveToFile(string pathname) {
            Position = 0;
            using (var outFile = new FileStream(pathname, FileMode.Create, FileAccess.Write))
                CopyTo(outFile);
        }

        public int Sizeof(Type type) => Sizeof(type, Version);
        
        public int Sizeof(Type type, double metadataVersion, int longSizeBytes = 8) {

            if (Reader.ObjectMappings.TryGetValue(type, out var streamType))
                type = streamType;

            int size = 0;
            foreach (var i in type.GetTypeInfo().GetFields())
            {
                // Only process fields for our selected object versioning (always process if none supplied)
                var versions = i.GetCustomAttributes<VersionAttribute>(false).Select(v => (v.Min, v.Max)).ToList();
                if (versions.Any() && !versions.Any(v => (v.Min <= metadataVersion || v.Min == -1) && (v.Max >= metadataVersion || v.Max == -1)))
                    continue;

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
