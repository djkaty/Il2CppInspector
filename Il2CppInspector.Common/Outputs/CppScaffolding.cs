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
using Il2CppInspector.Cpp.UnityHeaders;
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

        // Write the type header
        // This can be used by other output modules
        public void WriteTypes(string typeHeaderFile) {
            using var fs = new FileStream(typeHeaderFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.ASCII);

            writeHeader();

            // Write primitive type definitions for when we're not including other headers
            writeCode($@"#if defined(_GHIDRA_) || defined(_IDA_)
typedef unsigned __int8 uint8_t;
typedef unsigned __int16 uint16_t;
typedef unsigned __int32 uint32_t;
typedef unsigned __int64 uint64_t;
typedef __int8 int8_t;
typedef __int16 int16_t;
typedef __int32 int32_t;
typedef __int64 int64_t;
#endif

#if defined(_GHIDRA_)
typedef __int{model.Package.BinaryImage.Bits} size_t;
typedef size_t intptr_t;
typedef size_t uintptr_t;
#endif
");

            writeSectionHeader("IL2CPP internal types");
            writeCode(model.UnityHeaders.GetTypeHeaderText(model.WordSizeBits));

            // Stop MSVC complaining about out-of-bounds enum values
            if (model.TargetCompiler == CppCompilerType.MSVC)
                writeCode("#pragma warning(disable : 4369)");

            // Stop MSVC complaining about constant truncation of enum values
            if (model.TargetCompiler == CppCompilerType.MSVC)
                writeCode("#pragma warning(disable : 4309)");

            // C does not support namespaces
            writeCode("#if !defined(_GHIDRA_) && !defined(_IDA_)");
            writeCode("namespace app {");
            writeCode("#endif");
            writeLine("");

            writeTypesForGroup("Application types from method calls", "types_from_methods");
            writeTypesForGroup("Application types from generic methods", "types_from_generic_methods");
            writeTypesForGroup("Application types from usages", "types_from_usages");

            writeCode("#if !defined(_GHIDRA_) && !defined(_IDA_)");
            writeCode("}");
            writeCode("#endif");

            writer.Close();
        }

        public void Write(string projectPath) {
            // Ensure output directory exists and is not a file
            // A System.IOException will be thrown if it's a file'
            var srcUserPath = Path.Combine(projectPath, "user");
            var srcFxPath = Path.Combine(projectPath, "framework");
            var srcDataPath = Path.Combine(projectPath, "appdata");

            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(srcUserPath);
            Directory.CreateDirectory(srcFxPath);
            Directory.CreateDirectory(srcDataPath);

            // Write type definitions to il2cpp-types.h
            WriteTypes(Path.Combine(srcDataPath, "il2cpp-types.h"));

            // Write selected Unity API function file to il2cpp-api-functions.h
            // (this is a copy of the header file from an actual Unity install)
            var il2cppApiFile = Path.Combine(srcDataPath, "il2cpp-api-functions.h");
            var apiHeaderText = model.UnityHeaders.GetAPIHeaderText();

            using var fsApi = new FileStream(il2cppApiFile, FileMode.Create);
            writer = new StreamWriter(fsApi, Encoding.ASCII);

            writeHeader();

            // Elide APIs that aren't in the binary to avoid compile errors
            foreach (var line in apiHeaderText.Split('\n')) {
                var fnName = UnityHeaders.GetFunctionNameFromAPILine(line);

                if (string.IsNullOrEmpty(fnName))
                    writer.WriteLine(line);
                else if (model.AvailableAPIs.ContainsKey(fnName))
                    writer.WriteLine(line);
            }
            writer.Close();

            // Write API function pointers to il2cpp-api-functions-ptr.h
            var il2cppFnPtrFile = Path.Combine(srcDataPath, "il2cpp-api-functions-ptr.h");

            using var fs2 = new FileStream(il2cppFnPtrFile, FileMode.Create);
            writer = new StreamWriter(fs2, Encoding.ASCII);

            writeHeader();
            writeSectionHeader("IL2CPP API function pointers");

            // We could use model.AvailableAPIs here but that would exclude outputting the address
            // of API exports which for some reason aren't defined in our selected API header,
            // so although it doesn't affect the C++ compilation, we use GetAPIExports() instead for completeness
            var exports = model.Package.Binary.GetAPIExports();

            foreach (var export in exports) {
                writeCode($"#define {export.Key}_ptr 0x{model.Package.BinaryImage.MapVATR(export.Value):X8}");
            }

            writer.Close();

            // Write application type definition addresses to il2cpp-types-ptr.h
            var il2cppTypeInfoFile = Path.Combine(srcDataPath, "il2cpp-types-ptr.h");

            using var fs3 = new FileStream(il2cppTypeInfoFile, FileMode.Create);
            writer = new StreamWriter(fs3, Encoding.ASCII);

            writeHeader();
            writeSectionHeader("IL2CPP application-specific type definition addresses");

            foreach (var type in model.Types.Values.Where(t => t.TypeClassAddress != 0xffffffff_ffffffff)) {
                writeCode($"DO_TYPEDEF(0x{type.TypeClassAddress - model.Package.BinaryImage.ImageBase:X8}, {type.Name});");
            }

            writer.Close();

            // Write method pointers and signatures to il2cpp-functions.h
            var methodFile = Path.Combine(srcDataPath, "il2cpp-functions.h");

            using var fs4 = new FileStream(methodFile, FileMode.Create);
            writer = new StreamWriter(fs4, Encoding.ASCII);

            writeHeader();
            writeSectionHeader("IL2CPP application-specific method definition addresses and signatures");

            writeCode("using namespace app;");
            writeLine("");

            foreach (var method in model.Methods.Values.Where(m => m.HasCompiledCode)) {
                var arguments = string.Join(", ", method.CppFnPtrType.Arguments.Select(a => a.Type.Name + " " + (a.Name == "this" ? "__this" : a.Name)));
                writeCode($"DO_APP_FUNC(0x{method.MethodCodeAddress - model.Package.BinaryImage.ImageBase:X8}, {method.CppFnPtrType.ReturnType.Name}, "
                          + $"{method.CppFnPtrType.Name}, ({arguments}));");
            }

            writer.Close();

            // Write boilerplate code
            File.WriteAllText(Path.Combine(srcFxPath, "dllmain.cpp"), Resources.Cpp_DLLMainCpp);
            File.WriteAllText(Path.Combine(srcFxPath, "helpers.cpp"), Resources.Cpp_HelpersCpp);
            File.WriteAllText(Path.Combine(srcFxPath, "helpers.h"), Resources.Cpp_HelpersH);
            File.WriteAllText(Path.Combine(srcFxPath, "il2cpp-appdata.h"), Resources.Cpp_Il2CppAppDataH);
            File.WriteAllText(Path.Combine(srcFxPath, "il2cpp-init.cpp"), Resources.Cpp_Il2CppInitCpp);
            File.WriteAllText(Path.Combine(srcFxPath, "il2cpp-init.h"), Resources.Cpp_Il2CppInitH);
            File.WriteAllText(Path.Combine(srcFxPath, "pch-il2cpp.cpp"), Resources.Cpp_PCHIl2Cpp);
            File.WriteAllText(Path.Combine(srcFxPath, "pch-il2cpp.h"), Resources.Cpp_PCHIl2CppH);

            // Write user code without overwriting existing code
            void WriteIfNotExists(string path, string contents) { if (!File.Exists(path)) File.WriteAllText(path, contents); }

            WriteIfNotExists(Path.Combine(srcUserPath, "main.cpp"), Resources.Cpp_MainCpp);
            WriteIfNotExists(Path.Combine(srcUserPath, "main.h"), Resources.Cpp_MainH);

            // Write Visual Studio project and solution files
            var projectGuid = Guid.NewGuid();
            var projectName = "IL2CppDLL";
            var projectFile = projectName + ".vcxproj";

            WriteIfNotExists(Path.Combine(projectPath, projectFile),
                Resources.CppProjTemplate.Replace("%PROJECTGUID%", projectGuid.ToString()));

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var guid3 = Guid.NewGuid();
            var filtersFile = projectFile + ".filters";

            var filters = Resources.CppProjFilters
                .Replace("%GUID1%", guid1.ToString())
                .Replace("%GUID2%", guid2.ToString())
                .Replace("%GUID3%", guid3.ToString());

            WriteIfNotExists(Path.Combine(projectPath, filtersFile), filters);

            var solutionGuid = Guid.NewGuid();
            var solutionFile = projectName + ".sln";

            var sln = Resources.CppSlnTemplate
                .Replace("%PROJECTGUID%", projectGuid.ToString())
                .Replace("%PROJECTNAME%", projectName)
                .Replace("%PROJECTFILE%", projectFile)
                .Replace("%SOLUTIONGUID%", solutionGuid.ToString());

            WriteIfNotExists(Path.Combine(projectPath, solutionFile), sln);
        }

        private void writeHeader() {
            writeLine("// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("// Target Unity version: " + model.UnityHeaders);
            writeLine("");
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
