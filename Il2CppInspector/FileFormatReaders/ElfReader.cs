/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Il2CppInspector
{
    internal class ElfReader : FileFormatReader<ElfReader>
    {
        // Internal relocation entry helper
        private struct ElfReloc
        {
            public Elf Type;
            public uint Offset;
            public uint? Addend;
            public uint SymbolTable;
            public uint SymbolIndex;

            // Equality based on target address
            public override bool Equals(object obj) => obj is ElfReloc reloc && Equals(reloc);

            public bool Equals(ElfReloc other) {
                return Offset == other.Offset;
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = (int)Type;
                    hashCode = (hashCode * 397) ^ (int)Offset;
                    hashCode = (hashCode * 397) ^ Addend.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int)SymbolTable;
                    hashCode = (hashCode * 397) ^ (int)SymbolIndex;
                    return hashCode;
                }
            }

            // Cast operators (makes the below code MUCH easier to read)
            public ElfReloc(elf_32_rel rel, uint symbolTable) {
                Type = (Elf) (rel.r_info & 0xff);
                Offset = rel.r_offset;
                Addend = null;
                SymbolIndex = rel.r_info >> 8; // r_info >> 8 is an index into the symbol table
                SymbolTable = symbolTable;
            }

            public ElfReloc(elf_32_rela rela, uint symbolTable)
                : this(new elf_32_rel { r_info = rela.r_info, r_offset = rela.r_offset }, symbolTable) =>
                Addend = rela.r_addend;
        }

        private elf_32_phdr[] program_header_table;
        private elf_32_shdr[] section_header_table;
        private elf_32_dynamic[] dynamic_table;
        private elf_header elf_header;

        public ElfReader(Stream stream) : base(stream) { }

        public override string Format => "ELF";

        public override string Arch => (Elf) elf_header.e_machine switch {
            Elf.EM_386 => "x86",
            Elf.EM_ARM => "ARM",
            Elf.EM_X86_64 => "x64",
            Elf.EM_AARCH64 => "ARM64",
            _ => "Unsupported"
        };

        public override int Bits => (elf_header.m_arch == (uint) Elf.ELFCLASS64) ? 64 : 32;

        private elf_32_shdr getSection(Elf sectionIndex) => section_header_table.FirstOrDefault(x => x.sh_type == (uint) sectionIndex);
        private IEnumerable<elf_32_shdr> getSections(Elf sectionIndex) => section_header_table.Where(x => x.sh_type == (uint)sectionIndex);
        private elf_32_phdr getProgramHeader(Elf programIndex) => program_header_table.FirstOrDefault(x => x.p_type == (uint) programIndex);
        private elf_32_dynamic getDynamic(Elf dynamicIndex) => dynamic_table?.FirstOrDefault(x => x.d_tag == (uint) dynamicIndex);

        protected override bool Init() {
            elf_header = ReadObject<elf_header>();

            // Check for magic bytes
            if (elf_header.m_dwFormat != (uint) Elf.ELFMAG) {
                return false;
            }

            // 64-bit not supported
            if (elf_header.m_arch == (uint) Elf.ELFCLASS64) {
                return false;
            }

            program_header_table = ReadArray<elf_32_phdr>(elf_header.e_phoff, elf_header.e_phnum);
            section_header_table = ReadArray<elf_32_shdr>(elf_header.e_shoff, elf_header.e_shnum);

            if (getProgramHeader(Elf.PT_DYNAMIC) is elf_32_phdr PT_DYNAMIC)
                dynamic_table = ReadArray<elf_32_dynamic>(PT_DYNAMIC.p_offset, (int) PT_DYNAMIC.p_filesz / 8 /* sizeof(elf_32_dynamic) */);

            // Get global offset table
            var _GLOBAL_OFFSET_TABLE_ = getDynamic(Elf.DT_PLTGOT)?.d_un;
            if (_GLOBAL_OFFSET_TABLE_ == null)
                throw new InvalidOperationException("Unable to get GLOBAL_OFFSET_TABLE from PT_DYNAMIC");
            GlobalOffset = (uint) _GLOBAL_OFFSET_TABLE_;

            // Find all relocations; target address => (rela header (rels are converted to rela), symbol table base address, is rela?)
            var rels = new HashSet<ElfReloc>();

            // Two types: add value from offset in image, and add value from specified addend
            foreach (var relSection in getSections(Elf.SHT_REL))
                rels.UnionWith(
                    from rel in ReadArray<elf_32_rel>(relSection.sh_offset, (int) (relSection.sh_size / relSection.sh_entsize))
                    select new ElfReloc(rel, section_header_table[relSection.sh_link].sh_offset));
                
            foreach (var relaSection in getSections(Elf.SHT_RELA))
                rels.UnionWith(
                    from rela in ReadArray<elf_32_rela>(relaSection.sh_offset, (int)(relaSection.sh_size / relaSection.sh_entsize))
                    select new ElfReloc(rela, section_header_table[relaSection.sh_link].sh_offset));

            // Relocations in dynamic section
            if (getDynamic(Elf.DT_REL) is elf_32_dynamic dt_rel) {
                var dt_rel_count = getDynamic(Elf.DT_RELSZ).d_un / getDynamic(Elf.DT_RELENT).d_un;
                var dt_rel_list = ReadArray<elf_32_rel>(MapVATR(dt_rel.d_un), (int) dt_rel_count);
                var dt_symtab = getDynamic(Elf.DT_SYMTAB).d_un;
                rels.UnionWith(from rel in dt_rel_list select new ElfReloc(rel, dt_symtab));
            }

            if (getDynamic(Elf.DT_RELA) is elf_32_dynamic dt_rela) {
                var dt_rela_count = getDynamic(Elf.DT_RELASZ).d_un / getDynamic(Elf.DT_RELAENT).d_un;
                var dt_rela_list = ReadArray<elf_32_rela>(MapVATR(dt_rela.d_un), (int) dt_rela_count);
                var dt_symtab = getDynamic(Elf.DT_SYMTAB).d_un;
                rels.UnionWith(from rela in dt_rela_list select new ElfReloc(rela, dt_symtab));
            }

            // Process relocations
            // WARNING: This modifies the stream passed in the constructor
            if (BaseStream is FileStream)
                throw new InvalidOperationException("Input stream to ElfReader is a file. Please supply a mutable stream source.");

            var writer = new BinaryWriter(BaseStream);

            foreach (var rel in rels) {
                var symValue = ReadObject<elf_32_sym>(rel.SymbolTable + rel.SymbolIndex * 16 /* sizeof(elf_32_sym) */).st_value; // S

                // The addend is specified in the struct for rela, and comes from the target location for rel
                Position = MapVATR(rel.Offset);
                var addend = rel.Addend ?? ReadUInt32(); // A

                // Only handle relocation types we understand, skip the rest
                // Relocation types from https://docs.oracle.com/cd/E23824_01/html/819-0690/chapter6-54839.html#scrolltoc
                // and https://studfiles.net/preview/429210/page:18/
                (uint newValue, bool recognized) result = (rel.Type, (Elf) elf_header.e_machine) switch {
                    (Elf.R_ARM_ABS32, Elf.EM_ARM) => (symValue + addend, true), // S + A
                    (Elf.R_ARM_REL32, Elf.EM_ARM) => (symValue - rel.Offset + addend, true), // S - P + A
                    (Elf.R_ARM_COPY, Elf.EM_ARM) => (symValue, true), // S

                    (Elf.R_386_32, Elf.EM_386) => (symValue + addend, true), // S + A
                    (Elf.R_386_PC32, Elf.EM_386) => (symValue + addend - rel.Offset, true), // S + A - P
                    (Elf.R_386_GLOB_DAT, Elf.EM_386) => (symValue, true), // S
                    (Elf.R_386_JMP_SLOT, Elf.EM_386) => (symValue, true), // S

                    (Elf.R_AMD64_64, Elf.EM_AARCH64) => (symValue + addend, true), // S + A

                    _ => (0, false)
                };

                if (result.recognized) {
                    Position = MapVATR(rel.Offset);
                    writer.Write(result.newValue);
                }
            }
            Console.WriteLine($"Processed {rels.Count} relocations");

            return true;
        }

        public override Dictionary<string, uint> GetSymbolTable() {
            // Three possible symbol tables in ELF files
            var pTables = new List<(uint offset, uint count, uint strings)>();

            // String table (a sequence of null-terminated strings, total length in sh_size
            var SHT_STRTAB = getSection(Elf.SHT_STRTAB);

            if (SHT_STRTAB != null) {
                // Section header shared object symbol table (.symtab)
                if (getSection(Elf.SHT_SYMTAB) is elf_32_shdr SHT_SYMTAB)
                    pTables.Add((SHT_SYMTAB.sh_offset, SHT_SYMTAB.sh_size / SHT_SYMTAB.sh_entsize, SHT_STRTAB.sh_offset));
                
                // Section header executable symbol table (.dynsym)
                if (getSection(Elf.SHT_DYNSYM) is elf_32_shdr SHT_DYNSYM)
                    pTables.Add((SHT_DYNSYM.sh_offset, SHT_DYNSYM.sh_size / SHT_DYNSYM.sh_entsize, SHT_STRTAB.sh_offset));
            }

            // Symbol table in dynamic section (DT_SYMTAB)
            // Normally the same as .dynsym except that .dynsym may be removed in stripped binaries

            // Dynamic string table
            if (getDynamic(Elf.DT_STRTAB) is elf_32_dynamic DT_STRTAB) {
                if (getDynamic(Elf.DT_SYMTAB) is elf_32_dynamic DT_SYMTAB) {
                    // Find the next pointer in the dynamic table to calculate the length of the symbol table
                    var end = (from x in dynamic_table where x.d_un > DT_SYMTAB.d_un orderby x.d_un select x).First().d_un;

                    // Dynamic symbol table
                    pTables.Add((DT_SYMTAB.d_un, (end - DT_SYMTAB.d_un) / 16 /* sizeof(elf_32_sym) */, DT_STRTAB.d_un));
                }
            }

            // Now iterate through all of the symbol and string tables we found to build a full list
            var symbolTable = new Dictionary<string, uint>();

            foreach (var pTab in pTables) {
                var symbol_table = ReadArray<elf_32_sym>(pTab.offset, (int) pTab.count);

                foreach (var symbol in symbol_table) {
                    var name = ReadNullTerminatedString(pTab.strings + symbol.st_name);

                    // Avoid duplicates
                    symbolTable.TryAdd(name, symbol.st_value);
                }
            }

            return symbolTable;
        }

        public override uint[] GetFunctionTable() {
            // INIT_ARRAY contains a list of pointers to initialization functions (not all functions in the binary)
            // INIT_ARRAYSZ contains the size of INIT_ARRAY

            var init = MapVATR(getDynamic(Elf.DT_INIT_ARRAY).d_un);
            var size = getDynamic(Elf.DT_INIT_ARRAYSZ).d_un;

            return ReadArray<uint>(init, (int) size / 4);
        }

        // Map a virtual address to an offset into the image file. Throws an exception if the virtual address is not mapped into the file.
        // Note if uiAddr is a valid segment but filesz < memsz and the adjusted uiAddr falls between the range of filesz and memsz,
        // an exception will be thrown. This area of memory is assumed to contain all zeroes.
        public override uint MapVATR(uint uiAddr)
        {
            var program_header_table = this.program_header_table.First(x => uiAddr >= x.p_vaddr && uiAddr <= (x.p_vaddr + x.p_filesz));
            return uiAddr - (program_header_table.p_vaddr - program_header_table.p_offset);
        }
    }
}