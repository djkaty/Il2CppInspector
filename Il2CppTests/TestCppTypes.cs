/*
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        }
    }
}
