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
            // return (codeRegistration, metadataRegistration);
            return (0x1881205C0, 0x18812AE50);
        }
    }
}
