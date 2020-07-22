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
        public void TestCppTypeDeclarations() {
            // NOTE: This test doesn't check for correct results, only that parsing doesn't fail!

            var unityTypeHeaders = UnityHeaders.GetAllTypeHeaders();

            // Ensure we have read the embedded assembly resources
            Assert.IsTrue(unityTypeHeaders.Any());

            // Ensure we can interpret every header from every version of Unity without errors
            // This will throw InvalidOperationException if there is a problem
            foreach (var unityTypeHeader in unityTypeHeaders) {
                var cppTypes = new CppTypeCollection(64);
                cppTypes.AddFromDeclarationText(unityTypeHeader.GetText());

                foreach (var cppType in cppTypes.Types)
                    Debug.WriteLine("// " + cppType.Key + "\n" + cppType.Value.ToString("o"));
            }

            // Do a few sanity checks taken from real applications
            // NOTE: Does not provide full code coverage!

            var cppTypes2 = CppTypeCollection.FromUnityVersion(new UnityVersion("2019.3.1f1"));

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

            // Bitfield
            ct = (CppComplexType) cppTypes2["Il2CppType"];

            field = ct.Fields[0xB * 8 + 7].First();

            Assert.AreEqual(field.Name, "pinned");

            // Nested fields
            ct = (CppComplexType) cppTypes2["Il2CppWin32Decimal"];
            fields = ct.Flattened;

            field = fields[0x08].First();

            Assert.AreEqual(field.Name, "lo32");

            field = fields[0x08].Last();

            Assert.AreEqual(field.Name, "lo64");

            field = fields[0x0C].First();

            Assert.AreEqual(field.Name, "mid32");

            // Pointer alias
            var alias = (CppAlias) cppTypes2.GetType("Il2CppHString");

            Assert.AreEqual(alias.ElementType.GetType(), typeof(CppPointerType));
            Assert.AreEqual(alias.ElementType.Name, "Il2CppHString__ *");

            // Typedef struct with no tag
            Assert.True(cppTypes2.Types.ContainsKey("Il2CppGenericMethodIndices"));
            Assert.True(((CppComplexType)cppTypes2["Il2CppGenericMethodIndices"]).ComplexValueType == ComplexValueType.Struct);
        }
    }
}
