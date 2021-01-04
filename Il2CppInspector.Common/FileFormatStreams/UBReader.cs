/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System.IO;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public class UBReader : FileFormatStream<UBReader>
    {
        private FatHeader header;

        public override string DefaultFilename => "app";

        protected override bool Init() {
            // Fat headers are always big-endian regardless of architectures
            Endianness = Endianness.Big;

            header = ReadObject<FatHeader>();

            if ((UB) header.Magic != UB.FAT_MAGIC)
                return false;

            NumImages = header.NumArch;
            return true;
        }

        public override IFileFormatStream this[uint index] {
            get {
                Position = 0x8 + 0x14 * index; // sizeof(FatHeader), sizeof(FatArch)
                Endianness = Endianness.Big;

                var arch = ReadObject<FatArch>();

                Position = arch.Offset;
                Endianness = Endianness.Little;

                using var s = new BinaryObjectStream(ReadBytes((int) arch.Size));
                return (IFileFormatStream) MachOReader32.Load(s, LoadOptions, OnStatusUpdate) ?? MachOReader64.Load(s, LoadOptions, OnStatusUpdate);
            }
        }
    }
}
