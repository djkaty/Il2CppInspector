/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

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
    internal class FatHeader
    {
        public uint Magic;
        public uint NumArch;
    }

    // Big-endian
    internal class FatArch
    {
        public uint CPUType;
        public uint CPUSubType;
        public uint Offset;
        public uint Size;
        public uint Align;
    }
}
