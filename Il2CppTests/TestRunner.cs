/*
    Copyright 2019-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

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
        private async Task runTest(string testPath, LoadOptions loadOptions = null) {
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
            // XAPK (selects latest version assuming lexical order)
            if (testFile == null)
                testFile = Directory.GetFiles(testPath, "*.xapk").LastOrDefault();
            // APK (selects latest version assuming lexical order) (prefer XAPKs)
            if (testFile == null)
                testFile = Directory.GetFiles(testPath, "*.apk").LastOrDefault();

            // Set load options
            if (loadOptions == null)
                loadOptions = new LoadOptions();

            loadOptions.BinaryFilePath = testFile;

            // Set up plugins - place desired plugins in 'plugins' sub-folder of test folder
            PluginManager.Reload(testPath + @"\plugins");

            // Handlers for debugging output
            PluginManager.StatusHandler += (s, e) => {
                Console.WriteLine("Plugin " + e.Plugin.Name + ": " + e.Text);
            };

            PluginManager.ErrorHandler += (s, e) => {
                Assert.Fail($"{e.Error.Plugin.Name} throw an exception during {e.Error.Operation}: {e.Error.Exception.Message}.", e);
            };

            // Get plugin options - place desired options in <plugins-id>.options.txt for each plugin in test folder
            // in the format "key=value", one per line
            // Use %TESTDIR% to refer to the directory containing the test files
            foreach (var plugin in PluginManager.AvailablePlugins) {
                Console.WriteLine("Using plugin: " + plugin.Name);

                // Enable plugin
                var ourPlugin = PluginManager.Plugins[plugin.Id];
                ourPlugin.Enabled = true;

                // Look for options
                var optionsFile = $@"{testPath}\{plugin.Id}-options.txt";
                if (File.Exists(optionsFile)) {
                    var options = File.ReadAllLines(optionsFile);

                    // Parse options
                    foreach (var option in options) {
                        var kv = option.Split('=', 2);
                        if (kv.Length == 2) {
                            var key = kv[0].Trim();
                            var value = kv[1].Trim().Replace("%TESTDIR%", testPath);

                            Console.WriteLine($"Setting option: {key} = {value}");

                            // Null default values must be castable to object
                            var targetType = ourPlugin[key]?.GetType() ?? typeof(object);
                            ourPlugin[key] = Convert.ChangeType(value, targetType);
                        }
                    }
                }
                PluginManager.OptionsChanged(plugin);
            }

            List<Il2CppInspector> inspectors;
            using (new Benchmark("Load IL2CPP metadata and binary"))
                try {
                    inspectors = Il2CppInspector.LoadFromFile(testFile, testPath + @"\global-metadata.dat", loadOptions);
                } catch (FileNotFoundException) {
                    inspectors = Il2CppInspector.LoadFromPackage(new[] { testFile }, loadOptions);
                }

            // If null here, there was a problem parsing the files
            if (inspectors == null)
                throw new Exception("Could not understand IL2CPP binary or metadata");

            if (inspectors.Count == 0)
                throw new Exception("Could not find any images in the IL2CPP binary");

            // Dump each image in the binary separately
            var imageTasks = inspectors.Select((il2cpp, i) => Task.Run(async () =>
            {
                TypeModel model;
                using (new Benchmark("Create .NET type model"))
                    model = new TypeModel(il2cpp);

                var appModelTask = Task.Run(() => {
                    using (new Benchmark("Create application model"))
                        return new AppModel(model, makeDefaultBuild: false).Build(compiler: CppCompilerType.MSVC);
                });

                var nameSuffix = i++ > 0 ? "-" + (i - 1) : "";

                var compareTasks = new List<Task>();

                var csTask = Task.Run(() => {
                    using (new Benchmark("Create C# code stubs"))
                        new CSharpCodeStubs(model) {
                            ExcludedNamespaces = Constants.DefaultExcludedNamespaces,
                            SuppressMetadata = false,
                            MustCompile = true
                        }.WriteSingleFile(testPath + $@"\test-result{nameSuffix}.cs");

                    compareTasks.Add(Task.Run(() => compareFiles(testPath, nameSuffix + ".cs", $"test-result{nameSuffix}.cs")));
                });

                var appModel = await appModelTask;

                var jsonTask = Task.Run(() => {
                    using (new Benchmark("Create JSON metadata"))
                        new JSONMetadata(appModel)
                        .Write(testPath + $@"\test-result{nameSuffix}.json");

                    compareTasks.Add(Task.Run(() => compareFiles(testPath, nameSuffix + ".json", $"test-result{nameSuffix}.json")));
                });

                var cppTask = Task.Run(() => {
                    using (new Benchmark("Create C++ scaffolding"))
                        new CppScaffolding(appModel)
                        .Write(testPath + $@"\test-cpp-result{nameSuffix}");

                    compareTasks.Add(Task.Run(() => compareFiles(testPath, nameSuffix + ".h", $@"test-cpp-result{nameSuffix}\appdata\il2cpp-types.h")));
                });

                var pyTask = Task.Run(() => {
                    var python = new PythonScript(appModel);
                    foreach (var target in PythonScript.GetAvailableTargets())
                        python.WriteScriptToFile(testPath + $@"\test-{target.ToLower()}{nameSuffix}.py", target,
                        testPath + $@"\test-cpp-result{nameSuffix}\appdata\il2cpp-types.h",
                        testPath + $@"\test-result{nameSuffix}.json");
                });

                await Task.WhenAll(csTask, jsonTask, cppTask, pyTask);
                await Task.WhenAll(compareTasks);
            }));
            await Task.WhenAll(imageTasks);
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
