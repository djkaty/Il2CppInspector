/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using NoisyCowStudios.Bin2Object;

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
    // https://www.psxhax.com/threads/make-fself-gui-for-flat_zs-make_fself-py-script-by-cfwprophet.3494/
    internal class SElfReader : FileFormatReader<SElfReader>
    {
        public SElfReader(Stream stream) : base(stream) { }

        public override string Format => sceData.ProductType == (ulong) SElfExInfoTypes.PTYPE_FAKE? "FSELF" : "SELF";

        public override string Arch => "x64";

        public override int Bits => 64;

        private SElfHeader selfHeader;
        private SElfEntry[] entries;
        private SElfSCEData sceData;
        private elf_header<ulong> elfHeader;
        private elf_64_phdr[] pht;
        private elf_64_phdr getProgramHeader(Elf programIndex) => pht.FirstOrDefault(x => x.p_type == (uint) programIndex);

        protected override bool Init() {
            selfHeader = ReadObject<SElfHeader>();

            // Check for magic bytes
            if ((SElfConsts) selfHeader.Magic != SElfConsts.Magic)
                return false;

            if (selfHeader.Endian != 0x1)
                Endianness = Endianness.Big;

            // Read entries
            entries = ReadArray<SElfEntry>(selfHeader.NumberOfEntries);

            // We can't deal with encrypted or compressed segments right now
            if (entries.Any(e => e.HasBlocks && e.IsEncrypted))
                throw new NotImplementedException("This file contains encrypted segments not currently supported by Il2CppInspector.");

            if (entries.Any(e => e.HasBlocks && e.IsDeflated))
                throw new NotImplementedException("This file contains compressed segments not currently supported by Il2CppInspector.");

            // Read ELF header
            // PS4 files as follows:
            // m_arch = 0x2 (64-bit)
            // m_endian = 0x1 (little endian)
            // m_version = 0x1 (ELF version 1)
            // m_osabi = 0x9 (FreeBSD)
            // e_type = special type, see psdevwiki documentation; probably 0xFE10 or 0xFE18
            // e_machine = 0x3E (x86-64)
            var startOfElf = Position;
            elfHeader = ReadObject<elf_header<ulong>>();

            // Must be one of these supported binary types
            if (elfHeader.e_type != (ushort) Elf.ET_EXEC
                && elfHeader.e_type != (ushort) SElfETypes.ET_SCE_EXEC
                && elfHeader.e_type != (ushort) SElfETypes.ET_SCE_DYNEXEC
                && elfHeader.e_type != (ushort) SElfETypes.ET_SCE_DYNAMIC)
                return false;

            // There are no sections, but read all the program headers
            // Each segment of type PT_LOAD, PT_SCE_RELRO, PT_SCE_DYNLIBDATA and PT_SCE_COMMENT
            // generates two SELF entries above - one pointing to the ELF segment and one pointing to a digest.
            // Only p_vaddr is used for memory mapping; all other fields are ignored.
            // offset, memsz and filesz are taken from the SELF entries.
            // The digests are all-zero in FSELF files.
            // All other ELF segments are ignored completely.
            pht = ReadArray<elf_64_phdr>(startOfElf + (long) elfHeader.e_phoff, elfHeader.e_phnum);

            // Read extended info
            sceData = ReadObject<SElfSCEData>(startOfElf + (long) elfHeader.e_phoff + elfHeader.e_phentsize * elfHeader.e_phnum);

            // Get SELF entries which point to segments defined in phdrs
            var dataEntries = entries.Where(e => e.HasBlocks).ToList();

            // Fixup the used phdr entries
            foreach (var entry in dataEntries) {
                pht[entry.SegmentIndex].f_p_filesz = entry.EncryptedCompressedSize;
                pht[entry.SegmentIndex].f_p_offset = entry.FileOffset;
                pht[entry.SegmentIndex].p_memsz = entry.MemorySize;
            }

            // Filter out unused phdr entries
            var phdrIndices = dataEntries.Select(e => (int) e.SegmentIndex).ToList();
            pht = pht.Where((e, i) => phdrIndices.Contains(i)).ToArray();

            // Get offset of code section
            var codeSegment = pht.First(x => ((Elf) x.p_flags & Elf.PF_X) == Elf.PF_X);
            GlobalOffset = codeSegment.p_vaddr - codeSegment.p_offset;

            return true;
        }

        // Only the DT_INIT function equivalent
        public override uint[] GetFunctionTable() => new [] { MapVATR(elfHeader.e_entry) };

        public override uint MapVATR(ulong uiAddr) {
            var program_header_table = pht.First(x => uiAddr >= x.p_vaddr && uiAddr <= x.p_vaddr + x.p_filesz);
            return (uint) (uiAddr - (program_header_table.p_vaddr - program_header_table.p_offset));
        }

        public override ulong MapFileOffsetToVA(uint offset) {
            var segment = pht.First(x => offset >= x.p_offset && offset < x.p_offset + x.p_filesz);
            return segment.p_vaddr + offset - segment.p_offset;
        }
    }
}