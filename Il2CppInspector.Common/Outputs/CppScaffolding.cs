// Copyright 2020 Robert Xiao - https://robertxiao.ca/
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Reflection;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;

namespace Il2CppInspector.Outputs
{
    public class CppScaffolding
    {
        private readonly Il2CppModel model;
        public CppCompiler.Type Compiler = CppCompiler.Type.BinaryFormat;
        private StreamWriter writer;
        public UnityVersion UnityVersion;
        private CppDeclarationGenerator declGenerator;

        private readonly Regex rgxGCCalign = new Regex(@"__attribute__\s*?\(\s*?\(\s*?aligned\s*?\(\s*?([0-9]+)\s*?\)\s*?\)\s*?\)");
        private readonly Regex rgxMSVCalign = new Regex(@"__declspec\s*?\(\s*?align\s*?\(\s*?([0-9]+)\s*?\)\s*?\)");

        public CppScaffolding(Il2CppModel model) => this.model = model;

        public void WriteCppToFile(string outputFile) {
            declGenerator = new CppDeclarationGenerator(model, UnityVersion);
            UnityVersion = declGenerator.UnityVersion;

            // Can be overridden in the object initializer
            if (Compiler == CppCompiler.Type.BinaryFormat)
                Compiler = CppCompiler.GuessFromImage(model.Package.BinaryImage);

            using var fs = new FileStream(outputFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.UTF8);

            writeLine("// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("// Target Unity version: " + declGenerator.UnityHeader);
            writeLine("");

            // TODO: The implementation of C++ header output is temporary and will be replaced by a C++ type model in a later version
            writeSectionHeader("IL2CPP internal types");
            writeUnityHeaders();

            // Prevent conflicts with symbols that are in scope for compilers by default
            writeCode("namespace app {");
            writeLine("");

            writeSectionHeader("Application type definitions");
            writeTypesForMethods(model.MethodsByDefinitionIndex);

            writeSectionHeader("Application generic method type usages");
            writeTypesForMethods(model.GenericMethods.Values);

            writeSectionHeader("Application type usages");
            writeUsages();

            writeCode("}");

            writer.Close();
        }

        private void writeUnityHeaders() {
            var prefix = (model.Package.BinaryImage.Bits == 32) ? "#define IS_32BIT\n" : "";
            writeCode(prefix + declGenerator.UnityHeader.GetHeaderText());
        }

        private void writeTypesForMethods(IEnumerable<MethodBase> methods) {
            foreach (var method in methods.Where(m => m.VirtualAddress.HasValue)) {
                declGenerator.IncludeMethod(method);
                writeCode(declGenerator.GenerateRemainingTypeDeclarations());
            }
        }

        private void writeUsages() {
            foreach (var usage in model.Package.MetadataUsages) {
                switch (usage.Type) {
                    case MetadataUsageType.Type:
                    case MetadataUsageType.TypeInfo:
                        var type = model.GetMetadataUsageType(usage);
                        declGenerator.IncludeType(type);
                        writeCode(declGenerator.GenerateRemainingTypeDeclarations());
                        break;
                    case MetadataUsageType.MethodDef:
                    case MetadataUsageType.MethodRef:
                        var method = model.GetMetadataUsageMethod(usage);
                        declGenerator.IncludeMethod(method);
                        writeCode(declGenerator.GenerateRemainingTypeDeclarations());
                        break;
                }
            }
        }
 
        private void writeCode(string text) {
            if (Compiler == CppCompiler.Type.MSVC)
                text = rgxGCCalign.Replace(text, @"__declspec(align($1))");
            if (Compiler == CppCompiler.Type.GCC)
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
