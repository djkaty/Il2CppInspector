using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.Cpp
{
    public static class CppCompiler
    {
        public enum Type
        {
            BinaryFormat,  // Inheritance structs use C syntax, and will automatically choose MSVC or GCC based on inferred compiler.
            MSVC,          // Inheritance structs are laid out assuming the MSVC compiler, which recursively includes base classes
            GCC,           // Inheritance structs are laid out assuming the GCC compiler, which packs members from all bases + current class together
        }

        public static Type GuessFromImage(IFileFormatReader image) => (image is PEReader? Type.MSVC : Type.GCC);
    }
}
