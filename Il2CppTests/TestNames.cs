/*
    Copyright 2019 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.IO;
using Il2CppInspector.Reflection;
using NUnit.Framework;

namespace Il2CppInspector
{
    [TestFixture]
    public partial class FixedTests 
    {
        [Test]
        public void TestNames() {

            // Arrange
            // We're currently in IlCppTests\bin\Debug\netcoreapp3.0 or similar
            var testPath = Path.GetFullPath(Directory.GetCurrentDirectory() + @"\..\..\..\TestBinaries\References-ARMv7");

            // Build model
            var inspectors = Il2CppInspector.LoadFromFile(testPath + @"\References-ARMv7.so", testPath + @"\global-metadata.dat");
            var model = new TypeModel(inspectors[0]);

            var asm = model.GetAssembly("References.dll");

            // Act
            var t = asm.GetType("Il2CppTests.TestSources.Test");
            var m1 = t.GetMethod("MethodWithGenericAndClassRefs");
            var m2 = t.GetMethod("MethodWithInRefOut");
            var p1 = m1.DeclaredParameters;
            var p2 = m2.DeclaredParameters;

            // Assert
            var checks = new[] {
                (p1[0], "T&"),
                (p1[1], "Int32&"),
                (p1[2], "Test&"),

                (p2[0], "Int32&"),
                (p2[1], "Int32&"),
                (p2[2], "Int32&")
            };

            Assert.That(m1.ToString() == "Test& MethodWithGenericAndClassRefs[T](T ByRef, Int32 ByRef, Test ByRef)");
            Assert.That(m2.ToString() == "Void MethodWithInRefOut(Int32 ByRef, Int32 ByRef, Int32 ByRef)");

            foreach (var check in checks) {
                var c = check.Item1;

                Assert.That(c.ParameterType.Name, Is.EqualTo(check.Item2));
                Assert.That(c.ParameterType.IsByRef, Is.EqualTo(true));
                Assert.That(c.ParameterType.HasElementType, Is.EqualTo(true));
            }
        }
    }
}
