/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Il2CppInspector
{
    public class Il2CppInspector
    {
        public Il2CppBinary Binary { get; }
        public Metadata Metadata { get; }

        // Shortcuts
        public Dictionary<int, string> Strings => Metadata.Strings;
        public Il2CppTypeDefinition[] TypeDefinitions => Metadata.Types;
        public List<Il2CppType> TypeUsages => Binary.Types;
        public Dictionary<int, object> FieldDefaultValue { get; } = new Dictionary<int, object>();
        public List<int> FieldOffsets { get; }

        public Il2CppInspector(Il2CppBinary binary, Metadata metadata) {
            // Store stream representations
            Binary = binary;
            Metadata = metadata;

            // Get all field default values
            foreach (var fdv in Metadata.FieldDefaultValues) {
                // No default
                if (fdv.dataIndex == -1) {
                    FieldDefaultValue.Add(fdv.fieldIndex, null);
                    continue;
                }

                // Get pointer in binary to default value
                var pValue = Metadata.Header.fieldAndParameterDefaultValueDataOffset + fdv.dataIndex;
                var type = TypeUsages[fdv.typeIndex];

                // Default value is null
                if (pValue == 0) {
                    FieldDefaultValue.Add(fdv.fieldIndex, null);
                    continue;
                }

                object value = null;
                Metadata.Position = pValue;
                switch (type.type) {
                    case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
                        value = Metadata.ReadBoolean();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_U1:
                    case Il2CppTypeEnum.IL2CPP_TYPE_I1:
                        value = Metadata.ReadByte();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
                        // UTF-8 character assumed
                        value = BitConverter.ToChar(Metadata.ReadBytes(2), 0);
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_U2:
                        value = Metadata.ReadUInt16();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_I2:
                        value = Metadata.ReadInt16();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_U4:
                        value = Metadata.ReadUInt32();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_I4:
                        value = Metadata.ReadInt32();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_U8:
                        value = Metadata.ReadUInt64();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_I8:
                        value = Metadata.ReadInt64();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_R4:
                        value = Metadata.ReadSingle();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_R8:
                        value = Metadata.ReadDouble();
                        break;
                    case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
                        var uiLen = Metadata.ReadInt32();
                        value = Encoding.UTF8.GetString(Metadata.ReadBytes(uiLen));
                        break;
                }
                FieldDefaultValue.Add(fdv.fieldIndex, value);
            }

            // Get all field offsets

            // Versions from 22 onwards use an array of pointers in Binary.FieldOffsetData
            bool fieldOffsetsArePointers = (Metadata.Version >= 22);

            // Some variants of 21 also use an array of pointers
            if (Metadata.Version == 21) {
                var f = Binary.FieldOffsetData;
                // We detect this by relying on the fact Module, Object, ValueType, Attribute, _Attribute and Int32
                // are always the first six defined types, and that all but Int32 have no fields
                fieldOffsetsArePointers = (f[0] == 0 && f[1] == 0 && f[2] == 0 && f[3] == 0 && f[4] == 0 && f[5] > 0);
            }

            // All older versions use values directly in the array
            if (!fieldOffsetsArePointers) {
                FieldOffsets = Binary.FieldOffsetData.ToList();
            }
            // Convert pointer list into fields
            else {
                var offsets = new Dictionary<int, int>();
                for (var i = 0; i < TypeDefinitions.Length; i++) {
                    var def = TypeDefinitions[i];
                    var pFieldOffsets = Binary.FieldOffsetData[i];
                    if (pFieldOffsets != 0) {
                        Binary.Image.Stream.Position = Binary.Image.MapVATR((uint) pFieldOffsets);

                        for (var f = 0; f < def.field_count; f++)
                            offsets.Add(def.fieldStart + f, Binary.Image.Stream.ReadInt32());
                    }
                }
                FieldOffsets = offsets.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            }
        }

        public static List<Il2CppInspector> LoadFromFile(string codeFile, string metadataFile) {
            // Load the metadata file
            Metadata metadata;
            try {
                metadata = new Metadata(new MemoryStream(File.ReadAllBytes(metadataFile)));
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                return null;
            }

            // Load the il2cpp code file (try ELF, PE, Mach-O and Universal Binary)
            var memoryStream = new MemoryStream(File.ReadAllBytes(codeFile));
            IFileFormatReader stream =
                (((IFileFormatReader) ElfReader.Load(memoryStream) ??
                  PEReader.Load(memoryStream)) ??
                 MachOReader.Load(memoryStream)) ??
                UBReader.Load(memoryStream);
            if (stream == null) {
                Console.Error.WriteLine("Unsupported executable file format");
                return null;
            }

            // Multi-image binaries may contain more than one Il2Cpp image
            var processors = new List<Il2CppInspector>();
            foreach (var image in stream.Images) {
                Il2CppBinary binary;

                // We are currently supporting x86 and ARM architectures
                switch (image.Arch) {
                    case "x86":
                        binary = new Il2CppBinaryX86(image);
                        break;
                    case "ARM":
                        binary = new Il2CppBinaryARM(image);
                        break;
                    default:
                        Console.Error.WriteLine("Unsupported architecture");
                        return null;
                }

                // Find code and metadata regions
                if (!binary.Initialize(metadata.Version)) {
                    Console.Error.WriteLine("Could not process IL2CPP image");
                }
                else {
                    processors.Add(new Il2CppInspector(binary, metadata));
                }
            }
            return processors;
        }

        public string GetTypeName(Il2CppType pType) {
            string ret;
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE) {
                Il2CppTypeDefinition klass = TypeDefinitions[pType.datapoint];
                ret = Strings[klass.nameIndex];
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST) {
                Il2CppGenericClass generic_class = Binary.Image.ReadMappedObject<Il2CppGenericClass>(pType.datapoint);
                Il2CppTypeDefinition pMainDef = TypeDefinitions[generic_class.typeDefinitionIndex];
                ret = Strings[pMainDef.nameIndex];
                var typeNames = new List<string>();
                Il2CppGenericInst pInst =
                    Binary.Image.ReadMappedObject<Il2CppGenericInst>(generic_class.context.class_inst);
                var pointers = Binary.Image.ReadMappedArray<uint>(pInst.type_argv, (int) pInst.type_argc);
                for (int i = 0; i < pInst.type_argc; ++i) {
                    var pOriType = Binary.Image.ReadMappedObject<Il2CppType>(pointers[i]);
                    typeNames.Add(GetTypeName(pOriType));
                }
                ret += $"<{string.Join(", ", typeNames)}>";
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_ARRAY) {
                Il2CppArrayType arrayType = Binary.Image.ReadMappedObject<Il2CppArrayType>(pType.datapoint);
                var type = Binary.Image.ReadMappedObject<Il2CppType>(arrayType.etype);
                ret = $"{GetTypeName(type)}[]";
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY) {
                var type = Binary.Image.ReadMappedObject<Il2CppType>(pType.datapoint);
                ret = $"{GetTypeName(type)}[]";
            }
            else {
                if ((int) pType.type >= Il2CppConstants.CSharpTypeString.Count)
                    ret = "unknow";
                else
                    ret = Il2CppConstants.CSharpTypeString[(int) pType.type];
            }
            return ret;
        }
    }
}
