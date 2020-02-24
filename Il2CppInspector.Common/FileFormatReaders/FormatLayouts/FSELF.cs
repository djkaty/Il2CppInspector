/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    internal enum FSELFConsts : uint
    {
        Magic = 0x1D3D154F,
        Unk4 = 0x12010100
    }

    [Flags]
    internal enum FSELFSegmentFlags
    {
        Ordered = 0x1,
        Encrypted = 0x2,
        Signed = 0x4,
        Deflated = 0x8,
        Blocked = 0x800
    }

    // SCE-specific definitions for e_type
    internal enum FSELFTypes : ushort
    {
        ET_SCE_EXEC = 0xFE00,
        ET_SCE_RELEXEC = 0xFE04,
        ET_SCE_STUBLIB  = 0xFE0C,
        ET_SCE_DYNEXEC = 0xFE10, // SCE EXEC_ASLR (PS4 Executable with ASLR)
        ET_SCE_DYNAMIC = 0xFE18,
        ET_SCE_IOPRELEXEC = 0xFF80,
        ET_SCE_IOPRELEXEC2 = 0xFF81,
        ET_SCE_EERELEXEC = 0xFF90,
        ET_SCE_EERELEXEC2 = 0xFF91,
        ET_SCE_PSPRELEXEC = 0xFFA0,
        ET_SCE_PPURELEXEC = 0xFFA4,
        ET_SCE_ARMRELEXEC = 0xFFA5,
        ET_SCE_PSPOVERLAY = 0xFFA8
    }

#pragma warning disable CS0649
    internal class FSELFHeader
    {
        public uint Magic;
        public uint Unk4;
        public byte ContentType;
        public byte ProductType;
        public ushort Padding1;
        public ushort HeaderSize;
        public ushort MetadataSize;
        public uint SELFSize;
        public uint Padding2;
        public ushort NumberOfSegments;
        public ushort Unk2;
        public uint Padding3;
    }

    internal class FSELFSegment
    {
        public ulong Flags;
        public ulong FileOffset;
        public ulong EncryptedCompressedSize;
        public ulong MemorySize;
    }

    internal class FSELFSCE
    {
        public ulong AuthID;
        public ulong ProductType;
        public ulong Version_1;
        public ulong Version_2;
        [ArrayLength(FixedSize = 0x20)]
        public byte[] ContentID; // Only if NPDRM
        [ArrayLength(FixedSize = 0x20)]
        public byte[] SHA256Sum;
    }
#pragma warning restore CS0649
}
