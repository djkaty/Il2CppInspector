/*
    Copyright 2019-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppInspector.Reflection;
using Il2CppInspector.Outputs;
using NUnit.Framework;

namespace Il2CppInspector
{
    [TestFixture]
    public partial class TestRunner
    {
        private void runTest(string testPath) {
            // Android
            var testFile = testPath + @"\" + Path.GetFileName(testPath) + ".so";
            // Windows
            if (!File.Exists(testFile))
                testFile = testPath + @"\" + Path.GetFileName(testPath) + ".dll";
            if (!File.Exists(testFile))
                testFile = testPath + @"\GameAssembly.dll";
            // iOS
            if (!File.Exists(testFile))
                testFile = testPath + @"\" + Path.GetFileName(testPath);
            // Android
            if (!File.Exists(testFile))
                testFile = testPath + @"\libil2cpp.so";

            var inspectors = Il2CppInspector.LoadFromFile(testFile, testPath + @"\global-metadata.dat");

            // If null here, there was a problem parsing the files
            if (inspectors == null)
                throw new Exception("Could not understand IL2CPP binary or metadata");

            if (inspectors.Count == 0)
                throw new Exception("Could not find any images in the IL2CPP binary");

            // Dump each image in the binary separately
            int i = 0;
            foreach (var il2cpp in inspectors) {
                var model = new Il2CppModel(il2cpp);
                var nameSuffix = i++ > 0 ? "-" + (i - 1) : "";

                new CSharpCodeStubs(model) {
                    ExcludedNamespaces = Constants.DefaultExcludedNamespaces,
                    SuppressMetadata = false,
                    MustCompile = true
                }.WriteSingleFile(testPath + $@"\test-result{nameSuffix}.cs");

                new IDAPythonScript(model)
                    .WriteScriptToFile(testPath + $@"\test-ida-result{nameSuffix}.py");

                new CppScaffolding(model)
                    .WriteCppToFile(testPath + $@"\test-result{nameSuffix}.h");
            }

            // Compare test result with expected result
            for (i = 0; i < inspectors.Count; i++) {
                var expected = File.ReadAllLines(testPath + @"\..\..\TestExpectedResults\" + Path.GetFileName(testPath) + (i > 0 ? "-" + i : "") + ".cs");
                var actual = File.ReadAllLines(testPath + @"\test-result" + (i > 0 ? "-" + i : "") + ".cs");

                // Get rid of blank lines and trim the remaining lines
                expected = (from l in expected where !string.IsNullOrWhiteSpace(l) select l.Trim()).ToArray();
                actual = (from l in actual where !string.IsNullOrWhiteSpace(l) select l.Trim()).ToArray();

                CollectionAssert.AreEqual(expected, actual);
            }
        }
    }
}
