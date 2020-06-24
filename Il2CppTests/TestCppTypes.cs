/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.IO;
using Il2CppInspector.Cpp;
using Il2CppInspector.Outputs.UnityHeaders;
using Il2CppInspector.Reflection;
using NUnit.Framework;

namespace Il2CppInspector
{
    [TestFixture]
    public partial class FixedTests 
    {
        [Test]
        public void TestCppTypes() {
            // TODO: Flesh out CppTypes test

            var cppTypes = CppTypes.FromUnityHeaders(new UnityVersion("2019.3.1f1"));

            throw new NotImplementedException();
        }
    }
}
