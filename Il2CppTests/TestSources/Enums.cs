/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppTests.TestSources
{
    // Test to ensure that offsets in structs and classes are calculated correctly for various enum field sizes

#pragma warning disable CS0169
    public enum EnumOfSize1 : byte
    {
        Option1,
        Option2,
        Option3
    }

    public enum EnumOfSize2 : ushort
    {
        Option1,
        Option2,
        Option3
    }

    public enum EnumOfSize4 : int
    {
        Option1,
        Option2,
        Option3
    }

    public enum EnumOfSize8 : ulong
    {
        Option1,
        Option2,
        Option3
    }

    public struct StructWithEnumFields
    {
        ushort a;
        EnumOfSize1 b;
        ushort c;
        EnumOfSize2 d;
        ushort e;
        EnumOfSize4 f;
        ushort g;
        EnumOfSize8 h;
        ushort i;
    }

    public class ClassWithEnumFields
    {
        ushort a;
        EnumOfSize1 b;
        ushort c;
        EnumOfSize2 d;
        ushort e;
        EnumOfSize4 f;
        ushort g;
        EnumOfSize8 h;
        ushort i;
    }

    public class ClassWithEnumAutoProperties
    {
        ushort a { get; }
        EnumOfSize1 b { get; }
        ushort c { get; }
        EnumOfSize2 d { get; }
        ushort e { get; }
        EnumOfSize4 f { get; }
        ushort g { get; }
        EnumOfSize8 h { get; }
        ushort i { get; }
    }
#pragma warning disable CS0169
}