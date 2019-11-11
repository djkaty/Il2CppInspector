/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Text;

namespace Il2CppTests.TestSources
{
    internal class Test
    {
        // A virtual method
        public virtual void VirtualMethod() { }

        // Method with value type return type
        public double ValueTypeReturnMethod() => 1.DivideByXExtension(2.0f);

        // Method with reference type return type
        public StringBuilder ReferenceTypeReturnMethod() => new StringBuilder();
    }

    public static class TestExtension
    {
        // Extension method on int
        public static double DivideByXExtension(this int a, float x) => (double) a / x;
    }

    internal abstract class TestAbstract
    {
        public abstract void AbstractMethod();
    }

    internal class TestOverride : Test
    {
        public override void VirtualMethod() { }
    }

    internal class TestHideVirtual : Test
    {
        public new void VirtualMethod() { }
    }

    internal class TestHideOverride : TestOverride
    {
        public new void VirtualMethod() { }
    }

    internal class TestOverrideAbstract : TestAbstract
    {
        public override void AbstractMethod() { }
    }

    internal class TestHideAbstractOverride : TestOverrideAbstract
    {
        public new void AbstractMethod() { }
    }

    internal class TestHideVirtualAndNewVirtual : Test
    {
        public new virtual void VirtualMethod() { }
    }

    internal class TestHideOverrideAndNewVirtual : TestOverride
    {
        public new virtual void VirtualMethod() { }
    }

    internal abstract class TestAbstractNew : TestOverride
    {
        public abstract new void VirtualMethod();
    }

    internal class TestNewNonVirtualMethod : Test
    {
        public new int ValueTypeReturnMethod() => 1;
    }
}
