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
            pMetadataHdr = ReadObject<Il2CppGlobalMetadataHeader>();
            if (pMetadataHdr.sanity != 0xFAB11BAF)
            {
                throw new Exception("ERROR: Metadata file supplied is not valid metadata file.");
            }
            if (pMetadataHdr.version != 21 && pMetadataHdr.version != 22)
            {
                throw new Exception($"ERROR: Metadata file supplied is not a supported version[{pMetadataHdr.version}].");
            }
            var uiImageCount = pMetadataHdr.imagesCount / MySizeOf(typeof(Il2CppImageDefinition));
            var uiNumTypes = pMetadataHdr.typeDefinitionsCount / MySizeOf(typeof(Il2CppTypeDefinition));
            Images = ReadArray<Il2CppImageDefinition>(pMetadataHdr.imagesOffset, uiImageCount);
            //GetTypeDefFromIndex
            Types = ReadArray<Il2CppTypeDefinition>(pMetadataHdr.typeDefinitionsOffset, uiNumTypes);
            //GetMethodDefinition
            Methods = ReadArray<Il2CppMethodDefinition>(pMetadataHdr.methodsOffset, pMetadataHdr.methodsCount / MySizeOf(typeof(Il2CppMethodDefinition)));
            //GetParameterFromIndex
            parameterDefs = ReadArray<Il2CppParameterDefinition>(pMetadataHdr.parametersOffset, pMetadataHdr.parametersCount / MySizeOf(typeof(Il2CppParameterDefinition)));
            //GetFieldDefFromIndex
            Fields = ReadArray<Il2CppFieldDefinition>(pMetadataHdr.fieldsOffset, pMetadataHdr.fieldsCount / MySizeOf(typeof(Il2CppFieldDefinition)));
            //GetFieldDefaultFromIndex
            fieldDefaultValues = ReadArray<Il2CppFieldDefaultValue>(pMetadataHdr.fieldDefaultValuesOffset, pMetadataHdr.fieldDefaultValuesCount / MySizeOf(typeof(Il2CppFieldDefaultValue)));
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

        private int MySizeOf(Type type)
        {
            int size = 0;
            foreach (var i in type.GetTypeInfo().GetFields())
            {
                if (i.FieldType == typeof(int))
                {
                    size += 4;
                }
                else if (i.FieldType == typeof(uint))
                {
                    size += 4;
                }
                else if (i.FieldType == typeof(short))
                {
                    size += 2;
                }
                else if (i.FieldType == typeof(ushort))
                {
                    size += 2;
                }
            }
            return size;
        }
    }
}
