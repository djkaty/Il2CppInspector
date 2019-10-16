/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppInspector
{
    public abstract class Il2CppBinary
    {
        public IFileFormatReader Image { get; }

        public Il2CppCodeRegistration CodeRegistration { get; protected set; }
        public Il2CppMetadataRegistration MetadataRegistration { get; protected set; }

        public uint[] MethodPointers { get; set; }

        // NOTE: In versions <21 and earlier releases of v21, this array has the format:
        // global field index => field offset
        // In versions >=22 and later releases of v21, this array has the format:
        // type index => RVA in image where the list of field offsets for the type start (4 bytes per field)
        public int[] FieldOffsetData { get; private set; }

        // Every defined type
        public List<Il2CppType> Types { get; private set; }

        // From later versions of v24 onwards, this structure is stored for each module (image)
        // One assembly may contain multiple modules
        public Dictionary<string, Il2CppCodeGenModule> Modules { get; private set; }

        protected Il2CppBinary(IFileFormatReader stream) {
            Image = stream;
        }

        protected Il2CppBinary(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) {
            Image = stream;
            Configure(Image, codeRegistration, metadataRegistration);
        }

        // Architecture-specific search function
        protected abstract (uint, uint) ConsiderCode(uint loc, uint globalOffset);

        // Check all search locations
        public bool Initialize(double version, uint imageIndex = 0) {
            var subImage = Image[imageIndex];
            subImage.Stream.Version = version;
            var addrs = subImage.GetFunctionTable();

            Console.WriteLine("Function Table:");
            Console.WriteLine(string.Join(", ", from a in addrs select string.Format($"0x{a:X8}")));

            foreach (var loc in addrs)
                if (loc != 0) {
                    var (code, metadata) = ConsiderCode(loc, Image.GlobalOffset);
                    if (code != 0) {
                        Configure(subImage, code, metadata); 
                        return true;
                    }
                }
            return false;
        }

        private void Configure(IFileFormatReader image, uint codeRegistration, uint metadataRegistration) {
            // Root structures from which we find everything else
            CodeRegistration = image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
            MetadataRegistration = image.ReadMappedObject<Il2CppMetadataRegistration>(metadataRegistration);

            // The global method pointer list was deprecated in later versions of v24 in favour of Il2CppCodeGenModule
            if (Image.Stream.Version <= 24.0)
                MethodPointers = image.ReadMappedArray<uint>(CodeRegistration.pmethodPointers, (int) CodeRegistration.methodPointersCount);

            // After v24 method pointers and RGCTX data were stored in Il2CppCodeGenModules
            if (Image.Stream.Version >= 24.1) {
                Modules = new Dictionary<string, Il2CppCodeGenModule>();

                // Array of pointers to Il2CppCodeGenModule
                var modules = image.ReadMappedObjectPointerArray<Il2CppCodeGenModule>(CodeRegistration.pcodeGenModules, (int) CodeRegistration.codeGenModulesCount);

                foreach (var module in modules) {
                    var name = image.ReadMappedNullTerminatedString(module.moduleName);
                    Modules.Add(name, module);
                }
            }
            
            // Field offset data. Metadata <=21.x uses a value-type array; >=21.x uses a pointer array
            FieldOffsetData = image.ReadMappedArray<int>(MetadataRegistration.pfieldOffsets, MetadataRegistration.fieldOffsetsCount);
            
            // Type definitions (pointer array)
            Types = image.ReadMappedObjectPointerArray<Il2CppType>(MetadataRegistration.ptypes, MetadataRegistration.typesCount);
        }
    }
}
