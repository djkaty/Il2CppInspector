// Copyright (c) 2017-2020 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Model;
using Il2CppInspector.Outputs;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.CLI
{
    public class App
    {
        private class Options
        {
            [Option('i', "bin", Required = false, HelpText = "IL2CPP binary, APK or IPA input file", Default = "libil2cpp.so")]
            public string BinaryFile { get; set; }

            [Option('m', "metadata", Required = false, HelpText = "IL2CPP metadata file input (ignored for APK/IPA)", Default = "global-metadata.dat")]
            public string MetadataFile { get; set; }

            [Option('c', "cs-out", Required = false, HelpText = "C# output file (when using single-file layout) or path (when using per namespace, assembly or class layout)", Default = "types.cs")]
            public string CSharpOutPath { get; set; }

            [Option('p', "py-out", Required = false, HelpText = "Python script output file", Default = "il2cpp.py")]
            public string PythonOutFile { get; set; }

            [Option('h', "cpp-out", Required = false, HelpText = "C++ scaffolding / DLL injection project output path", Default = "cpp")]
            public string CppOutPath { get; set; }

            [Option('o', "json-out", Required = false, HelpText = "JSON metadata output file", Default = "metadata.json")]
            public string JsonOutPath { get; set; }

            [Option('e', "exclude-namespaces", Required = false, Separator = ',', HelpText = "Comma-separated list of namespaces to suppress in C# output, or 'none' to include all namespaces",
                Default = new [] {
                    "System",
                    "Mono",
                    "Microsoft.Reflection",
                    "Microsoft.Win32",
                    "Internal.Runtime",
                    "Unity",
                    "UnityEditor",
                    "UnityEngine",
                    "UnityEngineInternal",
                    "AOT",
                    "JetBrains.Annotations"
                })]
            public IEnumerable<string> ExcludedNamespaces { get; set; }

            [Option('l', "layout", Required = false, HelpText = "Partitioning of C# output ('single' = single file, 'namespace' = one file per namespace in folders, 'assembly' = one file per assembly, 'class' = one file per class in namespace folders, 'tree' = one file per class in assembly and namespace folders)", Default = "single")]
            public string LayoutSchema { get; set; }

            [Option('s', "sort", Required = false, HelpText = "Sort order of type definitions in C# output ('index' = by type definition index, 'name' = by type name). No effect when using file-per-class or tree layout", Default = "index")]
            public string SortOrder { get; set; }

            [Option('f', "flatten", Required = false, HelpText = "Flatten the namespace hierarchy into a single folder rather than using per-namespace subfolders. Only used when layout is per-namespace or per-class. Ignored for tree layout")]
            public bool FlattenHierarchy { get; set; }

            [Option('n', "suppress-metadata", Required = false, HelpText = "Diff tidying: suppress method pointers, field offsets and type indices from C# output. Useful for comparing two versions of a binary for changes with a diff tool")]
            public bool SuppressMetadata { get; set; }

            [Option('k', "must-compile", Required = false, HelpText = "Compilation tidying: try really hard to make code that compiles. Suppress generation of code for items with CompilerGenerated attribute. Comment out attributes without parameterless constructors or all-optional constructor arguments. Don't emit add/remove/raise on events. Specify AttributeTargets.All on classes with AttributeUsage attribute. Force auto-properties to have get accessors. Force regular properties to have bodies. Suppress global::Locale classes. Generate dummy parameterless base constructors and ref return fields.")]
            public bool MustCompile { get; set; }

            [Option("separate-attributes", Required = false, HelpText = "Place assembly-level attributes in their own AssemblyInfo.cs files. Only used when layout is per-assembly or tree")]
            public bool SeparateAssemblyAttributesFiles { get; set; }

            [Option('j', "project", Required = false, HelpText = "Create a Visual Studio solution and projects. Implies --layout tree, --must-compile and --separate-attributes")]
            public bool CreateSolution { get; set; }

            [Option("cpp-compiler", Required = false, HelpText = "Compiler to target for C++ output (MSVC or GCC); selects based on binary executable type by default", Default = CppCompilerType.BinaryFormat)]
            public CppCompilerType CppCompiler { get; set; }

            [Option('t', "script-target", Required = false, HelpText = "Application to target for Python script output (IDA or Ghidra) - case-sensitive", Default = "IDA")]
            public string ScriptTarget { get; set; }

            [Option("unity-path", Required = false, HelpText = "Path to Unity editor (when using --project). Wildcards select last matching folder in alphanumeric order", Default = @"C:\Program Files\Unity\Hub\Editor\*")]
            public string UnityPath { get; set; }

            [Option("unity-assemblies", Required = false, HelpText = "Path to Unity script assemblies (when using --project). Wildcards select last matching folder in alphanumeric order", Default = @"C:\Program Files\Unity\Hub\Editor\*\Editor\Data\Resources\PackageManager\ProjectTemplates\libcache\com.unity.template.3d-*\ScriptAssemblies")]
            public string UnityAssembliesPath { get; set; }

            [Option("unity-version", Required = false, HelpText = "Version of Unity used to create the input files, if known. Used to enhance Python, C++ and JSON output. If not specified, a close match will be inferred automatically.", Default = null)]
            public UnityVersion UnityVersion { get; set; }
        }

        // Adapted from: https://stackoverflow.com/questions/16376191/measuring-code-execution-time
        public class Benchmark : IDisposable 
        {
            private readonly Stopwatch timer = new Stopwatch();
            private readonly string benchmarkName;

            public Benchmark(string benchmarkName)
            {
                this.benchmarkName = benchmarkName;
                timer.Start();
            }

            public void Dispose() 
            {
                timer.Stop();
                Console.WriteLine($"{benchmarkName}: {timer.Elapsed.TotalSeconds:N2} sec");
            }
        }

        public static int Main(string[] args) =>
            Parser.Default.ParseArguments<Options>(args).MapResult(
                options => Run(options),
                _ => 1);

        private static int Run(Options options) {
            // Banner
            var asmInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            Console.WriteLine(asmInfo.ProductName);
            Console.WriteLine("Version " + asmInfo.ProductVersion);
            Console.WriteLine(asmInfo.LegalCopyright);
            Console.WriteLine("");
            
            // Check script target is valid
            if (!PythonScript.GetAvailableTargets().Contains(options.ScriptTarget)) {
                Console.Error.WriteLine($"Script target {options.ScriptTarget} is invalid.");
                Console.Error.WriteLine("Valid targets are: " + string.Join(", ", PythonScript.GetAvailableTargets()));
                return 1;
            }

            // Check excluded namespaces
            if (options.ExcludedNamespaces.Count() == 1 && options.ExcludedNamespaces.First().ToLower() == "none")
                options.ExcludedNamespaces = new List<string>();

            // Creating a Visual Studio solution requires Unity assembly references
            var unityPath = string.Empty;
            var unityAssembliesPath = string.Empty;

            if (options.CreateSolution) {
                unityPath = Utils.FindPath(options.UnityPath);
                unityAssembliesPath = Utils.FindPath(options.UnityAssembliesPath);

                if (!Directory.Exists(unityPath)) {
                    Console.Error.WriteLine($"Unity path {unityPath} does not exist");
                    return 1;
                }

                string editorPathSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? @"/Contents/Managed/UnityEditor.dll"
                    : @"\Editor\Data\Managed\UnityEditor.dll";

                if (!File.Exists(unityPath + editorPathSuffix)) {
                    Console.Error.WriteLine($"No Unity installation found at {unityPath}");
                    return 1;
                }
                
                if (!Directory.Exists(unityAssembliesPath)) {
                    Console.Error.WriteLine($"Unity assemblies path {unityAssembliesPath} does not exist");
                    return 1;
                }

                string uiDllPath = Path.Combine(unityAssembliesPath, "UnityEngine.UI.dll");
                if (!File.Exists(uiDllPath)) {
                    Console.Error.WriteLine($"No UnityEngine.UI.dll assemblies found at {uiDllPath}");
                    return 1;
                }

                Console.WriteLine("Using Unity editor at " + unityPath);
                Console.WriteLine("Using Unity assemblies at " + unityAssembliesPath);
            }

            // Check files exist and determine whether they're archives or not
            List<Il2CppInspector> il2cppInspectors;
            using (new Benchmark("Analyze IL2CPP data")) {

                if (!File.Exists(options.BinaryFile)) {
                    Console.Error.WriteLine($"File {options.BinaryFile} does not exist");
                    return 1;
                }

                try {
                    il2cppInspectors = Il2CppInspector.LoadFromPackage(options.BinaryFile);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex.Message);
                    return 1;
                }

                if (il2cppInspectors == null) {
                    if (!File.Exists(options.MetadataFile)) {
                        Console.Error.WriteLine($"File {options.MetadataFile} does not exist");
                        return 1;
                    }

                    il2cppInspectors = Il2CppInspector.LoadFromFile(options.BinaryFile, options.MetadataFile);
                }
            }

            if (il2cppInspectors == null)
                Environment.Exit(1);

            // Write output files for each binary
            int imageIndex = 0;
            foreach (var il2cpp in il2cppInspectors) {
                Console.WriteLine($"Processing image {imageIndex} - {il2cpp.BinaryImage.Arch} / {il2cpp.BinaryImage.Bits}-bit");

                // Create model
                TypeModel model;
                using (new Benchmark("Create .NET type model"))
                    model = new TypeModel(il2cpp);

                AppModel appModel;
                using (new Benchmark("Create C++ application model")) {
                    appModel = new AppModel(model, makeDefaultBuild: false).Build(options.UnityVersion, options.CppCompiler);
                }

                // C# signatures output
                using (new Benchmark("Generate C# code")) {
                    var writer = new CSharpCodeStubs(model) {
                        ExcludedNamespaces = options.ExcludedNamespaces.ToList(),
                        SuppressMetadata = options.SuppressMetadata,
                        MustCompile = options.MustCompile
                    };

                    var csOut = getOutputPath(options.CSharpOutPath, "cs", imageIndex);

                    if (options.CreateSolution)
                        writer.WriteSolution(csOut, unityPath, unityAssembliesPath);

                    else
                        switch (options.LayoutSchema.ToLower(), options.SortOrder.ToLower()) {
                            case ("single", "index"):
                                writer.WriteSingleFile(csOut, t => t.Index);
                                break;
                            case ("single", "name"):
                                writer.WriteSingleFile(csOut, t => t.Name);
                                break;

                            case ("namespace", "index"):
                                writer.WriteFilesByNamespace(csOut, t => t.Index, options.FlattenHierarchy);
                                break;
                            case ("namespace", "name"):
                                writer.WriteFilesByNamespace(csOut, t => t.Name, options.FlattenHierarchy);
                                break;

                            case ("assembly", "index"):
                                writer.WriteFilesByAssembly(csOut, t => t.Index, options.SeparateAssemblyAttributesFiles);
                                break;
                            case ("assembly", "name"):
                                writer.WriteFilesByAssembly(csOut, t => t.Name, options.SeparateAssemblyAttributesFiles);
                                break;

                            case ("class", _):
                                writer.WriteFilesByClass(csOut, options.FlattenHierarchy);
                                break;

                            case ("tree", _):
                                writer.WriteFilesByClassTree(csOut, options.SeparateAssemblyAttributesFiles);
                                break;
                        }

                    if (writer.GetAndClearLastException() is Exception ex)
                        Console.WriteLine("An error occurred: " + ex.Message);
                }

                // C++ output
                using (new Benchmark("Generate C++ code")) {
                    new CppScaffolding(appModel).Write(getOutputPath(options.CppOutPath, "", imageIndex));
                }

                // JSON output
                using (new Benchmark("Generate JSON metadata")) {
                    new JSONMetadata(appModel).Write(getOutputPath(options.JsonOutPath, "json", imageIndex));
                }

                // Python script output
                using (new Benchmark($"Generate {options.ScriptTarget} Python script")) {
                    new PythonScript(appModel).WriteScriptToFile(
                        getOutputPath(options.PythonOutFile, "py", imageIndex),
                        options.ScriptTarget,
                        Path.Combine(getOutputPath(options.CppOutPath, "", imageIndex), "appdata/il2cpp-types.h"),
                        getOutputPath(options.JsonOutPath, "json", imageIndex));
                }

                imageIndex++;
            }

            // Success exit code
            return 0;
        }

        private static string getOutputPath(string path, string extension, int suffix) {
            if (suffix == 0)
                return path;
            var imageSuffix = "-" + suffix;
            if (extension.Length > 0 && path.ToLower().EndsWith("." + extension))
                path = path.Insert(path.Length - (extension.Length + 1), imageSuffix);
            else
                path += imageSuffix;
            return path;
        }
    }
}
