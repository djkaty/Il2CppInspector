// Copyright 2020 Robert Xiao - https://robertxiao.ca/
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Reflection;
using Il2CppInspector.Cpp;
using Il2CppInspector.Model;
using Il2CppInspector.Properties;

namespace Il2CppInspector.Outputs
{
    public class CppScaffolding
    {
        private readonly AppModel model;
        private StreamWriter writer;

        private readonly Regex rgxGCCalign = new Regex(@"__attribute__\s*?\(\s*?\(\s*?aligned\s*?\(\s*?([0-9]+)\s*?\)\s*?\)\s*?\)");
        private readonly Regex rgxMSVCalign = new Regex(@"__declspec\s*?\(\s*?align\s*?\(\s*?([0-9]+)\s*?\)\s*?\)");

        public CppScaffolding(AppModel model) => this.model = model;

        public void Write(string outputPath) {
            // Ensure output directory exists and is not a file
            // A System.IOException will be thrown if it's a file'
            Directory.CreateDirectory(outputPath);

            // Write type definitions to il2cpp-types.h
            var typeHeaderFile = Path.Combine(outputPath, "il2cpp-types.h");

            using var fs = new FileStream(typeHeaderFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.UTF8);

            writeHeader();
            writeSectionHeader("IL2CPP internal types");
            writeCode(model.UnityHeaderText);

            writeCode("namespace app {");
            writeLine("");

            writeTypesForGroup("Application types from method calls", "types_from_methods");
            writeTypesForGroup("Application types from generic methods", "types_from_generic_methods");
            writeTypesForGroup("Application types from usages", "types_from_usages");

            writeCode("}");

            writer.Close();

            // Write API function pointers to il2cpp-function-ptr.h
            var il2cppFnPtrFile = Path.Combine(outputPath, "il2cpp-function-ptr.h");

            using var fs2 = new FileStream(il2cppFnPtrFile, FileMode.Create);
            writer = new StreamWriter(fs2, Encoding.UTF8);

            writeHeader();
            writeSectionHeader("IL2CPP API function pointers");

            // TODO: Use model.APIExports instead once it is implemented
            var exports = model.Package.Binary.GetAPIExports();

            foreach (var export in exports) {
                writeCode($"#define {export.Key}_ptr 0x{model.Package.BinaryImage.MapVATR(export.Value):X8}");
            }

            writer.Close();

            // Write application type definition addresses to il2cpp-type-ptr.h
            var il2cppTypeInfoFile = Path.Combine(outputPath, "il2cpp-type-ptr.h");

            using var fs3 = new FileStream(il2cppTypeInfoFile, FileMode.Create);
            writer = new StreamWriter(fs3, Encoding.UTF8);

            writeHeader();
            writeSectionHeader("IL2CPP application-specific type definition addresses");

            foreach (var type in model.Types.Values.Where(t => t.TypeClassAddress != 0xffffffff_ffffffff)) {
                writeCode($"DO_TYPEDEF(0x{type.TypeClassAddress - model.Package.BinaryImage.ImageBase:X8}, {type.Name});");
            }

            writer.Close();

            // Write method pointers and signatures to il2cpp-functions.h
            var methodFile = Path.Combine(outputPath, "il2cpp-functions.h");

            using var fs4 = new FileStream(methodFile, FileMode.Create);
            writer = new StreamWriter(fs4, Encoding.UTF8);

            writeHeader();
            writeSectionHeader("IL2CPP application-specific method definition addresses and signatures");

            writeCode("using namespace app;");
            writeLine("");

            foreach (var method in model.Methods.Values) {
                var arguments = string.Join(", ", method.CppFnPtrType.Arguments.Select(a => a.Type.Name + " " + (a.Name == "this" ? "__this" : a.Name)));
                writeCode($"DO_APP_FUNC(0x{method.MethodCodeAddress - model.Package.BinaryImage.ImageBase:X8}, {method.CppFnPtrType.ReturnType.Name}, "
                          + $"{method.CppFnPtrType.Name}, ({arguments}));");
            }

            writer.Close();

            // Write boilerplate code
            File.WriteAllText(Path.Combine(outputPath, "il2cpp-init.h"), Resources.Cpp_IL2CPPInitH);
            File.WriteAllText(Path.Combine(outputPath, "helpers.h"), Resources.Cpp_HelpersH);
            File.WriteAllText(Path.Combine(outputPath, "dllmain.h"), Resources.Cpp_DLLMainH);
            File.WriteAllText(Path.Combine(outputPath, "main.cpp"), Resources.Cpp_MainCpp);
            File.WriteAllText(Path.Combine(outputPath, "helpers.cpp"), Resources.Cpp_HelpersCpp);
            File.WriteAllText(Path.Combine(outputPath, "dllmain.cpp"), Resources.Cpp_DLLMainCpp);

            // Write Visual Studio project and solution files
            var projectGuid = Guid.NewGuid();
            var projectName = "IL2CppDLL";
            var projectFile = projectName + ".vcxproj";

            File.WriteAllText(Path.Combine(outputPath, projectFile),
                Resources.CppProjTemplate.Replace("%PROJECTGUID%", projectGuid.ToString()));

            var solutionGuid = Guid.NewGuid();
            var solutionFile = projectName + ".sln";

            var sln = Resources.CppSlnTemplate
                .Replace("%PROJECTGUID%", projectGuid.ToString())
                .Replace("%PROJECTNAME%", projectName)
                .Replace("%PROJECTFILE%", projectFile)
                .Replace("%SOLUTIONGUID%", solutionGuid.ToString());

            File.WriteAllText(Path.Combine(outputPath, solutionFile), sln);
        }

        private void writeHeader() {
            writeLine("// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("// Target Unity version: " + model.UnityHeader);
            writeLine("");
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
