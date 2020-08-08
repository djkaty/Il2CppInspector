/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

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
    internal class MachOReader32 : MachOReader<uint, MachOReader32, Convert32>
    {
        public MachOReader32(Stream stream) : base(stream) { }

        public override int Bits => 32;

        protected override bool checkMagicLE(MachO magic) => magic == MachO.MH_MAGIC;
        protected override bool checkMagicBE(MachO magic) => magic == MachO.MH_CIGAM;

        protected override MachO lc_Segment => MachO.LC_SEGMENT;

        public override uint MapVATR(ulong uiAddr) {
            var section = sections.First(x => uiAddr >= x.Address && uiAddr <= x.Address + x.Size);
            return (uint) uiAddr - (section.Address - section.ImageOffset);
        }

        public override ulong MapFileOffsetToVA(uint offset) {
            var section = sections.First(x => offset >= x.ImageOffset && offset < x.ImageOffset + x.Size);
            return section.Address + offset - section.ImageOffset;
        }
    }

    internal class MachOReader64 : MachOReader<ulong, MachOReader64, Convert64>
    {
        public MachOReader64(Stream stream) : base(stream) { }

        public override int Bits => 64;

        protected override bool checkMagicLE(MachO magic) => magic == MachO.MH_MAGIC_64;
        protected override bool checkMagicBE(MachO magic) => magic == MachO.MH_CIGAM_64;

        protected override MachO lc_Segment => MachO.LC_SEGMENT_64;

        public override uint MapVATR(ulong uiAddr) {
            var section = sections.First(x => uiAddr >= x.Address && uiAddr <= x.Address + x.Size);
            return (uint) (uiAddr - (section.Address - section.ImageOffset));
        }

        public override ulong MapFileOffsetToVA(uint offset) {
            var section = sections.First(x => offset >= x.ImageOffset && offset < x.ImageOffset + x.Size);
            return section.Address + offset - section.ImageOffset;
        }
    }

    // We need this convoluted generic TReader declaration so that "static T FileFormatReader.Load(Stream)"
    // is inherited to MachOReader32/64 with a correct definition of T
    internal abstract class MachOReader<TWord, TReader, TConvert> : FileFormatReader<TReader>
        where TWord : struct
        where TReader : FileFormatReader<TReader>
        where TConvert : IWordConverter<TWord>, new()
    {
        private readonly TConvert conv = new TConvert();

        private MachOHeader<TWord> header;
        protected readonly List<MachOSection<TWord>> sections = new List<MachOSection<TWord>>();
        private MachOSection<TWord> funcTab;
        private MachOSymtabCommand symTab;

        private List<Export> exports = new List<Export>();

        protected MachOReader(Stream stream) : base(stream) { }

        public override string Format => "Mach-O " + (Bits == 32 ? "32-bit" : "64-bit");

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

            // Process load commands
            for (var c = 0; c < header.NumCommands; c++) {
                var startPos = Position;
                var loadCommand = ReadObject<MachOLoadCommand>();

                switch ((MachO) loadCommand.Command) {

                    // Segments
                    case MachO cmd when cmd == lc_Segment:
                        var segment = ReadObject<MachOSegmentCommand<TWord>>();

                        // Code and data
                        if (segment.Name == "__TEXT" || segment.Name == "__DATA") {
                            for (int s = 0; s < segment.NumSections; s++) {
                                var section = ReadObject<MachOSection<TWord>>();
                                sections.Add(section);
                                if (section.Name == "__text") {
                                    GlobalOffset = (ulong) Convert.ChangeType(section.Address, typeof(ulong)) - section.ImageOffset;
                                }

                                // Initialization (pre-main) functions
                                if (section.Name == "__mod_init_func") {
                                    funcTab = section;
                                }
                            }
                        }
                        break;

                    // Location of static symbol table
                    case MachO.LC_SYMTAB:
                        symTab = ReadObject<MachOSymtabCommand>();
                        break;

                    case MachO.LC_DYSYMTAB:
                        // TODO: Implement Mach-O dynamic symbol table
                        break;

                    // Compressed dyld information
                    case MachO.LC_DYLD_INFO:
                    case MachO.LC_DYLD_INFO_ONLY:
                        var dyld = ReadObject<MachODyldInfoCommand>();

                        loadExportTrie(dyld.ExportOffset);
                        break;

                    // Encryption check
                    // If cryptid == 1, this binary is encrypted with FairPlay DRM
                    case MachO.LC_ENCRYPTION_INFO:
                    case MachO.LC_ENCRYPTION_INFO_64:
                        var encryptionInfo = ReadObject<MachOEncryptionInfo>();
                        if (encryptionInfo.CryptID != 0)
                            throw new NotImplementedException("This Mach-O executable is encrypted with FairPlay DRM and cannot be processed. Please provide a decrypted version of the executable.");
                        break;
                }

                // There might be other data after the load command so always use the specified total size to step forwards
                Position = startPos + loadCommand.Size;
            }

            // Must find __mod_init_func
            if (funcTab == null)
                return false;

            // Process relocations
            foreach (var section in sections) {
                var rels = ReadArray<MachO_relocation_info>(section.ImageRelocOffset, section.NumRelocEntries);

                // TODO: Implement Mach-O relocations
                if (rels.Any()) {
                    Console.WriteLine("Mach-O file contains relocations (feature not yet implemented)");
                    break;
                }
            }
            return true;
        }

        // Handle export trie
        private void loadExportTrie(uint trieOffset, uint nodeOffset = 0, string partialSymbol = "") {
            Position = trieOffset + nodeOffset;

            var size = ULEB128.Decode(this);

            // Terminal information
            if (size != 0) {
                var flags = ReadByte();
                var symbolKind = flags & 0x03;
                var symbolType = (flags >> 2) & 0x03;

                // 0 = regular, 1 = weak, 2 = re-export, 3 = stub
                switch (symbolType) {
                    case 0:
                        var address = ULEB128.Decode(this);
                        exports.Add(new Export {Name = partialSymbol, VirtualAddress = GlobalOffset + address});
                        break;
                    case 1:
                        var weakAddress = ULEB128.Decode(this);
                        exports.Add(new Export {Name = partialSymbol, VirtualAddress = GlobalOffset + weakAddress});
                        break;
                    case 2:
                        var ordinal = ULEB128.Decode(this);
                        var name = ReadNullTerminatedString();
                        break;
                    case 3:
                        var stubOffset = ULEB128.Decode(this);
                        var resolverOffset = ULEB128.Decode(this);
                        break;
                }
            }

            var branchCount = ReadByte();

            for (int branch = 0; branch < branchCount; branch++) {
                var prefix = ReadNullTerminatedString();
                var childNodeOffset = (uint) ULEB128.Decode(this);

                var currentPosition = Position;
                loadExportTrie(trieOffset, childNodeOffset, partialSymbol + prefix);
                Position = currentPosition;
            }
        }

        public override uint[] GetFunctionTable() => ReadArray<TWord>(funcTab.ImageOffset, conv.Int(funcTab.Size) / (Bits / 8)).Select(x => MapVATR(conv.ULong(x)) & 0xffff_fffe).ToArray();

        public override Dictionary<string, Symbol> GetSymbolTable() {
            var symbols = new Dictionary<string, Symbol>();

            // https://opensource.apple.com/source/cctools/cctools-795/include/mach-o/nlist.h
            // n_sect: https://opensource.apple.com/source/cctools/cctools-795/include/mach-o/stab.h

            var symbolList = ReadArray<MachO_nlist<TWord>>(symTab.SymOffset, (int) symTab.NumSyms);

            // This is a really naive implementation that ignores the values of n_type and n_sect
            // which may affect the interpretation of n_value
            foreach (var symbol in symbolList) {
                Position = symTab.StrOffset + symbol.n_strx;
                var name = (symbol.n_strx != 0) ? ReadNullTerminatedString() : "";
                var value = (ulong) Convert.ChangeType(symbol.n_value, typeof(ulong));

                // Ignore symbols with no address or name
                if (value == 0 || name.Length == 0)
                    continue;

                // Mask out the N_EXT and N_PEXT bits because we don't care about it
                var ntype = (MachO_NType) ((byte)symbol.n_type & ~(byte)MachO_NType.N_EXT & ~(byte)MachO_NType.N_PEXT);

                // For non-debugging symbols (no bits of N_STAB set), just leave the N_TYPE
                // Otherwise leave the whole n_type field (with N_EXT and N_PEXT removed)
                var dbg = (symbol.n_type & MachO_NType.N_STAB) != 0;

                if (dbg)
                    if (ntype == MachO_NType.N_BNSYM || ntype == MachO_NType.N_ENSYM
                        || ntype == MachO_NType.N_SO || ntype == MachO_NType.N_OSO)
                        continue;

                var type = ntype == MachO_NType.N_FUN? SymbolType.Function
                    : ntype == MachO_NType.N_STSYM || ntype == MachO_NType.N_GSYM || ntype == MachO_NType.N_SECT? SymbolType.Name
                    : SymbolType.Unknown;

                if (type == SymbolType.Unknown) {
                    Console.WriteLine($"Unknown symbol type: {((int) ntype):x2}   {value:x16}   " + CxxDemangler.CxxDemangler.Demangle(name));
                }

                // Ignore duplicates
                symbols.TryAdd(name, new Symbol { Name = name, VirtualAddress = value, Type = type });
            }
            return symbols;
        }

        public override IEnumerable<Export> GetExports() => exports;
    }
}
