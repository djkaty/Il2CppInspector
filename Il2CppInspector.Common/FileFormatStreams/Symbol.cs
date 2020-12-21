/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector
{
    public enum SymbolType
    {
        Function,
        Name,
        Import,
        Unknown
    }

    // A code file function export
    public class Symbol
    {
        public ulong VirtualAddress { get; set; }
        public string Name { get; set; }
        public SymbolType Type { get; set; }

        public string DemangledName => CxxDemangler.CxxDemangler.Demangle(Name);
    }
}
