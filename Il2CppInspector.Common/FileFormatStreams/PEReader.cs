/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    // References:
    // PE Header file: https://github.com/dotnet/llilc/blob/master/include/clr/ntimage.h
    // PE format specification: https://docs.microsoft.com/en-us/windows/win32/debug/pe-format?redirectedfrom=MSDN
    public class PEReader : FileFormatStream<PEReader>
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static bool FreeLibrary(IntPtr hLibModule);

        private COFFHeader coff;
        private IPEOptHeader pe;
        private PESection[] sections;
        private uint pFuncTable;
        private bool mightBePacked;

        // Section types we need to rename in obfuscated binaries
        private Dictionary<PE, string> wantedSectionTypes = new Dictionary<PE, string> {
            [PE.IMAGE_SCN_MEM_READ | PE.IMAGE_SCN_MEM_EXECUTE | PE.IMAGE_SCN_CNT_CODE]             = ".text",
            [PE.IMAGE_SCN_MEM_READ                            | PE.IMAGE_SCN_CNT_INITIALIZED_DATA] = ".rdata",
            [PE.IMAGE_SCN_MEM_READ | PE.IMAGE_SCN_MEM_WRITE   | PE.IMAGE_SCN_CNT_INITIALIZED_DATA] = ".data"
        };

        public override string DefaultFilename => "GameAssembly.dll";


        public override string Format => pe is PEOptHeader32 ? "PE32" : "PE32+";

        public override string Arch => coff.Machine switch {
            0x8664 => "x64", // IMAGE_FILE_MACHINE_AMD64
            0x1C0 => "ARM", // IMAGE_FILE_MACHINE_ARM
            0xAA64 => "ARM64", // IMAGE_FILE_MACHINE_ARM64
            0x1C4 => "ARM", // IMAGE_FILE_MACHINE_ARMINT (Thumb-2)
            0x14C => "x86", // IMAGE_FILE_MACHINE_I386
            0x1C2 => "ARM", // IMAGE_FILE_MACHINE_THUMB (Thumb)
            _ => "Unsupported"
        };

        // IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20B
        // IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10B
        // Could also use coff.Characteristics (IMAGE_FILE_32BIT_MACHINE) or coff.Machine
        public override int Bits => (PE) pe.Magic == PE.IMAGE_NT_OPTIONAL_HDR64_MAGIC ? 64 : 32;

        public override ulong ImageBase => pe.ImageBase;

        protected override bool Init() {
            // Check for MZ signature "MZ"
            if (ReadUInt16() != 0x5A4D)
                return false;

            // Get offset to PE header from DOS header
            Position = ReadUInt32(0x3C);

            // Check PE signature "PE\0\0"
            if (ReadUInt32() != 0x00004550)
                return false;

            // Read COFF Header
            coff = ReadObject<COFFHeader>();

            // Ensure presence of PE Optional header
            // Size will always be 0x60 (32-bit) or 0x70 (64-bit) + (0x10 ' 0x8) for 16 RVA entries @ 8 bytes each
            if (!((coff.SizeOfOptionalHeader == 0xE0 ? 32 :
                   coff.SizeOfOptionalHeader == 0xF0 ? (int?) 64 : null) is var likelyWordSize))
                return false;

            // Read PE optional header
            pe = likelyWordSize switch {
                32 => ReadObject<PEOptHeader32>(),
                64 => ReadObject<PEOptHeader64>(),
                _ => null
            };

            // Confirm architecture magic number matches expected word size
            if ((PE) pe.Magic != pe.ExpectedMagic)
                return false;

            // Get IAT
            var IATStart = pe.DataDirectory[12].VirtualAddress;
            var IATSize = pe.DataDirectory[12].Size;

            // Get sections table
            sections = ReadArray<PESection>(coff.NumberOfSections);

            // Unpacking must be done starting here, one byte after the end of the headers
            // Packed or previously packed with Themida? This is purely for information
            if (sections.FirstOrDefault(x => x.Name == ".themida") is PESection _)
                Console.WriteLine("Themida protection detected");

            // Packed with anything (including Themida)?
            mightBePacked = sections.FirstOrDefault(x => x.Name == ".rdata") is null;

            // Rename sections if needed (before potentially searching them or rewriting them to the stream)
            foreach (var section in sections.Where(s => wantedSectionTypes.Keys.Contains(s.Characteristics)))
                // Replace section name if blank or all whitespace
                if (Regex.IsMatch(section.Name, @"^\s*$"))
                    section.Name = wantedSectionTypes[section.Characteristics];

            // Get base of code
            GlobalOffset = pe.ImageBase + pe.BaseOfCode - sections.First(x => x.Name == ".text").PointerToRawData;

            // Confirm that .rdata section begins at same place as IAT
            var rData = sections.First(x => x.Name == ".rdata");
            mightBePacked |= rData.VirtualAddress != IATStart;

            // Calculate start of function pointer table
            pFuncTable = rData.PointerToRawData + IATSize;

            // Skip over __guard_check_icall_fptr and __guard_dispatch_icall_fptr if present, then the following zero offset
            Position = pFuncTable;
            if (pe is PEOptHeader32) {
                while (ReadUInt32() != 0)
                    pFuncTable += 4;
                pFuncTable += 4;
            }
            else {
                while (ReadUInt64() != 0)
                    pFuncTable += 8;
                pFuncTable += 8;
            }

            // In the fist go round, we signal that this is at least a valid PE file; we don't try to unpack yet
            return true;
        }

        // Load DLL into memory and save it as a new PE stream
        private void load() {
            // Check that the process is running in the same word size as the DLL
            // One way round this in future would be to spawn a new process of the correct word size
            if ((Environment.Is64BitProcess && Bits == 32) || (!Environment.Is64BitProcess && Bits == 64))
                throw new InvalidOperationException($"Cannot unpack a {Bits}-bit DLL from within a {(Environment.Is64BitProcess ? 64 : 32)}-bit process. Use the {Bits}-version of Il2CppInspector to unpack this DLL.");

            // Get file path
            // This error should never occur with the bundled CLI and GUI; only when used as a library by a 3rd party tool
            if (LoadOptions == null || !(LoadOptions.BinaryFilePath is string dllPath))
                throw new InvalidOperationException("To load a packed PE file, you must specify the DLL file path in LoadOptions");

            // Attempt to load DLL and run startup functions
            // NOTE: This can cause a CSE (AccessViolation) for certain types of protection
            // so only try to unpack as the final load strategy
            IntPtr hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero) {
                var lastErrorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Unable to load the DLL for unpacking: error code {lastErrorCode}");
            }

            // Maximum image size
            var size = sections.Last().VirtualAddress + sections.Last().VirtualSize;

            // Allocate memory for unpacked image
            var peBytes = new byte[size];

            // Copy relevant sections from unmanaged memory
            foreach (var section in sections.Where(s => wantedSectionTypes.Keys.Contains(s.Characteristics)))
                Marshal.Copy(IntPtr.Add(hModule, (int) section.VirtualAddress), peBytes, (int) section.VirtualAddress, (int) section.VirtualSize);
            
            // Decrease reference count for unload
            FreeLibrary(hModule);

            // Rebase
            pe.ImageBase = (ulong) hModule.ToInt64();

            // Rewrite sections to match memory layout
            foreach (var section in sections) {
                section.PointerToRawData = section.VirtualAddress;
                section.SizeOfRawData = section.VirtualSize;
            }

            // Truncate memory stream at start of COFF header
            var endOfSignature = ReadUInt32(0x3C) + 4; // DOS header + 4-byte PE signature
            SetLength(endOfSignature);

            // Re-write the stream (the headers are only necessary in case the user wants to save)
            Position = endOfSignature;
            WriteObject(coff);
            if (Bits == 32) WriteObject((PEOptHeader32) pe);
                       else WriteObject((PEOptHeader64) pe);
            WriteArray(sections);
            Write(peBytes, (int) Position, peBytes.Length - (int) Position);

            IsModified = true;
        }

        // Raw file / unpacked file load strategies
        public override IEnumerable<IFileFormatStream> TryNextLoadStrategy() {
            // First load strategy: the regular file
            yield return this;

            // Second load strategy: load the DLL into memory to unpack it
            if (mightBePacked) {
                Console.WriteLine("IL2CPP binary appears to be packed - attempting to unpack and retrying");
                StatusUpdate("Unpacking binary");
                load();
                yield return this;
            }
        }

        public override uint[] GetFunctionTable() {
            if (pFuncTable == 0)
                return Array.Empty<uint>();

            Position = pFuncTable;
            var addrs = new List<uint>();
            ulong addr;

            // Use TryMapVATR to avoid crash if function table is stripped or corrupted
            // Can happen with packed or previously packed files
            while ((addr = pe is PEOptHeader32? ReadUInt32() : ReadUInt64()) != 0 && TryMapVATR(addr, out uint fileOffset))
                addrs.Add(fileOffset & 0xfffffffc);
            return addrs.ToArray();
        }

        public override IEnumerable<Export> GetExports() {
            // Get exports table
            var ETStart = pe.DataDirectory[0].VirtualAddress + pe.ImageBase;

            // Get export RVAs
            var exportDirectoryTable = ReadObject<PEExportDirectory>(MapVATR(ETStart));
            var exportCount = (int) exportDirectoryTable.NumberOfFunctions;
            var exportAddresses = ReadArray<uint>(MapVATR(exportDirectoryTable.AddressOfFunctions + pe.ImageBase), exportCount);
            var exports = exportAddresses.Select((a, i) => new Export {
                Ordinal = (int) (exportDirectoryTable.Base + i),
                VirtualAddress = GlobalOffset + a
            }).ToDictionary(x => x.Ordinal, x => x);

            // Get export names
            var nameCount = (int) exportDirectoryTable.NumberOfNames;
            var namePointers = ReadArray<uint>(MapVATR(exportDirectoryTable.AddressOfNames + pe.ImageBase), nameCount);
            var ordinals = ReadArray<ushort>(MapVATR(exportDirectoryTable.AddressOfNameOrdinals + pe.ImageBase), nameCount);
            for (int i = 0; i < nameCount; i++) {
                var name = ReadNullTerminatedString(MapVATR(namePointers[i] + pe.ImageBase));
                var ordinal = (int) exportDirectoryTable.Base + ordinals[i];
                exports[ordinal].Name = name;
            }

            return exports.Values;
        }
         
        public override uint MapVATR(ulong uiAddr) {
            if (uiAddr == 0)
                return 0;

            var section = sections.First(x => uiAddr - pe.ImageBase >= x.VirtualAddress &&
                                              uiAddr - pe.ImageBase < x.VirtualAddress + x.SizeOfRawData);
            return (uint) (uiAddr - section.VirtualAddress - pe.ImageBase + section.PointerToRawData);
        }

        public override ulong MapFileOffsetToVA(uint offset) {
            var section = sections.First(x => offset >= x.PointerToRawData && offset < x.PointerToRawData + x.SizeOfRawData);

            return pe.ImageBase + section.VirtualAddress + offset - section.PointerToRawData;
        }

        public override IEnumerable<Section> GetSections() {
            return sections.Select(s => new Section {
                VirtualStart = pe.ImageBase + s.VirtualAddress,
                VirtualEnd = pe.ImageBase + s.VirtualAddress + s.VirtualSize - 1,
                ImageStart = s.PointerToRawData,
                ImageEnd = s.PointerToRawData + s.SizeOfRawData - 1,

                IsData = (s.Characteristics & PE.IMAGE_SCN_CNT_INITIALIZED_DATA) != 0,
                IsExec = (s.Characteristics & PE.IMAGE_SCN_CNT_CODE) != 0,
                IsBSS = (s.Characteristics & PE.IMAGE_SCN_CNT_UNINITIALIZED_DATA) != 0 || s.PointerToRawData == 0u,

                Name = s.Name
            });
        }
    }
}
