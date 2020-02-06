/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

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
        LC_FUNCTION_STARTS = 0x26,

        CPU_TYPE_X86 = 7,
        CPU_TYPE_X86_64 = 0x01000000 + CPU_TYPE_X86,
        CPU_TYPE_ARM = 12,
        CPU_TYPE_ARM64 = 0x01000000 + CPU_TYPE_ARM
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

    internal class MachOSymtabCommand
    {
        public uint SymOffset;
        public uint NumSyms;
        public uint StrOffset;
        public uint StrSize;
    }

    internal class MachO_nlist<TWord> where TWord : struct
    {
        public uint n_strx;
        public byte n_type;
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
