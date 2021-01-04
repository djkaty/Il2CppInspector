/*
    Copyright 2017 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector
{
#pragma warning disable CS0649
    // Structures and enums: https://cocoaintheshell.whine.fr/2009/07/universal-binary-mach-o-format/

    public enum UB : uint
    {
        FAT_MAGIC = 0xcafebabe
    }

    // Big-endian
    public class FatHeader
    {
        public uint Magic;
        public uint NumArch;
    }

    // Big-endian
    public class FatArch
    {
        public uint CPUType;
        public uint CPUSubType;
        public uint Offset;
        public uint Size;
        public uint Align;
    }
}
