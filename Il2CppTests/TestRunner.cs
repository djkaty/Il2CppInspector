/*
    Copyright 2019-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Il2CppInspector.Cpp;
using Il2CppInspector.Model;
using Il2CppInspector.Outputs;
using Il2CppInspector.Reflection;
using NUnit.Framework;

namespace Il2CppInspector
{
    internal class Benchmark : IDisposable
    {
        private readonly Stopwatch timer = new Stopwatch();
        private readonly string benchmarkName;

        public Benchmark(string benchmarkName) {
            this.benchmarkName = benchmarkName;
            timer.Start();
        }

        public void Dispose() {
            timer.Stop();
            Console.WriteLine($"{benchmarkName}: {timer.Elapsed.TotalSeconds:N2} sec");
        }
    }

    [TestFixture]
    public partial class TestRunner
    {
        private void runTest(string testPath, LoadOptions loadOptions = null) {
            // Android
            var testFile = testPath + @"\" + Path.GetFileName(testPath) + ".so";
            if (!File.Exists(testFile))
                testFile = testPath + @"\libil2cpp.so";
            // Windows
            if (!File.Exists(testFile))
                testFile = testPath + @"\" + Path.GetFileName(testPath) + ".dll";
            if (!File.Exists(testFile))
                testFile = testPath + @"\GameAssembly.dll";
            // iOS
            if (!File.Exists(testFile))
                testFile = testPath + @"\" + Path.GetFileName(testPath);
            // Linux process map
            if (!File.Exists(testFile))
                testFile = Directory.GetFiles(testPath, "*-maps.txt").FirstOrDefault();

            // Set load options
            if (loadOptions == null)
                loadOptions = new LoadOptions();

            loadOptions.BinaryFilePath = testFile;

            List<Il2CppInspector> inspectors;
            using (new Benchmark("Load IL2CPP metadata and binary"))
                inspectors = Il2CppInspector.LoadFromFile(testFile, testPath + @"\global-metadata.dat", loadOptions);

            // If null here, there was a problem parsing the files
            if (inspectors == null)
                throw new Exception("Could not understand IL2CPP binary or metadata");

            if (inspectors.Count == 0)
                throw new Exception("Could not find any images in the IL2CPP binary");

            // Dump each image in the binary separately

            Parallel.ForEach(inspectors, il2cpp => {
                TypeModel model;
                using (new Benchmark("Create .NET type model"))
                    model = new TypeModel(il2cpp);

                AppModel appModel;
                using (new Benchmark("Create application model"))
                    appModel = new AppModel(model, makeDefaultBuild: false).Build(compiler: CppCompilerType.MSVC);
                
                var nameSuffix = "-" + il2cpp.BinaryImage.Arch.ToLower();

                using (new Benchmark("Create C# code stubs"))
                    new CSharpCodeStubs(model) {
                        ExcludedNamespaces = Constants.DefaultExcludedNamespaces,
                        SuppressMetadata = false,
                        MustCompile = true
                    }.WriteSingleFile(testPath + $@"\test-result{nameSuffix}.cs");

                using (new Benchmark("Create JSON metadata"))
                    new JSONMetadata(appModel)
                        .Write(testPath + $@"\test-result{nameSuffix}.json");

                using (new Benchmark("Create C++ scaffolding"))
                    new CppScaffolding(appModel)
                        .Write(testPath + $@"\test-cpp-result{nameSuffix}");

                var python = new PythonScript(appModel);
                foreach (var target in PythonScript.GetAvailableTargets())
                    python.WriteScriptToFile(testPath + $@"\test-{target.ToLower()}{nameSuffix}.py", target,
                        testPath + $@"\test-cpp-result{nameSuffix}\appdata\il2cpp-types.h",
                        testPath + $@"\test-result{nameSuffix}.json");
            });

            // Compare test results with expected results
            using var _ = new Benchmark("Compare files");
            for (var i = 0; i < inspectors.Count; i++) {
                var suffix = (i > 0 ? "-" + i : "");

                compareFiles(testPath, suffix + ".cs", $"test-result{suffix}.cs");
                compareFiles(testPath, suffix + ".json", $"test-result{suffix}.json");
                compareFiles(testPath, suffix + ".h", $@"test-cpp-result{suffix}\appdata\il2cpp-types.h");
            }
        }

        // We have to pass testPath rather than storing it as a field so that tests can be parallelized
        private void compareFiles(string testPath, string expectedFilenameSuffix, string actualFilename) {
            var expected = File.ReadAllLines(testPath + @"\..\..\TestExpectedResults\" + Path.GetFileName(testPath) + expectedFilenameSuffix);
            var actual = File.ReadAllLines(testPath + @"\" + actualFilename);

            var extraInExpected = expected.Except(actual);
            var extraInActual = actual.Except(expected);

            string failureMessage = string.Empty;
            if (extraInActual.Any() || extraInExpected.Any())
                failureMessage =
                  $"\n\nExtra in actual ({extraInActual.Count()}):\n\n" + string.Join("\n", extraInActual.Take(100))
                + $"\n\nExtra in expected ({extraInExpected.Count()}):\n\n" + string.Join("\n", extraInExpected.Take(100));

            // We don't use Linq to strip whitespace lines or CollectionAssert to compare,
            // as we want to be able to determine the exact line number of the first mismatch
            for (int expLine = 0, actLine = 0; expLine < expected.Length || actLine < actual.Length; expLine++, actLine++) {
                while (expLine < expected.Length && string.IsNullOrWhiteSpace(expected[expLine]))
                    expLine++;
                while (actLine < actual.Length && string.IsNullOrWhiteSpace(actual[actLine]))
                    actLine++;

                if (expLine < expected.Length && actLine < actual.Length)
                    Assert.AreEqual(expected[expLine], actual[actLine], $"Mismatch at line {expLine + 1} / {actLine + 1} in {actualFilename}{failureMessage}\n");
            }
        }
    }
}
