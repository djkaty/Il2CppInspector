/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector.Cpp
{
    public enum CppCompilerType
    {
        BinaryFormat,  // Inheritance structs use C syntax, and will automatically choose MSVC or GCC based on inferred compiler.
        MSVC,          // Inheritance structs are laid out assuming the MSVC compiler, which recursively includes base classes
        GCC,           // Inheritance structs are laid out assuming the GCC compiler, which packs members from all bases + current class together
    }

    public static class CppCompiler
    {
        // Attempt to guess the compiler used to build the binary via its file type
        public static CppCompilerType GuessFromImage(IFileFormatReader image) => (image is PEReader? CppCompilerType.MSVC : CppCompilerType.GCC);
    }
}
