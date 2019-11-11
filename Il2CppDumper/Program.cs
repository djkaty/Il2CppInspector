// Copyright (c) 2017-2019 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppInspector.Reflection;
using Microsoft.Extensions.Configuration;

namespace Il2CppInspector
{
    public class App
    {
        static void Main(string[] args) {

            // Banner
            Console.WriteLine("Il2CppDumper");
            Console.WriteLine("(c) 2017-2019 Katy Coe - www.djkaty.com");
            Console.WriteLine("");

            // Command-line usage: dotnet run [--bin=<binary-file>] [--metadata=<metadata-file>] [--cs-out=<output-file>] [--py-out=<output-file>] [--exclude-namespaces=<ns1,n2,...>|none] [--suppress-compiler-generated=false]
            // Defaults to libil2cpp.so or GameAssembly.dll if binary file not specified
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();

            string imageFile = config["bin"] ?? "libil2cpp.so";
            string metaFile = config["metadata"] ?? "global-metadata.dat";
            string outCsFile = config["cs-out"] ?? "types.cs";
            string outPythonFile = config["py-out"] ?? "ida.py";
            if (!bool.TryParse(config["suppress-compiler-generated"], out var suppressGenerated))
                suppressGenerated = true;

            // Exclusions
            var excludedNamespaces = config["exclude-namespaces"]?.Split(',').ToList() ?? 
            new List<string> {
                "System",
                "Unity",
                "UnityEngine",
                "UnityEngineInternal",
                "Mono",
                "Microsoft.Win32",
            };

            if (excludedNamespaces.Count == 1 && excludedNamespaces[0].ToLower() == "none")
                excludedNamespaces = null;

            // Check files
            if (!File.Exists(imageFile)) {
                Console.Error.WriteLine($"File {imageFile} does not exist");
                Environment.Exit(1);
            }
            if (!File.Exists(metaFile)) {
                Console.Error.WriteLine($"File {metaFile} does not exist");
                Environment.Exit(1);
            }

            // Analyze data
            var il2cppInspectors = Il2CppInspector.LoadFromFile(imageFile, metaFile);
            if (il2cppInspectors == null)
                Environment.Exit(1);

            // Write output file
            int i = 0;
            foreach (var il2cpp in il2cppInspectors) {
                // Create model
                var model = new Il2CppModel(il2cpp);

                // C# signatures output
                new Il2CppCSharpDumper(model) {ExcludedNamespaces = excludedNamespaces, SuppressGenerated = suppressGenerated}
                    .WriteFile(outCsFile + (i++ > 0 ? "-" + (i-1) : ""));

                // IDA Python script output
                // TODO: IDA Python script output
            }
        }
    }
}
