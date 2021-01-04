/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector
{
    // Modifiers for use when loading binary files
    public class LoadOptions
    {
        // For ELF files, the virtual address to which we should rebase - ignored for other file types
        // Use zero to prevent rebasing
        public ulong ImageBase { get; set; } = 0ul;

        // For Linux process memory map inputs, we need the full path so we can find the .bin files
        // For packed PE files, we need the full path to reload the file via Win32 API
        // Ignored for all other cases
        public string BinaryFilePath { get; set; }
    }
}