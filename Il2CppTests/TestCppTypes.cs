/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Reflection;
using NUnit.Framework;

namespace Il2CppInspector
{
    [TestFixture]
    public partial class FixedTests 
    {
        [Test]
        public void TestCppTypes() {
            // NOTE: This test doesn't check for correct results, only that parsing doesn't fail!

            var unityAllHeaders = UnityHeader.GetAllHeaders();

            // Ensure we have read the embedded assembly resources
            Assert.IsTrue(unityAllHeaders.Any());

            // Ensure we can interpret every header from every version of Unity without errors
            // This will throw InvalidOperationException if there is a problem
            foreach (var unityHeader in unityAllHeaders) {
                var cppTypes = CppTypes.FromUnityHeaders(unityHeader);

                foreach (var cppType in cppTypes.Types)
                    Debug.WriteLine("// " + cppType.Key + "\n" + cppType.Value + "\n");
            }

            // Do a few sanity checks taken from real applications
            // NOTE: Does not provide full code coverage!

            var cppTypes2 = CppTypes.FromUnityVersion(new UnityVersion("2019.3.1f1"), 64);

            CppComplexType ct;
            CppField field;

            // Un-nested class
            ct = (CppComplexType) cppTypes2["Il2CppClass"];

            field = ct[0xD8].First();

            Assert.AreEqual(field.Name, "cctor_finished");

            field = ct[0x128].First();

            Assert.AreEqual(field.Name, "vtable");

            field = ct["cctor_finished"];

            Assert.AreEqual(field.OffsetBytes, 0xD8);

            field = ct["vtable"];

            Assert.AreEqual(field.OffsetBytes, 0x128);

            // Nested class
            ct = (CppComplexType) cppTypes2["Il2CppClass_Merged"];
            var fields = ct.Flattened;

            field = fields[0xD8].First();

            Assert.AreEqual(field.Name, "cctor_finished");

            field = fields[0x128].First();

            Assert.AreEqual(field.Name, "vtable");

            field = fields["cctor_finished"];

            Assert.AreEqual(field.OffsetBytes, 0xD8);

            field = fields["vtable"];

            Assert.AreEqual(field.OffsetBytes, 0x128);
        }
    }
}
