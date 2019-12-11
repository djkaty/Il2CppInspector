/*
    Copyright 2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

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
        // Check generic flags according to https://docs.microsoft.com/en-us/dotnet/api/system.type.isgenerictype?view=netframework-4.8
        [Test]
        public void TestGenericTypes() {

            // Arrange
            // We're currently in IlCppTests\bin\Debug\netcoreapp3.0 or similar
            var testPath = Path.GetFullPath(Directory.GetCurrentDirectory() + @"\..\..\..\TestBinaries\GenericTypes");

            // Build model
            var inspectors = Il2CppInspector.LoadFromFile(testPath + @"\GenericTypes.so", testPath + @"\global-metadata.dat");
            var model = new Il2CppModel(inspectors[0]);

            var asm = model.GetAssembly("GenericTypes.dll");

            // Act
            TypeInfo tDerived = asm.GetType("Il2CppTests.TestSources.Derived`1");
            TypeInfo tDerivedBase = tDerived.BaseType;

            // TODO: array of Derived<int>
            // TypeInfo tDerivedArray

            TypeInfo tT = asm.GetType("Il2CppTests.TestSources.Base`2").GenericTypeParameters[0];
            TypeInfo tF = tDerived.GetField("F").FieldType;
            TypeInfo tNested = asm.GetType("Il2CppTests.TestSources.Derived`1+Nested");

            DisplayGenericType(tDerived, "Derived<V>");
            DisplayGenericType(tDerivedBase, "Base type of Derived<V>");
            //DisplayGenericType(tDerivedArray, "Array of Derived<int>");
            DisplayGenericType(tT, "Type parameter T from Base<T>");
            DisplayGenericType(tF, "Field type, G<Derived<V>>");
            DisplayGenericType(tNested, "Nested type in Derived<V>");

            // Assert
            var checks = new[] {
                (tDerived, "Derived`1[V]", true, true, true, false),
                (tDerivedBase, "Base`2[System.String,V]", true, false, true, false),
                //(tDerivedArray, "Derived`1[System.Int32][]", false, false, false, false),
                (tT, "T", false, false, true, true),
                (tF, "G`1[Derived`1[V]]", true, false, true, false),
                (tNested, "Derived`1[V]+Nested[V]", true, true, true, false)
            };

            foreach (var check in checks) {
                var t = check.Item1;

                Assert.That(t.ToString(), Is.EqualTo(check.Item2));
                Assert.That(t.IsGenericType, Is.EqualTo(check.Item3));
                Assert.That(t.IsGenericTypeDefinition, Is.EqualTo(check.Item4));
                Assert.That(t.ContainsGenericParameters, Is.EqualTo(check.Item5));
                Assert.That(t.IsGenericParameter, Is.EqualTo(check.Item6));
            }
        }

        private void DisplayGenericType(TypeInfo t, string caption) {
            Console.WriteLine("\n{0}", caption);
            Console.WriteLine("    Type: {0}", t);

            Console.WriteLine("\t            IsGenericType: {0}", t.IsGenericType);
            Console.WriteLine("\t  IsGenericTypeDefinition: {0}", t.IsGenericTypeDefinition);
            Console.WriteLine("\tContainsGenericParameters: {0}", t.ContainsGenericParameters);
            Console.WriteLine("\t       IsGenericParameter: {0}", t.IsGenericParameter);
        }
    }
}
