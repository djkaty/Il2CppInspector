/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public class Metadata : BinaryObjectReader
    {
        public Il2CppGlobalMetadataHeader Header;

        public Il2CppImageDefinition[] Images { get; }
        public Il2CppTypeDefinition[] Types { get; }
        public Il2CppMethodDefinition[] Methods { get; }
        public Il2CppParameterDefinition[] Params { get; }
        public Il2CppFieldDefinition[] Fields { get; }
        public Il2CppFieldDefaultValue[] FieldDefaultValues { get; }
        public Il2CppPropertyDefinition[] Properties { get; }
        public Il2CppEventDefinition[] Events { get; }

        public int[] InterfaceUsageIndices { get; }

        public Dictionary<int, string> Strings { get; } = new Dictionary<int, string>();

        public Metadata(Stream stream) : base(stream)
        {
            // Read magic bytes
            if (ReadUInt32() != 0xFAB11BAF) {
                throw new Exception("ERROR: Metadata file supplied is not valid metadata file.");
            }

            // Set object versioning for Bin2Object from metadata version
            Version = ReadInt32();

            // Rewind and read metadata header in full
            Position -= 8;
            Header = ReadObject<Il2CppGlobalMetadataHeader>();
            if (Version < 21 || Version > 24)
            {
                throw new Exception($"ERROR: Metadata file supplied is not a supported version ({Header.version}).");
            }

            // Load all the relevant metadata using offsets provided in the header
            Images = ReadArray<Il2CppImageDefinition>(Header.imagesOffset, Header.imagesCount / Sizeof(typeof(Il2CppImageDefinition)));
            Types = ReadArray<Il2CppTypeDefinition>(Header.typeDefinitionsOffset, Header.typeDefinitionsCount / Sizeof(typeof(Il2CppTypeDefinition)));
            Methods = ReadArray<Il2CppMethodDefinition>(Header.methodsOffset, Header.methodsCount / Sizeof(typeof(Il2CppMethodDefinition)));
            Params = ReadArray<Il2CppParameterDefinition>(Header.parametersOffset, Header.parametersCount / Sizeof(typeof(Il2CppParameterDefinition)));
            Fields = ReadArray<Il2CppFieldDefinition>(Header.fieldsOffset, Header.fieldsCount / Sizeof(typeof(Il2CppFieldDefinition)));
            FieldDefaultValues = ReadArray<Il2CppFieldDefaultValue>(Header.fieldDefaultValuesOffset, Header.fieldDefaultValuesCount / Sizeof(typeof(Il2CppFieldDefaultValue)));
            Properties = ReadArray<Il2CppPropertyDefinition>(Header.propertiesOffset, Header.propertiesOffset / Sizeof(typeof(Il2CppPropertyDefinition)));
            Events = ReadArray<Il2CppEventDefinition>(Header.eventsOffset, Header.eventsOffset / Sizeof(typeof(Il2CppEventDefinition)));
            InterfaceUsageIndices = ReadArray<int>(Header.interfacesOffset, Header.interfacesCount / sizeof(int));
            // TODO: ParameterDefaultValue, GenericParameters, ParameterConstraints, GenericContainers, MetadataUsage, CustomAttributes

            // Get all string literals
            Position = Header.stringOffset;
            while (Position < Header.stringOffset + Header.stringCount)
                Strings.Add((int)Position - Header.stringOffset, ReadNullTerminatedString());
        }

        private int Sizeof(Type type)
        {
            int size = 0;
            foreach (var i in type.GetTypeInfo().GetFields())
            {
                // Only process fields for our selected object versioning
                var versionAttr = i.GetCustomAttribute<VersionAttribute>(false);
                if (versionAttr != null) {
                    if (versionAttr.Min != -1 && versionAttr.Min > Version)
                        continue;
                    if (versionAttr.Max != -1 && versionAttr.Max < Version)
                        continue;
                }

                if (i.FieldType == typeof(int) || i.FieldType == typeof(uint))
                    size += 4;
                if (i.FieldType == typeof(short) || i.FieldType == typeof(ushort))
                    size += 2;
            }
            return size;
        }
    }
}
