/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppInspector
{
    // References:
    // PE Header file: https://github.com/dotnet/llilc/blob/master/include/clr/ntimage.h
    // PE format specification: https://docs.microsoft.com/en-us/windows/win32/debug/pe-format?redirectedfrom=MSDN
    internal class PEReader : FileFormatReader<PEReader>
    {
        private COFFHeader coff;
        private IPEOptHeader pe;
        private PESection[] sections;
        private uint pFuncTable;

        public PEReader(Stream stream) : base(stream) {}

        public override string Format => pe is PEOptHeader32 ? "PE32" : "PE32+";

        public override string Arch => coff.Machine switch {
            0x8664 => "x64", // IMAGE_FILE_MACHINE_AMD64
            0x1C0 => "ARM", // IMAGE_FILE_MACHINE_ARM
            0xAA64 => "ARM64", // IMAGE_FILE_MACHINE_ARM64
            0x1C4 => "ARM", // IMAGE_FILE_MACHINE_ARMINT (Thumb-2)
            0x14C => "x86", // IMAGE_FILE_MACHINE_I386
            0x1C2 => "ARM", // IMAGE_FILE_MACHINE_THUMB (Thumb)
            _ => "Unsupported"
        };

        // IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20B
        // IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10B
        // Could also use coff.Characteristics (IMAGE_FILE_32BIT_MACHINE) or coff.Machine
        public override int Bits => (PE) pe.Magic == PE.IMAGE_NT_OPTIONAL_HDR64_MAGIC ? 64 : 32;

        protected override bool Init() {
            // Check for MZ signature "MZ"
            if (ReadUInt16() != 0x5A4D)
                return false;

            // Get offset to PE header from DOS header
            Position = ReadUInt32(0x3C);

            // Check PE signature "PE\0\0"
            if (ReadUInt32() != 0x00004550)
                return false;

            // Read COFF Header
            coff = ReadObject<COFFHeader>();

            // Ensure presence of PE Optional header
            // Size will always be 0x60 (32-bit) or 0x70 (64-bit) + (0x10 ' 0x8) for 16 RVA entries @ 8 bytes each
            if (!((coff.SizeOfOptionalHeader == 0xE0 ? 32 :
                   coff.SizeOfOptionalHeader == 0xF0 ? (int?) 64 : null) is var likelyWordSize))
                return false;

            // Read PE optional header
            pe = likelyWordSize switch {
                32 => ReadObject<PEOptHeader32>(),
                64 => ReadObject<PEOptHeader64>(),
                _ => null
            };

            // Confirm architecture magic number matches expected word size
            if ((PE) pe.Magic != pe.ExpectedMagic)
                return false;

            // Get IAT
            var IATStart = pe.DataDirectory[12].VirtualAddress;
            var IATSize = pe.DataDirectory[12].Size;

            // Get sections table
            sections = ReadArray<PESection>(coff.NumberOfSections);

            // Confirm that .rdata section begins at same place as IAT
            var rData = sections.First(x => x.Name == ".rdata");
            if (rData.VirtualAddress != IATStart)
                return false;

            // Calculate start of function pointer table
            pFuncTable = rData.PointerToRawData + IATSize;

            // Skip over __guard_check_icall_fptr and __guard_dispatch_icall_fptr if present, then the following zero offset
            Position = pFuncTable;
            if (pe is PEOptHeader32) {
                while (ReadUInt32() != 0)
                    pFuncTable += 4;
                pFuncTable += 4;
            }
            else {
                while (ReadUInt64() != 0)
                    pFuncTable += 8;
                pFuncTable += 8;
            }

            // Get base of code
            GlobalOffset = pe.ImageBase + pe.BaseOfCode - sections.First(x => x.Name == ".text").PointerToRawData;
            return true;
        }

        public override uint[] GetFunctionTable() {
            Position = pFuncTable;
            var addrs = new List<uint>();
            ulong addr;
            while ((addr = pe is PEOptHeader32? ReadUInt32() : ReadUInt64()) != 0)
                addrs.Add(MapVATR(addr) & 0xfffffffc);
            return addrs.ToArray();
        }

        public override uint MapVATR(ulong uiAddr) {
            if (uiAddr == 0)
                return 0;

            var section = sections.First(x => uiAddr - pe.ImageBase >= x.VirtualAddress &&
                                              uiAddr - pe.ImageBase < x.VirtualAddress + x.SizeOfRawData);
            return (uint) (uiAddr - section.VirtualAddress - pe.ImageBase + section.PointerToRawData);
        }
    }
}
