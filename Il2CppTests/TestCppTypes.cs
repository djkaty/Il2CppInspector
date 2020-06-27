/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Diagnostics;
using System.IO;
using Il2CppInspector.CppUtils;
using Il2CppInspector.CppUtils.UnityHeaders;
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

            foreach (var cppType in cppTypes.Types)
                Debug.WriteLine(cppType.Key + ":\n" + cppType.Value + "\n");

            throw new NotImplementedException();
        }
    }
}
