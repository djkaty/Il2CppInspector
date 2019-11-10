/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

namespace Il2CppTests.TestSources
{
#pragma warning disable CS0169
    internal class Test
    {
        private int[] foo;
        private int[] bar = new int[10];

        private struct fixedSizeArrayStruct
        {
            private unsafe fixed int fixedSizeArray[25];
        }

        private float[][] arrayOfArrays;
        private float[,] twoDimensionalArray;
        private float[,,] threeDimensionalArray;

        public int[] FooMethod(int[][] bar) => new int[20];

        public int[,] BarMethod(int[,,] baz) => new int[5, 6];

        // Unsafe fields
        private unsafe int*[] arrayOfPointer;
        private unsafe int** pointerToPointer;
        private unsafe float*[][,,][] confusedElephant;

        // Unsafe constructor
        public unsafe Test(int* u) {}

        // Unsafe delegate
        public unsafe delegate void OnUnsafe(int*ud);

        // Unsafe property
        public unsafe int* PointerProperty { get; set; }

        // Unsafe method (method with unsafe parameter)
        public unsafe void UnsafeMethod(int* unsafePointerArgument) {}

        // Unsafe method (method with unsafe return type)
        public unsafe int* UnsafeReturnMethod() => (int*) 0;

        // Unsafe method with both
        public unsafe int* UnsafeMethod2(int* i) => i;

        // Unsafe indexers
        public unsafe int* this[int i] => (int*) 0;
        public unsafe int this[int* p] => 0;
        public unsafe float* this[float* fp] => (float*) 0;

        // Unsafe generic type (unmanaged constraint introduced in C# 7.3)
        public class NestedUnsafe<T> where T : unmanaged
        {
            unsafe T* UnsafeGenericReturn() => null;
            unsafe void UnsafeGenericMethod(T* pt) {}
        }
    }
#pragma warning restore CS0169
}