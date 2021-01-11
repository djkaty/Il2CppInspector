/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

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
    public class ProcessMapReader : FileFormatStream<ProcessMapReader>
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
            // Where z = offset in file that the region was mapped from (we ignore this and build a file based on the memory dump)
            // Where aa:bb = device ID
            // Where c = inode

            var rgxProc = new Regex(@"^(?<start>[0-9A-Fa-f]{8})-(?<end>[0-9A-Fa-f]{8}) [rwxp\-]{4} [0-9A-Fa-f]{8} [0-9A-Fa-f]{2}:[0-9A-Fa-f]{2} \d+\s+(?<path>\S+)$", RegexOptions.Multiline);

            // Determine where libil2cpp.so was mapped into memory
            var il2cppMemory = rgxProc.Matches(text)
                                    .Where(m => m.Groups["path"].Value.EndsWith("libil2cpp.so"))
                                    .Select(m => new {
                                        Start = Convert.ToUInt32(m.Groups["start"].Value, 16),
                                        End = Convert.ToUInt32(m.Groups["end"].Value, 16)
                                    }).ToList();

            if (il2cppMemory.Count == 0)
                return false;

            // Get file path
            // This error should never occur with the bundled CLI and GUI; only when used as a library by a 3rd party tool
            if (LoadOptions == null || !(LoadOptions.BinaryFilePath is string mapsPath))
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

            // Find which file(s) are needed for each chunk of libil2cpp.so
            var chunks = il2cppMemory.Select(m => new {
                                    Memory = m,
                                    Files = files.Where(f => f.Start < m.End && f.End > m.Start).ToList()
            });

            // Set image base address for ELF loader
            // ELF loader will rebase the image and mark it as modified for saving
            LoadOptions.ImageBase = il2cppMemory.First().Start;

            // Merge the files, copying each chunk from one or more files to the specified offset in the merged file
            il2cpp = new BinaryObjectStream();

            foreach (var chunk in chunks) {
                var memoryNext = chunk.Memory.Start;
                il2cpp.Position = (long) (chunk.Memory.Start - LoadOptions.ImageBase);

                foreach (var file in chunk.Files) {
                    var fileStart = memoryNext - file.Start;

                    using var source = File.Open(file.Name, FileMode.Open, FileAccess.Read, FileShare.Read);

                    // Get the entire remaining chunk, or to the end of the file if it doesn't contain the end of the chunk
                    var length = (uint) Math.Min(chunk.Memory.End - memoryNext, source.Length);

                    Console.WriteLine($"Writing {length:x8} bytes from {Path.GetFileName(file.Name)} +{fileStart:x8} ({memoryNext:x8}) to target {il2cpp.Position:x8}");

                    // Can't use Stream.CopyTo as it doesn't support length parameter
                    var buffer = new byte[length];
                    source.Position = fileStart;
                    source.Read(buffer, 0, (int) length);
                    il2cpp.Write(buffer);

                    memoryNext += length;
                }
            }
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
