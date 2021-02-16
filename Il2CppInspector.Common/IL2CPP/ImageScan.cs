/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Il2CppInspector
{
    partial class Il2CppBinary
    {
        // Find a sequence of bytes
        // Adapted from https://stackoverflow.com/a/332667
        private int FindBytes(byte[] blob, byte[] signature, int requiredAlignment = 1, int startOffset = 0) {
            var firstMatchByte = Array.IndexOf(blob, signature[0], startOffset);
            var test = new byte[signature.Length];

            while (firstMatchByte >= 0 && firstMatchByte <= blob.Length - signature.Length) {
                Buffer.BlockCopy(blob, firstMatchByte, test, 0, signature.Length);
                if (test.SequenceEqual(signature) && firstMatchByte % requiredAlignment == 0)
                    return firstMatchByte;

                firstMatchByte = Array.IndexOf(blob, signature[0], firstMatchByte + 1);
            }
            return -1;
        }

        // Find all occurrences of a sequence of bytes, using word alignment by default
        private IEnumerable<uint> FindAllBytes(byte[] blob, byte[] signature, int alignment = 0) {
            var offset = 0;
            while (offset != -1) {
                offset = FindBytes(blob, signature, alignment != 0 ? alignment : Image.Bits / 8, offset);
                if (offset != -1) {
                    yield return (uint) offset;
                    offset += Image.Bits / 8;
                }
            }
        }

        // Find strings
        private IEnumerable<uint> FindAllStrings(byte[] blob, string str) => FindAllBytes(blob, Encoding.ASCII.GetBytes(str), 1);

        // Find 32-bit words
        private IEnumerable<uint> FindAllDWords(byte[] blob, uint word) => FindAllBytes(blob, BitConverter.GetBytes(word), 4);

        // Find 64-bit words
        private IEnumerable<uint> FindAllQWords(byte[] blob, ulong word) => FindAllBytes(blob, BitConverter.GetBytes(word), 8);

        // Find words for the current binary size
        private IEnumerable<uint> FindAllWords(byte[] blob, ulong word)
            => Image.Bits switch {
                32 => FindAllDWords(blob, (uint) word),
                64 => FindAllQWords(blob, word),
                _ => throw new InvalidOperationException("Invalid architecture bit size")
            };

        // Find all valid virtual address pointers to a virtual address
        private IEnumerable<ulong> FindAllMappedWords(byte[] blob, ulong va) {
            var fileOffsets = FindAllWords(blob, va);
            foreach (var offset in fileOffsets)
                if (Image.TryMapFileOffsetToVA(offset, out va))
                    yield return va;
        }

        // Find all valid virtual address pointers to a set of virtual addresses
        private IEnumerable<ulong> FindAllMappedWords(byte[] blob, IEnumerable<ulong> va) => va.SelectMany(a => FindAllMappedWords(blob, a));

        // Find all valid pointer chains to a set of virtual addresses with the specified number of indirections
        private IEnumerable<ulong> FindAllPointerChains(byte[] blob, IEnumerable<ulong> va, int indirections) {
            IEnumerable<ulong> vas = va;
            for (int i = 0; i < indirections; i++)
                vas = FindAllMappedWords(blob, vas);
            return vas;
        }

        // Scan the image for the needed data structures
        private (ulong, ulong) ImageScan(Metadata metadata) {
            Image.Position = 0;
            var imageBytes = Image.ReadBytes((int) Image.Length);

            var ptrSize = (uint) Image.Bits / 8;
            ulong codeRegistration = 0;
            IEnumerable<ulong> vas;

            // Find CodeRegistration
            // >= 24.2
            if (metadata.Version >= 24.2) {

                // < 27: mscorlib.dll is always the first CodeGenModule
                // >= 27: mscorlib.dll is always the last CodeGenModule (Assembly-CSharp.dll is always the first but non-Unity builds don't have this DLL)
                //        NOTE: winrt.dll + other DLLs can come after mscorlib.dll so we can't use its location to get an accurate module count
                var offsets = FindAllStrings(imageBytes, "mscorlib.dll\0");
                vas = offsets.Select(o => Image.MapFileOffsetToVA(o));

                // Unwind from string pointer -> CodeGenModule -> CodeGenModules + x
                vas = FindAllPointerChains(imageBytes, vas, 2);
                IEnumerable<ulong> codeRegVas = null;

                // We'll work back one pointer width at a time trying to find the first CodeGenModule
                // Let's hope there aren't more than 200 DLLs in any given application :)
                var maxCodeGenModules = 200;

                for (int backtrack = 0; backtrack < maxCodeGenModules && (codeRegVas?.Count() ?? 0) != 1; backtrack++) {
                    // Unwind from CodeGenModules + x -> CodeRegistration + y
                    codeRegVas = FindAllMappedWords(imageBytes, vas);

                    // The previous word must be the number of CodeGenModules
                    if (codeRegVas.Count() == 1) {
                        var codeGenModuleCount = Image.ReadMappedWord(codeRegVas.First() - ptrSize);

                        // Basic validity check
                        if (codeGenModuleCount <= 0 || codeGenModuleCount > maxCodeGenModules)
                            codeRegVas = Enumerable.Empty<ulong>();
                    }

                    // Move to the previous CodeGenModule if the above fails
                    vas = vas.Select(va => va - ptrSize);
                }

                if (!codeRegVas.Any())
                    return (0, 0);

                if (codeRegVas.Count() > 1)
                    throw new InvalidOperationException("More than one valid pointer chain found during data heuristics");

                // pCodeGenModules is the last field in CodeRegistration so we subtract the size of one pointer from the struct size
                codeRegistration = codeRegVas.First() - ((ulong) metadata.Sizeof(typeof(Il2CppCodeRegistration), Image.Version, Image.Bits / 8) - ptrSize);

                // In v24.3, windowsRuntimeFactoryTable collides with codeGenModules. So far no samples have had windowsRuntimeFactoryCount > 0;
                // if this changes we'll have to get smarter about disambiguating these two.
                var cr = Image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);

                if (Image.Version == 24.2 && cr.interopDataCount == 0) {
                    Image.Version = 24.3;
                    codeRegistration -= ptrSize * 2; // two extra words for WindowsRuntimeFactory
                }

                if (Image.Version == 27 && cr.reversePInvokeWrapperCount > 0x30000)
                {
                    // If reversePInvokeWrapperCount is a pointer, then it's because we're actually on 27.1 and there's a genericAdjustorThunks pointer interfering.
                    // We need to bump version to 27.1 and back up one more pointer.
                    Image.Version = 27.1;
                    codeRegistration -= ptrSize;
                }
            }

            // Find CodeRegistration
            // <= 24.1
            else {
                // The first item in CodeRegistration is the total number of method pointers
                vas = FindAllMappedWords(imageBytes, (ulong) metadata.Methods.Count(m => (uint) m.methodIndex != 0xffff_ffff));

                if (!vas.Any())
                    return (0, 0);

                // The count of method pointers will be followed some bytes later by
                // the count of custom attribute generators; the distance between them
                // depends on the il2cpp version so we just use ReadMappedObject to simplify the math
                foreach (var va in vas) {
                    var cr = Image.ReadMappedObject<Il2CppCodeRegistration>(va);

                    if (cr.customAttributeCount == metadata.AttributeTypeRanges.Length)
                        codeRegistration = va;
                }

                if (codeRegistration == 0)
                    return (0, 0);
            }

            // Find MetadataRegistration
            // >= 19
            var metadataRegistration = 0ul;

            // Find TypeDefinitionsSizesCount (4th last field) then work back to the start of the struct
            // This saves us from guessing where metadataUsagesCount is later
            var mrSize = (ulong) metadata.Sizeof(typeof(Il2CppMetadataRegistration), Image.Version, Image.Bits / 8);
            vas = FindAllMappedWords(imageBytes, (ulong) metadata.Types.Length).Select(a => a - mrSize + ptrSize * 4);

            // >= 19 && < 27
            if (Image.Version < 27)
                foreach (var va in vas) {
                    var mr = Image.ReadMappedObject<Il2CppMetadataRegistration>(va);
                    if (mr.metadataUsagesCount == (ulong) metadata.MetadataUsageLists.Length)
                        metadataRegistration = va;
                }

            // plagiarism. noun - https://www.lexico.com/en/definition/plagiarism
            //   the practice of taking someone else's work or ideas and passing them off as one's own.
            // Synonyms: copying, piracy, theft, strealing, infringement of copyright

            // >= 27
            else {
                // We're going to just sanity check all of the fields
                // All counts should be under a certain threshold
                // All pointers should be mappable to the binary

                var mrFieldCount = mrSize / (ulong) (Image.Bits / 8);
                foreach (var va in vas) {
                    var mrWords = Image.ReadMappedWordArray(va, (int) mrFieldCount);

                    // Even field indices are counts, odd field indices are pointers
                    bool ok = true;
                    for (var i = 0; i < mrWords.Length && ok; i++) {
                        ok = i % 2 == 0 ? mrWords[i] < 0x30000 : Image.TryMapVATR((ulong) mrWords[i], out _);
                    }
                    if (ok)
                        metadataRegistration = va;
                }
            }
            if (metadataRegistration == 0)
                return (0, 0);

            return (codeRegistration, metadataRegistration);
        }
    }
}
