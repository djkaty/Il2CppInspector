// Copyright (c) 2017-2019 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Il2CppInspector.Reflection;

namespace Il2CppInspector
{
    public class App
    {
        private class Options
        {
            [Option('i', "bin", Required = true, HelpText = "IL2CPP binary file input", Default = "libil2cpp.so")]
            public string BinaryFile { get; set; }

            [Option('m', "metadata", Required = true, HelpText = "IL2CPP metadata file input", Default = "global-metadata.dat")]
            public string MetadataFile { get; set; }

            [Option('c', "cs-out", Required = false, HelpText = "C# output file (when using single-file layout) or path (when using per namespace, assembly or class layout)", Default = "types.cs")]
            public string CSharpOutPath { get; set; }

            [Option('p', "py-out", Required = false, Hidden = true, HelpText = "IDA Python script output file", Default = "ida.py")]
            public string PythonOutFile { get; set; }

            [Option('e', "exclude-namespaces", Required = false, Separator = ',', HelpText = "Comma-separated list of namespaces to suppress in C# output, or 'none' to include all namespaces",
                Default = new [] {
                    "System",
                    "Unity",
                    "UnityEngine",
                    "UnityEngineInternal",
                    "Mono",
                    "Microsoft.Win32",
                })]
            public IEnumerable<string> ExcludedNamespaces { get; set; }

            [Option('l', "layout", Required = false, HelpText = "Partitioning of C# output ('single' = single file, 'namespace' = one file per namespace, 'assembly' = one file per assembly, 'class' = one file per class)", Default = "single")]
            public string LayoutSchema { get; set; }

            [Option('s', "sort", Required = false, HelpText = "Sort order of type definitions in C# output ('index' = by type definition index, 'name' = by type name). No effect when using file-per-class layout", Default = "index")]
            public string SortOrder { get; set; }

            [Option('f', "flatten", Required = false, HelpText = "Flatten the namespace hierarchy into a single folder rather than using per-namespace subfolders. Only used when layout is per-namespace or per-class", Default = false)]
            public bool FlattenHierarchy { get; set; }

            [Option('n', "suppress-metadata", Required = false, HelpText = "Diff tidying: suppress method pointers, field offsets and type indices from C# output. Useful for comparing two versions of a binary for changes with a diff tool", Default = false)]
            public bool SuppressMetadata { get; set; }

            [Option('g', "suppress-cg", Required = false, HelpText = "Compilation tidying: Suppress generation of C# code for items with CompilerGenerated attribute", Default = false)]
            public bool SuppressCompilerGenerated { get; set; }

            [Option('a', "comment-attributes", Required = false, HelpText = "Compilation tidying: comment out attributes without parameterless constructors or all-optional constructor arguments")]
            public bool CommentParameterizedAttributeConstructors { get; set; }
        }

        public static int Main(string[] args) =>
            Parser.Default.ParseArguments<Options>(args).MapResult(
                options => Run(options),
                _ => 1);

        private static int Run(Options options) {
            // Check excluded namespaces
            if (options.ExcludedNamespaces.Count() == 1 && options.ExcludedNamespaces.First().ToLower() == "none")
                options.ExcludedNamespaces = new List<string>();

            // Check files
            if (!File.Exists(options.BinaryFile)) {
                Console.Error.WriteLine($"File {options.BinaryFile} does not exist");
                return 1;
            }
            if (!File.Exists(options.MetadataFile)) {
                Console.Error.WriteLine($"File {options.MetadataFile} does not exist");
                return 1;
            }

            // Analyze data
            var il2cppInspectors = Il2CppInspector.LoadFromFile(options.BinaryFile, options.MetadataFile);
            if (il2cppInspectors == null)
                Environment.Exit(1);

            // Write output file
            int i = 0;
            foreach (var il2cpp in il2cppInspectors) {
                // Create model
                var model = new Il2CppModel(il2cpp);

                // C# signatures output
                var writer = new Il2CppCSharpDumper(model) {
                    ExcludedNamespaces = options.ExcludedNamespaces.ToList(),
                    SuppressGenerated = options.SuppressCompilerGenerated,
                    SuppressMetadata = options.SuppressMetadata,
                    CommentAttributes = options.CommentParameterizedAttributeConstructors
                };

                var imageSuffix = i++ > 0 ? "-" + (i - 1) : "";

                var csOut = options.CSharpOutPath;
                if (csOut.ToLower().EndsWith(".cs"))
                    csOut = csOut.Insert(csOut.Length - 3, imageSuffix);
                else
                    csOut += imageSuffix;

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
                        writer.WriteFilesByAssembly(csOut, t => t.Index);
                        break;
                    case ("assembly", "name"):
                        writer.WriteFilesByAssembly(csOut, t => t.Name);
                        break;

                    case ("class", _):
                        writer.WriteFilesByClass(csOut, options.FlattenHierarchy);
                        break;
                }

                // IDA Python script output
                // TODO: IDA Python script output
            }

            // Success exit code
            return 0;
        }
    }
}
