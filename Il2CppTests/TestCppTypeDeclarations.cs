/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

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
                var cppUnityHeaderTypes = new CppTypeCollection(64);
                cppUnityHeaderTypes.AddFromDeclarationText(unityTypeHeader.GetText());

                foreach (var cppType in cppUnityHeaderTypes.Types)
                    Debug.WriteLine("// " + cppType.Key + "\n" + cppType.Value.ToString("o"));
            }

            // Do a few sanity checks taken from real applications (64-bit)
            // NOTE: Does not provide full code coverage!

            var cppTypes = CppTypeCollection.FromUnityVersion(new UnityVersion("2019.3.1f1"));

            CppComplexType ct;
            CppField field;

            // Field offset tests

            // Un-nested class
            ct = (CppComplexType) cppTypes["Il2CppClass"];

            field = ct[0xE0].First();

            Assert.AreEqual("cctor_finished", field.Name);

            field = ct[0x130].First();

            Assert.AreEqual("vtable", field.Name);

            field = ct["cctor_finished"];

            Assert.AreEqual(0xE0, field.OffsetBytes);

            field = ct["vtable"];

            Assert.AreEqual(0x130, field.OffsetBytes);

            // Nested class
            ct = (CppComplexType) cppTypes["Il2CppClass_Merged"];
            var fields = ct.Flattened;

            field = fields[0xE0].First();

            Assert.AreEqual("cctor_finished", field.Name);

            field = fields[0x130].First();

            Assert.AreEqual("vtable", field.Name);

            field = fields["cctor_finished"];

            Assert.AreEqual(0xE0, field.OffsetBytes);

            field = fields["vtable"];

            Assert.AreEqual(0x130, field.OffsetBytes);

            // Bitfield
            ct = (CppComplexType) cppTypes["Il2CppType"];

            field = ct.Fields[0xB * 8 + 7].First();

            Assert.AreEqual("pinned", field.Name);

            // Nested fields
            ct = (CppComplexType) cppTypes["Il2CppWin32Decimal"];
            fields = ct.Flattened;

            field = fields[0x08].First();

            Assert.AreEqual("lo32", field.Name);

            field = fields[0x08].Last();

            Assert.AreEqual("lo64", field.Name);

            field = fields[0x0C].First();

            Assert.AreEqual("mid32", field.Name);

            // Pointer alias
            var alias = (CppAlias) cppTypes.GetType("Il2CppHString");

            Assert.AreEqual(typeof(CppPointerType), alias.ElementType.GetType());
            Assert.AreEqual("Il2CppHString__ *", alias.ElementType.Name);

            // Typedef struct with no tag
            Assert.True(cppTypes.Types.ContainsKey("Il2CppGenericMethodIndices"));
            Assert.True(((CppComplexType)cppTypes["Il2CppGenericMethodIndices"]).ComplexValueType == ComplexValueType.Struct);

            // Alignment tests

            // Il2CppArrayType has an int * (sizes) after three uint8_t
            Assert.AreEqual(0x10, cppTypes.GetComplexType("Il2CppArrayType")["sizes"].OffsetBytes);

            // Il2CppSafeArray has a uint32_t (element_size) after two uint16_t
            Assert.AreEqual(0x4, cppTypes.GetComplexType("Il2CppSafeArray")["element_size"].OffsetBytes);
        }
    }
}
