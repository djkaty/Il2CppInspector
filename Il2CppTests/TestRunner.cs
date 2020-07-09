/*
    Copyright 2019-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.IO;
using System.Linq;
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
                var appModel = new AppModel(model).Build();
                var nameSuffix = i++ > 0 ? "-" + (i - 1) : "";

                new CSharpCodeStubs(model) {
                    ExcludedNamespaces = Constants.DefaultExcludedNamespaces,
                    SuppressMetadata = false,
                    MustCompile = true
                }.WriteSingleFile(testPath + $@"\test-result{nameSuffix}.cs");

                new IDAPythonScript(appModel)
                    .WriteScriptToFile(testPath + $@"\test-ida-result{nameSuffix}.py");

                new CppScaffolding(appModel)
                    .WriteCppToFile(testPath + $@"\test-result{nameSuffix}.h");
            }

            // Compare test results with expected results
            for (i = 0; i < inspectors.Count; i++) {
                var suffix = (i > 0 ? "-" + i : "");

                compareFiles(testPath, suffix + ".cs", $"test-result{suffix}.cs");
                compareFiles(testPath, suffix + ".h", $"test-result{suffix}.h");
                compareFiles(testPath, suffix + ".py", $"test-ida-result{suffix}.py");
            }
        }

        // We have to pass testPath rather than storing it as a field so that tests can be parallelized
        private void compareFiles(string testPath, string expectedFilenameSuffix, string actualFilename) {
            var expected = File.ReadAllLines(testPath + @"\..\..\TestExpectedResults\" + Path.GetFileName(testPath) + expectedFilenameSuffix);
            var actual = File.ReadAllLines(testPath + @"\" + actualFilename);

            // Get rid of blank lines and trim the remaining lines
            expected = (from l in expected where !string.IsNullOrWhiteSpace(l) select l.Trim()).ToArray();
            actual = (from l in actual where !string.IsNullOrWhiteSpace(l) select l.Trim()).ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
