/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

namespace Il2CppTests.TestSources
{
    internal class Test
    {
        private int prop1 { get; set; }
        protected int prop2 { get; private set; }
        protected int prop3 { private get; set; }
        public static int prop4 { private get; set; }
    }
}
