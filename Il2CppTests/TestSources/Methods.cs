/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Text;

namespace Il2CppTests.TestSources
{
    internal class Test
    {
        // A parameter-less method
        public void ParameterlessMethod() { }

        // Method with value type return type
        public int ValueTypeReturnMethod() => 0;

        // Method with reference type return type
        public StringBuilder ReferenceTypeReturnMethod() => new StringBuilder();
    }
}
