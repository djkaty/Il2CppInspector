/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector
{
    // A code file function export
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
    }
}
