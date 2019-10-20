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
using System.Net.Sockets;

namespace Il2CppInspector
{
    internal class ElfReader : FileFormatReader<ElfReader>
    {
        private program_header_table[] program_table_element;
        private elf_32_shdr[] section_header_table;
        private elf_header elf_header;

        public ElfReader(Stream stream) : base(stream) { }

        public override string Arch {
            get {
                switch (elf_header.e_machine) {
                    case 0x03:
                        return "x86";
                    case 0x28:
                        return "ARM";
                    default:
                        return "Unsupported";
                }
            }
        }

        protected override bool Init() {
            elf_header = ReadObject<elf_header>();

            if (elf_header.m_dwFormat != 0x464c457f) {
                // Not an ELF file
                return false;
            }
            if (elf_header.m_arch == 2)//64
            {
                // 64-bit not supported
                return false;
            }
            program_table_element = ReadArray<program_header_table>(elf_header.e_phoff, elf_header.e_phnum);
            section_header_table = ReadArray<elf_32_shdr>(elf_header.e_shoff, elf_header.e_shnum);
            return true;
        }

        public override Dictionary<string, uint> GetSymbolTable() {
            // Three possible symbol tables in ELF files
            var pTables = new List<(uint offset, uint count, uint strings)>();

            // String table (a sequence of null-terminated strings, total length in sh_size
            var SHT_STRTAB = section_header_table.FirstOrDefault(x => x.sh_type == 3u); // SHT_STRTAB = 3

            if (SHT_STRTAB != null) {
                // Section header shared object symbol table (.symtab)
                if (section_header_table.FirstOrDefault(x => x.sh_type == 2u) is elf_32_shdr SHT_SYMTAB) // SHT_SYMTAB = 2
                    pTables.Add((SHT_SYMTAB.sh_offset, SHT_SYMTAB.sh_size / SHT_SYMTAB.sh_entsize, SHT_STRTAB.sh_offset));
                
                // Section header executable symbol table (.dynsym)
                if (section_header_table.FirstOrDefault(x => x.sh_type == 11u) is elf_32_shdr SHT_DYNSYM) // SHT_DYNSUM = 11
                    pTables.Add((SHT_DYNSYM.sh_offset, SHT_DYNSYM.sh_size / SHT_DYNSYM.sh_entsize, SHT_STRTAB.sh_offset));
            }

            // Symbol table in dynamic section (DT_SYMTAB)
            // Normally the same as .dynsym except that .dynsym may be removed in stripped binaries
            if (program_table_element.FirstOrDefault(x => x.p_type == 2u) is program_header_table PT_DYNAMIC) // PT_DYNAMIC = 2
            {
                // Get the dynamic table
                var dynamic_table = ReadArray<elf_32_dynamic>(PT_DYNAMIC.p_offset, (int) PT_DYNAMIC.p_filesz / 8 /* sizeof(elf_32_dynamic) */);

                // Dynamic string table
                var DT_STRTAB = dynamic_table.FirstOrDefault(x => x.d_tag == 5u); // DT_STRTAB = 5

                if (DT_STRTAB != null) {
                    if (dynamic_table.FirstOrDefault(x => x.d_tag == 6u) is elf_32_dynamic DT_SYMTAB) { // DT_SYMTAB = 6
                        // Find the next pointer in the dynamic table to calculate the length of the symbol table
                        var end = (from x in dynamic_table where x.d_un > DT_SYMTAB.d_un orderby x.d_un select x).First().d_un;

                        // Dynamic symbol table
                        pTables.Add((DT_SYMTAB.d_un, (end - DT_SYMTAB.d_un) / 16 /* sizeof(elf_32_sym) */, DT_STRTAB.d_un));
                    }
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
            // Find dynamic section
            var dynamic = new elf_32_shdr();
            var PT_DYNAMIC = program_table_element.First(x => x.p_type == 2u);
            dynamic.sh_offset = PT_DYNAMIC.p_offset;
            dynamic.sh_size = PT_DYNAMIC.p_filesz;

            // We need GOT, INIT_ARRAY and INIT_ARRAYSZ
            uint _GLOBAL_OFFSET_TABLE_ = 0;
            var init_array = new elf_32_shdr();
            Position = dynamic.sh_offset;
            var dynamicend = dynamic.sh_offset + dynamic.sh_size;
            while (Position < dynamicend) {
                var tag = ReadInt32();
                if (tag == 3) //DT_PLTGOT
                {
                    _GLOBAL_OFFSET_TABLE_ = ReadUInt32();
                    continue;
                }
                else if (tag == 25) //DT_INIT_ARRAY
                {
                    init_array.sh_offset = MapVATR(ReadUInt32());
                    continue;
                }
                else if (tag == 27) //DT_INIT_ARRAYSZ
                {
                    init_array.sh_size = ReadUInt32();
                    continue;
                }
                Position += 4;
            }
            if (_GLOBAL_OFFSET_TABLE_ == 0)
                throw new InvalidOperationException("Unable to get GLOBAL_OFFSET_TABLE from PT_DYNAMIC");
            GlobalOffset = _GLOBAL_OFFSET_TABLE_;
            return ReadArray<uint>(init_array.sh_offset, (int) init_array.sh_size / 4);
        }

        // Map a virtual address to an offset into the image file. Throws an exception if the virtual address is not mapped into the file.
        // Note if uiAddr is a valid segment but filesz < memsz and the adjusted uiAddr falls between the range of filesz and memsz,
        // an exception will be thrown. This area of memory is assumed to contain all zeroes.
        public override uint MapVATR(uint uiAddr)
        {
            var program_header_table = program_table_element.First(x => uiAddr >= x.p_vaddr && uiAddr <= (x.p_vaddr + x.p_filesz));
            return uiAddr - (program_header_table.p_vaddr - program_header_table.p_offset);
        }
    }
}
