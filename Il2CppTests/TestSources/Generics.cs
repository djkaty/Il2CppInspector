/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

namespace Il2CppTests.TestSources
{
    // Generic class
    internal class GenericClass<T>
    {
        // Generic method using generic type parameter of class
        public void GenericMethodWithClassGenericTypeParameter(T v) { }
    }
}
