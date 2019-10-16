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

        public List<Il2CppType> Types { get; } = new List<Il2CppType>();

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
                        subImage.FinalizeInit(this);
                        return true;
                    }
                }
            return false;
        }

        private void Configure(IFileFormatReader image, uint codeRegistration, uint metadataRegistration) {
            CodeRegistration = image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
            MetadataRegistration = image.ReadMappedObject<Il2CppMetadataRegistration>(metadataRegistration);
            MethodPointers = image.ReadMappedArray<uint>(CodeRegistration.pmethodPointers, (int) CodeRegistration.methodPointersCount);
            FieldOffsetData = image.ReadMappedArray<int>(MetadataRegistration.pfieldOffsets, MetadataRegistration.fieldOffsetsCount);
            var types = image.ReadMappedArray<uint>(MetadataRegistration.ptypes, MetadataRegistration.typesCount);
            for (int i = 0; i < MetadataRegistration.typesCount; i++)
                Types.Add(image.ReadMappedObject<Il2CppType>(types[i]));
        }
    }
}
