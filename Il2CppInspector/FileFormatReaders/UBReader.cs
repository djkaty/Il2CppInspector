/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.IO;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    internal class UBReader : FileFormatReader<UBReader>
    {
        private FatHeader header;

        public UBReader(Stream stream) : base(stream) { }

        protected override bool Init() {
            // Fat headers are always big-endian regardless of architectures
            Endianness = Endianness.Big;

            header = ReadObject<FatHeader>();

            if ((UB) header.Magic != UB.FAT_MAGIC)
                return false;

            NumImages = header.NumArch;
            return true;
        }

        public override IFileFormatReader this[uint index] {
            get {
                Position = 0x8 + 0x14 * index; // sizeof(FatHeader), sizeof(FatArch)
                Endianness = Endianness.Big;

                var arch = ReadObject<FatArch>();

                Position = arch.Offset;
                Endianness = Endianness.Little;

                using var s = new MemoryStream(ReadBytes((int) arch.Size));
                return (IFileFormatReader) MachOReader32.Load(s) ?? MachOReader64.Load(s);
            }
        }
    }
}
