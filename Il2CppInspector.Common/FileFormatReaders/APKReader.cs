/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Il2CppInspector
{
    // This is a wrapper for multiple binary files of different architectures within a single APK
    internal class APKReader : FileFormatReader<APKReader>
    {
        private ZipArchive zip;
        private ZipArchiveEntry[] binaryFiles;

        public APKReader(Stream stream) : base(stream) { }

        protected override bool Init() {

            // Check if it's a zip file first because ZipFile.OpenRead is extremely slow if it isn't
            if (ReadUInt32() != 0x04034B50)
                return false;

            try {
                zip = new ZipArchive(BaseStream);

                // Check for existence of global-metadata.dat
                if (!zip.Entries.Any(f => f.FullName == "assets/bin/Data/Managed/Metadata/global-metadata.dat"))
                    return false;

                // Get list of binary files
                binaryFiles = zip.Entries.Where(f => f.FullName.StartsWith("lib/") && f.Name == "libil2cpp.so").ToArray();

                // This package doesn't contain an IL2CPP application
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

        public override IFileFormatReader this[uint index] {
            get {
                Console.WriteLine($"Extracting binary from {binaryFiles[index].FullName}");
                IFileFormatReader loaded = null;

                // ZipArchiveEntry does not support seeking so we have to close and re-open for each possible load format
                var binary = binaryFiles[index].Open();
                loaded = ElfReader32.Load(binary, OnStatusUpdate);
                binary.Close();

                if (loaded != null)
                    return loaded;

                binary = binaryFiles[index].Open();
                loaded = ElfReader64.Load(binary, OnStatusUpdate);
                binary.Close();

                return loaded;
            }
        }
    }
}
