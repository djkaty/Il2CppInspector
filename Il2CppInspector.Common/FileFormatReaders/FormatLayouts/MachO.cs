/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
#pragma warning disable CS0649
    // Structures and enums: https://opensource.apple.com/source/cctools/cctools-870/include/mach-o/loader.h

    public enum MachO : uint
    {
        MH_MAGIC = 0xfeedface,
        MH_CIGAM = 0xcefaedfe,
        MH_MAGIC_64 = 0xfeedfacf,
        MH_CIGAM_64 = 0xcffaedfe,

        MH_EXECUTE = 0x2,

        LC_SEGMENT = 0x1,
        LC_SYMTAB = 0x2,
        LC_DYSYMTAB = 0xb,
        LC_SEGMENT_64 = 0x19,
        LC_ENCRYPTION_INFO = 0x21,
        LC_DYLD_INFO = 0x22,
        LC_DYLD_INFO_ONLY = 0x80000022,
        LC_FUNCTION_STARTS = 0x26,
        LC_ENCRYPTION_INFO_64 = 0x2C,

        CPU_TYPE_X86 = 7,
        CPU_TYPE_X86_64 = 0x01000000 + CPU_TYPE_X86,
        CPU_TYPE_ARM = 12,
        CPU_TYPE_ARM64 = 0x01000000 + CPU_TYPE_ARM,
    }

    [Flags]
    public enum MachO_NType : byte
    {
        // n_type masks
        N_STAB = 0xe0,
        N_PEXT = 0x10,
        N_TYPE = 0x0e,
        N_EXT  = 0x01,

        // n_stab bits
        N_UNDF = 0x00,
        N_ABS  = 0x02,
        N_SECT = 0x0e,
        N_PBUD = 0x0c,
        N_INDR = 0x0a,

        // n_type bits when N_STAB has some bits set
        N_GSYM  = 0x20,
        N_FNAME = 0x22,
        N_FUN   = 0x24,
        N_STSYM = 0x26,
        N_BNSYM = 0x2E,
        N_ENSYM = 0x4E,
        N_SO    = 0x64,
        N_OSO   = 0x66,
    }

    internal class MachOHeader<TWord> where TWord : struct
    {
        public uint Magic;
        public uint CPUType;
        public uint CPUSubType;
        public uint FileType;
        public uint NumCommands;
        public uint SizeOfCommands;
        public TWord Flags;
    }

    internal class MachOLoadCommand
    {
        public uint Command;
        public uint Size;
    }

    internal class MachOSegmentCommand<TWord> where TWord : struct
    {
        // MachOLoadCommand
        [String(FixedSize = 16)]
        public string Name;
        public TWord VirtualAddress;
        public TWord VirtualSize;
        public TWord ImageOffset;
        public TWord ImageSize;
        public uint VMMaxProt;
        public uint VMInitProt;
        public uint NumSections;
        public uint Flags;
    }

    internal class MachOSection<TWord> where TWord : struct
    {
        [String(FixedSize = 16)]
        public string Name;
        [String(FixedSize = 16)]
        public string SegmentName;
        public TWord Address;
        public TWord Size;
        public uint ImageOffset;
        public uint Align;
        public uint ImageRelocOffset;
        public int NumRelocEntries;
        public uint Flags;
        public uint Reserved1;
        public TWord Reserved2;
    }

    internal class MachOLinkEditDataCommand
    {
        // MachOLoadCommand
        public uint Offset;
        public uint Size;
    }

    internal class MachODyldInfoCommand
    {
        public uint RebaseOffset;
        public uint RebaseSize;
        public uint BindOffset;
        public uint BindSize;
        public uint WeakBindOffset;
        public uint WeakBindSize;
        public uint LazyBindOffset;
        public uint LazyBindSize;
        public uint ExportOffset;
        public uint ExportSize;
    }

    internal class MachOSymtabCommand
    {
        public uint SymOffset;
        public uint NumSyms;
        public uint StrOffset;
        public uint StrSize;
    }

    internal class MachOEncryptionInfo
    {
        // MachOLoadCommand
        public uint CryptOffset;
        public uint CryptSize;
        public uint CryptID;
    }

    internal class MachO_nlist<TWord> where TWord : struct
    {
        public MachO_NType n_type => (MachO_NType) f_n_type;
        public uint n_strx;
        public byte f_n_type;
        public byte n_sect;
        public ushort n_desc;
        public TWord n_value;
    }

    internal class MachO_relocation_info
    {
        public int r_address;
        public uint r_data;

        public uint r_symbolnum => r_data & 0x00ffffff;
        public bool r_pcrel => ((r_data >> 24) & 1) == 1;
        public uint r_length => (r_data >> 25) & 3;
        public bool r_extern => ((r_data >> 27) & 1) == 1;
        public uint r_type => r_data >> 28;
    }
}
