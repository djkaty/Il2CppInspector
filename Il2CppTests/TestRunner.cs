/*
    Copyright 2019-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.IO;
using System.Linq;
using Il2CppInspector.Cpp;
using Il2CppInspector.Model;
using Il2CppInspector.Outputs;
using Il2CppInspector.Reflection;
using NUnit.Framework;

namespace Il2CppInspector
{
    [TestFixture]
    public partial class TestRunner
    {
        private void runTest(string testPath) {
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

            var inspectors = Il2CppInspector.LoadFromFile(testFile, testPath + @"\global-metadata.dat");

            // If null here, there was a problem parsing the files
            if (inspectors == null)
                throw new Exception("Could not understand IL2CPP binary or metadata");

            if (inspectors.Count == 0)
                throw new Exception("Could not find any images in the IL2CPP binary");

            // Dump each image in the binary separately
            int i = 0;
            foreach (var il2cpp in inspectors) {
                var model = new TypeModel(il2cpp);
                var appModel = new AppModel(model, makeDefaultBuild: false).Build(compiler: CppCompilerType.MSVC);
                var nameSuffix = i++ > 0 ? "-" + (i - 1) : "";

                new CSharpCodeStubs(model) {
                    ExcludedNamespaces = Constants.DefaultExcludedNamespaces,
                    SuppressMetadata = false,
                    MustCompile = true
                }.WriteSingleFile(testPath + $@"\test-result{nameSuffix}.cs");

                new JSONMetadata(appModel)
                    .Write(testPath + $@"\test-result{nameSuffix}.json");

                new CppScaffolding(appModel)
                    .Write(testPath + $@"\test-cpp-result{nameSuffix}");
            }

            // Compare test results with expected results
            for (i = 0; i < inspectors.Count; i++) {
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
