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
            var app = new AppModel(model).Build();

            // Assert

            // Check VTable offset is accurate
            Assert.AreEqual(0x130, app.GetVTableOffset());

            // BufferedStream.Flush()
            Assert.AreEqual(11, app.GetVTableIndexFromClassOffset(0x1E0));

            var vtable = model.GetType("System.IO.BufferedStream").GetVTable();

            Assert.AreEqual("get_CanWrite", vtable[app.GetVTableIndexFromClassOffset(0x1E0)].Name);

            var method = app.Methods.Values.First(m => m.MethodCodeAddress == 0x7C94D4);
            Assert.AreEqual("Flush", method.Method.Name);
            Assert.AreEqual("System.IO.BufferedStream", method.Method.DeclaringType.FullName);
        }
    }
}
