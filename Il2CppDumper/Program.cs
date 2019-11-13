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

            [Option('m', "metadata", Required = true, HelpText = "IL2CPP metadata file input", Default = "global-metadata.data")]
            public string MetadataFile { get; set; }

            [Option('c', "cs-out", Required = false, HelpText = "C# output file or path", Default = "types.cs")]
            public string CSharpOutPath { get; set; }

            [Option('p', "py-out", Required = false, Hidden = true, HelpText = "IDA Python script output file", Default = "ida.py")]
            public string PythonOutFile { get; set; }

            [Option("exclude-namespaces", Required = false, Separator = ',', HelpText = "Comma-separated list of namespaces to suppress in C# output, or 'none' to include all namespaces",
                Default = new [] {
                    "System",
                    "Unity",
                    "UnityEngine",
                    "UnityEngineInternal",
                    "Mono",
                    "Microsoft.Win32",
                })]
            public IEnumerable<string> ExcludedNamespaces { get; set; }

            [Option("no-suppress-cg", Required = false, HelpText = "Don't suppress C# generation of items with CompilerGenerated attribute", Default = false)]
            public bool DontSuppressCompilerGenerated { get; set; }
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
                new Il2CppCSharpDumper(model) {ExcludedNamespaces = options.ExcludedNamespaces.ToList(), SuppressGenerated = !options.DontSuppressCompilerGenerated}
                    .WriteSingleFile(options.CSharpOutPath + (i++ > 0 ? "-" + (i-1) : ""));

                // IDA Python script output
                // TODO: IDA Python script output
            }

            // Success exit code
            return 0;
        }
    }
}
