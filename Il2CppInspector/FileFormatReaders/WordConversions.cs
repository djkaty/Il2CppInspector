/*
    Copyright 2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;

namespace Il2CppInspector
{
    // NOTE: What we really should have done here is add a TWord type parameter to FileFormatReader<T>
    // then we could probably avoid most of this
    interface IWordConverter<TWord> where TWord : struct
    {
        TWord Add(TWord a, TWord b);
        TWord Sub(TWord a, TWord b);
        TWord Div(TWord a, TWord b);
        TWord Div(TWord a, int b);
        int Int(TWord a);
        long Long(TWord a);
        ulong ULong(TWord a);
        bool Gt(TWord a, TWord b);
        uint[] UIntArray(TWord[] a);
    }

    internal class Convert32 : IWordConverter<uint>
    {
        public uint Add(uint a, uint b) => a + b;
        public uint Sub(uint a, uint b) => a - b;
        public uint Div(uint a, uint b) => a / b;
        public uint Div(uint a, int b) => a / (uint)b;
        public int Int(uint a) => (int)a;
        public long Long(uint a) => a;
        public ulong ULong(uint a) => a;
        public bool Gt(uint a, uint b) => a > b;
        public uint[] UIntArray(uint[] a) => a;
    }
    internal class Convert64 : IWordConverter<ulong>
    {
        public ulong Add(ulong a, ulong b) => a + b;
        public ulong Sub(ulong a, ulong b) => a - b;
        public ulong Div(ulong a, ulong b) => a / b;
        public ulong Div(ulong a, int b) => a / (uint)b;
        public int Int(ulong a) => (int)a;
        public long Long(ulong a) => (long)a;
        public ulong ULong(ulong a) => a;
        public bool Gt(ulong a, ulong b) => a > b;
        public uint[] UIntArray(ulong[] a) => Array.ConvertAll(a, x => (uint)x);
    }
}
