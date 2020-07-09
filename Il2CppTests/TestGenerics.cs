/*
    Copyright 2019-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.IO;
using System.Linq;
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
            var testPath = Path.GetFullPath(Directory.GetCurrentDirectory() + @"\..\..\..\TestBinaries\GenericTypes-ARMv7");

            // Build model
            var inspectors = Il2CppInspector.LoadFromFile(testPath + @"\GenericTypes-ARMv7.so", testPath + @"\global-metadata.dat");
            var model = new TypeModel(inspectors[0]);

            var asm = model.GetAssembly("GenericTypes.dll");

            // Act
            TypeInfo tBase = asm.GetType("Il2CppTests.TestSources.Base`2");
            TypeInfo tDerived = asm.GetType("Il2CppTests.TestSources.Derived`1");
            TypeInfo tDerived_closed = model.GetType("Il2CppTests.TestSources.Derived`1[System.Int32]");
            TypeInfo tDerivedBase = tDerived.BaseType;
            TypeInfo tDerivedBase_closed = tDerived_closed.BaseType;
            TypeInfo tDerivedArray = model.GetType("Il2CppTests.TestSources.Derived`1[System.Int32][]");
            Assert.That(tDerivedArray, Is.EqualTo(tDerived_closed.MakeArrayType()));

            TypeInfo tT = tBase.GenericTypeParameters[0];
            TypeInfo tU = tBase.GenericTypeParameters[1];
            TypeInfo tF = tDerived.GetField("F").FieldType;
            TypeInfo tF_closed = tDerived_closed.GetField("F").FieldType;
            TypeInfo tNested = asm.GetType("Il2CppTests.TestSources.Derived`1+Nested");

            TypeInfo tNG = asm.GetType("Il2CppTests.TestSources.NonGeneric");
            TypeInfo tGCWM = asm.GetType("Il2CppTests.TestSources.GenericClassWithMethods`1");
            TypeInfo tCGM = asm.GetType("Il2CppTests.TestSources.CallGenericMethods");
            MethodInfo mGMDINGC = tNG.GetMethod("GenericMethodDefinitionInNonGenericClass");
            MethodInfo mNGMIGC = tGCWM.GetMethod("NonGenericMethodInGenericClass");
            MethodInfo mNGMIGC2 = tGCWM.GetMethod("NonGenericMethodInGenericClass2");
            MethodInfo mGMDIGC = tGCWM.GetMethod("GenericMethodDefinitionInGenericClass");
            MethodInfo mGMDIGC2 = tGCWM.GetMethod("GenericMethodDefinitionInGenericClass2");

            TypeInfo tConstrainedRefType = asm.GetType("Il2CppTests.TestSources.ConstrainedRefType`1");
            MethodInfo mMAMCM = tConstrainedRefType.GetMethod("MultipleArgumentsMultipleConstraintsMethod");
            TypeInfo tB = mMAMCM.GetGenericArguments()[0];
            TypeInfo tI = mMAMCM.GetGenericArguments()[1];

            MethodBase mGMDINGC_closed = model.GetGenericMethod(
                "Il2CppTests.TestSources.NonGeneric.GenericMethodDefinitionInNonGenericClass", model.GetType("System.Single"));
            MethodBase mNGMIGC_closed = model.GetGenericMethod(
                "Il2CppTests.TestSources.GenericClassWithMethods`1[System.Int32].NonGenericMethodInGenericClass");
            MethodBase mNGMIGC2_closed = model.GetGenericMethod(
                "Il2CppTests.TestSources.GenericClassWithMethods`1[System.Int32].NonGenericMethodInGenericClass2");
            MethodBase mGMDIGC_closed = model.GetGenericMethod(
                "Il2CppTests.TestSources.GenericClassWithMethods`1[System.Int32].GenericMethodDefinitionInGenericClass", model.GetType("System.Int32"));
            MethodBase mGMDIGC2_closed = model.GetGenericMethod(
                "Il2CppTests.TestSources.GenericClassWithMethods`1[System.Int32].GenericMethodDefinitionInGenericClass2", model.GetType("System.String"));

            DisplayGenericType(tBase, "Generic type definition Base<T, U>");
            DisplayGenericType(tDerived, "Derived<V>");
            DisplayGenericType(tDerivedBase, "Base type of Derived<V>");
            DisplayGenericType(tDerivedArray, "Array of Derived<int>");
            DisplayGenericType(tT, "Type parameter T from Base<T,U>");
            DisplayGenericType(tU, "Type parameter U from Base<T,U>");
            DisplayGenericType(tF, "Field type, G<Derived<V>>");
            DisplayGenericType(tNested, "Nested type in Derived<V>");

            // Assert
            var typeChecks = new[] {
                (tBase, "Base`2[T,U]", true, true, true, false, -1),
                (tDerived, "Derived`1[V]", true, true, true, false, -1),
                (tDerivedBase, "Base`2[System.String,V]", true, false, true, false, -1),
                (tDerivedBase_closed, "Base`2[System.String,System.Int32]", true, false, false, false, -1),
                (tDerivedArray, "Derived`1[System.Int32][]", false, false, false, false, -1),
                (tT, "T", false, false, true, true, 0),
                (tU, "U", false, false, true, true, 1),
                (tF, "G`1[Derived`1[V]]", true, false, true, false, -1),
                (tF_closed, "G`1[Derived`1[System.Int32]]", true, false, false, false, -1),
                (tNested, "Derived`1[V]+Nested[V]", true, true, true, false, -1),
                (tB, "B", false, false, true, true, 0),
                (tB.BaseType, "Derived`1[R]", true, false, true, false, -1),
                (tI, "I", false, false, true, true, 1),
                (tI.ImplementedInterfaces.ElementAt(1), "IEnumerable`1[R]", true, false, true, false, -1),
            };

            var methodChecks = new[] {
                (mGMDINGC, "Void GenericMethodDefinitionInNonGenericClass[T](T)", true, true, true, false),
                (mNGMIGC,  "Void NonGenericMethodInGenericClass(T)", false, true, false, false),
                (mNGMIGC2, "Void NonGenericMethodInGenericClass2()", false, true, false, false),
                (mGMDIGC,  "Void GenericMethodDefinitionInGenericClass[U](U)", true, true, true, false),
                (mGMDIGC2, "Void GenericMethodDefinitionInGenericClass2[U](T, U)", true, true, true, false),

                (mGMDINGC_closed, "Void GenericMethodDefinitionInNonGenericClass[Single](Single)", true, false, false, true),
                (mNGMIGC_closed,  "Void NonGenericMethodInGenericClass(Int32)", false, false, false, false),
                (mNGMIGC2_closed, "Void NonGenericMethodInGenericClass2()", false, false, false, false),
                (mGMDIGC_closed,  "Void GenericMethodDefinitionInGenericClass[Int32](Int32)", true, false, false, true),
                (mGMDIGC2_closed, "Void GenericMethodDefinitionInGenericClass2[String](Int32, String)", true, false, false, true),
            };

            foreach (var check in typeChecks) {
                var t = check.Item1;

                Assert.That(t.ToString(), Is.EqualTo(check.Item2));
                Assert.That(t.IsGenericType, Is.EqualTo(check.Item3));
                Assert.That(t.IsGenericTypeDefinition, Is.EqualTo(check.Item4));
                Assert.That(t.ContainsGenericParameters, Is.EqualTo(check.Item5));
                Assert.That(t.IsGenericParameter, Is.EqualTo(check.Item6));
                if (t.IsGenericParameter)
                    Assert.That(t.GenericParameterPosition, Is.EqualTo(check.Item7));
            }

            foreach (var check in methodChecks) {
                var m = check.Item1;

                Assert.That(m.ToString(), Is.EqualTo(check.Item2));
                Assert.That(m.IsGenericMethod, Is.EqualTo(check.Item3));
                Assert.That(m.ContainsGenericParameters, Is.EqualTo(check.Item4));
                Assert.That(m.IsGenericMethodDefinition, Is.EqualTo(check.Item5));
                Assert.That(m.IsConstructedGenericMethod, Is.EqualTo(check.Item6));
            }
        }

        private void DisplayGenericType(TypeInfo t, string caption) {
            Console.WriteLine("\n{0}", caption);
            Console.WriteLine("    Type: {0}", t);

            Console.WriteLine("\t            IsGenericType: {0}", t.IsGenericType);
            Console.WriteLine("\t  IsGenericTypeDefinition: {0}", t.IsGenericTypeDefinition);
            Console.WriteLine("\tContainsGenericParameters: {0}", t.ContainsGenericParameters);
            Console.WriteLine("\t       IsGenericParameter: {0}", t.IsGenericParameter);

            if (t.IsGenericParameter)
                Console.WriteLine("\t GenericParameterPosition: {0}", t.GenericParameterPosition);
        }
    }
}
