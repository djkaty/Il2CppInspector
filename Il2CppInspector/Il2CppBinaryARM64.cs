using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector
{
    internal class Il2CppBinaryARM64 : Il2CppBinary
    {
        public Il2CppBinaryARM64(IFileFormatReader stream) : base(stream) { }

        public Il2CppBinaryARM64(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {
            // Assembly bytes to search for at start of each function
            ulong metadataRegistration, codeRegistration;
            byte[] buff;



            return (0, 0);
        }
    }
}