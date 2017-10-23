/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

namespace Il2CppInspector
{
    public abstract class Il2CppReader
    {
        public IFileFormatReader Image { get; }

        protected Il2CppReader(IFileFormatReader stream) {
            Image = stream;
        }

        protected Il2CppReader(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) {
            Image = stream;
            Configure(Image, codeRegistration, metadataRegistration);
        }

        public Il2CppCodeRegistration PtrCodeRegistration { get; protected set; }
        public Il2CppMetadataRegistration PtrMetadataRegistration { get; protected set; }

        // Architecture-specific search function
        protected abstract (uint, uint) Search(uint loc, uint globalOffset);

        // Check all search locations
        public bool Load(int version, uint imageIndex = 0) {
            var subImage = Image[imageIndex];
            Image.Stream.Version = version;
            var addrs = subImage.GetSearchLocations();
            foreach (var loc in addrs)
                if (loc != 0) {
                    var (code, metadata) = Search(loc, Image.GlobalOffset);
                    if (code != 0) {
                        Configure(subImage, code, metadata);
                        subImage.FinalizeInit(this);
                        return true;
                    }
                }
            return false;
        }

        private void Configure(IFileFormatReader image, uint codeRegistration, uint metadataRegistration) {
            PtrCodeRegistration = image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
            PtrMetadataRegistration = image.ReadMappedObject<Il2CppMetadataRegistration>(metadataRegistration);
            PtrCodeRegistration.methodPointers = image.ReadMappedArray<uint>(PtrCodeRegistration.pmethodPointers,
                (int) PtrCodeRegistration.methodPointersCount);
            PtrMetadataRegistration.fieldOffsets = image.ReadMappedArray<int>(PtrMetadataRegistration.pfieldOffsets,
                PtrMetadataRegistration.fieldOffsetsCount);
            var types = image.ReadMappedArray<uint>(PtrMetadataRegistration.ptypes, PtrMetadataRegistration.typesCount);
            PtrMetadataRegistration.types = new Il2CppType[PtrMetadataRegistration.typesCount];
            for (int i = 0; i < PtrMetadataRegistration.typesCount; ++i) {
                PtrMetadataRegistration.types[i] = image.ReadMappedObject<Il2CppType>(types[i]);
                PtrMetadataRegistration.types[i].Init();
            }
        }
    }
}
