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
            Configure(codeRegistration, metadataRegistration);
        }

        public Il2CppCodeRegistration PtrCodeRegistration { get; protected set; }
        public Il2CppMetadataRegistration PtrMetadataRegistration { get; protected set; }

        // Architecture-specific search function
        protected abstract (uint, uint) Search(uint loc, uint globalOffset);

        // Check all search locations
        public bool Load() {
            var addrs = Image.GetSearchLocations();
            foreach (var loc in addrs)
                if (loc != 0) {
                    var (code, metadata) = Search(loc, Image.GlobalOffset);
                    if (code != 0) {
                        Configure(code, metadata);
                        return true;
                    }
                }
            return false;
        }

        private void Configure(uint codeRegistration, uint metadataRegistration) {
            PtrCodeRegistration = Image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
            PtrMetadataRegistration = Image.ReadMappedObject<Il2CppMetadataRegistration>(metadataRegistration);
            PtrCodeRegistration.methodPointers = Image.ReadMappedArray<uint>(PtrCodeRegistration.pmethodPointers,
                (int) PtrCodeRegistration.methodPointersCount);
            PtrMetadataRegistration.fieldOffsets = Image.ReadMappedArray<int>(PtrMetadataRegistration.pfieldOffsets,
                PtrMetadataRegistration.fieldOffsetsCount);
            var types = Image.ReadMappedArray<uint>(PtrMetadataRegistration.ptypes, PtrMetadataRegistration.typesCount);
            PtrMetadataRegistration.types = new Il2CppType[PtrMetadataRegistration.typesCount];
            for (int i = 0; i < PtrMetadataRegistration.typesCount; ++i) {
                PtrMetadataRegistration.types[i] = Image.ReadMappedObject<Il2CppType>(types[i]);
                PtrMetadataRegistration.types[i].Init();
            }
        }

        public Il2CppType GetTypeFromTypeIndex(int idx) {
            return PtrMetadataRegistration.types[idx];
        }

        public int GetFieldOffsetFromIndex(int typeIndex, int fieldIndexInType) {
            var ptr = PtrMetadataRegistration.fieldOffsets[typeIndex];
            Image.Stream.Position = Image.MapVATR((uint) ptr) + 4 * fieldIndexInType;
            return Image.Stream.ReadInt32();
        }
    }
}
