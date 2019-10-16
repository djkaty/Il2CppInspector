/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Il2CppInspector
{
    // Il2CppInspector ties together the binary and metadata files into a congruent API surface
    public class Il2CppInspector
    {
        private Il2CppBinary Binary { get; }
        private Metadata Metadata { get; }

        // Shortcuts
        public double Version => Metadata.Version;

        public Dictionary<int, string> Strings => Metadata.Strings;
        public Il2CppTypeDefinition[] TypeDefinitions => Metadata.Types;
        public Il2CppImageDefinition[] Images => Metadata.Images;
        public Il2CppMethodDefinition[] Methods => Metadata.Methods;
        public Il2CppParameterDefinition[] Params => Metadata.Params;
        public Il2CppFieldDefinition[] Fields => Metadata.Fields;
        public Il2CppPropertyDefinition[] Properties => Metadata.Properties;
        public Il2CppEventDefinition[] Events => Metadata.Events;
        public int[] InterfaceUsageIndices => Metadata.InterfaceUsageIndices;
        public Dictionary<int, object> FieldDefaultValue { get; } = new Dictionary<int, object>();
        public List<int> FieldOffsets { get; }
        public List<Il2CppType> TypeUsages => Binary.Types;
        public Dictionary<string, Il2CppCodeGenModule> Modules => Binary.Modules;
        public uint[] GlobalMethodPointers => Binary.GlobalMethodPointers; // <=v24.0 only

        // TODO: Finish all file access in the constructor and eliminate the need for this
        public IFileFormatReader BinaryImage => Binary.Image;

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
            bool fieldOffsetsArePointers = (Version >= 22);

            // Some variants of 21 also use an array of pointers
            if (Version == 21) {
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
                        BinaryImage.Position = BinaryImage.MapVATR((uint) pFieldOffsets);

                        for (var f = 0; f < def.field_count; f++)
                            offsets.Add(def.fieldStart + f, BinaryImage.Stream.ReadInt32());
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

            Console.WriteLine("Detected metadata version " + metadata.Version);

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
    }
}
