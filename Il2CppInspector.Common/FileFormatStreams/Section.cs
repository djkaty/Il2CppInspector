/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector
{
    // A code file function export - end addresses are inclusive
    public class Section
    {
        public ulong VirtualStart;
        public ulong VirtualEnd;
        public uint ImageStart;
        public uint ImageEnd;
        public bool IsExec;
        public bool IsData;
        public bool IsBSS;
        public string Name;

        public int ImageLength => (int) (ImageEnd - ImageStart) + 1;
        public ulong VirtualLength => (VirtualEnd - VirtualStart) + 1;
    }
}
