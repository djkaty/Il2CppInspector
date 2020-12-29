/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    // This is a wrapper for a Linux memory dump
    // The supplied file is a text file containing the output of "cat /proc/["self"|process-id]/maps"
    // We re-construct libil2cpp.so from the *.bin files and return it as the first image
    internal class ProcessMapReader : FileFormatStream<ProcessMapReader>
    {
        private BinaryObjectStream il2cpp;

        public override string DefaultFilename => "maps.txt";

        protected override bool Init() {

            // Maps.txt is extremely unlikely to be larger than this, so don't waste time loading many megabytes of binary data for no reason
            if (Length > 256 * 1024)
                return false;

            // Get the entire stream as a string
            var text = System.Text.Encoding.ASCII.GetString(ToArray());

            // Line format is: https://stackoverflow.com/questions/1401359/understanding-linux-proc-id-maps
            // xxxxxxxx-yyyyyyyy ffff zzzzzzzz aa:bb c [whitespace] [image path]
            // Where x = the start address
            // Where y = the end address
            // Where f = permission flags (rwxp or -)
            // Where z = offset in file that the region was mapped from (NOTE: we ignore this and assume it's a contiguous run)
            // Where aa:bb = device ID
            // Where c = inode

            var rgxProc = new Regex(@"^(?<start>[0-9A-Fa-f]{8})-(?<end>[0-9A-Fa-f]{8}) [rwxp\-]{4} [0-9A-Fa-f]{8} [0-9A-Fa-f]{2}:[0-9A-Fa-f]{2} \d+\s+(?<path>\S+)$", RegexOptions.Multiline);

            // Determine where libil2cpp.so was mapped into memory
            var il2cppMemory = rgxProc.Matches(text)
                                    .Where(m => m.Groups["path"].Value.EndsWith("libil2cpp.so"))
                                    .Select(m => new { Start = Convert.ToUInt32(m.Groups["start"].Value, 16),
                                                         End = Convert.ToUInt32(m.Groups["end"].Value, 16) }).ToList();

            if (il2cppMemory.Count == 0)
                return false;

            // Get file path
            // This error should never occur with the bundled CLI and GUI; only when used as a library by a 3rd party tool
            if (!(LoadOptions.BinaryFilePath is string mapsPath))
                throw new InvalidOperationException("To load a Linux process map, you must specify the maps file path in LoadOptions");

            if (!mapsPath.ToLower().EndsWith("-maps.txt"))
                throw new InvalidOperationException("To load a Linux process map, the map file must not be renamed");

            var mapsDir = Path.GetDirectoryName(mapsPath);
            var mapsPrefix = Path.GetFileName(mapsPath[..^9]);

            // Get memory dump filenames and mappings
            var rgxFile = new Regex(@"^\S+?-(?<start>[0-9A-Za-z]{8})-(?<end>[0-9A-Za-z]{8})\.bin$");

            var files = Directory.GetFiles(mapsDir, mapsPrefix + "-*.bin")
                                    .Select(f => rgxFile.Match(f))
                                    .Where(m => m.Groups[0].Success)
                                    .Select(m => new {
                                        Start = Convert.ToUInt32(m.Groups["start"].Value, 16),
                                        End = Convert.ToUInt32(m.Groups["end"].Value, 16),
                                        Name = m.Groups[0].Value
                                    }).OrderBy(m => m.Start).ToList();

            // Determine which files contain libil2cpp.so
            var neededFiles = files.Where(f => il2cppMemory.Any(m => f.Start < m.End && f.End > m.Start)).OrderBy(f => f.Start).ToList();

            // Determine how much to trim from the start of the first file and the end of the last file
            var offsetFirst = il2cppMemory.First().Start - neededFiles.First().Start;
            var lengthLast  = il2cppMemory.Last().End - neededFiles.Last().Start;

            // Merge the files
            il2cpp = new BinaryObjectStream();

            for (var i = 0; i < neededFiles.Count; i++) {
                var offset = (i == 0)? offsetFirst : 0;
                var length = ((i == neededFiles.Count - 1)? lengthLast : neededFiles[i].End - neededFiles[i].Start) - offset;

                using var source = File.Open(neededFiles[i].Name, FileMode.Open, FileAccess.Read, FileShare.Read);

                // Can't use Stream.CopyTo as it doesn't support length parameter
                var buffer = new byte[length];
                source.Position = offset;
                source.Read(buffer, 0, (int) length);
                il2cpp.Write(buffer);
            }

            // Set image base address for ELF loader
            // ELF loader will rebase the image and mark it as modified for saving
            LoadOptions.ImageBase = il2cppMemory.First().Start;

            return true;
        }

        public override IFileFormatStream this[uint index] {
            get {
                // Get merged stream as ELF file
                return (IFileFormatStream) ElfReader32.Load(il2cpp, LoadOptions, OnStatusUpdate) ?? ElfReader64.Load(il2cpp, LoadOptions, OnStatusUpdate);
            }
        }
    }
}
