/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public interface IElfReader : IFileFormatStream
    {
        // Get PLT address
        uint GetPLTAddress();
    }

    public class ElfReader32 : ElfReader<uint, elf_32_phdr, elf_32_sym, ElfReader32, Convert32>
    {
        public ElfReader32() : base() {
            ElfReloc.GetRelocType = info => (Elf) (info & 0xff);
            ElfReloc.GetSymbolIndex = info => info >> 8;
        }

        public override int Bits => 32;
        protected override Elf ArchClass => Elf.ELFCLASS32;

        protected override void WriteWord(uint value) => Write(value);
    }

    public class ElfReader64 : ElfReader<ulong, elf_64_phdr, elf_64_sym, ElfReader64, Convert64>
    {
        public ElfReader64() : base() {
            ElfReloc.GetRelocType = info => (Elf) (info & 0xffff_ffff);
            ElfReloc.GetSymbolIndex = info => info >> 32;
        }

        public override int Bits => 64;
        protected override Elf ArchClass => Elf.ELFCLASS64;

        protected override void WriteWord(ulong value) => Write(value);
    }

    public abstract class ElfReader<TWord, TPHdr, TSym, TReader, TConvert> : FileFormatStream<TReader>, IElfReader
        where TWord : struct
        where TPHdr : Ielf_phdr<TWord>, new()
        where TSym : Ielf_sym<TWord>, new()
        where TConvert : IWordConverter<TWord>, new()
        where TReader : FileFormatStream<TReader>
    {
        public override string DefaultFilename => "libil2cpp.so";

        public override string Format => Bits == 32 ? "ELF" : "ELF64";

        public override string Arch => (Elf) ElfHeader.e_machine switch {
            Elf.EM_386 => "x86",
            Elf.EM_ARM => "ARM",
            Elf.EM_X86_64 => "x64",
            Elf.EM_AARCH64 => "ARM64",
            _ => "Unsupported"
        };

        public override int Bits => (ElfHeader.m_arch == (uint) Elf.ELFCLASS64) ? 64 : 32;

        public TPHdr[] PHT { get; private set; }
        public elf_shdr<TWord>[] SHT { get; private set; }
        public elf_dynamic<TWord>[] DynamicTable { get; private set; }
        public elf_header<TWord> ElfHeader { get; private set; }
        public Dictionary<string, elf_shdr<TWord>> SectionByName = new Dictionary<string, elf_shdr<TWord>>();

        public elf_shdr<TWord> GetSection(Elf sectionIndex) => SHT.FirstOrDefault(x => x.sh_type == (uint) sectionIndex);
        public IEnumerable<elf_shdr<TWord>> GetSections(Elf sectionIndex) => SHT.Where(x => x.sh_type == (uint) sectionIndex);
        public TPHdr GetProgramHeader(Elf programIndex) => PHT.FirstOrDefault(x => x.p_type == (uint) programIndex);
        public elf_dynamic<TWord> GetDynamicEntry(Elf dynamicIndex) => DynamicTable?.FirstOrDefault(x => (Elf) conv.ULong(x.d_tag) == dynamicIndex);

        private Dictionary<string, Symbol> symbolTable = new Dictionary<string, Symbol>();
        private List<Export> exports = new List<Export>();

        private List<(uint Start, uint End)> reverseMapExclusions = new List<(uint Start, uint End)>();
        private bool preferPHT = false;
        private bool isMemoryImage = false;

        protected abstract Elf ArchClass { get; }

        protected abstract void WriteWord(TWord value);

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

        protected override bool Init() {
            ElfHeader = ReadObject<elf_header<TWord>>();

            // Check for magic bytes
            if ((Elf) ElfHeader.m_dwFormat != Elf.ELFMAG)
                return false;

            // Ensure supported architecture
            if ((Elf) ElfHeader.m_arch != ArchClass)
                return false;

            // Get PHT and SHT
            PHT = ReadArray<TPHdr>(conv.Long(ElfHeader.e_phoff), ElfHeader.e_phnum);
            SHT = ReadArray<elf_shdr<TWord>>(conv.Long(ElfHeader.e_shoff), ElfHeader.e_shnum);

            // Determine if SHT is valid

            // These can happen as a result of conversions from other formats to ELF,
            // or if the SHT has been deliberately stripped
            if (!SHT.Any()) {
                Console.WriteLine("ELF binary has no SHT - reverting to PHT");
                preferPHT = true;
            }
            
            else if (SHT.All(s => conv.ULong(s.sh_addr) == 0ul)) {
                Console.WriteLine("ELF binary SHT is all-zero - reverting to PHT");
                preferPHT = true;
            }

            // Check for overlaps in sections that are memory-allocated on load
            else {
                var shtShouldBeOrdered = SHT
                    .Where(s => ((Elf) conv.Int(s.sh_flags) & Elf.SHF_ALLOC) == Elf.SHF_ALLOC)
                    .OrderBy(s => s.sh_addr)
                    .Select(s => new[] { conv.ULong(s.sh_addr), conv.ULong(s.sh_addr) + conv.ULong(s.sh_size) })
                    .SelectMany(s => s);

                // No sections that map into memory - this is probably a dumped image
                if (!shtShouldBeOrdered.Any()) {

                    // If the first file offset of the first PHT is zero, assume a dumped image
                    if (PHT.Any(t => conv.ULong(t.p_vaddr) == 0ul)) {
                        Console.WriteLine("ELF binary appears to be a dumped memory image");
                        isMemoryImage = true;
                    }
                    preferPHT = true;
                }

                // Sections overlap - this can happen if the ELF has been improperly generated or processed by another tool
                else {
                    var shtOverlap = shtShouldBeOrdered.Aggregate((x, y) => x <= y? y : ulong.MaxValue) == ulong.MaxValue;
                    if (shtOverlap) {
                        Console.WriteLine("ELF binary SHT contains invalid ranges - reverting to PHT");
                        preferPHT = true;
                    }
                }
            }

            // Dumped images must be rebased
            if (isMemoryImage) {
                if (LoadOptions == null || LoadOptions.ImageBase == 0)
                    throw new InvalidOperationException("To load a dumped ELF image, you must specify the image base virtual address");
            }

            // Rebase if requested (whether dumped or not) and treat it as a memory image,
            // disabling processing of relocations, symbols and decryption
            if (LoadOptions != null && LoadOptions.ImageBase != 0) {
                isMemoryImage = true;
                rebase(conv.FromULong(LoadOptions.ImageBase));
            }
            
            // Get dynamic table if it exists (must be done after rebasing)
            if (GetProgramHeader(Elf.PT_DYNAMIC) is TPHdr PT_DYNAMIC)
                DynamicTable = ReadArray<elf_dynamic<TWord>>(conv.Long(PT_DYNAMIC.p_offset), (int) (conv.Long(PT_DYNAMIC.p_filesz) / Sizeof(typeof(elf_dynamic<TWord>))));

            // Get offset of code section
            var codeSegment = PHT.First(x => ((Elf) x.p_flags & Elf.PF_X) == Elf.PF_X);
            GlobalOffset = conv.ULong(conv.Sub(codeSegment.p_vaddr, codeSegment.p_offset));

            // Nothing more to do if the image is a memory dump (no section names, relocations or decryption)
            if (isMemoryImage)
                return true;

            // Get section name mappings if there are any
            // This is currently only used to defeat the XOR obfuscation handled below
            // Note: There can be more than one section with the same name, or unnamed; we take the first section with a given name
            if (ElfHeader.e_shtrndx < SHT.Length) {
                var pStrtab = SHT[ElfHeader.e_shtrndx].sh_offset;
                foreach (var section in SHT) {
                    try {
                        var name = ReadNullTerminatedString(conv.Long(pStrtab) + section.sh_name);
                        SectionByName.TryAdd(name, section);
                    } catch (ArgumentOutOfRangeException) {
                        // Names have been stripped, maybe previously dumped image
                        break;
                    }
                }
            }

            // Find all relocations; target address => (rela header (rels are converted to rela), symbol table base address, is rela?)
            var rels = new HashSet<ElfReloc>();

            StatusUpdate("Finding relocations");

            // Two types: add value from offset in image, and add value from specified addend
            foreach (var relSection in GetSections(Elf.SHT_REL)) {
                reverseMapExclusions.Add(((uint) conv.Int(relSection.sh_offset), (uint) (conv.Int(relSection.sh_offset) + conv.Int(relSection.sh_size) - 1)));
                rels.UnionWith(
                    from rel in ReadArray<elf_rel<TWord>>(conv.Long(relSection.sh_offset), conv.Int(conv.Div(relSection.sh_size, relSection.sh_entsize)))
                    select new ElfReloc(rel, SHT[relSection.sh_link].sh_offset));
            }

            foreach (var relaSection in GetSections(Elf.SHT_RELA)) {
                reverseMapExclusions.Add(((uint) conv.Int(relaSection.sh_offset), (uint) (conv.Int(relaSection.sh_offset) + conv.Int(relaSection.sh_size) - 1)));
                rels.UnionWith(
                    from rela in ReadArray<elf_rela<TWord>>(conv.Long(relaSection.sh_offset), conv.Int(conv.Div(relaSection.sh_size, relaSection.sh_entsize)))
                    select new ElfReloc(rela, SHT[relaSection.sh_link].sh_offset));
            }

            // Relocations in dynamic section
            if (GetDynamicEntry(Elf.DT_REL) is elf_dynamic<TWord> dt_rel) {
                var dt_rel_count = conv.Int(conv.Div(GetDynamicEntry(Elf.DT_RELSZ).d_un, GetDynamicEntry(Elf.DT_RELENT).d_un));
                var dt_item_size = Sizeof(typeof(elf_rel<TWord>));
                var dt_start = MapVATR(conv.ULong(dt_rel.d_un));
                var dt_rel_list = ReadArray<elf_rel<TWord>>(dt_start, dt_rel_count);
                var dt_symtab = GetDynamicEntry(Elf.DT_SYMTAB).d_un;
                reverseMapExclusions.Add((dt_start, (uint) (dt_start + dt_rel_count * dt_item_size - 1)));
                rels.UnionWith(from rel in dt_rel_list select new ElfReloc(rel, dt_symtab));
            }

            if (GetDynamicEntry(Elf.DT_RELA) is elf_dynamic<TWord> dt_rela) {
                var dt_rela_count = conv.Int(conv.Div(GetDynamicEntry(Elf.DT_RELASZ).d_un, GetDynamicEntry(Elf.DT_RELAENT).d_un));
                var dt_item_size = Sizeof(typeof(elf_rela<TWord>));
                var dt_start = MapVATR(conv.ULong(dt_rela.d_un));
                var dt_rela_list = ReadArray<elf_rela<TWord>>(dt_start, dt_rela_count);
                var dt_symtab = GetDynamicEntry(Elf.DT_SYMTAB).d_un;
                reverseMapExclusions.Add((dt_start, (uint) (dt_start + dt_rela_count * dt_item_size - 1)));
                rels.UnionWith(from rela in dt_rela_list select new ElfReloc(rela, dt_symtab));
            }

            // Process relocations
            var relsz = Sizeof(typeof(TSym));

            var currentRel = 0;
            var totalRel = rels.Count();

            foreach (var rel in rels) {
                currentRel++;
                if (currentRel % 1000 == 0)
                    StatusUpdate($"Processing relocations ({currentRel * 100 / totalRel:F0}%)");

                var symValue = ReadObject<TSym>(conv.Long(rel.SymbolTable) + conv.Long(rel.SymbolIndex) * relsz).st_value; // S

                // Ignore relocations into memory addresses not mapped from the image
                try {
                    Position = MapVATR(conv.ULong(rel.Offset));
                }
                catch (InvalidOperationException) {
                    continue;
                }

                // The addend is specified in the struct for rela, and comes from the target location for rel
                var addend = rel.Addend ?? ReadObject<TWord>(); // A

                // Only handle relocation types we understand, skip the rest
                // Relocation types from https://docs.oracle.com/cd/E23824_01/html/819-0690/chapter6-54839.html#scrolltoc
                // and https://studfiles.net/preview/429210/page:18/
                // and http://infocenter.arm.com/help/topic/com.arm.doc.ihi0056b/IHI0056B_aaelf64.pdf (AArch64)
                (TWord newValue, bool recognized) result = (rel.Type, (Elf) ElfHeader.e_machine) switch {
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
                    WriteWord(result.newValue);
                }
            }
            Console.WriteLine($"Processed {rels.Count} relocations");

            // Build symbol and export tables
            processSymbols();

            return true;
        }

        // Rebase the image to a new virtual address
        private void rebase(TWord imageBase) {
            // Rebase PHT
            foreach (var segment in PHT) {
                segment.p_offset = segment.p_vaddr;
                segment.p_vaddr = conv.Add(segment.p_vaddr, imageBase);
                segment.p_filesz = segment.p_memsz;
            }

            // Rewrite to stream
            WriteArray(conv.Long(ElfHeader.e_phoff), PHT);
            IsModified = true;

            // Rebase dynamic table if it exists
            // Note we have to rebase the PHT first to get the correct location to read this
            if (!(GetProgramHeader(Elf.PT_DYNAMIC) is TPHdr PT_DYNAMIC))
                return;

            var dt = ReadArray<elf_dynamic<TWord>>(conv.Long(PT_DYNAMIC.p_offset), (int) (conv.Long(PT_DYNAMIC.p_filesz) / Sizeof(typeof(elf_dynamic<TWord>))));

            // Every table containing virtual address pointers
            // https://docs.oracle.com/cd/E19683-01/817-3677/chapter6-42444/index.html
            var tablesToRebase = new [] {
                Elf.DT_PLTGOT, Elf.DT_HASH, Elf.DT_STRTAB, Elf.DT_SYMTAB, Elf.DT_RELA,
                Elf.DT_INIT, Elf.DT_FINI, Elf.DT_REL, Elf.DT_JMPREL, Elf.DT_INIT_ARRAY, Elf.DT_FINI_ARRAY,
                Elf.DT_PREINIT_ARRAY, Elf.DT_MOVETAB, Elf.DT_VERDEF, Elf.DT_VERNEED, Elf.DT_SYMINFO
            };

            // Rebase dynamic tables
            foreach (var section in dt.Where(x => tablesToRebase.Contains((Elf) conv.ULong(x.d_tag))))
                section.d_un = conv.Add(section.d_un, imageBase);

            // Rewrite to stream
            WriteArray(conv.Long(PT_DYNAMIC.p_offset), dt);
        }

        private void processSymbols() {
            StatusUpdate("Processing symbols");

            // Three possible symbol tables in ELF files
            var pTables = new List<(TWord offset, TWord count, TWord strings)>();

            // String table (a sequence of null-terminated strings, total length in sh_size
            var SHT_STRTAB = GetSection(Elf.SHT_STRTAB);

            if (SHT_STRTAB != null) {
                // Section header shared object symbol table (.symtab)
                if (GetSection(Elf.SHT_SYMTAB) is elf_shdr<TWord> SHT_SYMTAB)
                    pTables.Add((SHT_SYMTAB.sh_offset, conv.Div(SHT_SYMTAB.sh_size, SHT_SYMTAB.sh_entsize), SHT_STRTAB.sh_offset));
                
                // Section header executable symbol table (.dynsym)
                if (GetSection(Elf.SHT_DYNSYM) is elf_shdr<TWord> SHT_DYNSYM)
                    pTables.Add((SHT_DYNSYM.sh_offset, conv.Div(SHT_DYNSYM.sh_size, SHT_DYNSYM.sh_entsize), SHT_STRTAB.sh_offset));
            }

            // Symbol table in dynamic section (DT_SYMTAB)
            // Normally the same as .dynsym except that .dynsym may be removed in stripped binaries

            // Dynamic string table
            if (GetDynamicEntry(Elf.DT_STRTAB) is elf_dynamic<TWord> DT_STRTAB) {
                if (GetDynamicEntry(Elf.DT_SYMTAB) is elf_dynamic<TWord> DT_SYMTAB) {
                    // Find the next pointer in the dynamic table to calculate the length of the symbol table
                    var end = (from x in DynamicTable where conv.Gt(x.d_un, DT_SYMTAB.d_un) orderby x.d_un select x).First().d_un;

                    // Dynamic symbol table
                    pTables.Add((
                        conv.FromUInt(MapVATR(conv.ULong(DT_SYMTAB.d_un))),
                        conv.Div(conv.Sub(end, DT_SYMTAB.d_un), Sizeof(typeof(TSym))),
                        DT_STRTAB.d_un
                    ));
                }
            }

            // Now iterate through all of the symbol and string tables we found to build a full list
            symbolTable.Clear();
            var exportTable = new Dictionary<string, Export>();

            foreach (var pTab in pTables) {
                var symbol_table = ReadArray<TSym>(conv.Long(pTab.offset), conv.Int(pTab.count));

                foreach (var symbol in symbol_table) {
                    string name = string.Empty;
                    try {
                        name = ReadNullTerminatedString(conv.Long(pTab.strings) + symbol.st_name);
                    } catch (ArgumentOutOfRangeException) {
                        // Name has been stripped, maybe previously dumped image
                        continue;
                    }

                    var type = symbol.type == Elf.STT_FUNC? SymbolType.Function
                               : symbol.type == Elf.STT_OBJECT || symbol.type == Elf.STT_COMMON? SymbolType.Name
                               : SymbolType.Unknown;

                    if (symbol.st_shndx == (ushort) Elf.SHN_UNDEF)
                        type = SymbolType.Import;

                    // Avoid duplicates
                    var symbolItem = new Symbol {Name = name, Type = type, VirtualAddress = conv.ULong(symbol.st_value) };
                    symbolTable.TryAdd(name, symbolItem);
                    if (symbol.st_shndx != (ushort) Elf.SHN_UNDEF)
                        exportTable.TryAdd(name, new Export {Name = symbolItem.DemangledName, VirtualAddress = conv.ULong(symbol.st_value)});
                }
            }

            exports = exportTable.Values.ToList();
        }

        public override Dictionary<string, Symbol> GetSymbolTable() => symbolTable;
        public override IEnumerable<Export> GetExports() => exports;

        public override uint[] GetFunctionTable() {
            // INIT_ARRAY contains a list of pointers to initialization functions (not all functions in the binary)
            // INIT_ARRAYSZ contains the size of INIT_ARRAY
            // INIT_ARRAY is probably broken in dumped images and resaved dumped images
            if (GetDynamicEntry(Elf.DT_INIT_ARRAY) == null || GetDynamicEntry(Elf.DT_INIT_ARRAYSZ) == null || isMemoryImage)
                return Array.Empty<uint>();

            var init = MapVATR(conv.ULong(GetDynamicEntry(Elf.DT_INIT_ARRAY).d_un));
            var size = GetDynamicEntry(Elf.DT_INIT_ARRAYSZ).d_un;

            var init_array = conv.UIntArray(ReadArray<TWord>(init, conv.Int(size) / (Bits / 8)));

            // Additionally, check if there is an old-style DT_INIT function and include it in the list if so
            if (GetDynamicEntry(Elf.DT_INIT) != null)
                init_array = init_array.Concat(conv.UIntArray(new[] { GetDynamicEntry(Elf.DT_INIT).d_un })).ToArray();

            return init_array.Select(x => MapVATR(x)).ToArray();
        }

        public override IEnumerable<Section> GetSections() {
            // If the sections have been stripped, use the segment list from the PHT instead
            if (preferPHT)
                return PHT.Select(p => new Section {
                    VirtualStart = conv.ULong(p.p_vaddr),
                    VirtualEnd   = conv.ULong(p.p_vaddr) + conv.ULong(p.p_memsz) - 1,
                    ImageStart   = (uint) conv.Int(p.p_offset),
                    ImageEnd     = (uint) conv.Int(p.p_offset) + (uint) conv.Int(p.p_filesz) - 1,

                    // Not correct but probably the best we can do
                    IsData       = ((Elf) p.p_flags & Elf.PF_R) != 0,
                    IsExec       = ((Elf) p.p_flags & Elf.PF_X) != 0,
                    IsBSS        = conv.Int(p.p_filesz) == 0,

                    Name         = string.Empty
                });

            // Return sections list
            return SHT.Select(s => new Section {
                VirtualStart = conv.ULong(s.sh_addr),
                VirtualEnd   = conv.ULong(s.sh_addr) + conv.ULong(s.sh_size) - 1,
                ImageStart   = (uint) conv.Int(s.sh_offset),
                ImageEnd     = (uint) conv.Int(s.sh_offset) + (uint) conv.Int(s.sh_size) - 1,

                IsData = ((Elf) conv.Int(s.sh_flags) & Elf.SHF_ALLOC) == Elf.SHF_ALLOC && ((Elf) conv.Int(s.sh_flags) & Elf.SHF_EXECINSTR) == 0 && (Elf) s.sh_type == Elf.SHT_PROGBITS,
                IsExec = ((Elf) conv.Int(s.sh_flags) & Elf.SHF_EXECINSTR) == Elf.SHF_EXECINSTR && (Elf) s.sh_type == Elf.SHT_PROGBITS,
                IsBSS  = (Elf) s.sh_type == Elf.SHT_NOBITS,

                Name = SectionByName.FirstOrDefault(sbn => conv.Int(sbn.Value.sh_offset) == conv.Int(s.sh_offset)).Key ?? string.Empty
            });
        }

        // Map a virtual address to an offset into the image file. Throws an exception if the virtual address is not mapped into the file.
        // Note if uiAddr is a valid segment but filesz < memsz and the adjusted uiAddr falls between the range of filesz and memsz,
        // an exception will be thrown. This area of memory is assumed to contain all zeroes.
        public override uint MapVATR(ulong uiAddr) {
            // Additions in the argument to MapVATR may cause an overflow which should be discarded for 32-bit files
            if (Bits == 32)
                uiAddr &= 0xffff_ffff;
             var program_header_table = this.PHT.First(x => uiAddr >= conv.ULong(x.p_vaddr) && uiAddr <= conv.ULong(conv.Add(x.p_vaddr, x.p_filesz)));
            return (uint) (uiAddr - conv.ULong(conv.Sub(program_header_table.p_vaddr, program_header_table.p_offset)));
        }

        public override ulong MapFileOffsetToVA(uint offset) {
            // Exclude relocation areas
            if (reverseMapExclusions.Any(r => offset >= r.Start && offset <= r.End))
                throw new InvalidOperationException("Attempt to map to a relocation address");

            var section = PHT.First(x => offset >= conv.Int(x.p_offset) && offset < conv.Int(x.p_offset) + conv.Int(x.p_filesz));
            return conv.ULong(section.p_vaddr) + offset - conv.ULong(section.p_offset);
        }
        
        // Get the address of the procedure linkage table (.got.plt) which is needed for some disassemblies
        public uint GetPLTAddress() => (uint) conv.ULong(GetDynamicEntry(Elf.DT_PLTGOT).d_un);
    }
}