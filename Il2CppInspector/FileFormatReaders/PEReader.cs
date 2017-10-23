/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppInspector
{
    internal class PEReader : FileFormatReader<PEReader>
    {
        private COFFHeader coff;
        private PEOptHeader pe;
        private PESection[] sections;
        private uint pFuncTable;

        public PEReader(Stream stream) : base(stream) {}

        public override string Arch {
            get {
                switch (coff.Machine) {
                    case 0x14C:
                        return "x86";
                    case 0x1C0: // ARMv7
                    case 0x1C4: // ARMv7 Thumb (T1)
                        return "ARM";
                    default:
                        return "Unsupported";
                }
            }
        }

        protected override bool Init() {
            // Check for MZ signature "MZ"
            if (ReadUInt16() != 0x5A4D)
                return false;

            // Get offset to PE header from DOS header
            Position = 0x3C;
            Position = ReadUInt32();

            // Check PE signature "PE\0\0"
            if (ReadUInt32() != 0x00004550)
                return false;

            // Read COFF Header
            coff = ReadObject<COFFHeader>();

            // Ensure presence of PE Optional header
            // Size will always be 0x60 + (0x10 ' 0x8) for 16 RVA entries @ 8 bytes each
            if (coff.SizeOfOptionalHeader != 0xE0)
                return false;

            // Read PE optional header
            pe = ReadObject<PEOptHeader>();

            // Ensure IMAGE_NT_OPTIONAL_HDR32_MAGIC (32-bit)
            if (pe.signature != 0x10B)
                return false;

            // Get IAT
            var IATStart = pe.DataDirectory[12].VirtualAddress;
            var IATSize = pe.DataDirectory[12].Size;

            // Get sections table
            sections = ReadArray<PESection>(coff.NumberOfSections);

            // Confirm that .rdata section begins at same place as IAT
            var rData = sections.First(x => x.Name == ".rdata");
            if (rData.BaseMemory != IATStart)
                return false;

            // Calculate start of function pointer table
            pFuncTable = rData.BaseImage + IATSize + 8;
            GlobalOffset = pe.ImageBase;
            return true;
        }

        public override uint[] GetSearchLocations() {
            Position = pFuncTable;
            var addrs = new List<uint>();
            uint addr;
            while ((addr = ReadUInt32()) != 0)
                addrs.Add(MapVATR(addr) & 0xfffffffc);
            return addrs.ToArray();
        }

        public override uint MapVATR(uint uiAddr) {
            if (uiAddr == 0)
                return 0;

            var section = sections.First(x => uiAddr - GlobalOffset >= x.BaseMemory &&
                                              uiAddr - GlobalOffset < x.BaseMemory + x.SizeMemory);
            return uiAddr - section.BaseMemory - GlobalOffset + section.BaseImage;
        }
    }
}
