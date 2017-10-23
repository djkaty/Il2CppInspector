/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;

namespace Il2CppInspector
{
    public abstract class Il2CppBinary
    {
        public IFileFormatReader Image { get; }

        public Il2CppCodeRegistration CodeRegistration { get; protected set; }
        public Il2CppMetadataRegistration MetadataRegistration { get; protected set; }

        public uint[] MethodPointers { get; set; }
        public int[] FieldOffsets { get; private set; }
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
        public bool Initialize(int version, uint imageIndex = 0) {
            var subImage = Image[imageIndex];
            Image.Stream.Version = version;
            var addrs = subImage.GetFunctionTable();
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
            FieldOffsets = image.ReadMappedArray<int>(MetadataRegistration.pfieldOffsets, MetadataRegistration.fieldOffsetsCount);
            var types = image.ReadMappedArray<uint>(MetadataRegistration.ptypes, MetadataRegistration.typesCount);
            for (int i = 0; i < MetadataRegistration.typesCount; i++)
                Types.Add(image.ReadMappedObject<Il2CppType>(types[i]));
        }
    }
}
