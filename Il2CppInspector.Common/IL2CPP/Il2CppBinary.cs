/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Il2CppInspector
{
    public abstract partial class Il2CppBinary
    {
        public IFileFormatReader Image { get; }

        public Il2CppCodeRegistration CodeRegistration { get; protected set; }
        public Il2CppMetadataRegistration MetadataRegistration { get; protected set; }

        // Information for disassembly reverse engineering
        public ulong CodeRegistrationPointer { get; private set; }
        public ulong MetadataRegistrationPointer { get; private set; }
        public ulong RegistrationFunctionPointer { get; private set; }
        public Dictionary<string, ulong> CodeGenModulePointers { get; } = new Dictionary<string, ulong>();

        // Only for <=v24.1
        public ulong[] GlobalMethodPointers { get; set; }

        // Only for >=v24.2
        public Dictionary<Il2CppCodeGenModule, ulong[]> ModuleMethodPointers { get; set; } = new Dictionary<Il2CppCodeGenModule, ulong[]>();

        // Only for >=v24.2. In earlier versions, invoker indices are stored in Il2CppMethodDefinition in the metadata file
        public Dictionary<Il2CppCodeGenModule, int[]> MethodInvokerIndices { get; set; } = new Dictionary<Il2CppCodeGenModule, int[]>();

        // NOTE: In versions <21 and earlier releases of v21, use FieldOffsets:
        // global field index => field offset
        // In versions >=22 and later releases of v21, use FieldOffsetPointers:
        // type index => RVA in image where the list of field offsets for the type start (4 bytes per field)
        
        // Negative field offsets from start of each function
        public uint[] FieldOffsets { get; private set; }

        // Pointers to field offsets
        public long[] FieldOffsetPointers { get; private set; }

        // Generated functions which call constructors on custom attributes
        // Only for <=24.3
        public ulong[] CustomAttributeGenerators { get; private set; }

        // IL2CPP-generated functions which implement MethodBase.Invoke with a unique signature per invoker, defined in Il2CppInvokerTable.cpp
        // One invoker specifies a return type and argument list. Multiple methods with the same signature can be invoked with the same invoker
        public ulong[] MethodInvokePointers { get; private set; }

        // Version 16 and below: method references for vtable
        public uint[] VTableMethodReferences { get; private set; }

        // Generic method specs for vtables
        public Il2CppMethodSpec[] MethodSpecs { get; private set; }

        // List of run-time concrete generic class and method signatures
        public List<Il2CppGenericInst> GenericInstances { get; private set; }

        // List of constructed generic method function pointers corresponding to each possible method instantiation
        public Dictionary<Il2CppMethodSpec, ulong> GenericMethodPointers { get; } = new Dictionary<Il2CppMethodSpec, ulong>();

        // List of invoker pointers for concrete generic methods from MethodSpecs (as above)
        public Dictionary<Il2CppMethodSpec, int> GenericMethodInvokerIndices { get; } = new Dictionary<Il2CppMethodSpec, int>();

        // Every type reference (TypeRef) sorted by index
        public List<Il2CppType> TypeReferences { get; private set; }

        // Every type reference index sorted by virtual address
        public Dictionary<ulong, int> TypeReferenceIndicesByAddress { get; private set; }

        // From v24.2 onwards, this structure is stored for each module (image)
        // One assembly may contain multiple modules
        public Dictionary<string, Il2CppCodeGenModule> Modules { get; private set; }

        protected Il2CppBinary(IFileFormatReader stream) {
            Image = stream;
        }

        protected Il2CppBinary(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) {
            Image = stream;
            Configure(codeRegistration, metadataRegistration);
        }

        // Load and initialize a binary of any supported architecture
        private static Il2CppBinary LoadImpl(IFileFormatReader stream) {
            // Get type from image architecture
            var type = Assembly.GetExecutingAssembly().GetType("Il2CppInspector.Il2CppBinary" + stream.Arch.ToUpper());
            if (type == null)
                throw new NotImplementedException("Unsupported architecture: " + stream.Arch);

            // Set width of long (convert to sizeof(int) for 32-bit files)
            if (stream[0].Bits == 32) {
                stream[0].Stream.PrimitiveMappings.Add(typeof(long), typeof(int));
                stream[0].Stream.PrimitiveMappings.Add(typeof(ulong), typeof(uint));
            }
            
            return (Il2CppBinary) Activator.CreateInstance(type, stream[0]);
        }

        public static Il2CppBinary Load(IFileFormatReader stream, double metadataVersion) {
            var inst = LoadImpl(stream);
            // Try to process the IL2CPP image; return the instance if succeeded, otherwise null
            return inst.InitializeWithoutMetadata(metadataVersion) ? inst : null;
        }

        // Supplying the Metadata class when loading a binary is optional
        // If it is specified and both symbol table and function scanning fail,
        // Metadata will be used to try to find the required structures with data analysis
        // If it is not specified, data analysis will not be performed
        public static Il2CppBinary Load(IFileFormatReader stream, Metadata metadata) {
            var inst = LoadImpl(stream);
            return inst.Initialize(metadata) ? inst : null;
        }

        // Architecture-specific search function
        protected abstract (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc);

        // Try to find data structures via symbol table lookup, init function analysis and data heuristics
        public bool Initialize(Metadata metadata) {
            Image.Version = metadata.Version;

            if (InitializeWithoutMetadata(metadata.Version))
                return true;

            // Resort to a full scan of the file
            var (codePtr, metadataPtr) = ImageScan(metadata);
            if (codePtr == 0) {
                Console.WriteLine("No matches via data heuristics");
                return false;
            }

            Console.WriteLine("Required structures acquired from data heuristics");
            Configure(codePtr, metadataPtr);
            return true;
        }

        // Try to find data structures via symbol table lookup and init function code analysis
        public bool InitializeWithoutMetadata(double version) {
            Image.Version = version;

            // Try searching the symbol table
            var symbols = Image.GetSymbolTable();

            if (symbols.Any()) {
                Console.WriteLine($"Symbol table(s) found with {symbols.Count} entries");

                symbols.TryGetValue("g_CodeRegistration", out var code);
                symbols.TryGetValue("g_MetadataRegistration", out var metadata);

                if (code == null)
                    symbols.TryGetValue("_g_CodeRegistration", out code);
                if (metadata == null)
                    symbols.TryGetValue("_g_MetadataRegistration", out metadata);

                if (code != null && metadata != null) {
                    Console.WriteLine("Required structures acquired from symbol lookup");
                    Configure(code.VirtualAddress, metadata.VirtualAddress);
                    return true;
                }
                else {
                    Console.WriteLine("No matches in symbol table");
                }
            }
            else if (symbols != null) {
                Console.WriteLine("No symbol table present in binary file");
            }
            else {
                Console.WriteLine("Symbol table search not implemented for this binary format");
            }

            // Try searching the function table
            var addrs = Image.GetFunctionTable();

            Debug.WriteLine("Function table:");
            Debug.WriteLine(string.Join(", ", from a in addrs select string.Format($"0x{a:X8}")));

            foreach (var loc in addrs) {
                var (code, metadata) = ConsiderCode(Image, loc);
                if (code != 0) {
                    RegistrationFunctionPointer = loc + Image.GlobalOffset;
                    Console.WriteLine("Required structures acquired from code heuristics. Initialization function: 0x{0:X16}", RegistrationFunctionPointer);
                    Configure(code, metadata); 
                    return true;
                }
            }

            Console.WriteLine("No matches via code heuristics");
            return false;
        }

        private void Configure(ulong codeRegistration, ulong metadataRegistration) {
            // Store locations
            CodeRegistrationPointer = codeRegistration;
            MetadataRegistrationPointer = metadataRegistration;

            Console.WriteLine("CodeRegistration struct found at 0x{0:X16} (file offset 0x{1:X8})", Image.Bits == 32 ? codeRegistration & 0xffff_ffff : codeRegistration, Image.MapVATR(codeRegistration));
            Console.WriteLine("MetadataRegistration struct found at 0x{0:X16} (file offset 0x{1:X8})", Image.Bits == 32 ? metadataRegistration & 0xffff_ffff : metadataRegistration, Image.MapVATR(metadataRegistration));

            // Root structures from which we find everything else
            CodeRegistration = Image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
            MetadataRegistration = Image.ReadMappedObject<Il2CppMetadataRegistration>(metadataRegistration);

            // The global method pointer list was deprecated in v24.2 in favour of Il2CppCodeGenModule
            if (Image.Version <= 24.1)
                GlobalMethodPointers = Image.ReadMappedArray<ulong>(CodeRegistration.pmethodPointers, (int) CodeRegistration.methodPointersCount);

            // After v24 method pointers and RGCTX data were stored in Il2CppCodeGenModules
            if (Image.Version >= 24.2) {
                Modules = new Dictionary<string, Il2CppCodeGenModule>();

                // In v24.3, windowsRuntimeFactoryTable collides with codeGenModules. So far no samples have had windowsRuntimeFactoryCount > 0;
                // if this changes we'll have to get smarter about disambiguating these two.
                if (CodeRegistration.codeGenModulesCount == 0) {
                    Image.Version = 24.3;
                    CodeRegistration = Image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
                }

                // Array of pointers to Il2CppCodeGenModule
                var codeGenModulePointers = Image.ReadMappedArray<ulong>(CodeRegistration.pcodeGenModules, (int) CodeRegistration.codeGenModulesCount);
                var modules = Image.ReadMappedObjectPointerArray<Il2CppCodeGenModule>(CodeRegistration.pcodeGenModules, (int) CodeRegistration.codeGenModulesCount);

                foreach (var mp in modules.Zip(codeGenModulePointers, (m, p) => new { Module = m, Pointer = p })) {
                    var module = mp.Module;

                    var name = Image.ReadMappedNullTerminatedString(module.moduleName);
                    Modules.Add(name, module);
                    CodeGenModulePointers.Add(name, mp.Pointer);

                    // Read method pointers
                    // If a module contains only interfaces, abstract methods and/or non-concrete generic methods,
                    // the entire method pointer array will be NULL values, causing the methodPointer to be mapped to .bss
                    // and therefore out of scope of the binary image
                    try {
                        ModuleMethodPointers.Add(module, Image.ReadMappedArray<ulong>(module.methodPointers, (int) module.methodPointerCount));
                    } catch (InvalidOperationException) {
                        ModuleMethodPointers.Add(module, new ulong[module.methodPointerCount]);
                    }

                    // Read method invoker pointer indices - one per method
                    MethodInvokerIndices.Add(module, Image.ReadMappedArray<int>(module.invokerIndices, (int) module.methodPointerCount));
                }
            }

            // Field offset data. Metadata <=21.x uses a value-type array; >=21.x uses a pointer array

            // Versions from 22 onwards use an array of pointers in Binary.FieldOffsetData
            bool fieldOffsetsArePointers = (Image.Version >= 22);

            // Some variants of 21 also use an array of pointers
            if (Image.Version == 21) {
                var fieldTest = Image.ReadMappedWordArray(MetadataRegistration.pfieldOffsets, 6);

                // We detect this by relying on the fact Module, Object, ValueType, Attribute, _Attribute and Int32
                // are always the first six defined types, and that all but Int32 have no fields
                fieldOffsetsArePointers = (fieldTest[0] == 0 && fieldTest[1] == 0 && fieldTest[2] == 0 && fieldTest[3] == 0 && fieldTest[4] == 0 && fieldTest[5] > 0);
            }

            // All older versions use values directly in the array
            if (!fieldOffsetsArePointers)
                FieldOffsets = Image.ReadMappedArray<uint>(MetadataRegistration.pfieldOffsets, (int)MetadataRegistration.fieldOffsetsCount);
            else
                FieldOffsetPointers = Image.ReadMappedWordArray(MetadataRegistration.pfieldOffsets, (int)MetadataRegistration.fieldOffsetsCount);

            // Type references (pointer array)
            var typeRefPointers = Image.ReadMappedArray<ulong>(MetadataRegistration.ptypes, (int) MetadataRegistration.typesCount);
            TypeReferenceIndicesByAddress = typeRefPointers.Zip(Enumerable.Range(0, typeRefPointers.Length), (a, i) => new { a, i }).ToDictionary(x => x.a, x => x.i);
            TypeReferences = Image.ReadMappedObjectPointerArray<Il2CppType>(MetadataRegistration.ptypes, (int) MetadataRegistration.typesCount);

            // Custom attribute constructors (function pointers)
            // This is managed in Il2CppInspector for metadata >= 27
            if (Image.Version < 27) {
                CustomAttributeGenerators = Image.ReadMappedArray<ulong>(CodeRegistration.customAttributeGenerators, (int) CodeRegistration.customAttributeCount);
            }
            
            // Method.Invoke function pointers
            MethodInvokePointers = Image.ReadMappedArray<ulong>(CodeRegistration.invokerPointers, (int) CodeRegistration.invokerPointersCount);

            // TODO: Function pointers as shown below
            // reversePInvokeWrappers
            // <=22: delegateWrappersFromManagedToNative, marshalingFunctions
            // >=21 <=22: ccwMarshalingFunctions
            // >=22: unresolvedVirtualCallPointers
            // >=23: interopData

            if (Image.Version < 19) {
                VTableMethodReferences = Image.ReadMappedArray<uint>(MetadataRegistration.methodReferences, (int)MetadataRegistration.methodReferencesCount);
            }

            // Generic type and method specs (open and closed constructed types)
            MethodSpecs = Image.ReadMappedArray<Il2CppMethodSpec>(MetadataRegistration.methodSpecs, (int) MetadataRegistration.methodSpecsCount);

            // Concrete generic class and method signatures
            GenericInstances = Image.ReadMappedObjectPointerArray<Il2CppGenericInst>(MetadataRegistration.genericInsts, (int) MetadataRegistration.genericInstsCount);

            // Concrete generic method pointers
            var genericMethodPointers = Image.ReadMappedArray<ulong>(CodeRegistration.genericMethodPointers, (int) CodeRegistration.genericMethodPointersCount);
            var genericMethodTable = Image.ReadMappedArray<Il2CppGenericMethodFunctionsDefinitions>(MetadataRegistration.genericMethodTable, (int) MetadataRegistration.genericMethodTableCount);
            foreach (var tableEntry in genericMethodTable) {
                GenericMethodPointers.Add(MethodSpecs[tableEntry.genericMethodIndex], genericMethodPointers[tableEntry.indices.methodIndex]);
                GenericMethodInvokerIndices.Add(MethodSpecs[tableEntry.genericMethodIndex], tableEntry.indices.invokerIndex);
            }
        }

        // IL2CPP API exports
        // This strips leading underscores and selects only il2cpp_* symbols which can be mapped into the binary
        // (therefore ignoring extern imports)
        // Some binaries have functions starting "il2cpp_z_" - ignore these too
        public Dictionary<string, ulong> GetAPIExports() {
            var exports = Image.GetExports()?
                .Where(e => (e.Name.StartsWith("il2cpp_") || e.Name.StartsWith("_il2cpp_") || e.Name.StartsWith("__il2cpp_"))
                    && !e.Name.Contains("il2cpp_z_"));

            if (exports == null)
                return new Dictionary<string, ulong>();

            var exportRgx = new Regex(@"^_+");
            var il2cppExports = new Dictionary<string, ulong>();
            
            foreach (var export in exports)
                if (Image.TryMapVATR(export.VirtualAddress, out _))
                    il2cppExports.Add(exportRgx.Replace(export.Name, ""), export.VirtualAddress);

            return il2cppExports;
        }
    }
}
