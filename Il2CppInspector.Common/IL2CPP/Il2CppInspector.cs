/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

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
        public ulong[] MethodInvokePointers { get; }
        public Il2CppMethodSpec[] MethodSpecs => Binary.MethodSpecs;
        public Dictionary<Il2CppMethodSpec, ulong> GenericMethodPointers { get; }
        public Dictionary<Il2CppMethodSpec, int> GenericMethodInvokerIndices => Binary.GenericMethodInvokerIndices;

        // TODO: Finish all file access in the constructor and eliminate the need for this
        public IFileFormatStream BinaryImage => Binary.Image;

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
                    //V29 uses the new compressed uint format
                    if (Metadata.Version >= 29)
                        value = Metadata.ReadUnityCompressedUInt(out _);
                    else
                        value = Metadata.ReadUInt32();
                    break;
                case Il2CppTypeEnum.IL2CPP_TYPE_I4:
                    //V29 uses the new compressed int format
                    if (Metadata.Version >= 29)
                        value = Metadata.ReadUnityCompressedInt(out _);
                    else
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
                    int uiLen;
                    if (Metadata.Version >= 29)
                        //As of v29 string length is compressed
                        uiLen = Metadata.ReadUnityCompressedInt(out _);
                    else
                        uiLen = Metadata.ReadInt32();
                    value = Encoding.UTF8.GetString(Metadata.ReadBytes(uiLen));
                    break;
            }

            return ((ulong) pValue, value);
        }

        private List<MetadataUsage> buildMetadataUsages() {
            // No metadata usages for versions < 19
            if (Version < 19)
                return null;

            // Metadata usages are lazily initialized during runtime for versions >= 27
            if (Version >= 27)
                return buildLateBindingMetadataUsages();

            // Version >= 19 && < 27
            var usages = new Dictionary<uint, MetadataUsage>();
            foreach (var metadataUsageList in Metadata.MetadataUsageLists) {
                for (var i = 0; i < metadataUsageList.count; i++) {
                    var metadataUsagePair = Metadata.MetadataUsagePairs[metadataUsageList.start + i];
                    usages.TryAdd(metadataUsagePair.destinationindex, MetadataUsage.FromEncodedIndex(this, metadataUsagePair.encodedSourceIndex));
                }
            }

            // Metadata usages (addresses)
            // Unfortunately the value supplied in MetadataRegistration.matadataUsagesCount seems to be incorrect,
            // so we have to calculate the correct number of usages above before reading the usage address list from the binary
            var count = usages.Keys.Max() + 1;
            var addresses = Binary.Image.ReadMappedArray<ulong>(Binary.MetadataRegistration.metadataUsages, (int) count);
            foreach (var usage in usages)
                usage.Value.SetAddress(addresses[usage.Key]);

            return usages.Values.ToList();
        }

        private List<MetadataUsage> buildLateBindingMetadataUsages() {
            // plagiarism. noun - https://www.lexico.com/en/definition/plagiarism
            //   the practice of taking someone else's work or ideas and passing them off as one's own.
            // Synonyms: copying, piracy, theft, strealing, infringement of copyright

            BinaryImage.Position = 0;
            var sequenceLength = 0;
            var threshold = 6000; // current versions of mscorlib generate about 6000-7000 metadata usages
            var usagesCount = 0;

            var words = BinaryImage.ReadArray<ulong>(0, (int) BinaryImage.Length / (BinaryImage.Bits / 8));

            // Scan the image looking for a sequential block of at least 'threshold' valid metadata tokens
            int pos;
            for (pos = 0; pos < words.Length && (usagesCount == 0 || sequenceLength > 0); pos++) {
                var word = words[pos];

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
                var pMetadataUsages = (uint) (pos * wordSize - (usagesCount + 1) * wordSize);
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

        // Thumb instruction pointers have the bottom bit set to signify a switch from ARM to Thumb when jumping
        private ulong getDecodedAddress(ulong addr) {
            if (BinaryImage.Arch != "ARM" && BinaryImage.Arch != "ARM64")
                return addr;

            return addr & 0xffff_ffff_ffff_fffe;
        }

        public Il2CppInspector(Il2CppBinary binary, Metadata metadata) {
            // Store stream representations
            Binary = binary;
            Metadata = metadata;

            // Get all field default values
            foreach (var fdv in Metadata.FieldDefaultValues)
                FieldDefaultValue.Add(fdv.fieldIndex, ((ulong, object)) getDefaultValue(fdv.typeIndex, fdv.dataIndex));

            // Get all parameter default values
            foreach (var pdv in Metadata.ParameterDefaultValues)
                ParameterDefaultValue.Add(pdv.parameterIndex, ((ulong, object)) getDefaultValue(pdv.typeIndex, pdv.dataIndex));

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
                            offsets.Add(def.fieldStart + f, available ? BinaryImage.ReadUInt32() : 0);
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

            // Decode addresses for Thumb etc. without altering the Il2CppBinary structure data
            CustomAttributeGenerators = CustomAttributeGenerators.Select(a => getDecodedAddress(a)).ToArray();
            MethodInvokePointers = Binary.MethodInvokePointers.Select(a => getDecodedAddress(a)).ToArray();
            GenericMethodPointers = Binary.GenericMethodPointers.ToDictionary(a => a.Key, a => getDecodedAddress(a.Value));

            // Get sorted list of function pointers from all sources
            // TODO: This does not include IL2CPP API functions
            var sortedFunctionPointers = (Version <= 24.1) ? Binary.GlobalMethodPointers.Select(a => getDecodedAddress(a)).ToList() : Binary.ModuleMethodPointers.SelectMany(module => module.Value).Select(a => getDecodedAddress(a)).ToList();

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
            if (Version >= 24.1 && Version < 29) {
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

            // Plugin hook PostProcessPackage
            PluginHooks.PostProcessPackage(this);
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
            start = getDecodedAddress(start);
            return (start, FunctionAddresses[start]);
        }

        // Get a concrete generic method pointer if available
        public (ulong Start, ulong End)? GetGenericMethodPointer(Il2CppMethodSpec spec) {
            if (GenericMethodPointers.TryGetValue(spec, out var start)) {
                return (start, FunctionAddresses[start]);
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

        #region Loaders

        // Finds and extracts the metadata and IL2CPP binary from one or more APK files, or one AAB or IPA file into MemoryStreams
        // Returns null if package not recognized or does not contain an IL2CPP application
        public static (MemoryStream Metadata, MemoryStream Binary)? GetStreamsFromPackage(IEnumerable<ZipArchive> zipStreams, bool silent = false) {
            try {
                MemoryStream metadataMemoryStream = null, binaryMemoryStream = null;
                ZipArchiveEntry androidAAB = null;
                ZipArchiveEntry ipaBinaryFolder = null;
                var binaryFiles = new List<ZipArchiveEntry>();

                // Iterate over each archive looking for the wanted files
                // There are three possibilities:
                // - A single IPA file containing global-metadata.dat and a single binary supporting one or more architectures
                //   (we return the binary inside the IPA to be loaded by MachOReader for single arch or UBReader for multi arch)
                // - A single APK or AAB file containing global-metadata.dat and one or more binaries (one per architecture)
                //   (we return the entire APK or AAB to be loaded by APKReader or AABReader)
                // - Multiple APK files, one of which contains global-metadadata.dat and the others contain one binary each
                //   (we return all of the binaries re-packed in memory to a new Zip file, to be loaded by APKReader)

                // We can't close the files because we might have to read from them after the foreach
                foreach (var zip in zipStreams) {
                    // Check for Android APK (split APKs will only fill one of these two variables)
                    var metadataFile = zip.Entries.FirstOrDefault(f => f.FullName == "assets/bin/Data/Managed/Metadata/global-metadata.dat");
                    binaryFiles.AddRange(zip.Entries.Where(f => f.FullName.StartsWith("lib/") && f.Name == "libil2cpp.so"));

                    // Check for Android AAB
                    androidAAB = zip.Entries.FirstOrDefault(f => f.FullName == "base/resources.pb");

                    if (androidAAB != null) {
                        metadataFile = zip.Entries.FirstOrDefault(f => f.FullName == "base/assets/bin/Data/Managed/Metadata/global-metadata.dat");
                        binaryFiles.AddRange(zip.Entries.Where(f => f.FullName.StartsWith("base/lib/") && f.Name == "libil2cpp.so"));
                    }

                    // Check for iOS IPA
                    ipaBinaryFolder = zip.Entries.FirstOrDefault(f => f.FullName.StartsWith("Payload/") && f.FullName.EndsWith(".app/") && f.FullName.Count(x => x == '/') == 2);

                    if (ipaBinaryFolder != null) {
                        var ipaBinaryName = ipaBinaryFolder.FullName[8..^5];
                        metadataFile = zip.Entries.FirstOrDefault(f => f.FullName == $"Payload/{ipaBinaryName}.app/Data/Managed/Metadata/global-metadata.dat");
                        binaryFiles.AddRange(zip.Entries.Where(f => f.FullName == $"Payload/{ipaBinaryName}.app/{ipaBinaryName}"));
                    }

                    // Found metadata?
                    if (metadataFile != null) {
                        // Extract the metadata file to memory
                        if (!silent)
                            Console.WriteLine($"Extracting metadata from (archive){Path.DirectorySeparatorChar}{metadataFile.FullName}");

                        metadataMemoryStream = new MemoryStream();
                        using var metadataStream = metadataFile.Open();
                        metadataStream.CopyTo(metadataMemoryStream);
                        metadataMemoryStream.Position = 0;
                    }
                }

                // This package doesn't contain an IL2CPP application
                if (metadataMemoryStream == null || !binaryFiles.Any()) {
                    Console.Error.WriteLine($"Package does not contain a complete IL2CPP application");
                    return null;
                }

                // IPAs will only have one binary (which may or may not be a UB covering multiple architectures)
                if (ipaBinaryFolder != null) {
                    if (!silent)
                        Console.WriteLine($"Extracting binary from {zipStreams.First()}{Path.DirectorySeparatorChar}{binaryFiles.First().FullName}");

                    // Extract the binary file or package to memory
                    binaryMemoryStream = new MemoryStream();
                    using var binaryStream = binaryFiles.First().Open();
                    binaryStream.CopyTo(binaryMemoryStream);
                    binaryMemoryStream.Position = 0;
                }

                // AABs or single APKs may have one or more binaries, one per architecture
                // Split APKs will have one binary per APK
                // Roll them up into a new in-memory zip file and load it via AABReader/APKReader
                else {
                    binaryMemoryStream = new MemoryStream();
                    using (var apkArchive = new ZipArchive(binaryMemoryStream, ZipArchiveMode.Create, true)) {
                        foreach (var binary in binaryFiles) {
                            // Don't waste time re-compressing data we just uncompressed
                            var archiveFile = apkArchive.CreateEntry(binary.FullName, CompressionLevel.NoCompression);
                            using var archiveFileStream = archiveFile.Open();
                            using var binarySourceStream = binary.Open();
                            binarySourceStream.CopyTo(archiveFileStream);
                        }
                    }

                    binaryMemoryStream.Position = 0;
                }

                return (metadataMemoryStream, binaryMemoryStream);
            }
            // Not an archive
            catch (InvalidDataException) {
                return null;
            }
        }

        public static (MemoryStream Metadata, MemoryStream Binary)? GetStreamsFromPackage(IEnumerable<string> packageFiles, bool silent = false) {
            // Check every item is a zip file first because ZipFile.OpenRead is extremely slow if it isn't
            foreach (var file in packageFiles)
                using (BinaryReader zipTest = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                    if (zipTest.ReadUInt32() != 0x04034B50)
                        return null;
                }

            // Check for an XAPK/Zip-style file
            if (packageFiles.Count() == 1) {
                try {
                    var xapk = ZipFile.OpenRead(packageFiles.First());
                    var apks = xapk.Entries.Where(f => f.FullName.EndsWith(".apk"));

                    // An XAPK/Zip file containing one or more APKs. Extract them
                    if (apks.Any()) {
                        var apkFiles = new List<MemoryStream>();
                        foreach (var apk in apks) {
                            var bytes = new MemoryStream();
                            using var apkStream = apk.Open();
                            apkStream.CopyTo(bytes);
                            apkFiles.Add(bytes);
                        }

                        return GetStreamsFromPackage(apkFiles.Select(f => new ZipArchive(f, ZipArchiveMode.Read)));
                    }
                }
                // Not an archive
                catch (InvalidDataException) {
                    return null;
                }
            }

            return GetStreamsFromPackage(packageFiles.Select(f => ZipFile.OpenRead(f)), silent);
        }

        // Load from an AAB, IPA or one or more APK files
        public static List<Il2CppInspector> LoadFromPackage(IEnumerable<string> packageFiles, LoadOptions loadOptions = null, EventHandler<string> statusCallback = null, bool silent = false) {
            var streams = GetStreamsFromPackage(packageFiles, silent);
            if (!streams.HasValue)
                return null;
            return LoadFromStream(streams.Value.Binary, streams.Value.Metadata, loadOptions, statusCallback, silent);
        }

        // Load from a binary file and metadata file
        public static List<Il2CppInspector> LoadFromFile(string binaryFile, string metadataFile, LoadOptions loadOptions = null, EventHandler<string> statusCallback = null, bool silent = false)
            => LoadFromStream(new FileStream(binaryFile, FileMode.Open, FileAccess.Read, FileShare.Read),
                new MemoryStream(File.ReadAllBytes(metadataFile)),
                loadOptions, statusCallback, silent);

        // Load from a binary stream and metadata stream
        // Must be a seekable stream otherwise we catch a System.IO.NotSupportedException
        public static List<Il2CppInspector> LoadFromStream(Stream binaryStream, MemoryStream metadataStream, LoadOptions loadOptions = null, EventHandler<string> statusCallback = null, bool silent = false) {
            // Silent operation if requested
            var stdout = Console.Out;
            if (silent)
                Console.SetOut(new StreamWriter(Stream.Null));

            // Load the metadata file
            Metadata metadata;
            try {
                metadata = Metadata.FromStream(metadataStream, statusCallback);
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                Console.SetOut(stdout);
                return null;
            }

            Console.WriteLine("Detected metadata version " + metadata.Version);

            // Load the il2cpp code file (try all available file formats)
            IFileFormatStream stream;
            try {
                stream = FileFormatStream.Load(binaryStream, loadOptions, statusCallback);

                if (stream == null)
                    throw new InvalidOperationException("Unsupported executable file format");
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                Console.SetOut(stdout);
                return null;
            }

            // Multi-image binaries may contain more than one Il2Cpp image
            var inspectors = LoadFromStream(stream, metadata, statusCallback);

            Console.SetOut(stdout);

            return inspectors;
        }

        public static List<Il2CppInspector> LoadFromStream(IFileFormatStream stream, Metadata metadata, EventHandler<string> statusCallback = null) {
            var processors = new List<Il2CppInspector>();
            foreach (var image in stream.Images) {
                Console.WriteLine("Container format: " + image.Format);
                Console.WriteLine("Container endianness: " + ((BinaryObjectStream) image).Endianness);
                Console.WriteLine("Architecture word size: {0}-bit", image.Bits);
                Console.WriteLine("Instruction set: " + image.Arch);
                Console.WriteLine("Global offset: 0x{0:X16}", image.GlobalOffset);

                // Architecture-agnostic load attempt
                try {
                    if (Il2CppBinary.Load(image, metadata, statusCallback) is Il2CppBinary binary) {
                        Console.WriteLine("IL2CPP binary version " + image.Version);

                        processors.Add(new Il2CppInspector(binary, metadata));
                    }
                    else {
                        Console.Error.WriteLine("Could not process IL2CPP image. This may mean the binary file is packed, encrypted or obfuscated in a way Il2CppInspector cannot process, that the file is not an IL2CPP image or that Il2CppInspector was not able to automatically find the required data.");
                        Console.Error.WriteLine("Please ensure you have downloaded and installed the latest set of core plugins and try again. Check the binary file in a disassembler to ensure that it is a valid IL2CPP binary before submitting a bug report!");
                    }
                }
                // Unknown architecture
                catch (NotImplementedException ex) {
                    Console.Error.WriteLine(ex.Message);
                }
            }

            // Plugin hook LoadPipelineEnding
            PluginHooks.LoadPipelineEnding(processors);

            return processors;
        }

        // Savers
        public void SaveMetadataToFile(string pathname) => Metadata.SaveToFile(pathname);
        public void SaveBinaryToFile(string pathname) => Binary.SaveToFile(pathname);

        #endregion
    }
}