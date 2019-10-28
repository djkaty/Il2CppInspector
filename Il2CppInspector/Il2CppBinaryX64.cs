/*
    Copyright 2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppBinaryX64 : Il2CppBinary
    {
        public Il2CppBinaryX64(IFileFormatReader stream) : base(stream) { }
        public Il2CppBinaryX64(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {
            return (0, 0);
        }
    }
}