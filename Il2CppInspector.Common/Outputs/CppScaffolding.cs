// Copyright 2020 Robert Xiao - https://robertxiao.ca/
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Reflection;
using Il2CppInspector.Cpp;
using Il2CppInspector.Model;

namespace Il2CppInspector.Outputs
{
    public class CppScaffolding
    {
        private readonly AppModel model;
        private StreamWriter writer;

        private readonly Regex rgxGCCalign = new Regex(@"__attribute__\s*?\(\s*?\(\s*?aligned\s*?\(\s*?([0-9]+)\s*?\)\s*?\)\s*?\)");
        private readonly Regex rgxMSVCalign = new Regex(@"__declspec\s*?\(\s*?align\s*?\(\s*?([0-9]+)\s*?\)\s*?\)");

        public CppScaffolding(AppModel model) => this.model = model;

        public void WriteCppToFile(string outputFile) {
            using var fs = new FileStream(outputFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.UTF8);

            writeLine("// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("// Target Unity version: " + model.UnityHeader);
            writeLine("");

            writeSectionHeader("IL2CPP internal types");
            writeCode(model.UnityHeaderText);

            // Prevent conflicts with symbols that are in scope for compilers by default
            writeCode("namespace app {");
            writeLine("");

            writeTypesForGroup("Application types from method calls", "types_from_methods");
            writeTypesForGroup("Application types from generic methods", "types_from_generic_methods");
            writeTypesForGroup("Application types from usages", "types_from_usages");

            writeCode("}");

            writer.Close();
        }
        
        private void writeUnityHeaders() {
            var prefix = (model.Package.BinaryImage.Bits == 32) ? "#define IS_32BIT\n" : "";
            writeCode(prefix + model.UnityHeader.GetHeaderText());
        }

        private void writeTypesForGroup(string header, string group) {
            writeSectionHeader(header);
            foreach (var cppType in model.GetDependencyOrderedCppTypeGroup(group))
                writeCode(cppType.ToString());
        }
        
        private void writeCode(string text) {
            if (model.TargetCompiler == CppCompilerType.MSVC)
                text = rgxGCCalign.Replace(text, @"__declspec(align($1))");
            if (model.TargetCompiler == CppCompilerType.GCC)
                text = rgxMSVCalign.Replace(text, @"__attribute__((aligned($1)))");

            var lines = text.Replace("\r", "").Split('\n');
            var cleanLines = lines.Select(s => s.ToEscapedString());
            var declString = string.Join('\n', cleanLines);
            if (declString != "")
                writeLine(declString);
        }

        private void writeSectionHeader(string name) {
            writeLine("// ******************************************************************************");
            writeLine("// * " + name);
            writeLine("// ******************************************************************************");
            writeLine("");
        }

        private void writeLine(string line) => writer.WriteLine(line);
    }
}
