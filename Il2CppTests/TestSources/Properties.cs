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

        // Read-only ndexers
        public string this[int i] => "foo";
        public string this[double d] => "bar";

        // Write-only indexer
        public string this[long l] { set {} }

        // Read/write indexer
        public string this[float f] { get => "baz"; set {} }

        // Multi-dimensional indexer
        public bool this[int i, int j] => true;
    }
}
