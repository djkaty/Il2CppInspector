/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppInspector
{
    // Sony PlayStation 4 fake signed ELF reader
    // Not compatible with PlayStation 3, PSP or Vita
    // References:
    // http://hitmen.c02.at/files/yapspd/psp_doc/chap26.html
    // https://www.psdevwiki.com/ps3/SELF_-_SPRX#File_Format
    // https://www.psdevwiki.com/ps4/SELF_File_Format
    // https://www.psxhax.com/threads/ps4-self-spkg-file-format-documentation-detailed-for-scene-devs.6636/
    // https://wiki.henkaku.xyz/vita/images/a/a2/Vita_SDK_specifications.pdf
    internal class FSELFReader : FileFormatReader<FSELFReader>
    {
        public FSELFReader(Stream stream) : base(stream) { }

        public override string Format => "FSELF";

        public override string Arch => "x64";

        public override int Bits => 64;

        protected override bool Init() {
            var fselfHeader = ReadObject<FSELFHeader>();

            // Check for magic bytes
            if ((FSELFConsts) fselfHeader.Magic != FSELFConsts.Magic)
                return false;

            if ((FSELFConsts) fselfHeader.Unk4 != FSELFConsts.Unk4)
                return false;

            // Read segments
            var segments = ReadArray<FSELFSegment>(fselfHeader.NumberOfSegments);

            // Read ELF header
            // PS4 files as follows:
            // m_arch = 0x2 (64-bit)
            // m_endian = 0x1 (little endian)
            // m_version = 0x1 (ELF version 1)
            // m_osabi = 0x9 (FreeBSD)
            // e_type = special type, see psdevwiki documentation; probably 0xFE10 or 0xFE18
            // e_machine = 0x3E (x86-64)
            var startOfElf = Position;
            var elfHeader = ReadObject<elf_header<ulong>>();

            // There are no sections, but read all the program headers
            var program_header_table = ReadArray<elf_64_phdr>(startOfElf + (long) elfHeader.e_phoff, elfHeader.e_phnum);

            // Read the special section
            var sceSpecial = ReadObject<FSELFSCE>();

            // TODO: Implement the rest of FSELF
            // TODO: Set GlobalOffset

            throw new NotImplementedException("Il2CppInspector does not have PRX support yet");

            return true;
        }

        public override Dictionary<string, ulong> GetSymbolTable() {
            throw new NotImplementedException();
        }

        public override uint[] GetFunctionTable() {
            throw new NotImplementedException();
        }

        public override uint MapVATR(ulong uiAddr) {
            throw new NotImplementedException();
        }
    }
}