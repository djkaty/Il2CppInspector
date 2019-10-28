/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector
{
    internal class ElfReader32 : ElfReader<uint, elf_32_phdr, elf_32_sym, ElfReader32, Convert32>
    {
        public ElfReader32(Stream stream) : base(stream) {
            ElfReloc.GetRelocType = info => (Elf) (info & 0xff);
            ElfReloc.GetSymbolIndex = info => info >> 8;
        }

        public override int Bits => 32;
        protected override Elf ArchClass => Elf.ELFCLASS32;

        protected override void Write(BinaryWriter writer, uint value) => writer.Write(value);
    }

    internal class ElfReader64 : ElfReader<ulong, elf_64_phdr, elf_64_sym, ElfReader64, Convert64>
    {
        public ElfReader64(Stream stream) : base(stream) {
            ElfReloc.GetRelocType = info => (Elf) (info & 0xffff_ffff);
            ElfReloc.GetSymbolIndex = info => info >> 32;
        }

        public override int Bits => 64;
        protected override Elf ArchClass => Elf.ELFCLASS64;

        protected override void Write(BinaryWriter writer, ulong value) => writer.Write(value);
    }

    interface IElfReader
    {
        uint GetPLTAddress();
    }

    internal abstract class ElfReader<TWord, TPHdr, TSym, TReader, TConvert> : FileFormatReader<TReader>, IElfReader
        where TWord : struct
        where TPHdr : Ielf_phdr<TWord>, new()
        where TSym : Ielf_sym<TWord>, new()
        where TConvert : IWordConverter<TWord>, new()
        where TReader : FileFormatReader<TReader>
    {
        private readonly TConvert conv = new TConvert();

        // Internal relocation entry helper
        protected class ElfReloc
        {
            public Elf Type;
            public TWord Offset;
            public TWord? Addend;
            public TWord SymbolTable;
            public TWord SymbolIndex;

            // Equality based on target address
            public override bool Equals(object obj) => obj is ElfReloc reloc && Equals(reloc);

            public bool Equals(ElfReloc other) {
                return Offset.Equals(other.Offset);
            }

            public override int GetHashCode() => Offset.GetHashCode();

            // Cast operators (makes the below code MUCH easier to read)
            public ElfReloc(elf_rel<TWord> rel, TWord symbolTable) {
                Offset = rel.r_offset;
                Addend = null;
                Type = GetRelocType(rel.r_info);
                SymbolIndex = GetSymbolIndex(rel.r_info);
                SymbolTable = symbolTable;
            }

            public ElfReloc(elf_rela<TWord> rela, TWord symbolTable)
                : this(new elf_rel<TWord> { r_info = rela.r_info, r_offset = rela.r_offset }, symbolTable) =>
                Addend = rela.r_addend;

            public static Func<TWord, Elf> GetRelocType;
            public static Func<TWord, TWord> GetSymbolIndex;
        }

        // See also: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/sizeof
        private int Sizeof(Type type) {
            int size = 0;
            foreach (var i in type.GetTypeInfo().GetFields()) {
                if (i.FieldType == typeof(byte) || i.FieldType == typeof(sbyte))
                    size += sizeof(byte);
                if (i.FieldType == typeof(long) || i.FieldType == typeof(ulong))
                    size += sizeof(ulong);
                if (i.FieldType == typeof(int) || i.FieldType == typeof(uint))
                    size += sizeof(uint);
                if (i.FieldType == typeof(short) || i.FieldType == typeof(ushort))
                    size += sizeof(ushort);
            }
            return size;
        }

        private TPHdr[] program_header_table;
        private elf_shdr<TWord>[] section_header_table;
        private elf_dynamic<TWord>[] dynamic_table;
        private elf_header<TWord> elf_header;

        public ElfReader(Stream stream) : base(stream) { }

        public override string Format => Bits == 32 ? "ELF" : "ELF64";

        public override string Arch => (Elf) elf_header.e_machine switch {
            Elf.EM_386 => "x86",
            Elf.EM_ARM => "ARM",
            Elf.EM_X86_64 => "x64",
            Elf.EM_AARCH64 => "ARM64",
            _ => "Unsupported"
        };

        public override int Bits => (elf_header.m_arch == (uint) Elf.ELFCLASS64) ? 64 : 32;

        private elf_shdr<TWord> getSection(Elf sectionIndex) => section_header_table.FirstOrDefault(x => x.sh_type == (uint) sectionIndex);
        private IEnumerable<elf_shdr<TWord>> getSections(Elf sectionIndex) => section_header_table.Where(x => x.sh_type == (uint) sectionIndex);
        private TPHdr getProgramHeader(Elf programIndex) => program_header_table.FirstOrDefault(x => x.p_type == (uint) programIndex);
        private elf_dynamic<TWord> getDynamic(Elf dynamicIndex) => dynamic_table?.FirstOrDefault(x => (Elf) conv.ULong(x.d_tag) == dynamicIndex);

        protected abstract Elf ArchClass { get; }

        protected abstract void Write(BinaryWriter writer, TWord value);

        protected override bool Init() {
            elf_header = ReadObject<elf_header<TWord>>();

            // Check for magic bytes
            if ((Elf) elf_header.m_dwFormat != Elf.ELFMAG)
                return false;

            // 64-bit not supported
            if ((Elf) elf_header.m_arch != ArchClass)
                return false;

            program_header_table = ReadArray<TPHdr>(conv.Long(elf_header.e_phoff), elf_header.e_phnum);
            section_header_table = ReadArray<elf_shdr<TWord>>(conv.Long(elf_header.e_shoff), elf_header.e_shnum);

            if (getProgramHeader(Elf.PT_DYNAMIC) is TPHdr PT_DYNAMIC)
                dynamic_table = ReadArray<elf_dynamic<TWord>>(conv.Long(PT_DYNAMIC.p_offset), (int) (conv.Long(PT_DYNAMIC.p_filesz) / Sizeof(typeof(elf_dynamic<TWord>))));

            // Get offset of code section
            var codeSegment = program_header_table.First(x => ((Elf) x.p_flags & Elf.PF_X) == Elf.PF_X);
            GlobalOffset = conv.ULong(conv.Sub(codeSegment.p_vaddr, codeSegment.p_offset));

            // Find all relocations; target address => (rela header (rels are converted to rela), symbol table base address, is rela?)
            var rels = new HashSet<ElfReloc>();

            // Two types: add value from offset in image, and add value from specified addend
            foreach (var relSection in getSections(Elf.SHT_REL))
                rels.UnionWith(
                    from rel in ReadArray<elf_rel<TWord>>(conv.Long(relSection.sh_offset), conv.Int(conv.Div(relSection.sh_size, relSection.sh_entsize)))
                    select new ElfReloc(rel, section_header_table[relSection.sh_link].sh_offset));
                
            foreach (var relaSection in getSections(Elf.SHT_RELA))
                rels.UnionWith(
                    from rela in ReadArray<elf_rela<TWord>>(conv.Long(relaSection.sh_offset), conv.Int(conv.Div(relaSection.sh_size, relaSection.sh_entsize)))
                    select new ElfReloc(rela, section_header_table[relaSection.sh_link].sh_offset));

            // Relocations in dynamic section
            if (getDynamic(Elf.DT_REL) is elf_dynamic<TWord> dt_rel) {
                var dt_rel_count = conv.Div(getDynamic(Elf.DT_RELSZ).d_un, getDynamic(Elf.DT_RELENT).d_un);
                var dt_rel_list = ReadArray<elf_rel<TWord>>(MapVATR(conv.ULong(dt_rel.d_un)), conv.Int(dt_rel_count));
                var dt_symtab = getDynamic(Elf.DT_SYMTAB).d_un;
                rels.UnionWith(from rel in dt_rel_list select new ElfReloc(rel, dt_symtab));
            }

            if (getDynamic(Elf.DT_RELA) is elf_dynamic<TWord> dt_rela) {
                var dt_rela_count = conv.Div(getDynamic(Elf.DT_RELASZ).d_un, getDynamic(Elf.DT_RELAENT).d_un);
                var dt_rela_list = ReadArray<elf_rela<TWord>>(MapVATR(conv.ULong(dt_rela.d_un)), conv.Int(dt_rela_count));
                var dt_symtab = getDynamic(Elf.DT_SYMTAB).d_un;
                rels.UnionWith(from rela in dt_rela_list select new ElfReloc(rela, dt_symtab));
            }

            // Process relocations
            // WARNING: This modifies the stream passed in the constructor
            if (BaseStream is FileStream)
                throw new InvalidOperationException("Input stream to ElfReader is a file. Please supply a mutable stream source.");

            var writer = new BinaryWriter(BaseStream);
            var relsz = Sizeof(typeof(TSym));

            foreach (var rel in rels) {
                var symValue = ReadObject<TSym>(conv.Long(rel.SymbolTable) + conv.Long(rel.SymbolIndex) * relsz).st_value; // S

                // The addend is specified in the struct for rela, and comes from the target location for rel
                Position = MapVATR(conv.ULong(rel.Offset));
                var addend = rel.Addend ?? ReadObject<TWord>(); // A

                // Only handle relocation types we understand, skip the rest
                // Relocation types from https://docs.oracle.com/cd/E23824_01/html/819-0690/chapter6-54839.html#scrolltoc
                // and https://studfiles.net/preview/429210/page:18/
                // and http://infocenter.arm.com/help/topic/com.arm.doc.ihi0056b/IHI0056B_aaelf64.pdf (AArch64)
                (TWord newValue, bool recognized) result = (rel.Type, (Elf) elf_header.e_machine) switch {
                    (Elf.R_ARM_ABS32, Elf.EM_ARM) => (conv.Add(symValue, addend), true), // S + A
                    (Elf.R_ARM_REL32, Elf.EM_ARM) => (conv.Add(conv.Sub(symValue, rel.Offset), addend), true), // S - P + A
                    (Elf.R_ARM_COPY, Elf.EM_ARM) => (symValue, true), // S

                    (Elf.R_AARCH64_ABS64, Elf.EM_AARCH64) => (conv.Add(symValue, addend), true), // S + A
                    (Elf.R_AARCH64_PREL64, Elf.EM_AARCH64) => (conv.Sub(conv.Add(symValue, addend), rel.Offset), true), // S + A - P
                    (Elf.R_AARCH64_GLOB_DAT, Elf.EM_AARCH64) => (conv.Add(symValue, addend), true), // S + A
                    (Elf.R_AARCH64_JUMP_SLOT, Elf.EM_AARCH64) => (conv.Add(symValue, addend), true), // S + A
                    (Elf.R_AARCH64_RELATIVE, Elf.EM_AARCH64) => (conv.Add(symValue, addend), true), // Delta(S) + A

                    (Elf.R_386_32, Elf.EM_386) => (conv.Add(symValue, addend), true), // S + A
                    (Elf.R_386_PC32, Elf.EM_386) => (conv.Sub(conv.Add(symValue, addend), rel.Offset), true), // S + A - P
                    (Elf.R_386_GLOB_DAT, Elf.EM_386) => (symValue, true), // S
                    (Elf.R_386_JMP_SLOT, Elf.EM_386) => (symValue, true), // S

                    (Elf.R_AMD64_64, Elf.EM_AARCH64) => (conv.Add(symValue, addend), true), // S + A

                    _ => (default(TWord), false)
                };

                if (result.recognized) {
                    Position = MapVATR(conv.ULong(rel.Offset));
                    Write(writer, result.newValue);
                }
            }
            Console.WriteLine($"Processed {rels.Count} relocations");

            return true;
        }

        public override Dictionary<string, ulong> GetSymbolTable() {
            // Three possible symbol tables in ELF files
            var pTables = new List<(TWord offset, TWord count, TWord strings)>();

            // String table (a sequence of null-terminated strings, total length in sh_size
            var SHT_STRTAB = getSection(Elf.SHT_STRTAB);

            if (SHT_STRTAB != null) {
                // Section header shared object symbol table (.symtab)
                if (getSection(Elf.SHT_SYMTAB) is elf_shdr<TWord> SHT_SYMTAB)
                    pTables.Add((SHT_SYMTAB.sh_offset, conv.Div(SHT_SYMTAB.sh_size, SHT_SYMTAB.sh_entsize), SHT_STRTAB.sh_offset));
                
                // Section header executable symbol table (.dynsym)
                if (getSection(Elf.SHT_DYNSYM) is elf_shdr<TWord> SHT_DYNSYM)
                    pTables.Add((SHT_DYNSYM.sh_offset, conv.Div(SHT_DYNSYM.sh_size, SHT_DYNSYM.sh_entsize), SHT_STRTAB.sh_offset));
            }

            // Symbol table in dynamic section (DT_SYMTAB)
            // Normally the same as .dynsym except that .dynsym may be removed in stripped binaries

            // Dynamic string table
            if (getDynamic(Elf.DT_STRTAB) is elf_dynamic<TWord> DT_STRTAB) {
                if (getDynamic(Elf.DT_SYMTAB) is elf_dynamic<TWord> DT_SYMTAB) {
                    // Find the next pointer in the dynamic table to calculate the length of the symbol table
                    var end = (from x in dynamic_table where conv.Gt(x.d_un, DT_SYMTAB.d_un) orderby x.d_un select x).First().d_un;

                    // Dynamic symbol table
                    pTables.Add((DT_SYMTAB.d_un, conv.Div(conv.Sub(end, DT_SYMTAB.d_un), Sizeof(typeof(TSym))), DT_STRTAB.d_un));
                }
            }

            // Now iterate through all of the symbol and string tables we found to build a full list
            var symbolTable = new Dictionary<string, ulong>();

            foreach (var pTab in pTables) {
                var symbol_table = ReadArray<TSym>(conv.Long(pTab.offset), conv.Int(pTab.count));

                foreach (var symbol in symbol_table) {
                    var name = ReadNullTerminatedString(conv.Long(pTab.strings) + symbol.st_name);

                    // Avoid duplicates
                    symbolTable.TryAdd(name, conv.ULong(symbol.st_value));
                }
            }

            return symbolTable;
        }

        public override uint[] GetFunctionTable() {
            // INIT_ARRAY contains a list of pointers to initialization functions (not all functions in the binary)
            // INIT_ARRAYSZ contains the size of INIT_ARRAY

            var init = MapVATR(conv.ULong(getDynamic(Elf.DT_INIT_ARRAY).d_un));
            var size = getDynamic(Elf.DT_INIT_ARRAYSZ).d_un;

            return conv.UIntArray(ReadArray<TWord>(init, conv.Int(size) / (Bits / 8)));
        }

        // Map a virtual address to an offset into the image file. Throws an exception if the virtual address is not mapped into the file.
        // Note if uiAddr is a valid segment but filesz < memsz and the adjusted uiAddr falls between the range of filesz and memsz,
        // an exception will be thrown. This area of memory is assumed to contain all zeroes.
        public override uint MapVATR(ulong uiAddr) {
            // Additions in the argument to MapVATR may cause an overflow which should be discarded for 32-bit files
            if (Bits == 32)
                uiAddr &= 0xffff_ffff;
             var program_header_table = this.program_header_table.First(x => uiAddr >= conv.ULong(x.p_vaddr) && uiAddr <= conv.ULong(conv.Add(x.p_vaddr, x.p_filesz)));
            return (uint) (uiAddr - conv.ULong(conv.Sub(program_header_table.p_vaddr, program_header_table.p_offset)));
        }
        
        // Get the address of the procedure linkage table (.got.plt) which is needed for some disassemblies
        public uint GetPLTAddress() => (uint) conv.ULong(getDynamic(Elf.DT_PLTGOT).d_un);
    }
}