/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector
{
    public abstract class Il2CppBinary
    {
        public IFileFormatReader Image { get; }

        public Il2CppCodeRegistration CodeRegistration { get; protected set; }
        public Il2CppMetadataRegistration MetadataRegistration { get; protected set; }

        // Only for <=v24.1
        public ulong[] GlobalMethodPointers { get; set; }

        // Only for >=v24.2
        public Dictionary<Il2CppCodeGenModule, ulong[]> ModuleMethodPointers { get; set; } = new Dictionary<Il2CppCodeGenModule, ulong[]>();

        // NOTE: In versions <21 and earlier releases of v21, use FieldOffsets:
        // global field index => field offset
        // In versions >=22 and later releases of v21, use FieldOffsetPointers:
        // type index => RVA in image where the list of field offsets for the type start (4 bytes per field)
        
        // Negative field offsets from start of each function
        public uint[] FieldOffsets { get; private set; }

        // Pointers to field offsets
        public long[] FieldOffsetPointers { get; private set; }

        // Generated functions which call constructors on custom attributes
        public ulong[] CustomAttributeGenerators { get; private set; }

        // Generic method specs for vtables
        public Il2CppMethodSpec[] MethodSpecs { get; private set; }

        // Every defined type
        public List<Il2CppType> Types { get; private set; }

        // From v24.2 onwards, this structure is stored for each module (image)
        // One assembly may contain multiple modules
        public Dictionary<string, Il2CppCodeGenModule> Modules { get; private set; }

        protected Il2CppBinary(IFileFormatReader stream) {
            Image = stream;
        }

        protected Il2CppBinary(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) {
            Image = stream;
            Configure(Image, codeRegistration, metadataRegistration);
        }

        // Load and initialize a binary of any supported architecture
        public static Il2CppBinary Load(IFileFormatReader stream, double metadataVersion) {
            // Get type from image architecture
            var type = Assembly.GetExecutingAssembly().GetType("Il2CppInspector.Il2CppBinary" + stream.Arch.ToUpper());
            if (type == null)
                throw new NotImplementedException("Unsupported architecture: " + stream.Arch);

            var inst = (Il2CppBinary) Activator.CreateInstance(type, stream);

            // Try to process the IL2CPP image; return the instance if succeeded, otherwise null
            return inst.Initialize(metadataVersion) ? inst : null;
        }

        // Architecture-specific search function
        protected abstract (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc);

        // Check all search locations
        public bool Initialize(double version, uint imageIndex = 0) {
            var subImage = Image[imageIndex];
            subImage.Version = version;

            // Try searching the symbol table
            var symbols = subImage.GetSymbolTable();

            if (symbols?.Any() ?? false) {
                Console.WriteLine($"Symbol table(s) found with {symbols.Count} entries");

                symbols.TryGetValue("g_CodeRegistration", out var code);
                symbols.TryGetValue("g_MetadataRegistration", out var metadata);

                if (code == 0)
                    symbols.TryGetValue("_g_CodeRegistration", out code);
                if (metadata == 0)
                    symbols.TryGetValue("_g_MetadataRegistration", out metadata);

                if (code != 0 && metadata != 0) {
                    Console.WriteLine("Required structures acquired from symbol lookup");
                    Configure(subImage, code, metadata);
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
            var addrs = subImage.GetFunctionTable();

            Debug.WriteLine("Function table:");
            Debug.WriteLine(string.Join(", ", from a in addrs select string.Format($"0x{a:X8}")));

            foreach (var loc in addrs)
                if (loc != 0) {
                    var (code, metadata) = ConsiderCode(subImage, loc);
                    if (code != 0) {
                        Console.WriteLine("Required structures acquired from code heuristics. Initialization function: 0x{0:X16}", loc + subImage.GlobalOffset);
                        Configure(subImage, code, metadata); 
                        return true;
                    }
                }

            Console.WriteLine("No matches via code heuristics");
            return false;
        }

        private void Configure(IFileFormatReader image, ulong codeRegistration, ulong metadataRegistration) {
            // Output locations
            Console.WriteLine("CodeRegistration struct found at 0x{0:X16} (file offset 0x{1:X8})", image.Bits == 32 ? codeRegistration & 0xffff_ffff : codeRegistration, image.MapVATR(codeRegistration));
            Console.WriteLine("MetadataRegistration struct found at 0x{0:X16} (file offset 0x{1:X8})", image.Bits == 32 ? metadataRegistration & 0xffff_ffff : metadataRegistration, image.MapVATR(metadataRegistration));

            // Set width of long (convert to sizeof(int) for 32-bit files)
            if (image.Bits == 32) {
                image.Stream.PrimitiveMappings.Add(typeof(long), typeof(int));
                image.Stream.PrimitiveMappings.Add(typeof(ulong), typeof(uint));
            }

            // Root structures from which we find everything else
            CodeRegistration = image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
            MetadataRegistration = image.ReadMappedObject<Il2CppMetadataRegistration>(metadataRegistration);

            // The global method pointer list was deprecated in v24.2 in favour of Il2CppCodeGenModule
            if (Image.Version <= 24.1)
                GlobalMethodPointers = image.ReadMappedArray<ulong>(CodeRegistration.pmethodPointers, (int) CodeRegistration.methodPointersCount);

            // After v24 method pointers and RGCTX data were stored in Il2CppCodeGenModules
            if (Image.Version >= 24.2) {
                Modules = new Dictionary<string, Il2CppCodeGenModule>();

                // Array of pointers to Il2CppCodeGenModule
                var modules = image.ReadMappedObjectPointerArray<Il2CppCodeGenModule>(CodeRegistration.pcodeGenModules, (int) CodeRegistration.codeGenModulesCount);

                foreach (var module in modules) {
                    var name = image.ReadMappedNullTerminatedString(module.moduleName);
                    Modules.Add(name, module);

                    // Read method pointers
                    ModuleMethodPointers.Add(module, image.ReadMappedArray<ulong>(module.methodPointers, (int) module.methodPointerCount));
                }
            }

            // Field offset data. Metadata <=21.x uses a value-type array; >=21.x uses a pointer array

            // Versions from 22 onwards use an array of pointers in Binary.FieldOffsetData
            bool fieldOffsetsArePointers = (image.Version >= 22);

            // Some variants of 21 also use an array of pointers
            if (image.Version == 21) {
                // Always 4-byte values even for 64-bit builds when array is NOT pointers
                var fieldTest = image.ReadMappedArray<uint>(MetadataRegistration.pfieldOffsets, 6);

                // We detect this by relying on the fact Module, Object, ValueType, Attribute, _Attribute and Int32
                // are always the first six defined types, and that all but Int32 have no fields
                fieldOffsetsArePointers = (fieldTest[0] == 0 && fieldTest[1] == 0 && fieldTest[2] == 0 && fieldTest[3] == 0 && fieldTest[4] == 0 && fieldTest[5] > 0);
            }

            // All older versions use values directly in the array
            if (!fieldOffsetsArePointers)
                FieldOffsets = image.ReadMappedArray<uint>(MetadataRegistration.pfieldOffsets, (int)MetadataRegistration.fieldOffsetsCount);
            else
                FieldOffsetPointers = image.ReadMappedWordArray(MetadataRegistration.pfieldOffsets, (int)MetadataRegistration.fieldOffsetsCount);

            // Type definitions (pointer array)
            Types = image.ReadMappedObjectPointerArray<Il2CppType>(MetadataRegistration.ptypes, (int) MetadataRegistration.typesCount);

            // Custom attribute constructors
            CustomAttributeGenerators = image.ReadMappedArray<ulong>(CodeRegistration.customAttributeGenerators, (int) CodeRegistration.customAttributeCount);

            // Generic method specs
            MethodSpecs = image.ReadMappedArray<Il2CppMethodSpec>(MetadataRegistration.methodSpecs, (int) MetadataRegistration.methodSpecsCount);
        }
    }
}
