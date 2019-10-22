/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    internal class MachOReader32 : MachOReader<uint, MachOReader32>
    {
        public MachOReader32(Stream stream) : base(stream) { }

        public override int Bits => 32;

        protected override bool checkMagicLE(MachO magic) => magic == MachO.MH_MAGIC;
        protected override bool checkMagicBE(MachO magic) => magic == MachO.MH_CIGAM;

        protected override MachO lc_Segment => MachO.LC_SEGMENT;
    }

    internal class MachOReader64 : MachOReader<ulong, MachOReader64>
    {
        public MachOReader64(Stream stream) : base(stream) { }

        public override int Bits => 64;

        protected override bool checkMagicLE(MachO magic) => magic == MachO.MH_MAGIC_64;
        protected override bool checkMagicBE(MachO magic) => magic == MachO.MH_CIGAM_64;

        protected override MachO lc_Segment => MachO.LC_SEGMENT_64;
    }

    // We need this convoluted generic TReader declaration so that "static T FileFormatReader.Load(Stream)"
    // is inherited to MachOReader32/64 with a correct definition of T
    internal abstract class MachOReader<TWord, TReader> : FileFormatReader<TReader> where TWord : struct where TReader : FileFormatReader<TReader>
    {
        private MachOHeader<TWord> header;
        private readonly List<MachOSection<TWord>> sections = new List<MachOSection<TWord>>();
        private uint pFuncTable;
        private uint sFuncTable;

        protected MachOReader(Stream stream) : base(stream) { }

        public override string Format => "Mach-O";

        public override string Arch => (MachO)header.CPUType switch
        {
            MachO.CPU_TYPE_ARM => "ARM",
            MachO.CPU_TYPE_ARM64 => "ARM64",
            MachO.CPU_TYPE_X86 => "x86",
            MachO.CPU_TYPE_X86_64 => "x64",
            _ => "Unsupported"
        };

        protected abstract bool checkMagicLE(MachO magic);
        protected abstract bool checkMagicBE(MachO magic);
        protected abstract MachO lc_Segment { get; }

        protected override bool Init() {
            // Detect endianness - default is little-endianness
            MachO magic = (MachO)ReadUInt32();

            if (checkMagicBE(magic))
                Endianness = Endianness.Big;

            if (!checkMagicBE(magic) && !checkMagicLE(magic))
                return false;

            header = ReadObject<MachOHeader<TWord>>(0);

            // Must be executable file
            if ((MachO)header.FileType != MachO.MH_EXECUTE)
                return false;

            MachOLinkEditDataCommand functionStarts = null;

            // Process load commands
            for (var c = 0; c < header.NumCommands; c++) {
                var startPos = Position;
                var loadCommand = ReadObject<MachOLoadCommand>();

                if ((MachO)loadCommand.Command == lc_Segment) {
                    var segment = ReadObject<MachOSegmentCommand<TWord>>();
                    if (segment.Name == "__TEXT" || segment.Name == "__DATA") {
                        for (int s = 0; s < segment.NumSections; s++) {
                            var section = ReadObject<MachOSection<TWord>>();
                            sections.Add(section);
                            if (section.Name == "__text") {
                                GlobalOffset = (ulong)Convert.ChangeType(section.Address, typeof(ulong)) - section.ImageOffset;
                            }
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

        public override uint MapVATR(ulong uiAddr) {
            var section = sections.First(x => uiAddr >= (uint)(object)x.Address && uiAddr <= (uint)(object)x.Address + (uint)(object)x.Size);
            return (uint) (uiAddr - ((ulong) Convert.ChangeType(section.Address, typeof(ulong)) - section.ImageOffset));
        }
    }
}
