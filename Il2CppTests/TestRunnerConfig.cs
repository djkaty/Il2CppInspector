/*
    Copyright 2019-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector
{
    // Configuration options for test runner
    // Determines which integration tests to run
    public partial class TestRunner
    {
        public const bool GenerateCS = true;
        public const bool GenerateDLL = true;
        public const bool GenerateJSON = true;
        public const bool GenerateCpp = true;
        public const bool GeneratePython = true;

        public const bool EnableCompare = true;
    }
}
