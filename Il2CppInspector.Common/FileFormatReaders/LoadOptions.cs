/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector
{
    // Modifiers for use when loading binary files
    public class LoadOptions
    {
        // For dumped ELF files, the virtual address to which we should rebase - ignored for other file types
        // Use 2^64-1 to prevent rebasing on a dumped file
        public ulong ImageBase { get; set; }

        // For Linux process memory map inputs, we need the full path so we can find the .bin files
        public string BinaryFilePath { get; set; }
    }
}