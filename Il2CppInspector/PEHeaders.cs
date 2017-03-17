/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
#pragma warning disable CS0649
    internal class COFFHeader
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    internal class PEOptHeader
    {
        public ushort signature;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData;
        public uint ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOSVersion;
        public ushort MinorOSVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint Checksum;
        public ushort Subsystem;
        public ushort DLLCharacteristics;
        public uint SizeOfStackReserve;
        public uint SizeOfStackCommit;
        public uint SizeOfHeapReserve;
        public uint SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        [ArrayLength(FieldName = "NumberOfRvaAndSizes")]
        public RvaEntry[] DataDirectory;
    }

    internal class RvaEntry
    {
        public uint VirtualAddress;
        public uint Size;
    }

    internal class PESection
    {
        [String(FixedSize=8)]
        public string Name;
        public uint SizeMemory;
        public uint BaseMemory; // RVA
        public uint SizeImage; // Size in file
        public uint BaseImage; // Base address in file
        [ArrayLength(FixedSize=12)]
        public byte[] Reserved;
        public uint Flags;
    }
#pragma warning restore CS0649
}
