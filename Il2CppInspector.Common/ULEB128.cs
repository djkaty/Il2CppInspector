/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System.IO;

namespace Il2CppInspector
{
    internal class ULEB128
    {
        public static ulong Decode(IFileFormatReader next) {
            ulong uleb = 0;
            byte b = 0x80;
            for (var shift = 0; b >> 7 == 1; shift += 7) {
                b = next.ReadByte();
                uleb |= (ulong) (b & 0x7f) << shift;
            }
            return uleb;
        }
    }
}
