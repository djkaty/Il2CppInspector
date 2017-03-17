/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppReaderX86 : Il2CppReader
    {
        public Il2CppReaderX86(IFileFormatReader stream) : base(stream) { }
        public Il2CppReaderX86(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }
        protected override (uint, uint) Search(uint loc, uint globalOffset) {
            uint funcPtr, metadata, code;

            // Variant 1

            // Assembly bytes to search for at start of each function
            var bytes = new byte[] { 0x6A, 0x00, 0x6A, 0x00, 0x68 };
            Image.Position = loc;
            var buff = Image.ReadBytes(5);
            if (bytes.SequenceEqual(buff)) {
                // Next 4 bytes are the function pointer being pushed onto the stack
                funcPtr = Image.ReadUInt32();

                // Start of next instruction
                if (Image.ReadByte() != 0xB9)
                    return (0, 0);

                // Jump to Il2CppCodegenRegistration
                Image.Position = Image.MapVATR(funcPtr) + 6;
                metadata = Image.ReadUInt32();
                Image.Position = Image.MapVATR(funcPtr) + 11;
                code = Image.ReadUInt32();
                return (code, metadata);
            }

            // Variant 2
            bytes = new byte[] { 0x55, 0x89, 0xE5, 0x53, 0x83, 0xE4, 0xF0, 0x83, 0xEC, 0x20, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x5B };
            Image.Position = loc;
            buff = Image.ReadBytes(16);
            if (!bytes.SequenceEqual(buff))
                return (0, 0);

            Image.Position += 8;
            funcPtr = Image.MapVATR(Image.ReadUInt32() + globalOffset);
            if (funcPtr > Image.Stream.BaseStream.Length)
                return (0, 0);
            Image.Position = funcPtr + 0x22;
            metadata = Image.ReadUInt32() + globalOffset;
            Image.Position = funcPtr + 0x2C;
            code = Image.ReadUInt32() + globalOffset;
            return (code, metadata);
        }
    }
}
