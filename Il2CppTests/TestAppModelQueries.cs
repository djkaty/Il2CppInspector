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
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using NUnit.Framework;

namespace Il2CppInspector
{
    [TestFixture]
    public partial class FixedTests 
    {
        // Tests for static analysis queries
        [Test]
        public void TestAppModelQueries() {
            // Arrange
            // We're currently in IlCppTests\bin\Debug\netcoreapp3.0 or similar
            var testPath = Path.GetFullPath(Directory.GetCurrentDirectory() + @"\..\..\..\TestBinaries\ArraysAndPointers-ARM64");

            // Act
            var inspectors = Il2CppInspector.LoadFromFile(testPath + @"\ArraysAndPointers-ARM64.so", testPath + @"\global-metadata.dat");
            var model = new TypeModel(inspectors[0]);
            var app = new AppModel(model);

            // Assert

            // Check VTable offset is accurate
            Assert.AreEqual(0x130, app.GetVTableOffset());

            // BufferedStream.Flush()
            Assert.AreEqual(11, app.GetVTableIndexFromClassOffset(0x1E0));

            var vtable = model.GetType("System.IO.BufferedStream").GetVTable();

            // Check vtable calculations are correct
            Assert.AreEqual("get_CanWrite", vtable[app.GetVTableIndexFromClassOffset(0x1E0)].Name);

            // Check method lookup is correct
            var method = app.Methods.Values.First(m => m.MethodCodeAddress == 0x7C94D4);
            Assert.AreEqual("Flush", method.Method.Name);
            Assert.AreEqual("System.IO.BufferedStream", method.Method.DeclaringType.FullName);

            // AsyncStateMachineAttribute CAG - 0x3B7C58 - Type from Il2CppType**
            // adrp x9,0xfca000 - ldr x9,[x9, #0x90] - ldr x0,[x9]

            // Check Il2CppType * lookup is correct via AppModel
            var typeRefPtr = (ulong) app.Image.ReadMappedWord(0xFCA090);
            var typeFromRef = app.Types.Values.First(t => t.TypeRefPtrAddress == typeRefPtr).Type;

            Assert.AreEqual("System.IO.StreamReader+<ReadAsyncInternal>d__65", typeFromRef.FullName);

            // Check Il2CppType * lookup is correct via AddressMap
            var map = app.GetAddressMap();
            var appTypeReference = map[typeRefPtr];
            Assert.AreEqual(typeof(AppTypeReference), appTypeReference.GetType());

            Assert.AreEqual(typeFromRef, ((AppTypeReference) appTypeReference).Type.Type);
            Assert.AreEqual(typeRefPtr, ((AppTypeReference) appTypeReference).Type.TypeRefPtrAddress);
        }
    }
}
