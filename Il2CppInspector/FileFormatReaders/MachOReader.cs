/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    internal class MachOReader : FileFormatReader<MachOReader>
    {
        private MachOHeader header;
        private uint pFuncTable;
        private uint sFuncTable;
        private bool is64;
        private List<MachOSection> sections = new List<MachOSection>();
        private List<MachOSection64> sections64 = new List<MachOSection64>();

        public MachOReader(Stream stream) : base(stream) { }

        public override string Arch {
            get {
                switch ((MachO)header.CPUType) {
                    case MachO.CPU_TYPE_ARM:
                    case MachO.CPU_TYPE_ARM64:
                        return "ARM";
                    case MachO.CPU_TYPE_X86:
                    case MachO.CPU_TYPE_X86_64:
                        return "x86";
                    default:
                        return "Unsupported";
                }
            }
        }

        protected override bool Init() {
            // Detect endianness - default is little-endianness
            MachO magic = (MachO)ReadUInt32();
            if (magic == MachO.MH_CIGAM || magic == MachO.MH_CIGAM_64) {
                Endianness = Endianness.Big;
            }
            else if (magic != MachO.MH_MAGIC && magic != MachO.MH_MAGIC_64) {
                return false;
            }

            Console.WriteLine("Endianness: {0}", Endianness);

            Position -= sizeof(uint);
            header = ReadObject<MachOHeader>();

            // 64-bit files have an extra 4 bytes after the header
            is64 = false;
            if (magic == MachO.MH_MAGIC_64) {
                is64 = true;
                ReadUInt32();
            }
            Console.WriteLine("Architecture: {0}-bit", is64 ? 64 : 32);

            // Must be executable file
            if ((MachO) header.FileType != MachO.MH_EXECUTE)
                return false;

            Console.WriteLine("CPU Type: " + (MachO)header.CPUType);

            MachOLinkEditDataCommand functionStarts = null;

            for (var c = 0; c < header.NumCommands; c++) {
                var startPos = Position;
                var loadCommand = ReadObject<MachOLoadCommand>();

                if ((MachO)loadCommand.Command == MachO.LC_SEGMENT) {
                    var segment = ReadObject<MachOSegmentCommand>();
                    if (segment.Name == "__TEXT" || segment.Name == "__DATA") {
                        for (int s = 0; s < segment.NumSections; s++) {
                            var section = ReadObject<MachOSection>();
                            sections.Add(section);
                            if (section.Name == "__text")
                                GlobalOffset = section.Address - section.ImageOffset;
                        }
                    }
                }
                else if ((MachO)loadCommand.Command == MachO.LC_SEGMENT_64) {
                    var segment = ReadObject<MachOSegmentCommand64>();
                    if (segment.Name == "__TEXT" || segment.Name == "__DATA") {
                        for (int s = 0; s < segment.NumSections; s++) {
                            var section64 = ReadObject<MachOSection64>();
                            sections64.Add(section64);
                            if (section64.Name == "__text")
                                GlobalOffset = (uint)section64.Address - section64.ImageOffset;
                        }
                    }
                }

                if ((MachO)loadCommand.Command == MachO.LC_FUNCTION_STARTS) {
                    functionStarts = ReadObject<MachOLinkEditDataCommand>();
                }

                // There might be other data after the load command so always use the specified total size to step forwards
                Position = startPos + loadCommand.Size;
            }

            // Must find LC_FUNCTION_STARTS load command
            if (functionStarts == null)
                return false;

            pFuncTable = functionStarts.Offset;
            sFuncTable = functionStarts.Size;
            return true;
        }

        public override uint[] GetFunctionTable() {
            Position = pFuncTable;
            var functionPointers = new List<uint>();

            // Decompress ELB128 list of function offsets
            // https://en.wikipedia.org/wiki/LEB128
            uint previous = 0;
            while (Position < pFuncTable + sFuncTable) {
                uint result = 0;
                int shift = 0;
                byte b;
                do {
                    b = ReadByte();
                    result |= (uint)((b & 0x7f) << shift);
                    shift += 7;
                } while ((b & 0x80) != 0);
                if (result > 0) {
                    if (previous == 0)
                        result &= 0xffffffc;
                    previous += result;
                    functionPointers.Add(previous);
                }
            }
            return functionPointers.ToArray();
        }

        public override uint MapVATR(uint uiAddr) {
            if (!is64) {
                var section = sections.First(x => uiAddr >= x.Address && uiAddr <= (x.Address + x.Size));
                return uiAddr - (section.Address - section.ImageOffset);
            }
            var section64 = sections64.First(x => uiAddr >= x.Address && uiAddr <= (x.Address + x.Size));
            return uiAddr - ((uint)section64.Address - section64.ImageOffset);
        }
    }
}
