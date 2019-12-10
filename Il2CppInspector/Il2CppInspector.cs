/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;
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

        // All method pointers (start => end)
        private Dictionary<ulong, ulong> methodPointers { get; }

        // Attribute indexes (>=24.1) arranged by customAttributeStart and token
        public Dictionary<int, Dictionary<uint, int>> AttributeIndicesByToken { get; }

        // Shortcuts
        public double Version => Metadata.Version;

        public Dictionary<int, string> Strings => Metadata.Strings;
        public Il2CppTypeDefinition[] TypeDefinitions => Metadata.Types;
        public Il2CppAssemblyDefinition[] Assemblies => Metadata.Assemblies;
        public Il2CppImageDefinition[] Images => Metadata.Images;
        public Il2CppMethodDefinition[] Methods => Metadata.Methods;
        public Il2CppParameterDefinition[] Params => Metadata.Params;
        public Il2CppFieldDefinition[] Fields => Metadata.Fields;
        public Il2CppPropertyDefinition[] Properties => Metadata.Properties;
        public Il2CppEventDefinition[] Events => Metadata.Events;
        public Il2CppGenericContainer[] GenericContainers => Metadata.GenericContainers;
        public Il2CppGenericParameter[] GenericParameters => Metadata.GenericParameters;
        public int[] GenericConstraintIndices => Metadata.GenericConstraintIndices;
        public Il2CppCustomAttributeTypeRange[] AttributeTypeRanges => Metadata.AttributeTypeRanges;
        public Il2CppInterfaceOffsetPair[] InterfaceOffsets => Metadata.InterfaceOffsets;
        public int[] InterfaceUsageIndices => Metadata.InterfaceUsageIndices;
        public int[] NestedTypeIndices => Metadata.NestedTypeIndices;
        public int[] AttributeTypeIndices => Metadata.AttributeTypeIndices;
        public uint[] VTableMethodIndices => Metadata.VTableMethodIndices;
        public Dictionary<int, (ulong, object)> FieldDefaultValue { get; } = new Dictionary<int, (ulong, object)>();
        public Dictionary<int, (ulong, object)> ParameterDefaultValue { get; } = new Dictionary<int, (ulong, object)>();
        public List<long> FieldOffsets { get; }
        public List<Il2CppType> TypeUsages => Binary.Types;
        public Dictionary<string, Il2CppCodeGenModule> Modules => Binary.Modules;
        public ulong[] CustomAttributeGenerators => Binary.CustomAttributeGenerators;
        public Il2CppMethodSpec[] MethodSpecs => Binary.MethodSpecs;

        // TODO: Finish all file access in the constructor and eliminate the need for this
        public IFileFormatReader BinaryImage => Binary.Image;

        private (ulong MetadataAddress, object Value)? getDefaultValue(int typeIndex, int dataIndex) {
            // No default
            if (dataIndex == -1)
                return (0ul, null);

            // Get pointer in binary to default value
            var pValue = Metadata.Header.fieldAndParameterDefaultValueDataOffset + dataIndex;
            var type = TypeUsages[typeIndex];

            // Default value is null
            if (pValue == 0)
                return (0ul, null);

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
            return ((ulong) pValue, value);
        }

        public Il2CppInspector(Il2CppBinary binary, Metadata metadata) {
            // Store stream representations
            Binary = binary;
            Metadata = metadata;

            // Get all field default values
            foreach (var fdv in Metadata.FieldDefaultValues)
                FieldDefaultValue.Add(fdv.fieldIndex, ((ulong,object)) getDefaultValue(fdv.typeIndex, fdv.dataIndex));

            // Get all parameter default values
            foreach (var pdv in Metadata.ParameterDefaultValues)
                ParameterDefaultValue.Add(pdv.parameterIndex, ((ulong,object)) getDefaultValue(pdv.typeIndex, pdv.dataIndex));

            // Get all field offsets
            if (Binary.FieldOffsets != null) {
                FieldOffsets = Binary.FieldOffsets.Select(x => (long) x).ToList();
            }

            // Convert pointer list into fields
            else {
                var offsets = new Dictionary<int, long>();
                for (var i = 0; i < TypeDefinitions.Length; i++) {
                    var def = TypeDefinitions[i];
                    var pFieldOffsets = Binary.FieldOffsetPointers[i];
                    if (pFieldOffsets != 0) {
                        bool available = true;

                        // If the target address range is not mapped in the file, assume zeroes
                        try {
                            BinaryImage.Position = BinaryImage.MapVATR((ulong) pFieldOffsets);
                        }
                        catch (InvalidOperationException) {
                            available = false;
                        }

                        for (var f = 0; f < def.field_count; f++)
                            offsets.Add(def.fieldStart + f, available? BinaryImage.ReadWord() : 0);
                    }
                }

                FieldOffsets = offsets.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            }

            // Get sorted list of method pointers
            var sortedMethodPointers = (Version <= 24.1)?
                Binary.GlobalMethodPointers.OrderBy(m => m).ToList() :
                Binary.ModuleMethodPointers.SelectMany(module => module.Value).OrderBy(m => m).Distinct().ToList();

            // Guestimate method end addresses
            methodPointers = new Dictionary<ulong, ulong>(sortedMethodPointers.Count);
            for (var i = 0; i < sortedMethodPointers.Count - 1; i++)
                methodPointers.TryAdd(sortedMethodPointers[i], sortedMethodPointers[i + 1]);
            // The last method end pointer will be incorrect but there is no way of calculating it
            methodPointers.TryAdd(sortedMethodPointers[^1], sortedMethodPointers[^1]);

            // Organize custom attribute indices
            if (Version >= 24.1) {
                AttributeIndicesByToken = new Dictionary<int, Dictionary<uint, int>>();
                foreach (var image in Images) {
                    var attsByToken = new Dictionary<uint, int>();
                    for (int i = 0; i < image.customAttributeCount; i++) {
                        var index = image.customAttributeStart + i;
                        var token = AttributeTypeRanges[index].token;
                        attsByToken.Add(token, index);
                    }
                    AttributeIndicesByToken.Add(image.customAttributeStart, attsByToken);
                }
            }
        }

        public (ulong Start, ulong End)? GetMethodPointer(Il2CppCodeGenModule module, Il2CppMethodDefinition methodDef) {
            // Find method pointer
            if (methodDef.methodIndex < 0)
                return null;

            ulong start = 0;

            // Global method pointer array
            if (Version <= 24.1) {
                start = Binary.GlobalMethodPointers[methodDef.methodIndex];
            }

            // Per-module method pointer array uses the bottom 24 bits of the method's metadata token
            // Derived from il2cpp::vm::MetadataCache::GetMethodPointer
            if (Version >= 24.2) {
                var method = (methodDef.token & 0xffffff);
                if (method == 0)
                    return null;

                // In the event of an exception, the method pointer is not set in the file
                // This probably means it has been optimized away by the compiler, or is an unused generic method
                try {
                    // Remove ARM Thumb marker LSB if necessary
                    start = Binary.ModuleMethodPointers[module][method - 1];
                }
                catch (IndexOutOfRangeException) {
                    return null;
                }
            }

            if (start == 0)
                return null;

            // Consider the end of the method to be the start of the next method (or zero)
            // The last method end will be wrong but there is no way to calculate it
            return (start & 0xffff_ffff_ffff_fffe, methodPointers[start]);
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

            // Load the il2cpp code file (try all available file formats)
            IFileFormatReader stream = FileFormatReader.Load(codeFile);
            if (stream == null) {
                Console.Error.WriteLine("Unsupported executable file format");
                return null;
            }

            // Multi-image binaries may contain more than one Il2Cpp image
            var processors = new List<Il2CppInspector>();
            foreach (var image in stream.Images) {
                Console.WriteLine("Container format: " + image.Format);
                Console.WriteLine("Container endianness: " + ((BinaryObjectReader) image).Endianness);
                Console.WriteLine("Architecture word size: {0}-bit", image.Bits);
                Console.WriteLine("Instruction set: " + image.Arch);
                Console.WriteLine("Global offset: 0x{0:X16}", image.GlobalOffset);

                // Architecture-agnostic load attempt
                try {
                    if (Il2CppBinary.Load(image, metadata.Version) is Il2CppBinary binary) {
                        processors.Add(new Il2CppInspector(binary, metadata));
                    }
                    else {
                        Console.Error.WriteLine("Could not process IL2CPP image");
                    }
                }
                // Unknown architecture
                catch (NotImplementedException ex) {
                    Console.Error.WriteLine(ex.Message);
                }
            }
            return processors;
        }
    }
}
