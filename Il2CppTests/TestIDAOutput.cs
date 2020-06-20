/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System.IO;
using Il2CppInspector.Outputs.UnityHeaders;
using Il2CppInspector.Reflection;
using NUnit.Framework;

namespace Il2CppInspector
{
    [TestFixture]
    public partial class FixedTests
    {
        [Test]
        public void TestVersions() {
            Assert.That(UnityHeader.GetHeaderForVersion("5.3.1p4").ToString(), Is.EqualTo("5.3.0 - 5.3.1"));
            Assert.That(UnityHeader.GetHeaderForVersion("5.6.4").ToString(), Is.EqualTo("5.6.0 - 5.6.7"));
            Assert.That(new UnityVersion("2020.1.0b5").ToString(), Is.EqualTo("2020.1.0b5"));
            Assert.That(new UnityVersion("2020.1").ToString(), Is.EqualTo("2020.1.0"));
            Assert.That(new UnityVersion("5.3.1").CompareTo("5.3.1p4") == 0);
            Assert.That(new UnityVersion("5.3.1rc0").CompareTo("5.3.1p2") < 0);
            Assert.That(new UnityVersion("5.3.1f1").CompareTo("5.3.1p0") < 0);
        }
    }
}
