/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    // Source: https://github.com/dotnet/llilc/blob/master/include/clr/ntimage.h

    public enum PE : uint
    {
        IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b,
        IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b
    }

#pragma warning disable CS0649
    // _IMAGE_FILE_HEADER
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

    // _IMAGE_OPTIONAL_HEADER
    internal interface IPEOptHeader
    {
        PE ExpectedMagic { get; }
        ushort Magic { get; }
        ulong ImageBase { get; }
        uint BaseOfCode { get; }
        RvaEntry[] DataDirectory { get; }
    }

    internal class PEOptHeader32 : IPEOptHeader
    {
        public PE ExpectedMagic => PE.IMAGE_NT_OPTIONAL_HDR32_MAGIC;
        public ushort Magic => f_Magic;
        public ulong ImageBase => f_ImageBase;
        public uint BaseOfCode => f_BaseOfCode;
        public RvaEntry[] DataDirectory => f_DataDirectory;

        public ushort f_Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint f_BaseOfCode;
        public uint BaseOfData;
        public uint f_ImageBase;
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
        public RvaEntry[] f_DataDirectory;
    }

    // _IMAGE_OPTIONAL_HEADER64
    internal class PEOptHeader64 : IPEOptHeader
    {
        public PE ExpectedMagic => PE.IMAGE_NT_OPTIONAL_HDR64_MAGIC;
        public ushort Magic => f_Magic;
        public ulong ImageBase => f_ImageBase;
        public uint BaseOfCode => f_BaseOfCode;
        public RvaEntry[] DataDirectory => f_DataDirectory;

        public ushort f_Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint f_BaseOfCode;
        public ulong f_ImageBase;
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
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        [ArrayLength(FieldName = "NumberOfRvaAndSizes")]
        public RvaEntry[] f_DataDirectory;
    }

    internal class RvaEntry
    {
        public uint VirtualAddress;
        public uint Size;
    }

    // _IMAGE_SECTION_HEADER
    internal class PESection
    {
        [String(FixedSize=8)]
        public string Name;
        public uint VirtualSize; // Size in memory
        public uint VirtualAddress; // Base address in memory (RVA)
        public uint SizeOfRawData; // Size in file
        public uint PointerToRawData; // Base address in file
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public uint Characteristics;
    }

    // _IMAGE_EXPORT_DIRECTORY
    internal class PEExportDirectory
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public uint Name;
        public uint Base;
        public uint NumberOfFunctions;
        public uint NumberOfNames;
        public uint AddressOfFunctions;
        public uint AddressOfNames;
        public uint AddressOfNameOrdinals;
    }
#pragma warning restore CS0649
}
