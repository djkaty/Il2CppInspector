/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Il2CppInspector
{
    internal class ElfReader : FileFormatReader<ElfReader>
    {
        private program_header_table[] program_header_table;
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
        private program_header_table getProgramHeader(Elf programIndex) => program_header_table.FirstOrDefault(x => x.p_type == (uint) programIndex);
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

            program_header_table = ReadArray<program_header_table>(elf_header.e_phoff, elf_header.e_phnum);
            section_header_table = ReadArray<elf_32_shdr>(elf_header.e_shoff, elf_header.e_shnum);

            if (getProgramHeader(Elf.PT_DYNAMIC) is program_header_table PT_DYNAMIC)
                dynamic_table = ReadArray<elf_32_dynamic>(PT_DYNAMIC.p_offset, (int) PT_DYNAMIC.p_filesz / 8 /* sizeof(elf_32_dynamic) */);

            // Get global offset table
            var _GLOBAL_OFFSET_TABLE_ = getDynamic(Elf.DT_PLTGOT)?.d_un;
            if (_GLOBAL_OFFSET_TABLE_ == null)
                throw new InvalidOperationException("Unable to get GLOBAL_OFFSET_TABLE from PT_DYNAMIC");
            GlobalOffset = (uint) _GLOBAL_OFFSET_TABLE_;

            // TODO: Find all relocations
            
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