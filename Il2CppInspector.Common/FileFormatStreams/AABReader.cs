/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Il2CppInspector
{
    // This is a wrapper for multiple binary files of different architectures within a single AAB
    public class AABReader : FileFormatStream<AABReader>
    {
        private ZipArchive zip;
        private ZipArchiveEntry[] binaryFiles;

        public override string DefaultFilename => "Package.aab";

        protected override bool Init() {

            // Check if it's a zip file first because ZipFile.OpenRead is extremely slow if it isn't
            // 0x04034B50 = magic file header
            // 0x02014B50 = central directory file header (will appear if we merged a split AAB in memory)
            var magic = ReadUInt32();
            if (magic != 0x04034B50 && magic != 0x02014B50)
                return false;

            try {
                zip = new ZipArchive(this);

                // Get list of binary files
                binaryFiles = zip.Entries.Where(f => f.FullName.StartsWith("base/lib/") && f.Name == "libil2cpp.so").ToArray();

                // This package doesn't contain an IL2CPP binary
                if (!binaryFiles.Any())
                    return false;
            }

            // Not an archive
            catch (InvalidDataException) {
                return false;
            }

            NumImages = (uint) binaryFiles.Count();
            return true;
        }

        public override IFileFormatStream this[uint index] {
            get {
                Console.WriteLine($"Extracting binary from {binaryFiles[index].FullName}");
                IFileFormatStream loaded = null;

                // ZipArchiveEntry does not support seeking so we have to close and re-open for each possible load format
                var binary = binaryFiles[index].Open();
                loaded = ElfReader32.Load(binary, LoadOptions, OnStatusUpdate);
                binary.Close();

                if (loaded != null)
                    return loaded;

                binary = binaryFiles[index].Open();
                loaded = ElfReader64.Load(binary, LoadOptions, OnStatusUpdate);
                binary.Close();

                return loaded;
            }
        }
    }
}
