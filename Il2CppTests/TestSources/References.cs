/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

namespace Il2CppTests.TestSources
{
#pragma warning disable CS0169
    internal class Test
    {
        private float floatField;

        public void MethodWithRefParameters(int a, ref int b, int c, ref int d) {}

        // In parameters were introduced in C# 7.2
        public void MethodWithInRefOut(in int a, ref int b, out int c) => c = 1;

        public ref float MethodWithRefReturnType() => ref floatField;

        // Reference to generic type will require a new type to be created
        // Reference to reference type
        // Reference to reference type return type
        private Test test;
        public ref Test MethodWithGenericAndClassRefs<T>(ref T argGeneric, ref int argValueType, ref Test argClass) => ref test;
    }

    // Ref structs were introduced in C# 7.2 - creates IsByRefLike attribute on type in assembly
    // Attribute doesn't seem to be retained by IL2CPP?
    public ref struct RefStruct
    {
        private int structField1;
    }
#pragma warning restore CS0169
}