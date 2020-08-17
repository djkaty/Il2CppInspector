/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Il2CppInspector
{
    // Il2CppInspector ties together the binary and metadata files into a congruent API surface
    public class Il2CppInspector
    {
        public Il2CppBinary Binary { get; }
        public Metadata Metadata { get; }

        // All function pointers including attribute initialization functions etc. (start => end)
        public Dictionary<ulong, ulong> FunctionAddresses { get; }

        // Attribute indexes (>=24.1) arranged by customAttributeStart and token
        public Dictionary<int, Dictionary<uint, int>> AttributeIndicesByToken { get; }

        // Merged list of all metadata usage references
        public List<MetadataUsage> MetadataUsages { get; }

        // Shortcuts
        public double Version => Math.Max(Metadata.Version, Binary.Image.Version);

        public Dictionary<int, string> Strings => Metadata.Strings;
        public string[] StringLiterals => Metadata.StringLiterals;
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
        public Il2CppFieldRef[] FieldRefs => Metadata.FieldRefs;
        public Dictionary<int, (ulong, object)> FieldDefaultValue { get; } = new Dictionary<int, (ulong, object)>();
        public Dictionary<int, (ulong, object)> ParameterDefaultValue { get; } = new Dictionary<int, (ulong, object)>();
        public List<long> FieldOffsets { get; }
        public List<Il2CppType> TypeReferences => Binary.TypeReferences;
        public Dictionary<ulong, int> TypeReferenceIndicesByAddress => Binary.TypeReferenceIndicesByAddress;
        public List<Il2CppGenericInst> GenericInstances => Binary.GenericInstances;
        public Dictionary<string, Il2CppCodeGenModule> Modules => Binary.Modules;
        public ulong[] CustomAttributeGenerators { get; }
        public ulong[] MethodInvokePointers => Binary.MethodInvokePointers;
        public Il2CppMethodSpec[] MethodSpecs => Binary.MethodSpecs;
        public Dictionary<Il2CppMethodSpec, ulong> GenericMethodPointers => Binary.GenericMethodPointers;
        public Dictionary<Il2CppMethodSpec, int> GenericMethodInvokerIndices => Binary.GenericMethodInvokerIndices;

        // TODO: Finish all file access in the constructor and eliminate the need for this
        public IFileFormatReader BinaryImage => Binary.Image;

        private (ulong MetadataAddress, object Value)? getDefaultValue(int typeIndex, int dataIndex) {
            // No default
            if (dataIndex == -1)
                return (0ul, null);

            // Get pointer in binary to default value
            var pValue = Metadata.Header.fieldAndParameterDefaultValueDataOffset + dataIndex;
            var typeRef = TypeReferences[typeIndex];

            // Default value is null
            if (pValue == 0)
                return (0ul, null);

            object value = null;
            Metadata.Position = pValue;
            switch (typeRef.type) {
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

        private List<MetadataUsage> buildMetadataUsages()
        {
            // No metadata usages for versions < 19
            if (Version < 19)
                return null;

            // Metadata usages are lazily initialized during runtime for versions >= 27
            if (Version >= 27)
                return buildLateBindingMetadataUsages();

            // Version >= 19 && <= 24.3
            var usages = new Dictionary<uint, MetadataUsage>();
            foreach (var metadataUsageList in Metadata.MetadataUsageLists)
            {
                for (var i = 0; i < metadataUsageList.count; i++)
                {
                    var metadataUsagePair = Metadata.MetadataUsagePairs[metadataUsageList.start + i];
                    usages.TryAdd(metadataUsagePair.destinationindex, MetadataUsage.FromEncodedIndex(this, metadataUsagePair.encodedSourceIndex));
                }
            }
            
            // Metadata usages (addresses)
            // Unfortunately the value supplied in MetadataRegistration.matadataUsagesCount seems to be incorrect,
            // so we have to calculate the correct number of usages above before reading the usage address list from the binary
            var addresses = Binary.Image.ReadMappedArray<ulong>(Binary.MetadataRegistration.metadataUsages, usages.Count);
            foreach (var usage in usages)
                usage.Value.SetAddress(addresses[usage.Key]);

            return usages.Values.ToList();
        }

        public List<MetadataUsage> buildLateBindingMetadataUsages()
        {
            // plagiarism. noun - https://www.lexico.com/en/definition/plagiarism
            //   the practice of taking someone else's work or ideas and passing them off as one's own.
            // Synonyms: copying, piracy, theft, strealing, infringement of copyright

            BinaryImage.Position = 0;
            var sequenceLength = 0;
            var threshold = 6000; // current versions of mscorlib generate about 6000-7000 metadata usages
            var usagesCount = 0;

            // Scan the image looking for a sequential block of at least 'threshold' valid metadata tokens
            while (BinaryImage.Position < BinaryImage.Length && (usagesCount == 0 || sequenceLength > 0)) {
                var word = BinaryImage.ReadObject<ulong>();

                if (word % 2 != 1 || word >> 32 != 0) {
                    sequenceLength = 0;
                    continue;
                }

                var potentialUsage = MetadataUsage.FromEncodedIndex(this, (uint) word);
                switch (potentialUsage.Type) {
                    case MetadataUsageType.Type:
                    case MetadataUsageType.TypeInfo:
                    case MetadataUsageType.MethodDef:
                    case MetadataUsageType.MethodRef:
                    case MetadataUsageType.FieldInfo:
                    case MetadataUsageType.StringLiteral:
                        sequenceLength++;

                        if (sequenceLength >= threshold)
                            usagesCount = sequenceLength;
                        break;
                    default:
                        sequenceLength = 0;
                        break;
                }
            }

            // If we found a block, read all the tokens and map them with their VAs to MetadataUsage objects
            if (usagesCount > 0) {
                var wordSize = BinaryImage.Bits / 8;
                var pMetadataUsages = (uint) (BinaryImage.Position - (usagesCount + 1) * wordSize);
                var pMetadataUsagesVA = BinaryImage.MapFileOffsetToVA(pMetadataUsages);
                var usageTokens = BinaryImage.ReadWordArray(pMetadataUsages, usagesCount);
                var usages = usageTokens.Zip(Enumerable.Range(0, usagesCount)
                    .Select(a => pMetadataUsagesVA + (ulong) (a * wordSize)), (t, a) => MetadataUsage.FromEncodedIndex(this, (uint) t, a));

                Console.WriteLine("Late binding metadata usage block found successfully for metadata v27");
                return usages.ToList();
            }

            Console.WriteLine("Late binding metadata usage block could not be auto-detected - metadata usage references will not be available for this project");
            return null;
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
                            offsets.Add(def.fieldStart + f, available? BinaryImage.ReadUInt32() : 0);
                    }
                }

                FieldOffsets = offsets.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            }

            // Build list of custom attribute generators
            if (Version < 27)
                CustomAttributeGenerators = Binary.CustomAttributeGenerators;

            else {
                var cagCount = Images.Sum(i => i.customAttributeCount);
                CustomAttributeGenerators = new ulong[cagCount];

                foreach (var image in Images) {
                    // Get CodeGenModule for this image
                    var codeGenModule = Binary.Modules[Strings[image.nameIndex]];
                    var cags = BinaryImage.ReadMappedWordArray(codeGenModule.customAttributeCacheGenerator, (int) image.customAttributeCount);
                    cags.CopyTo(CustomAttributeGenerators, image.customAttributeStart);
                }
            }

            // Get sorted list of function pointers from all sources
            // TODO: This does not include IL2CPP API functions
            var sortedFunctionPointers = (Version <= 24.1)?
            Binary.GlobalMethodPointers.ToList() :
            Binary.ModuleMethodPointers.SelectMany(module => module.Value).ToList();

            sortedFunctionPointers.AddRange(CustomAttributeGenerators);
            sortedFunctionPointers.AddRange(MethodInvokePointers);
            sortedFunctionPointers.AddRange(GenericMethodPointers.Values);
            sortedFunctionPointers.Sort();
            sortedFunctionPointers = sortedFunctionPointers.Distinct().ToList();

            // Guestimate function end addresses
            FunctionAddresses = new Dictionary<ulong, ulong>(sortedFunctionPointers.Count);
            for (var i = 0; i < sortedFunctionPointers.Count - 1; i++)
                FunctionAddresses.Add(sortedFunctionPointers[i], sortedFunctionPointers[i + 1]);
            // The last method end pointer will be incorrect but there is no way of calculating it
            FunctionAddresses.Add(sortedFunctionPointers[^1], sortedFunctionPointers[^1]);

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
                    if (image.customAttributeCount > 0)
                        AttributeIndicesByToken.Add(image.customAttributeStart, attsByToken);
                }
            }

            // Merge all metadata usage references into a single distinct list
            MetadataUsages = buildMetadataUsages();
        }

        // Get a method pointer if available
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
            return (start & 0xffff_ffff_ffff_fffe, FunctionAddresses[start]);
        }

        // Get a concrete generic method pointer if available
        public (ulong Start, ulong End)? GetGenericMethodPointer(Il2CppMethodSpec spec) {
            if (GenericMethodPointers.TryGetValue(spec, out var start)) {
                return (start & 0xffff_ffff_ffff_fffe, FunctionAddresses[start]);
            }
            return null;
        }

        // Get a method invoker index from a method definition
        public int GetInvokerIndex(Il2CppCodeGenModule module, Il2CppMethodDefinition methodDef) {
            if (Version <= 24.1) {
                return methodDef.invokerIndex;
            }

            // Version >= 24.2
            var methodInModule = (methodDef.token & 0xffffff);
            return Binary.MethodInvokerIndices[module][methodInModule - 1];
        }

        public MetadataUsage[] GetVTable(Il2CppTypeDefinition definition) {
            MetadataUsage[] res = new MetadataUsage[definition.vtable_count];
            for (int i = 0; i < definition.vtable_count; i++) {
                var encodedIndex = VTableMethodIndices[definition.vtableStart + i];
                MetadataUsage usage = MetadataUsage.FromEncodedIndex(this, encodedIndex);
                if (usage.SourceIndex != 0)
                    res[i] = usage;
            }
            return res;
        }

        // Finds and extracts the metadata and IL2CPP binary from an APK or IPA file into MemoryStreams
        // Returns null if package not recognized or does not contain an IL2CPP application
        public static (MemoryStream Metadata, MemoryStream Binary)? GetStreamsFromPackage(string packageFile, bool silent = false) {
            try {
                // Check if it's a zip file first because ZipFile.OpenRead is extremely slow if it isn't
                using (BinaryReader zipTest = new BinaryReader(File.Open(packageFile, FileMode.Open))) {
                    if (zipTest.ReadUInt32() != 0x04034B50)
                        return null;
                }

                using ZipArchive zip = ZipFile.OpenRead(packageFile);

                Stream metadataStream, binaryStream;

                // Check for Android APK
                var metadataFile = zip.Entries.FirstOrDefault(f => f.FullName == "assets/bin/Data/Managed/Metadata/global-metadata.dat");
                var binaryFiles = zip.Entries.Where(f => f.FullName.StartsWith("lib/") && f.Name == "libil2cpp.so");

                // Check for iOS IPA
                var ipaBinaryFolder = zip.Entries.FirstOrDefault(f => f.FullName.StartsWith("Payload/") && f.FullName.EndsWith(".app/") && f.FullName.Count(x => x == '/') == 2);

                if (ipaBinaryFolder != null) {
                    var ipaBinaryName = ipaBinaryFolder.FullName[8..^5];
                    metadataFile = zip.Entries.FirstOrDefault(f => f.FullName == $"Payload/{ipaBinaryName}.app/Data/Managed/Metadata/global-metadata.dat");
                    binaryFiles = zip.Entries.Where(f => f.FullName == $"Payload/{ipaBinaryName}.app/{ipaBinaryName}");
                }

                // This package doesn't contain an IL2CPP application
                if (metadataFile == null || !binaryFiles.Any()) {
                    Console.Error.WriteLine($"Package {packageFile} does not contain an IL2CPP application");
                    return null;
                }

                // Extract the metadata file to memory
                if (!silent)
                    Console.WriteLine($"Extracting metadata from {packageFile}{Path.DirectorySeparatorChar}{metadataFile.FullName}");

                var metadataMemoryStream = new MemoryStream();
                metadataStream = metadataFile.Open();
                metadataStream.CopyTo(metadataMemoryStream);
                metadataMemoryStream.Position = 0;

                // Extract the binary file or package to memory
                var binaryMemoryStream = new MemoryStream();

                // IPAs will only have one binary (which may or may not be a UB covering multiple architectures)
                if (ipaBinaryFolder != null) {
                    if (!silent)
                        Console.WriteLine($"Extracting binary from {packageFile}{Path.DirectorySeparatorChar}{binaryFiles.First().FullName}");

                    binaryStream = binaryFiles.First().Open();
                    binaryStream.CopyTo(binaryMemoryStream);
                    binaryMemoryStream.Position = 0;
                }

                // APKs may have one or more binaries, one per architecture
                // We'll read the entire APK and load those via APKReader
                else {
                    binaryMemoryStream = new MemoryStream(File.ReadAllBytes(packageFile));
                }

                return (metadataMemoryStream, binaryMemoryStream);
            }

            // Not an archive
            catch (InvalidDataException) {
                return null;
            }
        }

        // Load from an APK or IPA file
        public static List<Il2CppInspector> LoadFromPackage(string packageFile, bool silent = false) {
            var streams = GetStreamsFromPackage(packageFile, silent);
            if (!streams.HasValue)
                return null;
            return LoadFromStream(streams.Value.Binary, streams.Value.Metadata, silent);
        }

        // Load from a binary file and metadata file
        public static List<Il2CppInspector> LoadFromFile(string binaryFile, string metadataFile, bool silent = false)
            => LoadFromStream(new FileStream(binaryFile, FileMode.Open, FileAccess.Read),
                                new MemoryStream(File.ReadAllBytes(metadataFile)),
                                silent);

        // Load from a binary stream and metadata stream
        // Must be a seekable stream otherwise we catch a System.IO.NotSupportedException
        public static List<Il2CppInspector> LoadFromStream(Stream binaryStream, Stream metadataStream, bool silent = false) {

            // Silent operation if requested
            var stdout = Console.Out;
            if (silent)
                Console.SetOut(new StreamWriter(Stream.Null));

            // Load the metadata file
            Metadata metadata;
            try {
                metadata = new Metadata(metadataStream);
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                Console.SetOut(stdout);
                return null;
            }

            Console.WriteLine("Detected metadata version " + metadata.Version);

            // Load the il2cpp code file (try all available file formats)
            IFileFormatReader stream;
            try {
                stream = FileFormatReader.Load(binaryStream);

                if (stream == null)
                    throw new InvalidOperationException("Unsupported executable file format");
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                Console.SetOut(stdout);
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
                    if (Il2CppBinary.Load(image, metadata) is Il2CppBinary binary) {
                        Console.WriteLine("IL2CPP binary version " + image.Version);

                        processors.Add(new Il2CppInspector(binary, metadata));
                    }
                    else {
                        Console.Error.WriteLine("Could not process IL2CPP image. This may mean the binary file is packed, encrypted or obfuscated, that the file is not an IL2CPP image or that Il2CppInspector was not able to automatically find the required data.");
                        Console.Error.WriteLine("Please check the binary file in a disassembler to ensure that it is an unencrypted IL2CPP binary before submitting a bug report!");
                    }
                }
                // Unknown architecture
                catch (NotImplementedException ex) {
                    Console.Error.WriteLine(ex.Message);
                }
            }

            Console.SetOut(stdout);
            return processors;
        }
    }
}
