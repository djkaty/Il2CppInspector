/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public enum SElfConsts : uint
    {
        Magic = 0x1D3D154F
    }

    [Flags]
    public enum SElfEntryFlags : ulong
    {
        Ordered = 0x1,
        Encrypted = 0x2,
        Signed = 0x4,
        Deflated = 0x8,
        WindowMask = 0x700,
        Blocks = 0x800,
        BlockSizeMask = 0xF000,
        Digests = 0x10000,
        Extents = 0x20000,
        SegmentIndexMask = 0x_000F_FFF0_0000
    }

    // SCE-specific definitions for e_type
    public enum SElfETypes : ushort
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

    // SCE-specific definitions for program header type
    public enum SElfPTypes : uint
    {
        PT_SCE_RELA = 0x60000000,
        PT_SCE_DYNLIBDATA = 0x61000000,
        PT_SCE_PROCPARAM = 0x61000001,
        PT_SCE_MODULE_PARAM = 0x61000002,
        PT_SCE_RELRO = 0x61000010,
        PT_SCE_COMMENT = 0x6FFFFF00,
        PT_SCE_VERSION = 0x6FFFFF01
    }

    // Extended info types
    public enum SElfExInfoTypes
    {
        PTYPE_FAKE = 0x1,
        PTYPE_NPDRM_EXEC = 0x4,
        PTYPE_NPDRM_DYNLIB = 0x5,
        PTYPE_SYSTEM_EXEC = 0x8,
        PTYPE_SYSTEM_DYNLIB = 0x9,
        PTYPE_HOST_KERNEL = 0xC,
        PTYPE_SECURE_MODULE = 0xE,
        PTYPE_SECURE_KERNEL = 0xF
    }

#pragma warning disable CS0649
    public class SElfHeader
    {
        public uint Magic;
        public byte Version;
        public byte Mode;
        public byte Endian;
        public byte Attributes;
        public uint KeyType;
        public ushort HeaderSize;
        public ushort MetadataSize;
        public ulong FileSize;
        public ushort NumberOfEntries;
        public ushort Flags;
        public uint Padding;
    }

    public class SElfEntry
    {
        public ulong Flags;
        public ulong FileOffset;
        public ulong EncryptedCompressedSize;
        public ulong MemorySize;

        public bool IsEncrypted => (Flags & (ulong) SElfEntryFlags.Encrypted) != 0;
        public bool IsDeflated => (Flags & (ulong) SElfEntryFlags.Deflated) != 0;
        public bool HasBlocks => (Flags & (ulong) SElfEntryFlags.Blocks) != 0;
        public ushort SegmentIndex => (ushort) ((Flags & (ulong) SElfEntryFlags.SegmentIndexMask) >> 20);
    }

    public class SElfSCEData
    {
        public ulong ProductAuthID;
        public ulong ProductType;
        public ulong AppVersion;
        public ulong FirmwareVersion;
        [ArrayLength(FixedSize = 0x20)]
        public byte[] ContentID; // Only if NPDRM
        [ArrayLength(FixedSize = 0x20)]
        public byte[] SHA256Digest;
    }
#pragma warning restore CS0649
}
