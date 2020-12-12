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
        public ulong? ImageBase { get; set; }
    }
}