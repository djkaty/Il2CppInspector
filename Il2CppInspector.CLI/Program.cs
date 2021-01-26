/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Xml.Schema;
using CommandLine;
using CommandLine.Text;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Model;
using Il2CppInspector.Outputs;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.CLI
{
    public class App
    {
        // Default file paths for output modules
        private const string CsOutDefault = "types.cs";
        private const string PyOutDefault = "il2cpp.py";
        private const string CppOutDefault = "cpp";
        private const string JsonOutDefault = "metadata.json";
        private const string DllOutDefault = "dll";

        private class Options
        {
            [Option('i', "bin", Required = false, Separator = ',', HelpText = "IL2CPP binary, APK, AAB, XAPK, IPA, Zip or Linux process map text input file(s) (single file or comma-separated list for split APKs)", Default = new[] { "libil2cpp.so" })]
            public IEnumerable<string> BinaryFiles { get; set; }

            [Option('m', "metadata", Required = false, HelpText = "IL2CPP metadata file input (ignored for APK/AAB/XAPK/IPA/Zip)", Default = "global-metadata.dat")]
            public string MetadataFile { get; set; }

            [Option("image-base", Required = false, HelpText = "For ELF memory dumps, the image base address in hex (ignored for standard ELF files and other file formats)")]
            public string ElfImageBaseString { get; set; }

            [Option("select-outputs", Required = false, HelpText = "Only generate outputs specified on the command line (use --cs-out, --py-out, --cpp-out, --json-out, --dll-out to select outputs). If not specified, all outputs are generated")]
            public bool SpecifiedOutputsOnly { get; set; }

            [Option('c', "cs-out", Required = false, HelpText = "(Default: " + CsOutDefault + ") C# output file (when using single-file layout) or path (when using per namespace, assembly or class layout)")]
            public string CSharpOutPath { get; set; }

            [Option('p', "py-out", Required = false, HelpText = "(Default: " + PyOutDefault + ") Python script output file")]
            public string PythonOutFile { get; set; }

            [Option('h', "cpp-out", Required = false, HelpText = "(Default: " + CppOutDefault + ") C++ scaffolding / DLL injection project output path")]
            public string CppOutPath { get; set; }

            [Option('o', "json-out", Required = false, HelpText = "(Default: " + JsonOutDefault + ") JSON metadata output file")]
            public string JsonOutPath { get; set; }

            [Option('d', "dll-out", Required = false, HelpText = "(Default: " + DllOutDefault + ") .NET assembly shim DLLs output path")]
            public string DllOutPath { get; set; }

            [Option("metadata-out", Required = false, HelpText = "IL2CPP metadata file output (for extracted or decrypted metadata; ignored otherwise)")]
            public string MetadataFileOut { get; set; }

            [Option("binary-out", Required = false, HelpText = "IL2CPP binary file output (for extracted or decrypted binaries; ignored otherwise; suffixes will be appended for multiple files)")]
            public string BinaryFileOut { get; set; }

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

            [Option("suppress-dll-metadata", Required = false, HelpText = "Diff tidying: suppress method pointers, field offsets and type indices attributes from DLL output. Useful for comparing two versions of a binary for changes")]
            public bool SuppressDllMetadata { get; set; }

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

            [Option("unity-version-from-asset", Required = false, HelpText = "A Unity asset file used to determine the exact Unity version. Overrides --unity-version.", Default = null)]
            public string UnityVersionAsset { get; set; }

            [Option("plugins", Required = false, HelpText = "Specify options for plugins. Enclose each plugin's configuration in quotes as follows: --plugins \"pluginone --option1 value1 --option2 value2\" \"plugintwo --option...\". Use --plugins <name> to get help on a specific plugin")]
            public IEnumerable<string> PluginOptions { get; set; }
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

        public static void Main(string[] args) {
            var parser = new Parser(config => config.HelpWriter = null);
            var result = parser.ParseArguments<Options>(args);
            result.WithParsed(options => Run(options))
                  .WithNotParsed(errors => DisplayHelp(result, errors));
        }

        private static int DisplayHelp(ParserResult<Options> result, IEnumerable<Error> errors) {
            Console.Error.WriteLine(HelpText.AutoBuild(result));

            var help = new HelpText();
            help.Heading = "Available plugins:";
            help.Copyright = string.Empty;
            help.AddDashesToOption = false;
            help.AdditionalNewLineAfterOption = true;
            help.MaximumDisplayWidth = 80;
            help.AutoHelp = false;
            help.AutoVersion = false;

            var pluginOptions = PluginOptions.GetPluginOptionTypes();
            if (pluginOptions.Any())
                Console.Error.WriteLine(help.AddVerbs(PluginOptions.GetPluginOptionTypes()));
            return 1;
        }

        private static int Run(Options options) {

            // Banner
            var asmInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            Console.WriteLine(asmInfo.ProductName);
            Console.WriteLine("Version " + asmInfo.ProductVersion);
            Console.WriteLine(asmInfo.LegalCopyright);
            Console.WriteLine("");

            // Safe plugin manager load
            try {
                PluginManager.EnsureInit();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is DirectoryNotFoundException) {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            // Check plugin options are valid
            if (!PluginOptions.ParsePluginOptions(options.PluginOptions, PluginOptions.GetPluginOptionTypes()))
                return 1;

            // Show which plugins are in use
            foreach (var plugin in PluginManager.EnabledPlugins)
                Console.WriteLine("Using plugin: " + plugin.Name);

            // Make sure at least one output is specified if the user has restricted outputs
            if (options.SpecifiedOutputsOnly
                && options.CSharpOutPath == null
                && options.PythonOutFile == null
                && options.CppOutPath == null
                && options.JsonOutPath == null
                && options.DllOutPath == null) {
                Console.Error.WriteLine("At least one output must be specified when using --select-outputs.");
                Console.Error.WriteLine("Use --cs-out, --py-out, --cpp-out, --json-out and/or --dll-out, or omit --select-outputs to generate all output types");
                return 1;
            }

            // Check script target is valid
            if (!PythonScript.GetAvailableTargets().Contains(options.ScriptTarget)) {
                Console.Error.WriteLine($"Script target {options.ScriptTarget} is invalid.");
                Console.Error.WriteLine("Valid targets are: " + string.Join(", ", PythonScript.GetAvailableTargets()));
                return 1;
            }

            // Set load options
            var loadOptions = new LoadOptions {
                BinaryFilePath = options.BinaryFiles.First()
            };

            // Check image base
            if (!string.IsNullOrEmpty(options.ElfImageBaseString)) {
                try {
                    loadOptions.ImageBase = Convert.ToUInt64(options.ElfImageBaseString, 16);
                } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
                    Console.Error.WriteLine("Image base must be a 32 or 64-bit hex value (optionally starting with '0x')");
                    return 1;
                }
            }

            // Check Unity asset
            if (options.UnityVersionAsset != null) {
                try {
                    options.UnityVersion = UnityVersion.FromAssetFile(options.UnityVersionAsset);

                    Console.WriteLine("Unity asset file has version " + options.UnityVersion);
                }
                catch (FileNotFoundException) {
                    Console.Error.WriteLine($"Unity asset file {options.UnityVersionAsset} does not exist");
                    return 1;
                } catch (ArgumentException) {
                    Console.Error.WriteLine("Could not determine Unity version from asset file - ignoring");
                }
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

            // Set plugin handlers
            PluginManager.ErrorHandler += (s, e) => {
                Console.Error.WriteLine($"The plugin {e.Error.Plugin.Name} encountered an error while executing {e.Error.Operation}: {e.Error.Exception.Message}."
                                            + " Plugin has been disabled.");
            };

            PluginManager.StatusHandler += (s, e) => {
                Console.WriteLine("Plugin " + e.Plugin.Name + ": " + e.Text);
            };

            // Check that specified binary files exist
            foreach (var file in options.BinaryFiles)
                if (!File.Exists(file)) {
                    Console.Error.WriteLine($"File {file} does not exist");
                    return 1;
                }

            // Check files exist and determine whether they're archives or not
            bool isExtractedFromPackage = false;
            List<Il2CppInspector> il2cppInspectors;
            using (new Benchmark("Analyze IL2CPP data")) {

                try {
                    il2cppInspectors = Il2CppInspector.LoadFromPackage(options.BinaryFiles, loadOptions);
                    isExtractedFromPackage = true;
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex.Message);
                    return 1;
                }

                if (il2cppInspectors == null) {
                    isExtractedFromPackage = false;

                    if (!File.Exists(options.MetadataFile)) {
                        Console.Error.WriteLine($"File {options.MetadataFile} does not exist");
                        return 1;
                    }

                    try {
                        il2cppInspectors = Il2CppInspector.LoadFromFile(options.BinaryFiles.First(), options.MetadataFile, loadOptions);
                    }
                    catch (Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                        return 1;
                    }
                }
            }

            if (il2cppInspectors == null)
                Environment.Exit(1);

            // Save metadata and binary if extracted or modified and save requested
            if (!string.IsNullOrEmpty(options.MetadataFileOut)) {
                if (isExtractedFromPackage || il2cppInspectors[0].Metadata.IsModified) {
                    Console.WriteLine($"Saving metadata file to {options.MetadataFileOut}");

                    il2cppInspectors[0].SaveMetadataToFile(options.MetadataFileOut);
                } else
                    Console.WriteLine("Metadata file was not modified - skipping save");
            }

            if (!string.IsNullOrEmpty(options.BinaryFileOut)) {
                var outputIndex = 0;
                foreach (var il2cpp in il2cppInspectors) {
                    // If there's an extension, strip the leading period
                    var ext = Path.GetExtension(options.BinaryFileOut);
                    if (ext.Length > 0)
                        ext = ext.Substring(1);
                    var outPath = getOutputPath(options.BinaryFileOut, ext, outputIndex);

                    if (isExtractedFromPackage || il2cpp.Binary.IsModified) {
                        Console.WriteLine($"Saving binary file to {outPath}");

                        il2cpp.SaveBinaryToFile(outPath);
                    } else
                        Console.WriteLine("Binary file was not modified - skipping save");

                    outputIndex++;
                }
            }

            // Determine which outputs to generate
            var GenerateCS = !string.IsNullOrEmpty(options.CSharpOutPath);
            var GeneratePython = !string.IsNullOrEmpty(options.PythonOutFile);
            var GenerateCpp = !string.IsNullOrEmpty(options.CppOutPath) || GeneratePython;
            var GenerateJSON = !string.IsNullOrEmpty(options.JsonOutPath) || GeneratePython;
            var GenerateDLL = !string.IsNullOrEmpty(options.DllOutPath);

            if (!options.SpecifiedOutputsOnly)
                GenerateCS = GeneratePython = GenerateCpp = GenerateJSON = GenerateDLL = true;

            // Set defaults for outputs where the user hasn't specified a path
            options.CSharpOutPath = options.CSharpOutPath ?? CsOutDefault;
            options.PythonOutFile = options.PythonOutFile ?? PyOutDefault;
            options.CppOutPath = options.CppOutPath ?? CppOutDefault;
            options.JsonOutPath = options.JsonOutPath ?? JsonOutDefault;
            options.DllOutPath = options.DllOutPath ?? DllOutDefault;

            var NeedAppModel = GeneratePython | GenerateJSON | GenerateCpp;

            // Write output files for each binary
            int imageIndex = 0;
            foreach (var il2cpp in il2cppInspectors) {
                Console.WriteLine($"Processing image {imageIndex} - {il2cpp.BinaryImage.Arch} / {il2cpp.BinaryImage.Bits}-bit");

                // Create type model
                TypeModel model;
                using (new Benchmark("Create .NET type model"))
                    model = new TypeModel(il2cpp);

                // Create application model only if needed
                AppModel appModel = null;
                if (NeedAppModel)
                    using (new Benchmark("Create C++ application model")) {
                        appModel = new AppModel(model, makeDefaultBuild: false).Build(options.UnityVersion, options.CppCompiler);
                    }

                // C# signatures output
                if (GenerateCS)
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
                if (GenerateCpp)
                    using (new Benchmark("Generate C++ code")) {
                        new CppScaffolding(appModel).Write(getOutputPath(options.CppOutPath, "", imageIndex));
                    }

                // JSON output
                if (GenerateJSON)
                    using (new Benchmark("Generate JSON metadata")) {
                        new JSONMetadata(appModel).Write(getOutputPath(options.JsonOutPath, "json", imageIndex));
                    }

                // Python script output
                if (GeneratePython)
                    using (new Benchmark($"Generate {options.ScriptTarget} Python script")) {
                        new PythonScript(appModel).WriteScriptToFile(
                            getOutputPath(options.PythonOutFile, "py", imageIndex),
                            options.ScriptTarget,
                            Path.Combine(getOutputPath(options.CppOutPath, "", imageIndex), "appdata/il2cpp-types.h"),
                            getOutputPath(options.JsonOutPath, "json", imageIndex));
                    }

                // DLL output
                if (GenerateDLL)
                    using (new Benchmark("Generate .NET assembly shim DLLs"))
                        new AssemblyShims(model) {
                            SuppressMetadata = options.SuppressDllMetadata
                        }
                        .Write(getOutputPath(options.DllOutPath, "", imageIndex));

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
