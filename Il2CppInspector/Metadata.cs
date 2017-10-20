/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public class Metadata : BinaryObjectReader
    {
        private Il2CppGlobalMetadataHeader pMetadataHdr;

        public Il2CppImageDefinition[] Images { get; }
        public Il2CppTypeDefinition[] Types { get; }
        public Il2CppMethodDefinition[] Methods { get; }
        public Il2CppParameterDefinition[] parameterDefs;
        public Il2CppFieldDefinition[] Fields { get; }
        public Il2CppFieldDefaultValue[] fieldDefaultValues;

        public string GetImageName(Il2CppImageDefinition image) => GetString(image.nameIndex);
        public string GetTypeNamespace(Il2CppTypeDefinition type) => GetString(type.namespaceIndex);
        public string GetTypeName(Il2CppTypeDefinition type) => GetString(type.nameIndex);

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
            pMetadataHdr = ReadObject<Il2CppGlobalMetadataHeader>();
            if (Version != 21 && Version != 22 && Version != 23)
            {
                throw new Exception($"ERROR: Metadata file supplied is not a supported version ({pMetadataHdr.version}).");
            }

            var uiImageCount = pMetadataHdr.imagesCount / Sizeof(typeof(Il2CppImageDefinition));
            var uiNumTypes = pMetadataHdr.typeDefinitionsCount / Sizeof(typeof(Il2CppTypeDefinition));
            Images = ReadArray<Il2CppImageDefinition>(pMetadataHdr.imagesOffset, uiImageCount);
            //GetTypeDefFromIndex
            Types = ReadArray<Il2CppTypeDefinition>(pMetadataHdr.typeDefinitionsOffset, uiNumTypes);
            //GetMethodDefinition
            Methods = ReadArray<Il2CppMethodDefinition>(pMetadataHdr.methodsOffset, pMetadataHdr.methodsCount / Sizeof(typeof(Il2CppMethodDefinition)));
            //GetParameterFromIndex
            parameterDefs = ReadArray<Il2CppParameterDefinition>(pMetadataHdr.parametersOffset, pMetadataHdr.parametersCount / Sizeof(typeof(Il2CppParameterDefinition)));
            //GetFieldDefFromIndex
            Fields = ReadArray<Il2CppFieldDefinition>(pMetadataHdr.fieldsOffset, pMetadataHdr.fieldsCount / Sizeof(typeof(Il2CppFieldDefinition)));
            //GetFieldDefaultFromIndex
            fieldDefaultValues = ReadArray<Il2CppFieldDefaultValue>(pMetadataHdr.fieldDefaultValuesOffset, pMetadataHdr.fieldDefaultValuesCount / Sizeof(typeof(Il2CppFieldDefaultValue)));
        }

        public Il2CppFieldDefaultValue GetFieldDefaultFromIndex(int idx)
        {
            return fieldDefaultValues.FirstOrDefault(x => x.fieldIndex == idx);
        }

        public int GetDefaultValueFromIndex(int idx)
        {
            return pMetadataHdr.fieldAndParameterDefaultValueDataOffset + idx;
        }

        public string GetString(int idx)
        {
            return ReadNullTerminatedString(pMetadataHdr.stringOffset + idx);
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
