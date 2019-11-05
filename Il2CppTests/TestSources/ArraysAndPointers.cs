/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

namespace Il2CppTests.TestSources
{
#pragma warning disable CS0169
    internal unsafe class Test
    {
        private int[] foo;
        private int[] bar = new int[10];

        private struct fixedSizeArrayStruct
        {
            private fixed int fixedSizeArray[25];
        }

        private float[][] arrayOfArrays;
        private float[,] twoDimensionalArray;
        private float[,,] threeDimensionalArray;

        public int[] FooMethod(int[][] bar) => new int[20];

        public int[,] BarMethod(int[,,] baz) => new int[5, 6];

        private int*[] arrayOfPointer;
        private int** pointerToPointer;

        private float*[][,,][] confusedElephant;
    }
#pragma warning restore CS0169
}