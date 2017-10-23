/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

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
        LC_SEGMENT_64 = 0x19,
        LC_FUNCTION_STARTS = 0x26,

        CPU_TYPE_X86 = 7,
        CPU_TYPE_X86_64 = 0x01000000 + CPU_TYPE_X86,
        CPU_TYPE_ARM = 12,
        CPU_TYPE_ARM64 = 0x01000000 + CPU_TYPE_ARM
    }

    internal class MachOHeader
    {
        public uint Magic;
        public uint CPUType;
        public uint CPUSubType;
        public uint FileType;
        public uint NumCommands;
        public uint SizeOfCommands;
        public uint Flags;
        // 64-bit header has an extra 32-bit Reserved field
    }

    internal class MachOLoadCommand
    {
        public uint Command;
        public uint Size;
    }

    internal class MachOSegmentCommand
    {
        // MachOLoadCommand
        [String(FixedSize = 16)]
        public string Name;
        public uint VirtualAddress;
        public uint VirtualSize;
        public uint ImageOffset;
        public uint ImageSize;
        public uint VMMaxProt;
        public uint VMInitProt;
        public uint NumSections;
        public uint Flags;
    }

    internal class MachOSegmentCommand64
    {
        // MachOLoadCommand
        [String(FixedSize = 16)]
        public string Name;
        public ulong VirtualAddress;
        public ulong VirtualSize;
        public ulong ImageOffset;
        public ulong ImageSize;
        public uint VMMaxProt;
        public uint VMInitProt;
        public uint NumSections;
        public uint Flags;
    }

    internal class MachOSection
    {
        [String(FixedSize = 16)]
        public string Name;
        [String(FixedSize = 16)]
        public string SegmentName;
        public uint Address;
        public uint Size;
        public uint ImageOffset;
        public uint Align;
        public uint ImageRelocOffset;
        public uint NumRelocEntries;
        public uint Flags;
        public uint Reserved1;
        public uint Reserved2;
    }

    internal class MachOSection64
    {
        [String(FixedSize = 16)]
        public string Name;
        [String(FixedSize = 16)]
        public string SegmentName;
        public ulong Address;
        public ulong Size;
        public uint ImageOffset;
        public uint Align;
        public uint ImageRelocOffset;
        public uint NumRelocEntries;
        public uint Flags;
        public uint Reserved1;
        public uint Reserved2;
        public uint Reserved3;
    }

    internal class MachOLinkEditDataCommand
    {
        // MachOLoadCommand
        public uint Offset;
        public uint Size;
    }
}
